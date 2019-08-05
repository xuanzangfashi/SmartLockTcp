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
                try
                {
                    switch (header)
                    {
                        case EDataHeader.EInit:
                            {
                                if (InitDone)
                                    return;
                                LockId = jobj["lock_id"].ToString();
                                var phone_id = jobj["phone_id"].ToString();

                                if (!SmartLockTcpHandlerManager.Instance.SmartLockMap.ContainsKey(LockId))
                                {
                                    //Thread tmpThread = new Thread(SelfProcessingDisconnectThread);
                                    //tmpThread.Start("Smart lock does not connect yet!");

                                    //SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(this, "Smart lock does not connect yet!");
                                    ReceiveZeroDisconnect = true;
                                    DisconnectReason = "Smart lock does not connect yet!";
                                    goto ReturnPoint;
                                }

                                var tmpPair = SmartLockTcpHandlerManager.Instance.SmartLockMap[LockId];
                                if (tmpPair.app != null)
                                {
                                    //tmpPair.app.ProcessSema.WaitOne();
                                    ReceiveZeroDisconnect = true;
                                    DisconnectReason = "An app had already connected to the server!";
                                    Debuger.PrintStr(ReceiveZeroDisconnect.ToString(), EPRINT_TYPE.NORMAL);
                                    goto ReturnPoint;

                                    // SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(tmpPair.app, "Another app instance has connected!");//断开上一个app
                                    //tmpPair.app = null;
                                }
                                tmpPair.app = this;

                                for (int i = 0; i < tmpPair.sl.LockInfo.phone_id.Length; i++)
                                {
                                    if (tmpPair.sl.LockInfo.phone_id[i] == phone_id)
                                    {
                                        CurrentPhoneIndex = i;
                                    }
                                }
                                if (CurrentPhoneIndex == -1)
                                {
                                    ReceiveZeroDisconnect = true;
                                    DisconnectReason = "phone id does not match!";
                                    //Thread tmpThread = new Thread(SelfProcessingDisconnectThread);
                                    //tmpThread.Start("phone id does not match!");
                                    goto ReturnPoint;
                                }
                                if (CurrentPhoneIndex >= tmpPair.sl.LockInfo.selected_factor.Length)
                                {
                                    ReceiveZeroDisconnect = true;
                                    DisconnectReason = "this phone does not setup factor!";
                                    //Thread tmpThread = new Thread(SelfProcessingDisconnectThread);
                                    //tmpThread.Start("this phone does not setup factor!");
                                    goto ReturnPoint;
                                }

                                //Corresponding_SL = tmpPair.sl;
                                CurrentPair = tmpPair;
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "data" }, new string[] { "0", CurrentPair.sl.LockInfo.lock_location.ToString() });
                                InitDone = true;

                            }
                            break;
                        case EDataHeader.EGpsTrigger:
                            {
                                if (CurrentPair.sl.LockInfo.factor_state[CurrentPhoneIndex][0] == EFactorState.EFail || CurrentPair.sl.LockInfo.factor_state[CurrentPhoneIndex][0] == EFactorState.EUndetected)
                                {
                                    if (bool.Parse(jobj["result"].ToString()))
                                    {
                                        CurrentPair.sl.LockInfo.factor_state[CurrentPhoneIndex][0] = EFactorState.ESuccess;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "1", "0 pass", "200" });
                                    }
                                    else
                                    {
                                        CurrentPair.sl.LockInfo.factor_state[CurrentPhoneIndex][0] = EFactorState.EFail;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "1", "0 fail", "200" });
                                    }
                                }
                                #region Old_Part
                                //var location_str_arr = jobj["app_location"].ToString().Split(',');
                                //Vector3 app_location = new Vector3();
                                //app_location.X = float.Parse(location_str_arr[0]);
                                //app_location.Y = float.Parse(location_str_arr[1]);
                                //app_location.Z = float.Parse(location_str_arr[2]);
                                //Vector3 deltaVec = app_location - CurrentPair.sl.LockInfo.lock_location;
                                //float physicsLength = deltaVec.Length();
                                //Debuger.PrintStr("physicsLength: " + physicsLength.ToString(), EPRINT_TYPE.NORMAL);
                                //if (CurrentPair.sl.LockInfo.factor_state[0] == EFactorState.EUndetected ||
                                //    CurrentPair.sl.LockInfo.factor_state[0] == EFactorState.EFail)
                                //    if (physicsLength < 5000f)
                                //    {
                                //        foreach (var i in CurrentPair.app_ls)
                                //        {
                                //            if (i == this)
                                //                continue;
                                //            SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(i, "other phone triggered!");
                                //            CurrentPair.app_ls.Remove(i);
                                //        }
                                //        CurrentPair.sl.LockInfo.factor_state[0] = EFactorState.ESuccess;
                                //        var jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, 
                                //            new string[] { ((int)EDataHeader.EAppLocation).ToString(), "0 pass", "200" });
                                //
                                //
                                //        var light_state = LightManager.GetLightState(CurrentPair.sl.LockInfo, CurrentPair.sl.FailMakeupCount);
                                //        jstr.jobj[0]["state"] = new JValue((int)light_state);
                                //
                                //        this.Sender.WriteSendData(jstr.jobj[0].ToString());
                                //        CurrentPair.sl.Sender.WriteSendData(jstr.jobj[0].ToString());
                                //    }
                                //if (CurrentPair.sl.LockInfo.factor_state[1] == EFactorState.EUndetected ||
                                //    CurrentPair.sl.LockInfo.factor_state[1] == EFactorState.EFail)
                                //    if (physicsLength < 2000f)
                                //    {
                                //        foreach (var i in CurrentPair.app_ls)
                                //        {
                                //            if (i == this)
                                //                continue;
                                //            SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(i, "other phone triggered!");
                                //            CurrentPair.app_ls.Remove(i);
                                //        }
                                //        var jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, 
                                //            new string[] { ((int)EDataHeader.EAppLocation).ToString(), "1 pass", "200" });
                                //
                                //
                                //        var light_state = LightManager.GetLightState(CurrentPair.sl.LockInfo, CurrentPair.sl.FailMakeupCount);
                                //        jstr.jobj[0]["state"] = new JValue((int)light_state);
                                //
                                //
                                //        this.Sender.WriteSendData(jstr.jobj[0].ToString());
                                //        CurrentPair.sl.Sender.WriteSendData(jstr.jobj[0].ToString());
                                //    }
                                #endregion
                            }
                            break;
                        case EDataHeader.EGeofencingTrigger:
                            {
                                if (CurrentPair.sl.LockInfo.factor_state[CurrentPhoneIndex][1] == EFactorState.EFail || CurrentPair.sl.LockInfo.factor_state[CurrentPhoneIndex][1] == EFactorState.EUndetected)
                                {
                                    if (bool.Parse(jobj["result"].ToString()))
                                    {
                                        CurrentPair.sl.LockInfo.factor_state[CurrentPhoneIndex][1] = EFactorState.ESuccess;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "2", "1 pass", "200" });
                                    }
                                    else
                                    {
                                        CurrentPair.sl.LockInfo.factor_state[CurrentPhoneIndex][1] = EFactorState.EFail;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "2", "1 fail", "200" });
                                    }
                                }
                            }
                            break;
                        case EDataHeader.ECommitFactor:
                            {
                                //string[] factors = jobj["factors"].ToString().Split(',');
                                var c_phone_id = jobj["phone_id"].ToString();
                                var c_lock_id = jobj["lock_id"].ToString();
                                MySqlConnection conn = null;
                                string sql_re = null;
                                var reader = SqlWorker.MySqlQuery("beach_smart_lock", "user_data", new string[] { "selected_factor", "phone_id" }, "lock_id", c_lock_id, out conn, out sql_re);
                                reader.Read();
                                var selected_factor = reader.GetString(0).Split('|');
                                var phone_id = reader.GetString(1).Split(',');
                                reader.Close();
                                conn.Close();
                                reader = null;
                                conn = null;
                                for (int i = 0; i < phone_id.Length; i++)
                                {
                                    if (phone_id[i] == c_phone_id)
                                    {
                                        selected_factor[i] = jobj["factors"].ToString();
                                        SqlWorker.MySqlEdit("beach_smart_lock", "user_data", "lock_id", c_lock_id,
                                            new string[] { "selected_factor" }, new string[] { string.Join('|', selected_factor) });
                                        var tmppair = SmartLockTcpHandlerManager.Instance.SmartLockMap[c_lock_id];

                                        /*abort app threads. dotnet core does not support the Abort() function.Interrrut() will throw exception when the thread has been block by socket.receive()*/

                                        if (InitDone)//Phone Connected and it's in unlock process
                                        {
                                            ReceiveZeroDisconnect = true;
                                            DisconnectReason = "factor change,lock thread reset";
                                            Debuger.PrintStr(ReceiveZeroDisconnect.ToString(), EPRINT_TYPE.NORMAL);
                                            //goto ReturnPoint;
                                        }
                                        else
                                        {

                                        }

                                        tmppair.sl.OnFactorChange();
                                        break;
                                    }
                                }
                                
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debuger.PrintStr(ex.Message, EPRINT_TYPE.ERROR);
                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result" }, new string[] { ((int)header).ToString(), "json param not complete!" });
                }
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
