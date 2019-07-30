using Json;
using LinuxTcpServerDotnetCore.SmartLock.Statics;
using LinuxTcpServerDotnetCore.Statics;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Sockets;

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
        public TcpConnectionHandler_App(TcpClient client) : base(client)
        {

        }

        public TcpConnectionHandler_App()
        {

        }

        public override void Init(TcpClient client)
        {
            base.Init(client);

        }

        protected override void ProcessData()
        {
            base.ProcessData();
            string receiveData;
            var relen = Receiver.ReadRecevieData(out receiveData);
            //Debuger.PrintStr(relen.ToString(), EPRINT_TYPE.NORMAL);
            if (relen > 0)
            {
                JObject jobj = JObject.Parse(receiveData);
                if (jobj == null)
                {
                    Debuger.PrintStr("Parse Json string fail!", EPRINT_TYPE.WARNING);
                    return;
                }


                EDataHeader header = (EDataHeader)int.Parse(jobj["type"].ToString());
                //Debuger.PrintStr(header.ToString(), EPRINT_TYPE.NORMAL);
                //string message_id = jobj["msg_id"].ToString();
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
                                var tmpPair = SmartLockTcpHandlerManager.Instance.SmartLockMap[LockId];
                                tmpPair.app_ls.Add(this);
                                //Corresponding_SL = tmpPair.sl;
                                CurrentPair = tmpPair;
                                CurrentPair.app_ls.Add(this);
                                InitDone = true;

                            }
                            break;
                        case EDataHeader.EGpsTrigger:
                            {
                                if (CurrentPair.sl.LockInfo.factor_state[0] == EFactorState.EFail || CurrentPair.sl.LockInfo.factor_state[0] == EFactorState.EUndetected)
                                {
                                    if (bool.Parse(jobj["result"].ToString()))
                                    {
                                        CurrentPair.sl.LockInfo.factor_state[0] = EFactorState.ESuccess;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "1", "0 pass", "200" });
                                    }
                                    else
                                    {
                                        CurrentPair.sl.LockInfo.factor_state[0] = EFactorState.EFail;
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
                                if (CurrentPair.sl.LockInfo.factor_state[1] == EFactorState.EFail || CurrentPair.sl.LockInfo.factor_state[1] == EFactorState.EUndetected)
                                {
                                    if (bool.Parse(jobj["result"].ToString()))
                                    {
                                        CurrentPair.sl.LockInfo.factor_state[1] = EFactorState.ESuccess;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "2", "1 pass", "200" });
                                    }
                                    else
                                    {
                                        CurrentPair.sl.LockInfo.factor_state[1] = EFactorState.EFail;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "2", "1 fail", "200" });
                                    }
                                }
                            }
                            break;
                    }
                }
                catch(Exception ex)
                {
                    Debuger.PrintStr(ex.Message, EPRINT_TYPE.ERROR);
                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result" }, new string[] { ((int)header).ToString(), "json param not complete!" });
                }
                if (jstr != null)
                {
                    var light_state = (int)LightManager.GetLightState(CurrentPair.sl.LockInfo, CurrentPair.sl.FailMakeupCount);
                    if (CurrentPair.sl.LockInfo.multiple_human == EFactorState.ESuccess)
                        light_state += 100;
                    jstr.jobj[0]["state"] = new JValue(light_state);

                    this.Sender.WriteSendData(jstr.jobj[0].ToString());
                    foreach (var i in CurrentPair.app_ls)
                    {
                        i.Sender.WriteSendData(jstr.jobj[0].ToString());
                    }
                }

            }
            else
            {
                ReceiveZeroDisconnect = true;
            }
            CurrentSemaCount = ProcessSema.Release();

        }

        protected override void OnDisconnect(string reason)
        {
            Debuger.PrintStr("lock_id: " + CurrentPair.sl.LockInfo.lock_id + "____App Handler disconnect reason: " + reason, EPRINT_TYPE.NORMAL);
            CurrentPair.app_ls.Remove(this);
            if (CurrentPair.sl == null && CurrentPair.app_ls.Count == 0)
            {
                CurrentPair = default(FSmartLockPair);
                SmartLockTcpHandlerManager.Instance.SmartLockMap.Remove(LockId);
            }
        }
    }
}
