﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BulkyBook.Models
{
    public class CoverType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

    }
}
