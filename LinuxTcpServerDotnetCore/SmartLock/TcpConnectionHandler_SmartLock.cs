using Json;
using LinuxTcpServerDotnetCore.SmartLock.Statics;
using LinuxTcpServerDotnetCore.Statics;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Sql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace LinuxTcpServerDotnetCore.SmartLock
{
    public class TcpConnectionHandler_SmartLock : TcpConnectionHandler
    {
        public bool MutilpleHuman = false;
        public bool FingerprintPass = false;
        public bool FacePass = false;
        public bool PinPass = false;
        public int FailMakeupCount = 0;
        public ETcpHandlerType HandlerType = ETcpHandlerType.ESmartLock;
        public List<TcpConnectionHandler_App> Corresponding_App;
        public bool InitDone = false;
        public FSmartLockPair CurrentPair;

        public FLockInfo LockInfo;

        public TcpConnectionHandler_SmartLock(TcpClient client) : base(client)
        {
            Corresponding_App = new List<TcpConnectionHandler_App>();
        }

        public TcpConnectionHandler_SmartLock()
        {

        }


        public override void Init(TcpClient client)
        {
            base.Init(client);
            Corresponding_App = new List<TcpConnectionHandler_App>();
        }

        protected override void ProcessData()
        {
            base.ProcessData();
            string receiveData;
            var reLen = Receiver.ReadRecevieData(out receiveData);
            if (reLen > 0)
            {
                JObject jobj = null;
                try
                {

                    jobj = JObject.Parse(receiveData);
                }
                catch(Exception ex)
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
                switch (header)
                {
                    case EDataHeader.EInit:
                        {
                            if (InitDone)
                                goto ReturnPoint;
                            var lock_id = jobj["lock_id"].ToString();
                            CurrentPair = new FSmartLockPair(false);
                            CurrentPair.sl = this;
                            CurrentPair.app_ls = this.Corresponding_App;
                            SmartLockTcpHandlerManager.Instance.SmartLockMap.Add(lock_id, CurrentPair);
                            InitDone = true;
                            LockInfo = new FLockInfo();
                            MySqlConnection conn;
                            string restr;
                            var reader = SqlWorker.MySqlQuery("beach_smart_lock", "user_data", new string[] { "id","username","phone_id","bluetooth_id","device_id","fingerprint_data","face_data","pin","selected_factor","lock_location"
                            }, "lock_id", lock_id, out conn, out restr);
                            if (reader != null)
                            {
                                if (reader.Read())
                                {
                                    LockInfo.id = reader.GetString("id");
                                    LockInfo.lock_id = lock_id;
                                    LockInfo.username = reader.GetString("username");
                                    LockInfo.phone_id = reader.GetString("phone_id").Split(',');
                                    LockInfo.bluetooth_id = reader.GetString("bluetooth_id").Split(',');
                                    LockInfo.device_id = reader.GetString("device_id").Split(',');
                                    LockInfo.fingerprint_data = reader.GetString("fingerprint_data").Split(',');
                                    LockInfo.face_data = reader.GetString("face_data").Split(',');
                                    LockInfo.pin = reader.GetString("pin");
                                    var tmp = reader.GetString("selected_factor").Split(',');
                                    LockInfo.selected_factor = new int[tmp.Length];
                                    for (int i = 0; i < tmp.Length; i++)
                                    {
                                        LockInfo.selected_factor[i] = int.Parse(tmp[i]);
                                    }
                                    float x, y, z;
                                    var tmp1 = reader.GetString("lock_location").Split(',');
                                    x = float.Parse(tmp1[0]);
                                    y = float.Parse(tmp1[1]);
                                    z = float.Parse(tmp1[2]);
                                    LockInfo.lock_location = new System.Numerics.Vector3(x, y, z);
                                    LockInfo.factor_state = new EFactorState[5];
                                    for (var i = 0; i < 5; i++)
                                    {
                                        foreach (var j in LockInfo.selected_factor)
                                        {
                                            if (j == i)
                                            {
                                                LockInfo.factor_state[i] = EFactorState.EUndetected;
                                                break;
                                            }
                                            else
                                            {
                                                LockInfo.factor_state[i] = EFactorState.EUnSelected;
                                            }
                                        }
                                    }
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
                            if (LockInfo.factor_state[2] == EFactorState.EFail || LockInfo.factor_state[2] == EFactorState.EUndetected)
                            {
                                bool match = false;
                                var rePhone_sn = jobj["phone_id"].ToString();
                                foreach (var i in LockInfo.phone_id)
                                {
                                    if (string.Compare(i, rePhone_sn) == 0)
                                    {
                                        match = true;
                                        break;
                                    }
                                }
                                if (match)
                                {
                                    LockInfo.factor_state[2] = EFactorState.ESuccess;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "3", "2 pass", "200" });
                                }
                                else
                                {
                                    LockInfo.factor_state[2] = EFactorState.EFail;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "3", "2 fail", "200" });
                                }

                            }
                        }
                        break;
                    case EDataHeader.EBluetoothTagDetected:
                        {
                            if (LockInfo.factor_state[3] == EFactorState.EFail || LockInfo.factor_state[3] == EFactorState.EUndetected)
                            {
                                bool match = false;
                                var reBluetoothTag = jobj["bluetooth_tag"].ToString();
                                foreach (var i in LockInfo.bluetooth_id)
                                {
                                    if (string.Compare(i, reBluetoothTag) == 0)
                                    {
                                        match = true;
                                        break;
                                    }
                                }
                                if (match)
                                {
                                    LockInfo.factor_state[3] = EFactorState.ESuccess;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "4", "3 pass", "200" });
                                }
                                else
                                {
                                    LockInfo.factor_state[3] = EFactorState.EFail;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "4", "3 fail", "200" });
                                }
                            }
                        }
                        break;
                    case EDataHeader.EDeviceDetected:
                        {
                            if (LockInfo.factor_state[4] == EFactorState.EFail || LockInfo.factor_state[4] == EFactorState.EUndetected)
                            {
                                bool match = false;
                                var reDeviceId = jobj["device_id"].ToString();
                                foreach (var i in LockInfo.device_id)
                                {
                                    if (string.Compare(i, reDeviceId) == 0)
                                    {
                                        match = true;
                                        break;
                                    }
                                }
                                if (match)
                                {
                                    LockInfo.factor_state[4] = EFactorState.ESuccess;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "5", "4 pass", "200" });
                                }
                                else
                                {
                                    LockInfo.factor_state[4] = EFactorState.EFail;
                                    jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "5", "4 fail", "200" });
                                }
                            }
                        }
                        break;
                    case EDataHeader.EHumanCountImg:
                        {
                            if (MutilpleHuman)
                                break;
                            var img_data = jobj["raw_img"].ToString();
                            byte[] raw_img = Encoding.ASCII.GetBytes(img_data);
                            FileStream fs = new FileStream("D:/human_count_imgs/" + Guid.NewGuid().ToString(), FileMode.Create);
                            BinaryWriter bw = new BinaryWriter(fs);
                            bw.Write(raw_img);
                            bw.Close();
                            fs.Close();
                            Random random = new Random();
                            var tmp = random.Next(1, 100);
                            if (tmp > 80)
                            {
                                MutilpleHuman = true;
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "6", "5 fail", "200" });
                            }
                            else
                            {
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "6", "5 pass", "200" });
                            }
                        }
                        break;
                    case EDataHeader.EFingerprintData:
                        {
                            if (FingerprintPass)
                                break;
                            bool match = false;
                            var finger_data = jobj["finger_data"].ToString();
                            foreach (var i in LockInfo.fingerprint_data)
                            {
                                if (string.Compare(finger_data, i) == 0)
                                {
                                    match = true;
                                    break;
                                }
                            }
                            if (match)
                            {
                                FingerprintPass = true;
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "7", "6 pass", "200" });
                                FailMakeupCount++;
                            }
                            else
                            {
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "7", "6 fail", "200" });
                            }
                        }
                        break;
                    case EDataHeader.EFaceData:
                        {
                            if (FacePass)
                                break;
                            var img_data = jobj["raw_img"].ToString();
                            byte[] raw_img = Encoding.ASCII.GetBytes(img_data);
                            FileStream fs = new FileStream("D:/face_data_imgs/" + Guid.NewGuid().ToString(), FileMode.Create);
                            BinaryWriter bw = new BinaryWriter(fs);
                            bw.Write(raw_img);
                            bw.Close();
                            fs.Close();
                            Random random = new Random();
                            var tmp = random.Next(1, 100);
                            if (tmp > 10)
                            {
                                FacePass = true;
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "8", "7 fail", "200" });
                            }
                            else
                            {
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "8", "7 pass", "200" });
                            }
                        }
                        break;
                    case EDataHeader.EPinInput:
                        {
                            if (PinPass)
                                break;
                            var pin_str = jobj["pin"].ToString();
                            if (LockInfo.pin == pin_str)
                            {
                                PinPass = true;
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "9", "8 pass", "200" });
                                FailMakeupCount++;
                            }
                            else
                            {
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "9", "8 fail", "200" });
                            }
                        }
                        break;
                    case EDataHeader.EVoiceInput:
                        {
                            if (jobj["voice_data"].ToString() == "1")
                            {
                                MutilpleHuman = false;
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "10", "", "200" });
                            }
                            else if (jobj["voice_data"].ToString() == "2")
                            {
                                jstr = JsonWorker.MakeSampleJson(new string[] { "type", "result", "state" }, new string[] { "10", "keep door open", "200" });
                            }
                        }
                        break;
                }

                if (jstr != null)
                {
                    var light_state = (int)LightManager.GetLightState(LockInfo, FailMakeupCount);
                    if (MutilpleHuman)
                        light_state += 100;
                    jstr.jobj[0]["state"] = new JValue(light_state);

                    this.Sender.WriteSendData(jstr.jobj[0].ToString());
                    foreach (var i in Corresponding_App)
                    {
                        i.Sender.WriteSendData(jstr.jobj[0].ToString());
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


        protected override void OnDisconnect(string reason)
        {
            Debuger.PrintStr("lock_id: " + LockInfo.lock_id + "____Smartlock Handler disconnect reason: " + reason, EPRINT_TYPE.NORMAL);
            CurrentPair.sl = null;
            if(CurrentPair.app_ls == null)
            {
                
            }
            else if (CurrentPair.sl == null && CurrentPair.app_ls.Count == 0)
            {
                CurrentPair = default(FSmartLockPair);
                SmartLockTcpHandlerManager.Instance.SmartLockMap.Remove(LockInfo.lock_id);
            }
        }
        protected override void OnConnect()
        {

        }
    }
}
