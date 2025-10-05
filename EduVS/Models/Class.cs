using System.ComponentModel.DataAnnotations;

namespace EduVS.Models
{
    public class Class
    {
        [Key] public int Id { get; set; }
        [Required] public string Name { get; set; } = null!;
        [Required] public int Year { get; set; }
        public ICollection<ClassStudent> StudentLinks { get; set; } = new List<ClassStudent>();
    }
}
