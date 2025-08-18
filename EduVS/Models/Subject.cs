using System.ComponentModel.DataAnnotations;

namespace EduVS.Models
{
    public class Subject
    {
        [Key]
        [MaxLength(10)]
        public string Code { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        public ICollection<Test> Tests { get; set; } = new List<Test>();

        public override string ToString() => $"{Code} - {Name}";
    }
}
