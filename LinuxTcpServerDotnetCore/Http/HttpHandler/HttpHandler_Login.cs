using Json;
using LinuxTcpServerDotnetCore.SmartLock;
using LinuxTcpServerDotnetCore.Statics;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Sql;
using System;
using System.Net;

namespace LinuxTcpServerDotnetCore.Http.HttpHandler
{
    class HttpHandler_Login : HttpRequestHandler
    {
        protected override string PostHandle(HttpListenerRequest request, HttpListenerResponse response)
        {
            string returnString = null;
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

            var user_name = jobj["user_name"].ToString();
            var password = jobj["password"].ToString();

            MySqlConnection conn;
            string restr;
            var reader = SqlWorker.MySqlQuery("beach_smart_lock", "user_data", new string[] { "password" }, "user_name", user_name, out conn, out restr);
            if(reader.Read())
            {
                if(reader.GetString(0) == password)
                {
                    var guid = Guid.NewGuid().ToString();
                    SmartLockTcpHandlerManager.Instance.AccountKeys.Add(user_name, guid);
                    returnString = JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "success", guid, "200" }).jstr;
                }
                else
                {
                    returnString = JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "fail", "password wrong", "200" }).jstr;
                }
            }
            else
            {
                returnString = JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "fail", "cannot find exist account", "200" }).jstr;
            }
            reader.Close();
            conn.Close();




            return returnString;
        }

        protected override bool OnCreate()
        {
            return base.OnCreate();
        }
    }
}