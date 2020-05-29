﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.DTOs.Requests
{
    public class EnrollStudentRequest
    {
        [Required]
        [RegularExpression("^s[0-9]+$")]
        public string IndexNumber { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public DateTime Birthdate { get; set; }

        [Required]
        public string Studies { get; set; }
    }
}
