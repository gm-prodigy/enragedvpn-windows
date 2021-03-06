using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnRagedGUI.JsonObjects
{
    internal class LoginJsonClass
    {
        public class User
        {
            public string id { get; set; }
            public string token { get; set; }
            public string refreshToken { get; set; }
            public bool active { get; set; }
            public long tokenExipry { get; set; }
        }
        public class Root
        {
            public bool error { get; set; }
            public int code { get; set; }
            public User user { get; set; }
        }
    }
}
