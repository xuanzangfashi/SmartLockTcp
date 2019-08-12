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
    public sealed class TcpConnectionHandler_App : TcpConnectionHandler
    {
        public ETcpHandlerType HandlerType = ETcpHandlerType.EApp;

        //public TcpConnectionHandler_SmartLock Corresponding_SL;
        //public List<TcpConnectionHandler_App> CurrentApps;
        public FSmartLockPair CurrentPair;
        public bool InitDone = false;
        public string LockId;
        public int CurrentPhoneIndex = -1;
        public string User_name;

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
                goto ReturnPoint;

            if (relen > 0)
            {
                JObject jobj = JObject.Parse(receiveData);

                string key;
                try
                {
                    key = jobj["key"].ToString();
                }
                catch
                {
                    ReceiveZeroDisconnect = true;
                    DisconnectReason = "cannot parse init json string";
                    goto ReturnPoint;
                }

                if (SmartLockTcpHandlerManager.Instance.AccountKeys.ContainsKey(User_name))
                    if (SmartLockTcpHandlerManager.Instance.AccountKeys[User_name] == key)
                    {

                        #region OLD
                        //JObject jobj = JObject.Parse(receiveData);
                        //if (jobj == null)
                        //{
                        //    Debuger.PrintStr("Parse Json string fail!", EPRINT_TYPE.WARNING);
                        //    return;
                        //}
                        //
                        //
                        //EDataHeader header = (EDataHeader)int.Parse(jobj["type"].ToString());
                        //
                        //JsonObject jstr = null;
                        //
                        //if (jstr != null)
                        //{
                        //    this.Sender.WriteSendData(jstr.jstr);
                        //    CurrentPair.sl.Sender.WriteSendData(jstr.jstr);
                        //}
                        #endregion
                        if (!InitDone)
                        {
                            try
                            {
                                var lock_id = jobj["lock_id"].ToString();
                                User_name = jobj["user_name"].ToString();
                                if (SmartLockTcpHandlerManager.Instance.SmartLockMap.ContainsKey(lock_id))
                                {
                                    CurrentPair = SmartLockTcpHandlerManager.Instance.SmartLockMap[lock_id];
                                    CurrentPair.app = this;
                                    this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "normal", "init done!", "200" }).jstr);
                                }
                                else
                                {
                                    ReceiveZeroDisconnect = true;
                                    DisconnectReason = "votas lock has not connect yet!";
                                    goto ReturnPoint;
                                }
                            }
                            catch
                            {
                                ReceiveZeroDisconnect = true;
                                DisconnectReason = "cannot parse init json string";
                                goto ReturnPoint;
                            }
                        }
                        else
                        {
                            var to_target = jobj["target"].ToString();
                            if (to_target == "0")//to server
                            {
                                var tag = jobj["tag"].ToString();
                                var str = jobj["content"].ToString();

                            }
                            else
                            {
                                if (CurrentPair.sl == null)
                                {
                                    //this.Sender.WriteSendData();
                                    ReceiveZeroDisconnect = true;
                                    DisconnectReason = JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "normal", "votas lock lost connection!", "200" }).jstr;
                                    goto ReturnPoint;
                                }
                                else
                                {
                                    CurrentPair.sl.Sender.WriteSendData(receiveData);
                                }
                            }
                        }

                    }
                    else
                    {
                        ReceiveZeroDisconnect = true;
                        DisconnectReason = "account key is wrong";
                        goto ReturnPoint;
                    }
                else
                {
                    ReceiveZeroDisconnect = true;
                    DisconnectReason = "user has not connect yet!";
                    goto ReturnPoint;
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
