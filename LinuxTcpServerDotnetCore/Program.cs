using LinuxTcpServerDotnetCore.Http;
using LinuxTcpServerDotnetCore.Http.HttpHandler;
using LinuxTcpServerDotnetCore.SmartLock;
using LinuxTcpServerDotnetCore.Statics;
using Sql;
using System;

namespace LinuxTcpServerDotnetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            LinuxTcpManager.CreateInstance();


            if (StaticObjects.InitObjects())
            {
                Debuger.PrintStr("Init StaticObjects done!", EPRINT_TYPE.NORMAL);
            }
            else
            {
                Debuger.PrintStr("Init StaticObjects failed!", EPRINT_TYPE.ERROR);
                Debuger.ExitProgram();
                return;
            }
            if (SqlWorker.MySqlInit())
            {
                Debuger.PrintStr("Init SqlWorker done!", EPRINT_TYPE.NORMAL);
            }
            else
            {
                //Debuger.PrintStr("Init SqlWorker failed!", EPRINT_TYPE.ERROR);
                //Debuger.ExitProgram();
                //return;
            }
            if (!HttpListenerManager.Instance.Init())
            {
                Debuger.PrintStr("Init HttpListenerManager failed!", EPRINT_TYPE.ERROR);
                Debuger.ExitProgram();
                return;
            }
            else
            {
                Debuger.PrintStr("Init HttpListenerManager done!", EPRINT_TYPE.NORMAL);
            }
            Debuger.PrintStr($"Waiting for client request,time:{DateTime.Now.ToString()}", EPRINT_TYPE.NORMAL);
            if (StaticObjects.IsForceGC)
            {
                Debuger.StartForceGC(StaticObjects.ForceGCInterval);
            }

            //create all http request handlers
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_BindVotas>("bind_votas");
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_EditDevice>("edit_device");
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_EditFactor>("edit_factor");
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_Login>("login");
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_Register>("register");
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_SetAccountPassword>("reset_account_password");
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_SetVoiceKeyword>("set_voice_keyword");
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_SetVotasPassword>("set_votas_password");
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_Logout>("HttpHandler_Logout");
            SmartLockTcpHandlerManager.CreateInstance();

            while (true)
            {
                switch (Debuger.InputCommand())
                {
                    case ECOMMAND_TYPE.EXIT:
                        return;
                        break;
                    case ECOMMAND_TYPE.NORMAL:
                        break;
                }

            }
        }
    }
}
