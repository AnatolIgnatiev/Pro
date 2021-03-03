using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pro.Models
{
    public class ProUser
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        public int? RoleId { get; set; }
        public Role Role { get; set; }
    }
}
