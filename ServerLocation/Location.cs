using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnRagedGUI
{
    public class Location
    {
        string id;
        string name;
        string serverIP;

        public string Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string ServerIP { get => serverIP; set => serverIP = value; }
    }
}
