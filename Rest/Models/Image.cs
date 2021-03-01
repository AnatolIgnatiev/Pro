using Newtonsoft.Json;
using System;

namespace Pro.Models
{
    public class Image
    {
        [JsonProperty("ImageUri")]
        public string ImageUri { get; set; }

        [JsonProperty("ThumbnailUri")]
        public Uri ThumbnailUri { get; set; }
    }
}