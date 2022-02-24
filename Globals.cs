using EnRagedGUI.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    }


}
