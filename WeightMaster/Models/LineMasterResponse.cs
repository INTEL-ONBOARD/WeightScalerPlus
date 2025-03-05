using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMaster.Models
{
    public class LineMasterResponse
    {
        public string? Status { get; set; }
        public List<LineMaster>? Data { get; set; }
    }

    public class LineMaster
    {
        public string? LineName { get; set; }
        public string? lmaster { get; set; }
    }

}
