using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WeightMaster.Models
{
    public class TransactionLogBlockModel
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
        public string? Date { get; set; }
        public int BoxCount { get; set; }
        public int BagCount { get; set; }
        public int MaximumNormalLeafWeight { get; set; }
        public int TotalLeafWeight { get; set; }
        public int ActualNormalLeafWeight { get; set; }
        public int TotalGoldLeafWeight { get; set; }
        public int Water { get; set; }
        public int Morapuwata { get; set; }
        public int Thambimata { get; set; }
        public int Reject { get; set; }
        public int BoxWeight { get; set; }
        public int FinalGreenLeafCount { get; set; }
        public int FinalGoldLeafCount { get; set; }
        public double RealValue { get; set; }

    }
}
