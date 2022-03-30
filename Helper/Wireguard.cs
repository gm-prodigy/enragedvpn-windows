using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static EnRagedGUI.JsonObjects.MainApiJsonClass;
using static EnRagedGUI.Properties.Settings;

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
            Default.LastLocationId = selectedLocation;

            using var client = new HttpClient();
            client.BaseAddress = new Uri(Globals.API_IP);
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Properties.Settings.Default.token);

            //send request
            HttpResponseMessage responseMessage = await client.GetAsync($"/server/nodes/generate/{selectedLocation}");

            var responseJson = await responseMessage.Content.ReadAsStringAsync();

            Console.WriteLine(responseJson);

            var ServerObject = System.Text.Json.JsonSerializer.Deserialize<Root>(responseJson);

            try
            {
                if (ServerObject.code == 200)
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

                if (ServerObject.code == 702)
                {
                    throw new Exception("Account not active, or subscription have expired!");
                }

                if (ServerObject.code == 429)
                {
                    throw new Exception("Too many request, slow down a little!");
                }

                if (ServerObject.code == 401)
                {

                    switch (ServerObject.message)
                    {
                        case "Token error: invalid signature":
                            Default.token = "";
                            Default.Save();

                            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                            mainWindow.Content = new Login();
                            throw new Exception("Login credentials have expired!");
                        case "Token error: jwt expired":
                            await Account.GetNewToken();
                            break;
                    }

                    Console.WriteLine(ServerObject.message);
                    //if (ServerObject.message == "Token error: invalid signature")
                    //{
                    //    Default.token = "";
                    //    Default.Save();

                    //    MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                    //    mainWindow.Content = new Login();
                    //    throw new Exception("Login credentials have expired!");
                    //}

                    //if (ServerObject.message == "Token error: jwt expired")
                    //{
                    //    await Account.GetNewToken();
                    //    break;
                    //}
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
