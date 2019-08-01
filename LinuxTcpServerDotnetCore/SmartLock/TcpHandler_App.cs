using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LinuxTcpServerDotnetCore.SmartLock
{
    class TcpHandler_App : TcpHandler
    {
        public TcpHandler_App()
        {

        }

        public TcpHandler_App(Int32 port, IPAddress ip) : base(port, ip)
        {
            this.OnConnectCallback = OnConnect;
        }

        public override void Init(int port, IPAddress ip)
        {
            base.Init(port, ip);
            this.ConnectThread.Name = "App_Listener";
            this.OnConnectCallback = OnConnect;
        }

        protected void OnConnect(TcpClient client)
        {
            TcpConnectionHandler.CreateTcpConnectionHandler<TcpConnectionHandler_App>().Init(client);
        }
    }
}
