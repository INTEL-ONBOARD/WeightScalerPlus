using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeightMaster.Models
{
    public class MemberResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public List<Members> Data { get; set; }
    }

    public class Members
    {
        [JsonProperty("custom_membernum")]
        public string? CustomMemberNum { get; set; } = string.Empty;

        [JsonProperty("custom_premembernum")]
        public string? CustomPreMemberNum { get; set; } = string.Empty;

        [JsonProperty("custom_name_with_initials")]
        public string? CustomNameWithInitials { get; set; } = string.Empty;

        [JsonProperty("cell_number")]
        public string? CellNumber { get; set; } = string.Empty;
    }

}
