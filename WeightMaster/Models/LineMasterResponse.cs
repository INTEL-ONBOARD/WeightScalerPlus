using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace WeightMaster.Models
{
    public class LineMasterResponse
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public List<LineMaster>? Data { get; set; }
    }

    public class LineMaster
    {
        [JsonPropertyName("linename")]
        public string? LineName { get; set; }

        [JsonPropertyName("linemaster")]
        public string? LineMasterName { get; set; }
    }
}
