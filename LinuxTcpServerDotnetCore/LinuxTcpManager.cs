using LinuxTcpServerDotnetCore.TemplateClass;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LinuxTcpServerDotnetCore
{
    class LinuxTcpManager : Singleton<LinuxTcpManager>
    {
        Int32 Port = 7878;
        IPAddress Ip = IPAddress.Any;
        List<TcpConnectionHandler> TcpHandler_Ls;

        public LinuxTcpManager()
        {
            TcpHandler_Ls = new List<TcpConnectionHandler>();
        }

        public void AddTcpHandlerToList(TcpConnectionHandler v)
        {
            TcpHandler_Ls.Add(v);
        }
       
    }
}
