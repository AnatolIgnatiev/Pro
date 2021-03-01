using Newtonsoft.Json;
using Pro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pro.Rest.Models
{
    public class SearchResult
    {

        [JsonProperty("Suggestions")]
        public List<Suggestion> Suggestions { get; set; }
    }
}
