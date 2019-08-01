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
                Debuger.PrintStr("Init StaticObjects faild!", EPRINT_TYPE.ERROR);
                Debuger.ExitProgram();
                return;
            }
            if (SqlWorker.MySqlInit())
            {
                Debuger.PrintStr("Init SqlWorker done!", EPRINT_TYPE.NORMAL);
            }
            else
            {
                //Debuger.PrintStr("Init SqlWorker faild!", EPRINT_TYPE.ERROR);
                //Debuger.ExitProgram();
                //return;
            }
            if (!HttpListenerManager.Instance.Init())
            {
                Debuger.PrintStr("Init HttpListenerManager faild!", EPRINT_TYPE.ERROR);
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
            HttpRequestHandler.CreateHttpRequestHandler<HttpHandler_ReceiveCommitMsg>("app_commit");
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
