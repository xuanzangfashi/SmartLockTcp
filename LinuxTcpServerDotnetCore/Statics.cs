using System;
using System.Xml;
using System.Timers;

namespace LinuxTcpServerDotnetCore.Statics
{
    public static class StaticObjects
    {
        private static string ServerConfigXmlPath = "./ServerConfig.xml";
        public static string SqlUrl { get; private set; }// = "server=192.168.50.53;port=3306;user=root;password=wuxiaohan; database=runInteractive;";
        public static string HttpContentDir { get; private set; }// = "D:/HttpContent/apache/Apache24/htdocs/RUN_Interactive/";
        public static string HttpContentUrl { get; private set; }// = "http://192.168.50.53:5757/";
        public static string HttpServerIp { get; private set; }// = "http://192.168.50.53:5656/";
        public static bool IsForceGC { get; private set; }// = false;
        public static int ForceGCInterval { get; private set; } //= 100;
        public static long StreamSizeLimit { get; private set; }
        public static long SingleMemoryBarSize { get; private set; }

        public static bool InitObjects()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ServerConfigXmlPath);
                var rootNode = xmlDoc.FirstChild.NextSibling;
                var sqlUrlElement = rootNode.SelectNodes("SqlUrl")[0] as XmlElement;
                SqlUrl = sqlUrlElement.InnerText;
                var httpContentDirElement = rootNode.SelectNodes("HttpContentDir")[0] as XmlElement;
                HttpContentDir = httpContentDirElement.InnerText;
                var httpContentUrlElement = rootNode.SelectNodes("HttpContentUrl")[0] as XmlElement;
                HttpContentUrl = httpContentUrlElement.InnerText;
                var HttpServerIpElement = rootNode.SelectNodes("HttpServerIp")[0] as XmlElement;
                HttpServerIp = HttpServerIpElement.InnerText;
                var ForceGCElement = rootNode.SelectNodes("ForceGC")[0] as XmlElement;
                IsForceGC = bool.Parse(ForceGCElement.InnerText);
                var ForceGCIntervalElement = rootNode.SelectNodes("ForceGCInterval")[0] as XmlElement;
                ForceGCInterval = int.Parse(ForceGCIntervalElement.InnerText);
                var StreamSizeLimitElement = rootNode.SelectNodes("StreamSizeLimit")[0] as XmlElement;
                StreamSizeLimit = long.Parse(StreamSizeLimitElement.InnerText);
                var SingleMemoryBarSizeElement = rootNode.SelectNodes("SingleMemoryBarSize")[0] as XmlElement;
                SingleMemoryBarSize = long.Parse(SingleMemoryBarSizeElement.InnerText);
                xmlDoc = null;
                return true;
            }
            catch (Exception ex)
            {
                Debuger.PrintStr(ex.Message, EPRINT_TYPE.ERROR);
                SqlUrl = HttpContentDir = HttpContentUrl = HttpServerIp = null;
                return false;
            }
        }
    }

    public enum EPRINT_TYPE
    {
        ERROR, NORMAL, WARNING,
    }

    public enum ECOMMAND_TYPE
    {
        EXIT, NORMAL,
    }

    public static class Debuger
    {
        private static Timer ForceGCTimer = null;
        public static int ForceGCInterval { get { return ForceGCInterval; } set { ForceGCTimer.Interval = value; } }
        public static void PrintStr(string str, EPRINT_TYPE type, bool typePrefix = true)
        {

            switch (type)
            {
                case EPRINT_TYPE.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case EPRINT_TYPE.NORMAL:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case EPRINT_TYPE.WARNING:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
            }
            if (typePrefix)
            {
                Console.WriteLine(type.ToString() + ": " + str);
            }
            else
            {
                Console.WriteLine(str);
            }
            Console.ForegroundColor = ConsoleColor.Green;
        }

        public static void ExitProgram()
        {
            Console.ReadKey();
            return;
        }

        public static ECOMMAND_TYPE InputCommand()
        {
            string command = Console.ReadLine();
            if (command == "exit")
            {
                return ECOMMAND_TYPE.EXIT;
            }
            return ECOMMAND_TYPE.NORMAL;
        }

        public static void StartForceGC(int ms = 100)
        {
            if (ForceGCTimer == null)
            {
                ForceGCTimer = new Timer(ms);
                ForceGCTimer.Elapsed += new ElapsedEventHandler(ForceGC);
                ForceGCTimer.Start();
            }
            else
            {
                ForceGCTimer.Stop();
                ForceGCTimer.Interval = ms;
                ForceGCTimer.Start();
            }
        }

        private static void ForceGC(object obj, ElapsedEventArgs e)
        {
            GC.Collect(0, GCCollectionMode.Forced);
            PrintStr("Force GC Collection!", EPRINT_TYPE.WARNING);
        }
    }
}
