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
        public FLockInfo LockInfo;

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
                goto ReturnPoint;
            if (reLen > 0)
            {
                JObject jobj = JObject.Parse(receiveData);
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
                        lock_id = jobj["lock_id"].ToString();
                        FSmartLockPair pair = new FSmartLockPair();
                        pair.sl = this;
                        SmartLockTcpHandlerManager.Instance.SmartLockMap.Add(lock_id, pair);
                        CurrentPair = pair;
                        InitDone = true;
                        this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" },
                            new string[] { "normal", "init done!", "200" }).jstr);


                        LockInfo = new FLockInfo();

                        MySqlConnection conn;
                        string restr;

                        var reader = SqlWorker.MySqlQuery("beach_smart_lock", "lock_data", new string[] { "id"
                            ,"accounts" }, "lock_id", lock_id, out conn, out restr);
                        reader.Read();
                        LockInfo.id = reader.GetString(0);
                        reader.Close();
                        conn.Close();


                        var accounts = reader.GetString(1).Split(',');
                        string[] locateColNames = new string[accounts.Length];
                        for(int i = 0; i < locateColNames.Length; i++)
                            locateColNames[i] = ("bound_lock_id");
                        LockInfo.user_infos = new FUserInfo[accounts.Length];
                        reader = SqlWorker.MySqlLocateQuery("beach_smart_lock", "user_data", new string[] { "id","user_name",
                        "bound_lock_id","phone_bluetooth","bluetooth_tag","device_id","pin"}, locateColNames, accounts,out conn);
                        int i_ = 0;
                        while(reader.Read())
                        {
                            LockInfo.user_infos[i_].id = reader.GetString(0);
                            LockInfo.user_infos[i_].user_name = reader.GetString(1);
                            LockInfo.user_infos[i_].bound_lock_id = reader.GetString(2);
                            LockInfo.user_infos[i_].phone_bluetooth = reader.GetString(3);
                            LockInfo.user_infos[i_].bluetooth_tag= reader.GetString(4);
                            LockInfo.user_infos[i_].device_id = reader.GetString(5);
                            LockInfo.user_infos[i_].pin = reader.GetString(6);
                            i_++;
                        }
                        reader.Close();
                        conn.Close();
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
                    var to_target = jobj["target"].ToString();
                    if (to_target == "0")//to server
                    {
                        var tag = jobj["tag"].ToString();
                        var str = jobj["content"].ToString();
                        switch (tag)
                        {
                            case "bluetooth_tag":
                                {
                                    foreach(var i in LockInfo.user_infos)
                                    {
                                        if(i.bluetooth_tag.IndexOf(str) != -1 && i.user_name == CurrentPair.app.User_name)
                                        {
                                            this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type",
                                            "result","code"}, new string[] { "success", "true", "200" }).jstr);
                                            goto ReturnPoint;;
                                        }
                                    }
                                    this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type",
                                            "result","code"}, new string[] { "success", "false", "200" }).jstr);
                                    goto ReturnPoint;;
                                }
                                break;
                            case "phone_bluetooth":
                                {
                                    foreach (var i in LockInfo.user_infos)
                                    {
                                        if (i.phone_bluetooth.IndexOf(str) != -1 && i.user_name == CurrentPair.app.User_name)
                                        {
                                            this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type",
                                            "result","code"}, new string[] { "success", "true", "200" }).jstr);
                                            goto ReturnPoint;;
                                        }
                                    }
                                    this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type",
                                            "result","code"}, new string[] { "success", "false", "200" }).jstr);
                                    goto ReturnPoint;;
                                }
                                break;
                            case "device_id":
                                {
                                    foreach (var i in LockInfo.user_infos)
                                    {
                                        if (i.device_id.IndexOf(str) != -1 && i.user_name == CurrentPair.app.User_name)
                                        {
                                            this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type",
                                            "result","code"}, new string[] { "success", "true", "200" }).jstr);
                                            goto ReturnPoint;;
                                        }
                                    }
                                    this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type",
                                            "result","code"}, new string[] { "success", "false", "200" }).jstr);
                                    goto ReturnPoint;;
                                }
                                break;
                            case "pin":
                                {
                                    foreach (var i in LockInfo.user_infos)
                                    {
                                        if (i.pin.IndexOf(str) != -1 && i.user_name == CurrentPair.app.User_name)
                                        {
                                            this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type",
                                            "result","code"}, new string[] { "success", "true", "200" }).jstr);
                                            goto ReturnPoint;;
                                        }
                                    }
                                    this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type",
                                            "result","code"}, new string[] { "success", "false", "200" }).jstr);
                                    goto ReturnPoint;;
                                }
                                break;
                        }
                    }
                    else //to phone
                    {
                        if (CurrentPair.app == null)
                        {
                            this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "normal", "none app connect to this unlock pair", "200" }).jstr);
                        }
                        else
                        {
                            CurrentPair.app.Sender.WriteSendData(receiveData);
                        }
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
