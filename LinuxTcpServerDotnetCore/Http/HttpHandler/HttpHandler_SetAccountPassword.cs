using Json;
using LinuxTcpServerDotnetCore.SmartLock;
using LinuxTcpServerDotnetCore.Statics;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Sql;
using System;
using System.Collections.Generic;
using System.Net;

namespace LinuxTcpServerDotnetCore.Http.HttpHandler
{
    class HttpHandler_SetAccountPassword : HttpRequestHandler
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
            var new_password = jobj["new_password"].ToString();

            if(SqlWorker.MySqlIsExist("beach_smart_lock","user_data","user_name",user_name))
            {
                SqlWorker.MySqlEdit("beach_smart_lock", "user_data", "user_name", user_name, new string[] { "password" }, new string[] { new_password });
                return JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "success", "done", "200" }).jstr;

            }
            else
            {
                return JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "error", "account does not exist", "200" }).jstr;
            }
        }
    }
}
