using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnRagedGUI.JsonObjects
{
    public class Token
    {
        public class Data
        {
            public string token { get; set; }
            public string refreshToken { get; set; }
            public long tokenExpiry { get; set; }
        }

        public class Root
        {
            public bool error { get; set; }
            public int code { get; set; }
            public Data data { get; set; }
        }
    }
}
