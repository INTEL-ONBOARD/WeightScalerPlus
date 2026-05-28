using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace WeightMaster.Models
{
    public class LineResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public List<LineData> Data { get; set; } = new();
    }

    public class LineData
    {
        [JsonProperty("lineid")]
        [JsonPropertyName("lineid")]
        public int LineId { get; set; }

        [JsonPropertyName("linename")]
        public string LineName { get; set; }

        [JsonPropertyName("linemaster")]
        public string LineMaster { get; set; }
    }
}
