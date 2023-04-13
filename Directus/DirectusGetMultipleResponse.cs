using Newtonsoft.Json;

namespace TNRD.Zeepkist.GTR.Auth.Directus;

public class DirectusGetMultipleResponse<T>
{
    [JsonProperty("meta")] public Metadata? Metadata { get; set; }
    [JsonProperty("data")] public T[] Data { get; set; } = null!;

    public bool HasItems => Data.Length > 0;

    public T? FirstItem => Data[0];
}

public class DirectusGetSingleResponse<T>
{
    [JsonProperty("data")] public T? Data { get; set; }
}
