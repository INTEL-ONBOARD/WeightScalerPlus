using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMaster.Models
{
    public class memDbLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? CustomMemberNum { get; set; } = string.Empty;

        public string? CustomPreMemberNum { get; set; } = string.Empty;

        public string? CustomNameWithInitials { get; set; } = string.Empty;

        public string? CellNumber { get; set; } // Nullable
    }
}
