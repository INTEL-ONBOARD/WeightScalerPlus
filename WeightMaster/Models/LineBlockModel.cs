using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeightMaster.Models
{
    public class LineBlockModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("lineid")]
        public int LineId { get; set; }

        [Required]
        public string LineName { get; set; } = string.Empty;

        [Required]
        public string LineMaster { get; set; } = string.Empty;
    }
}
