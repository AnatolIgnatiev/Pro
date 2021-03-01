using Pro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pro
{
    public class ListParseResult
    {
        public Task<List<TableRow>> Rows { get; set; }
        public DateTime DueDate { get; set; }
    }
}
