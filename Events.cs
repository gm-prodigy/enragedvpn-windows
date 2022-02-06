using System;
using System.IO;
using System.Threading;
using System.Windows;
using static EnRagedGUI.Globals;

namespace EnRagedGUI
{
    internal class Events
    {
        public static void TailTransfer()
        {
            Tunnel.Driver.Adapter adapter = null;
            while (ThreadsRunning)
            {
                if (adapter == null)
                {
                    while (ThreadsRunning)
                    {
                        try
                        {
                            adapter = Tunnel.Service.GetAdapter(ConfigFile);
                            break;
                        }
                        catch
                        {
                            try
                            {
                                Thread.Sleep(1000);
                            }
                            catch { }
                        }
                    }
                }
                if (adapter == null)
                    continue;
                try
                {
                    ulong rx = 0;
                    ulong tx = 0;

                    var config = adapter.GetConfiguration();
                    foreach (var peer in config.Peers)
                    {
                        rx += peer.RxBytes;
                        tx += peer.TxBytes;
                    }
                    //Invoke(new Action<ulong, ulong>(updateTransferTitle), new object[] { rx, tx });
                    //Dispatcher.Invoke(new Action<ulong, ulong>(transferUsage), new object[] { rx, tx });
                    Thread.Sleep(1000);
                }
                catch { adapter = null; }
            }
        }

        public static void TailLog()
        {
            Console.WriteLine("hey");
            var cursor = Tunnel.Ringlogger.CursorAll;
            while (ThreadsRunning)
            {
                var lines = log.FollowFromCursor(ref cursor);
                foreach (var line in lines)
                {
                    Console.WriteLine(line + "\r\n");

                    if (line.Contains("Shutting down"))
                    {
                        if (Connected)
                        {
                            ConnectionButtonColour = "White";
                            Connected = false;
                        }    
                    }

                    if (line.Contains("retrying (try 3)"))
                    {
                        Tunnel.Service.Remove(ConfigFile, true);
                        try { File.Delete(ConfigFile); } catch { }
                        ConnectionButtonColour = "White";
                        Connected = false;

                        MessageBox.Show("We did not hear back from the server so you have been disconnected!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                        //string s1 = "retrying (try 1)";
                        //string s2 = "Shutting down";

                        //line.Contains("aaaaaaaaaaaaaaaaa");

                        //Logger.test(line);

                        try
                    {
                        Thread.Sleep(300);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }
    }
}
