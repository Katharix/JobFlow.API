using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.DTOs
{
    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string OrganizationName { get; set; }
        public string Industry { get; set; }
        public int Size { get; set; }
        public bool IsAdmin { get; set; } // Determines if this is the first user (Admin)
    }

}
