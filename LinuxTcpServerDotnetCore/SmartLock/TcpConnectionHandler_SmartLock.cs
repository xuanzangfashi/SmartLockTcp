using Json;
using LinuxTcpServerDotnetCore.SmartLock.Statics;
using LinuxTcpServerDotnetCore.Statics;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Sql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace LinuxTcpServerDotnetCore.SmartLock
{
    public class TcpConnectionHandler_SmartLock : TcpConnectionHandler
    {
        //public int FailMakeupCount = 0;
        public ETcpHandlerType HandlerType = ETcpHandlerType.ESmartLock;
        public bool InitDone = false;
        public FSmartLockPair CurrentPair;
        public string lock_id;
        //public FLockInfo LockInfo;

        public TcpConnectionHandler_SmartLock(TcpClient client) : base(client)
        {
            //Corresponding_App = new List<TcpConnectionHandler_App>();
        }

        public TcpConnectionHandler_SmartLock()
        {

        }


        public override void Init(TcpClient client)
        {
            base.Init(client);
            //Corresponding_App = new List<TcpConnectionHandler_App>();
            this.ConnectionMainThread.Name = this.GetType().Name;
        }

        protected override void ProcessData()
        {
            base.ProcessData();
            string receiveData;
            var reLen = Receiver.ReadRecevieData(out receiveData);
            if (reLen == -1)//reLen==-1 means that Thread.Interrup has been called in somewhere,just return this function end thread
                return;
            if (reLen > 0)
            {
                #region OLD
                //JObject jobj = null;
                //try
                //{

                //jobj = JObject.Parse(receiveData);
                //}
                //catch (Exception ex)
                //{
                //jobj = null;
                //Debuger.PrintStr(ex.Message, EPRINT_TYPE.ERROR);
                //}
                //if (jobj == null)
                //{
                //Debuger.PrintStr("Parse Json string fail!", EPRINT_TYPE.WARNING);
                //goto ReturnPoint;
                //}


                ////EDataHeader header = (EDataHeader)int.Parse(jobj["type"].ToString());
                ////string message_id = jobj["msg_id"].ToString();
                //JsonObject jstr = null;


                //if (jstr != null)
                //{
                //if (CurrentPair.app != null)
                //CurrentPair.app.Sender.WriteSendData(jstr.jstr);
                //this.Sender.WriteSendData(jstr.jstr);

                //}
                #endregion
                if (!InitDone)
                {
                    try
                    {
                        JObject jobj = JObject.Parse(receiveData);
                        lock_id = jobj["lock_id"].ToString();
                        FSmartLockPair pair = new FSmartLockPair();
                        pair.sl = this;
                        SmartLockTcpHandlerManager.Instance.SmartLockMap.Add(lock_id, pair);
                        CurrentPair = pair;
                        InitDone = true;
                        this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "normal", "init done!", "200" }).jstr);
                    }
                    catch
                    {
                        ReceiveZeroDisconnect = true;
                        DisconnectReason = "cannot parse init json string!";
                        goto ReturnPoint;
                    }

                }
                else
                {
                    //this.Sender.WriteSendData(receiveData);
                    if(CurrentPair.app == null)
                    {
                        this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "normal", "none app connect to this unlock pair", "200" }).jstr);
                    }
                    else
                    {
                        CurrentPair.app.Sender.WriteSendData(receiveData);
                    }
                }
            }
            else
            {
                ReceiveZeroDisconnect = true;
            }
            ReturnPoint:;
            CurrentSemaCount = ProcessSema.Release();
        }

        public void OnFactorChange()
        {
            /*interrupt e thread. and receive will return -1, 
             * the process function will returned and release Sema another new process thread
             * and e thread will be start soon*/
            this.e.IsBackground = true;
            this.e.Interrupt();
            ProcessSema.WaitOne();
            Reset();
            ProcessSema.Release();



        }

        public void Reset()
        {
            InitDone = false;
            //FailMakeupCount = 0;
            //LockInfo = default(FLockInfo);
            this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type", "result" }, new string[] { "11", "reset" }).jstr);
        }


        protected override void OnDisconnect(string reason)
        {
            Debuger.PrintStr("lock_id: " + lock_id + "____Smartlock Handler disconnect reason: " + reason, EPRINT_TYPE.NORMAL);
            CurrentPair.sl = null;
            if (CurrentPair.app == null)
            {

            }
            else
            {
                CurrentPair = default(FSmartLockPair);
                SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(CurrentPair.app, "Smart lock disconnected!");
                SmartLockTcpHandlerManager.Instance.SmartLockMap.Remove(lock_id);
            }
        }
        protected override void OnConnect()
        {

        }
    }
}
