using Json;
using LinuxTcpServerDotnetCore.SmartLock;
using LinuxTcpServerDotnetCore.SmartLock.Statics;
using LinuxTcpServerDotnetCore.Statics;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Sql;
using System;
using System.Net;

namespace LinuxTcpServerDotnetCore.Http.HttpHandler
{
    class HttpHandler_ReceiveCommitMsg : HttpRequestHandler
    {
        protected override bool OnCreate()
        {
            return base.OnCreate();
        }

        protected override string PostHandle(HttpListenerRequest request, HttpListenerResponse response)
        {
            var httpStreamWorker = HttpStreamWorker.CreateHttpStreamWorker(request.RemoteEndPoint.ToString());
            var json_data = httpStreamWorker.ReadInputStreamToString(request.InputStream, (int)request.ContentLength64);
            JObject jobj = null;
            try
            {
                jobj = JObject.Parse(json_data);
            }
            catch (Exception ex)
            {
                Debuger.PrintStr(ex.Message, EPRINT_TYPE.ERROR);
                return JsonWorker.MakeSampleJson(new string[] { "type", "result" }, new string[] { "error", "json string can not be parsed by JObject!" }).jstr;
            }
            var commit_type = (EHttpCommitMsgType)int.Parse(jobj["commit_type"].ToString());

            switch (commit_type)
            {
                case EHttpCommitMsgType.EFactorChange:
                    {
                        var c_phone_id = jobj["phone_id"].ToString();
                        var c_lock_id = jobj["lock_id"].ToString();
                        if(SmartLockTcpHandlerManager.Instance.SmartLockMap.ContainsKey(c_lock_id))
                        {

                        }
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
                                tmppair.app.e.IsBackground = true;                           
                                tmppair.app.e.Interrupt();                                       
                                tmppair.app.ConnectionMainThread.IsBackground = true;           
                                tmppair.app.ConnectionMainThread.Interrupt();

                                SmartLockTcpHandlerManager.Instance.DisconnectTcpConnectionHandler(tmppair.app,"factor change,lock thread reset");



                                tmppair.sl.OnFactorChange();
                                break;
                            }
                        }

                    }
                    break;
            }

            return "";
        }

        protected override string GetHandle(HttpListenerRequest request, HttpListenerResponse response)
        {
            return JsonWorker.MakeSampleReturnJson(null, new string[] { "Warning", "Not response for get method in current subdir!", "404" }).jstr;
        }
    }
}
