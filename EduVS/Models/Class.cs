using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
