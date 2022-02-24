using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static EnRagedGUI.JsonObjects.MainApiJsonClass;

namespace EnRagedGUI.Helper
{
    public class Wireguard
    {

        public static class ConnectionState
        {
            private static string name;
            private static string id;
            private static string ip;

            public static string Name { get => name; set => name = value; }
            public static string Id { get => id; set => id = value; }
            public static string Ip { get => ip; set => ip = value; }
        }

        public static async Task<string> GenerateNewConfigAsync(string selectedLocation)
        {
            //log.Write("Generating keys");
            //log.Write("Exchanging keys with EnRaged Services");
            //log.Write("Generating Configuration");

            using var client = new HttpClient();
            client.BaseAddress = new Uri(Globals.API_IP);
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Properties.Settings.Default.token);

            //send request
            HttpResponseMessage responseMessage = await client.GetAsync($"/server/nodes/generate/{selectedLocation}");

            var responseJson = await responseMessage.Content.ReadAsStringAsync();

            var ServerObject = System.Text.Json.JsonSerializer.Deserialize<Root>(responseJson);

            try
            {
                if (ServerObject.status == 200)
                {

                    var certificate = ServerObject.certificate.data;
                    return Wireguard.FormatConfiguration(
                        certificate.Interface.PrivateKey,
                        certificate.Interface.Address,
                        certificate.Interface.DNS,
                        certificate.Peer.PublicKey,
                        certificate.Peer.PresharedKey,
                        certificate.Peer.Endpoint);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


            return "";
        }


        public static string FormatConfiguration(
            string privateKey,
            string address,
            string dns,
            string publicKey,
            string presharedKey,
            string endPoint
            ) => $@"[Interface]
PrivateKey = {privateKey}
Address = {address}
DNS = {dns}

[Peer]
PublicKey = {publicKey}
PresharedKey = {presharedKey}
EndPoint = {endPoint}
AllowedIPs = {(!Properties.Settings.Default.KillSwitch ? "0.0.0.0/1, 128.0.0.0/1, ::/1, 8000::/1" : "0.0.0.0/0, ::/0")}";


    }
}
