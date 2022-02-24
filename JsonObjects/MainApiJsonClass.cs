using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnRagedGUI.JsonObjects
{
    internal class MainApiJsonClass
    {
        public class Root
        {
            public bool error { get; set; }
            public int status { get; set; }
            public Certificate certificate { get; set; }
        }

        public class Server
        {
            public string id { get; set; }
            public string serverIp { get; set; }
            public string countryCode { get; set; }
            public string country { get; set; }
            public string city { get; set; }
            public string latitude { get; set; }
            public string longitude { get; set; }
            public string hostname { get; set; }
            public string name { get; set; }
            public string publicKey { get; set; }
            public bool active { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
        }

        public class Certificate
        {
            public Server server { get; set; }
            public Data data { get; set; }
        }
        public class Peer
        {
            public string PublicKey { get; set; }
            public string PresharedKey { get; set; }
            public string Endpoint { get; set; }
            public string AllowedIPs { get; set; }
        }

        public class Data
        {
            public Interface Interface { get; set; }
            public Peer Peer { get; set; }
        }

        public class Interface
        {
            public string PrivateKey { get; set; }
            public string Address { get; set; }
            public string DNS { get; set; }
        }
    }
}
