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
    class HttpHandler_BindVotas : HttpRequestHandler
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
            string votas_id = null;
            string user_name = null;
            string account_key = null;
            try
            {
                votas_id = jobj["lock_id"].ToString();
                user_name = jobj["user_name"].ToString();
                account_key = jobj["key"].ToString();
            }
            catch (Exception ex)
            {
                return JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "error", ex.Message, "200" }).jstr;

            }

            if (SmartLockTcpHandlerManager.Instance.AccountKeys.ContainsKey(user_name))
            {
                if (SmartLockTcpHandlerManager.Instance.AccountKeys[user_name] == account_key)
                {
                    if (SqlWorker.MySqlIsExist("beach_smart_lock", "lock_data", "lock_id", votas_id))
                    {
                        MySqlConnection conn;
                        string restr;
                        var reader = SqlWorker.MySqlQuery("beach_smart_lock", "lock_data", new string[] { "accounts" }, "lock_id", votas_id, out conn, out restr);
                        reader.Read();
                        var tmpAccounts = reader.GetString(0);
                        reader.Close();
                        conn.Close();
                        var accounts = tmpAccounts.Split(',');
                        List<string> account_ls = new List<string>();
                        account_ls.AddRange(accounts);
                        account_ls.Add(user_name);
                        accounts = account_ls.ToArray();
                        tmpAccounts = string.Join(',', accounts);
                        SqlWorker.MySqlEdit("beach_smart_lock", "lock_data", "lock_id", votas_id, new string[] { "accounts" }, new string[] { tmpAccounts });
                        return JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "success", "done", "200" }).jstr;
                    }
                    else
                    {
                        return JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "error", "your votas lock has not registered yet", "200" }).jstr;
                    }
                }
                else
                {
                    return JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "error", "dynamic key wrong", "200" }).jstr;
                }
            }
            else
            {
                return JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "error", "user has not login yet", "200" }).jstr;
            }
        }

        protected override bool OnCreate()
        {
            return base.OnCreate();
        }
    }
}
