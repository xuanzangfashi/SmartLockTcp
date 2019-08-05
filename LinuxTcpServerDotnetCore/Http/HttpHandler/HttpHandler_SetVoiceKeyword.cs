﻿using Json;
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
    class HttpHandler_SetVoiceKeyword : HttpRequestHandler
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

            var votas_id = jobj["lock_id"].ToString();
            var user_name = jobj["user_name"].ToString();
            var account_key = jobj["key"].ToString();

            if (SmartLockTcpHandlerManager.Instance.AccountKeys.ContainsKey(user_name))
            {
                if (SmartLockTcpHandlerManager.Instance.AccountKeys[user_name] == account_key)
                {
                    if (SqlWorker.MySqlIsExist("beach_smart_lock", "lock_data", "lock_id", votas_id))
                    {
                        if (SmartLockTcpHandlerManager.Instance.SmartLockMap.ContainsKey(votas_id))
                        {
                            SmartLockTcpHandlerManager.Instance.SmartLockMap[votas_id].sl.Sender.WriteSendData(json_data);
                            return JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "success", "done", "200" }).jstr;
                        }
                        else
                        {
                            return JsonWorker.MakeSampleJson(new string[] { "type", "result", "code" }, new string[] { "error", "your votas lock has not connected yet", "200" }).jstr;
                        }
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
    }
}
