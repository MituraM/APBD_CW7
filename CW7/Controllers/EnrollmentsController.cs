using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Cw3.DTOs.Requests;
using Cw3.DTOs.Responses;
using Cw3.Models;
using Cw3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cw3.Controllers
{
    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IDbService _dbService;

        public EnrollmentsController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet("{studentIndex}")]
        public IActionResult GetEnrollment(string studentIndex)
        {
            var enrollment = _dbService.GetStudentEnrollment(studentIndex);

            if (enrollment == null)
            {
                return NotFound("No enrollment found for given index.");
            }

            return Ok(enrollment);
        }

        [HttpPost]
        [Authorize(Roles = "employee")]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            var student = new Student
            {
                IndexNumber = request.IndexNumber,
                FirstName = request.FirstName,
                LastName = request.LastName,
                BirthDate = request.Birthdate,
                Course = request.Studies,
                Semester = 1
            };

            var study = _dbService.GetStudy(student.Course);
            if (study == null)
            {
                return BadRequest("Study does not exist");
            }

            Enrollment enrollment;
            try
            {
                 enrollment = _dbService.EnrollStudent(student, study);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            var response = new EnrollStudentResponse
            {
                LastName = student.LastName,
                Study = student.Course,
                Semester = enrollment.Semester,
                StartDate = enrollment.StartDate
            };

            return Created("", response);
        }

        [HttpPost("promotions")]
        [Authorize(Roles = "employee")]
        public IActionResult PromoteStudent(PromoteStudentsRequest request)
        {
            var enrollment = _dbService.GetLatestEnrollment(request.Studies, request.Semester);
            if (enrollment == null)
            {
                return NotFound("Enrollment does not exist");
            }

            Enrollment newEnrollment;
            try
            {
                newEnrollment = _dbService.PromoteStudents(enrollment.Course, enrollment.Semester);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            var response = new PromoteStudentsResponse
            {
                IdEnrollment = newEnrollment.IdEnrollment,
                Course = request.Studies,
                Semester = newEnrollment.Semester,
            };

            return Created("", response);
        }
    }
}

