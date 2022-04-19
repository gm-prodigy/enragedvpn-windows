using System;

namespace EnRagedGUI.Models
{
    public class ConnectionType : EventArgs
    {

        public ConnectionStatus Connection { get; set; }
        public string ConnectionId { get; set; }
        public string ConnectionName { get; set; }
        public bool ConnectionNotification { get; set; }
    }

    public enum ConnectionStatus
    {
        Connecting,
        Connected,
        Disconnected,
        Reconnecting
    }
}
