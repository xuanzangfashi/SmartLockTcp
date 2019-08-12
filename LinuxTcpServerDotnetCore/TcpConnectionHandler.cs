using Json;
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
        public Thread ConnectionMainThread;
        protected TcpReceiver Receiver;
        public TcpSender Sender;
        public TcpClient Client;
        public Semaphore ProcessSema;
        protected int CurrentSemaCount;
        public bool ReceiveZeroDisconnect = false;
        public string DisconnectReason = "";
        public Thread e;

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
            client.ReceiveTimeout = 100000000;
        }

        protected void MainLoop()
        {
            while (true)
            {
                try
                {
                    ProcessSema.WaitOne();
                    if (Client.Connected && !ReceiveZeroDisconnect)
                    {
                        
                        Debuger.PrintStr("got sema1", EPRINT_TYPE.NORMAL);
                        e = new Thread(ProcessData);
                        e.Name = this as TcpConnectionHandler_SmartLock == null ? "app_recv" : "sl_recv";
                        e.Start();
                    }
                    else
                    {
                        Debuger.PrintStr("wait for disconnect", EPRINT_TYPE.NORMAL);
                        if (e.IsAlive)
                        {
                            e.IsBackground = true;
                            e.Interrupt();
                        }
                        //ProcessSema.WaitOne();
                        Debuger.PrintStr("a handler lost connection", EPRINT_TYPE.WARNING);
                        ProcessSema.Release();
                        SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(this, DisconnectReason == "" ? "lost connection!" : DisconnectReason);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debuger.PrintStr(ex.Message, EPRINT_TYPE.WARNING);
                    return;
                }
            }
        }

        protected virtual void ProcessData()
        {
            // ProcessSema.WaitOne();
        }

        public void Disconnect(string reason)
        {
            var jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "1", reason, "200" }).jstr;
            this.Sender.WriteSendData(jstr);
            Client.Client.Disconnect(false);
            Client.Close();

            this.Sender = null;
            this.Receiver = null;
            this.Client.Dispose();
            ProcessSema.Close();
            ProcessSema = null;

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
