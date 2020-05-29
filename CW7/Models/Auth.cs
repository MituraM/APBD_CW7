using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.Models
{
    public class Auth
    {
        public string IndexNumber { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public IList<string> Roles { get; set; }
        public string RefreshToken{ get; set; }
    }
}
