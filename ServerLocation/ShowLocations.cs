using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using static EnRagedGUI.App.Globals;
using static EnRagedGUI.Properties.Settings;

namespace EnRagedGUI
{
    class ShowLocations
    {

        static ShowLocations()
        {

            using var webClient = new HttpClient();
            string rawJSON = null;
            try
            {
                rawJSON = webClient.GetStringAsync($"{Default.BaseUrl}server/nodes").GetAwaiter().GetResult();

            }
            catch (Exception)
            {
                AllLocations = new();
                return;
            }

            LocationCollection locationCollection = JsonConvert.DeserializeObject<LocationCollection>(rawJSON);

            AllLocations = locationCollection.Locations;
        }

        public static List<Location> AllLocations { get; set; }

        public static List<Location> GetLocations() => AllLocations;
    }
}