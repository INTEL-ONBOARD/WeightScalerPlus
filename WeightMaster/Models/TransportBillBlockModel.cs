using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMaster.Models
{
    public class TransportBillBlockModel
    {
        public int Id { get; set; }
        public string? billNo { get; set; }
        public string? dateCreated { get; set; } // Assuming this should remain as string for format "YYYY-MM-DD"
        public string? lineName { get; set; }
    }
}
