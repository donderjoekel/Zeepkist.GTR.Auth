using Newtonsoft.Json;

namespace TNRD.Zeepkist.GTR.Auth.Directus;

public class Metadata
{
    [JsonProperty("total_count")] public int? TotalCount { get; set; }
    [JsonProperty("filter_count")] public int? FilterCount { get; set; }
}
