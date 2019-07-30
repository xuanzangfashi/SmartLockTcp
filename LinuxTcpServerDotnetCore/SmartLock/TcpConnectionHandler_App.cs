using Json;
using LinuxTcpServerDotnetCore.SmartLock.Statics;
using LinuxTcpServerDotnetCore.Statics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace LinuxTcpServerDotnetCore.SmartLock
{
    public class TcpConnectionHandler_App : TcpConnectionHandler
    {
        public ETcpHandlerType HandlerType = ETcpHandlerType.EApp; 

        public TcpConnectionHandler_SmartLock Corresponding_SL;
        public List<TcpConnectionHandler_App> CurrentApps;
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

                switch (header)
                {
                    case EDataHeader.EInit:
                        {
                            if (InitDone)
                                return;
                            LockId = jobj["lock_id"].ToString();
                            var tmpPair = SmartLockTcpHandlerManager.Instance.SmartLockMap[LockId];
                            tmpPair.app_ls.Add(this);
                            Corresponding_SL = tmpPair.sl;
                            Corresponding_SL.Corresponding_App.Add(this);
                            InitDone = true;
                            CurrentApps = tmpPair.app_ls;
                            CurrentPair = tmpPair;
                        }
                        break;
                    case EDataHeader.EAppLocation:
                        {
                            var location_str_arr = jobj["app_location"].ToString().Split(',');
                            Vector3 app_location = new Vector3();
                            app_location.X = float.Parse(location_str_arr[0]);
                            app_location.Y = float.Parse(location_str_arr[1]);
                            app_location.Z = float.Parse(location_str_arr[2]);
                            Vector3 deltaVec = app_location - Corresponding_SL.LockInfo.lock_location;
                            float physicsLength = deltaVec.Length();
                            Debuger.PrintStr("physicsLength: " + physicsLength.ToString(), EPRINT_TYPE.NORMAL);
                            if (Corresponding_SL.LockInfo.factor_state[0] == EFactorState.EUndetected ||
                                Corresponding_SL.LockInfo.factor_state[0] == EFactorState.EFail)
                                if (physicsLength < 5000f)
                                {
                                    foreach (var i in CurrentApps)
                                    {
                                        if (i == this)
                                            continue;
                                        SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(i, "other phone triggered!");
                                        CurrentApps.Remove(i);
                                    }
                                    Corresponding_SL.LockInfo.factor_state[0] = EFactorState.ESuccess;
                                    var jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, 
                                        new string[] { ((int)EDataHeader.EAppLocation).ToString(), "0 pass", "200" });


                                    var light_state = LightManager.GetLightState(Corresponding_SL.LockInfo,Corresponding_SL.FailMakeupCount);
                                    jstr.jobj[0]["state"] = new JValue((int)light_state);

                                    this.Sender.WriteSendData(jstr.jobj[0].ToString());
                                    Corresponding_SL.Sender.WriteSendData(jstr.jobj[0].ToString());
                                }
                            if (Corresponding_SL.LockInfo.factor_state[1] == EFactorState.EUndetected ||
                                Corresponding_SL.LockInfo.factor_state[1] == EFactorState.EFail)
                                if (physicsLength < 2000f)
                                {
                                    foreach (var i in CurrentApps)
                                    {
                                        if (i == this)
                                            continue;
                                        SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(i, "other phone triggered!");
                                        CurrentApps.Remove(i);
                                    }
                                    var jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, 
                                        new string[] { ((int)EDataHeader.EAppLocation).ToString(), "1 pass", "200" });


                                    var light_state = LightManager.GetLightState(Corresponding_SL.LockInfo,Corresponding_SL.FailMakeupCount);
                                    jstr.jobj[0]["state"] = new JValue((int)light_state);


                                    this.Sender.WriteSendData(jstr.jobj[0].ToString());
                                    Corresponding_SL.Sender.WriteSendData(jstr.jobj[0].ToString());
                                }
                        }
                        break;
                }
            }
            CurrentSemaCount = ProcessSema.Release();

        }

        protected override void OnDisconnect(string reason)
        {
            Debuger.PrintStr("lock_id: " + Corresponding_SL.LockInfo.lock_id + "____App Handler disconnect reason: " + reason, EPRINT_TYPE.NORMAL);
            CurrentApps.Remove(this);
            if (CurrentPair.sl == null && CurrentPair.app_ls.Count == 0)
            {
                CurrentPair = default(FSmartLockPair);
                SmartLockTcpHandlerManager.Instance.SmartLockMap.Remove(LockId);
            }
        }
    }
}
