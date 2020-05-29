using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Cw3.Middlewares;
using Cw3.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Cw3
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidIssuer = "Gakko",
                            ValidAudience = "Students",
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]))
                        };
                    });

            services.AddSingleton<IDbService, SqlServerDbService>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IDbService dbService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // Log every request to the API
            app.UseMiddleware<LoggingMiddleware>();

            // Check if the user is a student
            app.Use(async (context, next) => {
                context.Request.Headers.TryGetValue("Index", out var studentIndex);

                if (string.IsNullOrEmpty(studentIndex))
                {
                    context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Index number is missing.");
                    return;
                }

                var student = dbService.GetStudent(studentIndex);

                if (student == null)
                {
                    context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Invalid index number used.");
                    return;
                }

                await next();
            });

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
