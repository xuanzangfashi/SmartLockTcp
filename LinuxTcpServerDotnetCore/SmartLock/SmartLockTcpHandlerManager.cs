using LinuxTcpServerDotnetCore.SmartLock.Statics;
using LinuxTcpServerDotnetCore.TemplateClass;
using System.Collections.Generic;
using System.Net;

namespace LinuxTcpServerDotnetCore.SmartLock
{
    class SmartLockTcpHandlerManager : Singleton<SmartLockTcpHandlerManager>
    {
        public Dictionary<string, FSmartLockPair> SmartLockMap;
        public Dictionary<string, string> AccountKeys;

        public SmartLockTcpHandlerManager()
        {
            SmartLockMap = new Dictionary<string, FSmartLockPair>();
            AccountKeys = new Dictionary<string, string>();
            var app_handler = TcpHandler.CreateTcpHandler<TcpHandler_App>();
            app_handler.Init(7000, IPAddress.Any);
            var sl_handler = TcpHandler.CreateTcpHandler<TcpHandler_SmartLock>();
            sl_handler.Init(8000, IPAddress.Any);
        }

        public void DisconnectTcpConnectionHandler(TcpConnectionHandler handler,string reason)
        {
            handler.Disconnect(reason);
        }
    }
}
