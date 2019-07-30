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
            SmartLockTcpHandlerManager.CreateInstance();
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
