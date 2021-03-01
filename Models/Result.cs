using System;
using System.Collections.Generic;

#nullable disable

namespace Pro.Models
{
    public partial class Result
    {
        public int Id { get; set; }
        public string PartId { get; set; }
        public string Brand { get; set; }
        public string OriginalPrice { get; set; }
        public string? FirstPrice { get; set; }
        public string? SecondPrice { get; set; }
        public string SearchId { get; set; }
        public bool IsSuccessful { get; set; }

        public virtual Search Search { get; set; }
    }
}
