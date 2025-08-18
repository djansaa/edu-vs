using System;
using System.ComponentModel.DataAnnotations;

namespace EduVS.Models
{
    public class Test
    {
        [Key]
        public int Id { get; set; }

        public string? SubjectCode { get; set; }

        public DateTime TimeStamp { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string TemplatePathA { get; set; } = null!;

        public string? TemplatePathB { get; set; }

        public Subject? Subject { get; set; }
    }
}
