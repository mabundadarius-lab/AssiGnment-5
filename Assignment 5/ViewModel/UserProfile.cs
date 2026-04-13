using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssiGnment_5.ViewModel
{
    internal class UserProfile
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string EmailAddress { get; set; }
        public string Bio { get; set; }
        public string ProfileIconPath { get; set; } // Bonus: picture path
    }
}
