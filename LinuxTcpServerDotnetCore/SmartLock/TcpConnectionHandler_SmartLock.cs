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
        public int FailMakeupCount = 0;
        public ETcpHandlerType HandlerType = ETcpHandlerType.ESmartLock;
        public bool InitDone = false;
        public FSmartLockPair CurrentPair;
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
                return;
            if (reLen > 0)
            {
                JObject jobj = null;
                try
                {

                    jobj = JObject.Parse(receiveData);
                }
                catch (Exception ex)
                {
                    jobj = null;
                    Debuger.PrintStr(ex.Message, EPRINT_TYPE.ERROR);
                }
                if (jobj == null)
                {
                    Debuger.PrintStr("Parse Json string fail!", EPRINT_TYPE.WARNING);
                    goto ReturnPoint;
                }


                EDataHeader header = (EDataHeader)int.Parse(jobj["type"].ToString());
                //string message_id = jobj["msg_id"].ToString();
                JsonObject jstr = null;
                try
                {
                    switch (header)
                    {
                        case EDataHeader.EInit:
                            {
                                if (InitDone)
                                    goto ReturnPoint;
                                var lock_id = jobj["lock_id"].ToString();
                                CurrentPair = new FSmartLockPair(false);
                                CurrentPair.sl = this;
                                SmartLockTcpHandlerManager.Instance.SmartLockMap.Add(lock_id, CurrentPair);
                                InitDone = true;
                                LockInfo = new FLockInfo();
                                MySqlConnection conn;
                                string restr;
                                var reader = SqlWorker.MySqlQuery("beach_smart_lock", "user_data", new string[] { "id","phone_id","bluetooth_id","device_id","pin","selected_factor","lock_location"
                            }, "lock_id", lock_id, out conn, out restr);
                                if (reader != null)
                                {
                                    if (reader.Read())
                                    {
                                        LockInfo.id = reader.GetString("id");
                                        LockInfo.lock_id = lock_id;
                                        LockInfo.phone_id = reader.GetString("phone_id").Split(',');
                                        LockInfo.bluetooth_id = reader.GetString("bluetooth_id").Split(',');
                                        LockInfo.device_id = reader.GetString("device_id").Split(',');
                                        LockInfo.pin = reader.GetString("pin");
                                        var tmp = reader.GetString("selected_factor").Split('|');
                                        LockInfo.selected_factor = new int[tmp.Length][];
                                        LockInfo.factor_state = new EFactorState[tmp.Length][];
                                        for (int i = 0; i < tmp.Length; i++)
                                        {
                                            var tmp_1 = tmp[i].Split(',');
                                            LockInfo.selected_factor[i] = new int[tmp_1.Length];
                                            LockInfo.factor_state[i] = new EFactorState[5];
                                            for (int j = 0; j < tmp_1.Length; j++)
                                            {
                                                LockInfo.selected_factor[i][j] = int.Parse(tmp_1[i]);
                                                for (int k = 0; k < LockInfo.factor_state[i].Length; k++)
                                                {
                                                    if (k == LockInfo.selected_factor[i][j])
                                                    {
                                                        LockInfo.factor_state[i][k] = EFactorState.EUndetected;
                                                    }
                                                    else
                                                    {
                                                        LockInfo.factor_state[i][k] = EFactorState.EUnSelected;
                                                    }
                                                }
                                            }
                                        }
                                        float x, y, z;
                                        var tmp1 = reader.GetString("lock_location").Split(',');
                                        x = float.Parse(tmp1[0]);
                                        y = float.Parse(tmp1[1]);
                                        z = float.Parse(tmp1[2]);
                                        LockInfo.lock_location = new System.Numerics.Vector3(x, y, z);

                                        LockInfo.resident_factor_state = new EFactorState[] { EFactorState.EUnSelected, EFactorState.EUnSelected, EFactorState.EUnSelected };
                                        LockInfo.multiple_human = EFactorState.EFail;

                                    }

                                    reader.Close();
                                    reader = null;
                                    conn.Close();
                                    conn = null;
                                    restr = null;
                                }
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result" }, new string[] { "0", "lock init done!" });
                                break;
                            }
                        case EDataHeader.EPhoneBluetoothDetected:
                            {
                                if (LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][2] == EFactorState.EFail || LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][2] == EFactorState.EUndetected)
                                {
                                    if (bool.Parse(jobj["result"].ToString()))
                                    {
                                        LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][2] = EFactorState.ESuccess;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "3", "2 pass", "200" });
                                    }
                                    else
                                    {
                                        LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][2] = EFactorState.EFail;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "3", "2 fail", "200" });
                                    }

                                }
                            }
                            break;
                        case EDataHeader.EBluetoothTagDetected:
                            {
                                if (LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][3] == EFactorState.EFail || LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][3] == EFactorState.EUndetected)
                                {
                                    if (bool.Parse(jobj["result"].ToString()))
                                    {
                                        LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][3] = EFactorState.ESuccess;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "4", "3 pass", "200" });
                                    }
                                    else
                                    {
                                        LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][3] = EFactorState.EFail;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "4", "3 fail", "200" });
                                    }
                                }
                            }
                            break;
                        case EDataHeader.EDeviceDetected:
                            {
                                if (LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][4] == EFactorState.EFail || LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][4] == EFactorState.EUndetected)
                                {
                                    if (bool.Parse(jobj["result"].ToString()))
                                    {
                                        LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][4] = EFactorState.ESuccess;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "5", "4 pass", "200" });
                                    }
                                    else
                                    {
                                        LockInfo.factor_state[CurrentPair.app.CurrentPhoneIndex][4] = EFactorState.EFail;
                                        jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "5", "4 fail", "200" });
                                    }
                                }
                            }
                            break;
                        case EDataHeader.EHumanCountImg:
                            {

                                if (bool.Parse(jobj["result"].ToString()))
                                {
                                    LockInfo.multiple_human = EFactorState.ESuccess;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "6", "5 pass", "200" });
                                }
                                else
                                {
                                    LockInfo.multiple_human = EFactorState.EFail;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "6", "5 fail", "200" });
                                }
                            }
                            break;
                        case EDataHeader.EFingerprintData:
                            {
                                if (LockInfo.resident_factor_state[1] == EFactorState.ESuccess)
                                    break;
                                var finger_result = jobj["result"].ToString();
                                if (bool.Parse(finger_result))
                                {
                                    LockInfo.resident_factor_state[1] = EFactorState.ESuccess;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "7", "6 pass", "200" });
                                    FailMakeupCount++;
                                }
                                else
                                {
                                    LockInfo.resident_factor_state[1] = EFactorState.EFail;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "7", "6 fail", "200" });
                                }
                            }
                            break;
                        case EDataHeader.EFaceData:
                            {
                                if (LockInfo.resident_factor_state[2] == EFactorState.ESuccess)
                                    break;
                                var img_result = jobj["result"].ToString();
                                if (bool.Parse(img_result))
                                {
                                    LockInfo.resident_factor_state[2] = EFactorState.ESuccess;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "8", "7 pass", "200" });
                                }
                                else
                                {
                                    LockInfo.resident_factor_state[2] = EFactorState.EFail;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "8", "7 fail", "200" });
                                }
                            }
                            break;
                        case EDataHeader.EPinInput:
                            {
                                if (LockInfo.resident_factor_state[0] == EFactorState.ESuccess)
                                    break;
                                var pin_str = jobj["pin"].ToString();
                                if (LockInfo.pin == pin_str)
                                {
                                    LockInfo.resident_factor_state[0] = EFactorState.ESuccess;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "9", "8 pass", "200" });
                                    FailMakeupCount++;
                                }
                                else
                                {
                                    LockInfo.resident_factor_state[0] = EFactorState.EFail;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "9", "8 fail", "200" });
                                }
                            }
                            break;
                        case EDataHeader.EVoiceInput:
                            {
                                if (jobj["voice_data"].ToString() == "1")
                                {
                                    LockInfo.multiple_human = EFactorState.EFail;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "10", "", "200" });
                                }
                                else if (jobj["voice_data"].ToString() == "2")
                                {
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "10", "keep door open", "200" });
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
                    if (CurrentPair.app != null)
                        CurrentPair.app.Sender.WriteSendData(jstr.jstr);
                    this.Sender.WriteSendData(jstr.jstr);

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
            Reset();
            
            
        }

        public void Reset()
        {
            InitDone = false;
            FailMakeupCount = 0;
            LockInfo = default(FLockInfo);
            this.Sender.WriteSendData(JsonWorker.MakeSampleJson(new string[] { "type", "result" }, new string[] { "11", "reset" }).jstr);
        }


        protected override void OnDisconnect(string reason)
        {
            Debuger.PrintStr("lock_id: " + LockInfo.lock_id + "____Smartlock Handler disconnect reason: " + reason, EPRINT_TYPE.NORMAL);
            CurrentPair.sl = null;
            if (CurrentPair.app == null)
            {

            }
            else
            {
                CurrentPair = default(FSmartLockPair);
                SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(CurrentPair.app, "Smart lock disconnected!");
                SmartLockTcpHandlerManager.Instance.SmartLockMap.Remove(LockInfo.lock_id);
            }
        }
        protected override void OnConnect()
        {

        }
    }
}
