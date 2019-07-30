using Json;
using LinuxTcpServerDotnetCore.SmartLock;
using LinuxTcpServerDotnetCore.Statics;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LinuxTcpServerDotnetCore
{
    public class TcpHandler
    {
        protected delegate void OnClientConnect(TcpClient client);
        protected OnClientConnect OnConnectCallback;
        Int32 Port = 7878;
        IPAddress Ip = IPAddress.Any;
        protected Thread ConnectThread;
        //protected TcpReceiver Receiver;
        //public TcpSender Sender;
        //public TcpClient Client;
        TcpListener server;
        //protected Semaphore ProcessSema;
        //protected int CurrentSemaCount;
        //protected bool ReceiveZeroDisconnect = false;
        public TcpHandler(Int32 port, IPAddress ip)
        {
            Init(port, ip);
        }

        public TcpHandler()
        {

        }

        public virtual void Init(Int32 port, IPAddress ip)
        {
            this.Port = port;
            this.Ip = ip;
            
            ConnectThread = new Thread(TcpThreadLoop);
            ConnectThread.Start();
        }

        public virtual void TcpThreadLoop()
        {
            server = new TcpListener(Ip, Port);
            server.Start();

            
            while (true)
            {
                var client = server.AcceptTcpClient();
               // OnConnect();
                Debuger.PrintStr(client.Client.RemoteEndPoint.ToString(), EPRINT_TYPE.WARNING);

                OnConnectCallback(client);


                //TcpConnectionHandler tcp_ch = new TcpConnectionHandler(client);



                //string receiveData;
                //Receiver.ReadRecevieData(out receiveData);
            }
        }




        public static T CreateTcpHandler<T>() where T : TcpHandler, new()
        {
            T handler = new T();
            //LinuxTcpManager.Instance.AddTcpHandlerToList(handler);
            return handler;
        }
    }
}
