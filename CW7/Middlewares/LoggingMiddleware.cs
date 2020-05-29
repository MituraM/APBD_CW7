using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cw3.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly string LogPath = "requestsLogs.txt";

        private readonly RequestDelegate _next;

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var request = httpContext.Request;
            request.EnableBuffering();

            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                var logBuilder = new StringBuilder();
                logBuilder.Append('[');
                logBuilder.Append(DateTime.Now);
                logBuilder.Append("] ");
                logBuilder.Append(request.Method);
                logBuilder.Append(' ');
                logBuilder.Append(request.Path);
                logBuilder.Append(request.QueryString.Value);
                logBuilder.Append('\n');
                logBuilder.Append(await reader.ReadToEndAsync());

                await File.AppendAllTextAsync(LogPath, logBuilder.ToString());
            }

            request.Body.Position = 0;

            await _next(httpContext);
        }
    }
}
