using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;


namespace LinuxTcpServerDotnetCore
{
    public class TcpSender
    {
        TcpClient Client;

        public TcpSender(TcpClient client)
        {
            this.Client = client;
        }

        public int WriteSendData(string data)
        {
            var stream = Client.GetStream();

            byte[] buffer = Encoding.UTF8.GetBytes(data);
            
            return Client.Client.Send(buffer);
        }
    }
}
