using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMaster.Models
{
    public class userModel
    {
        public string Status { get; set; }
        public List<User> Users { get; set; }
    }

    public class User
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public List<string> Roles { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
    }

}
