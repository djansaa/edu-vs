using System.ComponentModel.DataAnnotations.Schema;

namespace EduVS.Models
{
    public class ClassStudent
    {
        public int ClassId { get; set; }
        public int StudentId { get; set; }

        [ForeignKey(nameof(ClassId))] public Class Class { get; set; } = null!;
        [ForeignKey(nameof(StudentId))] public Student Student { get; set; } = null!;
    }
}
