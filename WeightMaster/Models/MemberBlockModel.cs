using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMaster.Models
{
    public class MemberBlockModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Default to empty string if null
        public string CustomMemberNum { get; set; } = string.Empty; // Default to empty string if null
        public string CustomNameWithInitials { get; set; } = string.Empty; // Default to empty string if null
        public string? CellNumber { get; set; } // Nullable property since "cell_number" can be null
    }
}
