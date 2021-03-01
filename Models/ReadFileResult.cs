using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pro.Models
{
    public class ReadFileResult
    {
        public IFormFile FileName { get; set; }

        public bool Success { get; set; }

        public List<SearchRequest> Requests { get; set; }
    }
}
