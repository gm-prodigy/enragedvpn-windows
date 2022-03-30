using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static EnRagedGUI.JsonObjects.Token;
using static EnRagedGUI.Properties.Settings;

namespace EnRagedGUI.Helper
{
    public class Account
    {
        public static async Task GetNewToken()
        {

            using var client = new HttpClient();
            client.BaseAddress = new Uri(Globals.API_IP);
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Default.RefreshToken);

            //send request
            HttpResponseMessage responseMessage = await client.GetAsync($"/auth/refresh");

            var responseJson = await responseMessage.Content.ReadAsStringAsync();

            var ServerObject = System.Text.Json.JsonSerializer.Deserialize<Root>(responseJson);

            try
            {
                if (ServerObject.code == 200)
                {
                    Default.token = ServerObject.data.token;
                    Default.RefreshToken = ServerObject.data.refreshToken;
                    Default.Save();
                    return;
                }
                else
                {
                    Default.token = "";
                    Default.RefreshToken = "";
                    Default.Save();

                    if (Default.isConnected)
                    {
                        await Task.Run(() =>
                        {
                            Tunnel.Service.Remove(Globals.ConfigFile, true);
                            try { File.Delete(Globals.ConfigFile); } catch { }
                        });
                        Default.isConnected = false;
                    }
                    MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                    mainWindow.Content = new Login();
                    throw new Exception("Login credentials have expired!");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
