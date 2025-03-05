using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMaster.Models
{
    public class UserLoginModel
    {
        public int Id { get; set; } // Primary key
        public string? Email { get; set; }
        public string? Password { get; set; }

        public DateTime LoginDateTime { get; set; } // Date and Time of login
        public bool Status { get; set; }
    }
}
