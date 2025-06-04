using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeightMaster.Models
{
    public class GreenLeafPostModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string LeafHandoverDate { get; set; } = string.Empty;

        public string Factory { get; set; } = string.Empty;

        public string TransportLineName { get; set; } = string.Empty;

        public string TransportAgent { get; set; } = string.Empty;

        public string LeafWeightOfficer { get; set; } = string.Empty;

        public string Supervisor { get; set; } = string.Empty;

        public string MemberNumber { get; set; } = string.Empty;

        public string? PreMemberNumber { get; set; }

        public int BagCount { get; set; }

        public int BoxCount { get; set; }

        public double RealWeight { get; set; }

        public double TotalWeight { get; set; }

        public double NomalLeafWeight { get; set; }

        public double GoldLeafWeight { get; set; }

        public double Wathurata { get; set; }

        public double Morapuwata { get; set; }

        public double Thambimata { get; set; }

        public double Rejected { get; set; }

        public double BagWeight { get; set; }

        public double BoxWeight { get; set; }

        public int FinalGreenLeafCount { get; set; }

        public int FinalGoldLeafCount { get; set; }

        public string CreatedUser { get; set; } = string.Empty;

        public string UpdatedUser { get; set; } = string.Empty;
    }
}
