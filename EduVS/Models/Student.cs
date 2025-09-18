using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduVS.Models
{
    public class Student
    {
        [Key] public int Id { get; set; }
        [Required] public string Name { get; set; } = null!;
        [Required] public string Surname { get; set; } = null!;
        [Required, EmailAddress] public string Email { get; set; } = null!;
        public ICollection<ClassStudent> ClassLinks { get; set; } = new List<ClassStudent>();
    }
}
