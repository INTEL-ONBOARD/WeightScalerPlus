using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WeightMaster.Models
{
    public class FinalTransactionBlockModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? LineName { get; set; }
        public string? TransportAgent { get; set; }
        public string? Company { get; set; }
        public string? LeafWeightOfficer { get; set; }
        public string? Supervisor { get; set; }
        public string? BarcodeDetails { get; set; }
        public string? NameWithInitials { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime Date { get; set; }
        public int BagCount { get; set; }
        public float MaximumNormalLeafWeight { get; set; }
        public float TotalLeafWeight { get; set; }
        public float ActualNormalLeafWeight { get; set; }
        public float TotalGoldLeafWeight { get; set; }
        public float Water { get; set; }
        public float Morapuwata { get; set; }
        public float Thambimata { get; set; }
        public float Reject { get; set; }
        public float BagWeight { get; set; }
        public int FinalGreenLeafCount { get; set; }
        public int FinalGoldLeafCount { get; set; }
        public float RealValue { get; set; }
    }
}