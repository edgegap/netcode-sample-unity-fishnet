using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class PaginationDTO
    {
        [JsonProperty("page_size")]
        public uint PageSize;

        [JsonProperty("count")]
        public uint Count;

        [JsonProperty("next_cursor")]
        public string NextCursor;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
