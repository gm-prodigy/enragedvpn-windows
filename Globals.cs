using EnRagedGUI.Helper;
using FluentScheduler;
using Squirrel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static EnRagedGUI.Helper.Wireguard;

namespace EnRagedGUI
{
    internal class Globals
    {
        public const string API_IP = "https://api.enragedvpn.com/";
        //public static bool Connected;
        public static string ConnectionButtonColour = "White";
        public static readonly string UserDirectory = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "Config");
        public static readonly string ConfigFile = Path.Combine(UserDirectory, "enragedvpn.conf");
        public static readonly string LogFile = Path.Combine(UserDirectory, "log.bin");

        public static Tunnel.Ringlogger log;
        public static Thread logPrintingThread, transferUpdateThread;
        public volatile static bool ThreadsRunning;


        public static async Task CheckForUpdate()
        {

            try
            {
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/EnRagedVPN/enragedvpn-client"))
                {


                    //retry:
                    //    var updateInfo = default(UpdateInfo);

                    //    try
                    //    {
                    //        updateInfo = await mgr.CheckForUpdate();
                    //        await mgr.DownloadReleases(updateInfo.ReleasesToApply);
                    //        await mgr.ApplyReleases(updateInfo);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine(ex.Message);
                    //    }

                    await mgr.UpdateApp();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"{ex.Message}, Error finding latest version");
            }

            Console.WriteLine("update!1122");
        }
    }


}
