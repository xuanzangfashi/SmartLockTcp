using Json;
using LinuxTcpServerDotnetCore.SmartLock.Statics;
using LinuxTcpServerDotnetCore.Statics;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Sql;
using System;
using System.Net.Sockets;
using System.Threading;

namespace LinuxTcpServerDotnetCore.SmartLock
{
    public class TcpConnectionHandler_App : TcpConnectionHandler
    {
        public ETcpHandlerType HandlerType = ETcpHandlerType.EApp;

        //public TcpConnectionHandler_SmartLock Corresponding_SL;
        //public List<TcpConnectionHandler_App> CurrentApps;
        public FSmartLockPair CurrentPair;
        public bool InitDone = false;
        public string LockId;
        public int CurrentPhoneIndex = -1;


        public TcpConnectionHandler_App(TcpClient client) : base(client)
        {

        }

        public TcpConnectionHandler_App()
        {

        }

        public override void Init(TcpClient client)
        {
            base.Init(client);
            this.ConnectionMainThread.Name = this.GetType().Name;

        }

        protected override void ProcessData()
        {
            base.ProcessData();
            string receiveData;
            var relen = Receiver.ReadRecevieData(out receiveData);
            if (relen == -1)/*reLen==-1 means that Thread.Interrup has been called in somewhere,just return this function end thread*/
                return;

            if (relen > 0)
            {
                JObject jobj = JObject.Parse(receiveData);
                if (jobj == null)
                {
                    Debuger.PrintStr("Parse Json string fail!", EPRINT_TYPE.WARNING);
                    return;
                }


                EDataHeader header = (EDataHeader)int.Parse(jobj["type"].ToString());

                JsonObject jstr = null;
               
                if (jstr != null)
                {
                    this.Sender.WriteSendData(jstr.jstr);
                    CurrentPair.sl.Sender.WriteSendData(jstr.jstr);
                }

            }
            else
            {
                ReceiveZeroDisconnect = true;
            }
            ReturnPoint:;
            if (ProcessSema != null)
            {
                CurrentSemaCount = ProcessSema.Release();
                Debuger.PrintStr("release", EPRINT_TYPE.NORMAL);

            }

        }

        void SelfProcessingDisconnectThread(object reason)
        {
            ProcessSema.WaitOne();
            SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(this, (string)reason);
        }

        protected override void OnDisconnect(string reason)
        {
            Debuger.PrintStr("____App Handler disconnect reason: " + reason, EPRINT_TYPE.NORMAL);
            if (CurrentPair != null)
            {
                if (CurrentPair.app == this)
                    CurrentPair.app = null;
                if (CurrentPair.sl != null)
                {
                    SmartLockTcpHandlerManager.Instance.SmartLockMap.Remove(LockId);
                }
            }
        }
    }
}
