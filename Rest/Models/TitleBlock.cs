using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pro.Rest.Models
{
    public class TitleBlock
    {
        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Str")]
        public string Str { get; set; }
    }
}
