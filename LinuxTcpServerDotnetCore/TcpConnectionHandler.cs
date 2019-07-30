using LinuxTcpServerDotnetCore.SmartLock;
using LinuxTcpServerDotnetCore.Statics;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LinuxTcpServerDotnetCore
{
    public class TcpConnectionHandler
    {
        protected Thread ConnectionMainThread;
        protected TcpReceiver Receiver;
        public TcpSender Sender;
        public TcpClient Client;
        protected Semaphore ProcessSema;
        protected int CurrentSemaCount;
        protected bool ReceiveZeroDisconnect = false;

        public TcpConnectionHandler(TcpClient client)
        {
            Receiver = new TcpReceiver(client);
            Sender = new TcpSender(client);
            ProcessSema = new Semaphore(1, 1);

            ConnectionMainThread = new Thread(MainLoop);
            ConnectionMainThread.Start();
        }

        public TcpConnectionHandler()
        {

        }

        public virtual void Init(TcpClient client)
        {
            Receiver = new TcpReceiver(client);
            Sender = new TcpSender(client);
            ProcessSema = new Semaphore(1, 1);
            this.Client = client;
            ConnectionMainThread = new Thread(MainLoop);
            ConnectionMainThread.Start();
        }

        protected void MainLoop()
        {
            while(true)
            {
                if (Client.Connected)
                {
                    if (ProcessSema.WaitOne(1))
                    {
                        Thread e = new Thread(ProcessData);
                        e.Start();
                    }
                }
                if (!Client.Connected || ReceiveZeroDisconnect)
                {
                    ProcessSema.WaitOne();
                    //ProcessSema.WaitOne();
                    ProcessSema.Close();
                    ProcessSema = null;

                    Debuger.PrintStr("a handler lost connection", EPRINT_TYPE.WARNING);
                    SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(this, "lost connection!");
                    break;
                }
            }
        }

        protected virtual void ProcessData()
        {
            // ProcessSema.WaitOne();
        }

        public void Disconnect(string reason)
        {
            Client.Client.Disconnect(false);
            Client.Close();
            //var jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "1", reason, "200" }).jstr;
            //this.Sender.WriteSendData(jstr);
            this.Sender = null;
            this.Receiver = null;
            this.Client.Dispose();


            //ConnectThread.Abort();
            OnDisconnect(reason);
        }


        protected virtual void OnConnect()
        {

        }



        protected virtual void OnDisconnect(string reason)
        {

        }

        public static T CreateTcpConnectionHandler<T>() where T : TcpConnectionHandler, new()
        {
            T handler = new T();
            //LinuxTcpManager.Instance.AddTcpHandlerToList(handler);
            return handler;
        }
    }
}
