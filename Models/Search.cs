using System;
using System.Collections.Generic;

#nullable disable

namespace Pro.Models
{
    public partial class Search
    {
        public Search()
        {
            Results = new HashSet<Result>();
        }

        public string SearchId { get; set; }
        public DateTime Date { get; set; }

        public virtual ICollection<Result> Results { get; set; }
    }
}
