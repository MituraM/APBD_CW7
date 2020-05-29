using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Cw3.Services;
using Cw3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cw3.Controllers
{
    [Route("api/students")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IDbService _dbService;

        public StudentsController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet]
        public IActionResult GetStudents()
        {
            var students = _dbService.GetStudents();
            return Ok(students);
        }

        [HttpGet("{indexNumber}")]
        public IActionResult GetStudent(string indexNumber)
        {
            var student = _dbService.GetStudent(indexNumber);
            if (student == null)
            {
                return NotFound("Student with the given ID not found.");
            }

            return Ok(student);
        }

        [HttpPost]
        public IActionResult CreateStudent(Student student)
        {
            //... add to DB
            //... generate index number
            student.IndexNumber = $"s{new Random().Next(1, 20000)}";
            return Ok(student);
        }

        [HttpPut("{id}")]
        public IActionResult PutStudent(int id, Student student)
        {
            return Ok("Update finished");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteStudent(int id)
        {
            return Ok("Remove finished");
        }
    }
}
