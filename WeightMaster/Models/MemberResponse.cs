    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace WeightMaster.Models
{
    public class Member
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } // Default to empty string if null

        [JsonPropertyName("custom_membernum")]
        public string CustomMemberNum { get; set; }// Default to empty string if null

        [JsonPropertyName("custom_name_with_initials")]
        public string CustomNameWithInitials { get; set; } // Default to empty string if null

        [JsonPropertyName("cell_number")]
        public string? CellNumber { get; set; } // Nullable property since "cell_number" can be null
    }

    public class Data
    {
        [JsonPropertyName("members")]
        public List<Member> Members { get; set; } = new List<Member>(); // Default to an empty list if null
    }

    public class Response
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // Default to empty string if null

        [JsonPropertyName("data")]
        public Data Data { get; set; } = new Data(); // Default to an empty Data object if null
    }
}