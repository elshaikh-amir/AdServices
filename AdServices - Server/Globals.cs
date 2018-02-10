using AdServices___Server.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdServices___Server.Ad_System;

namespace AdServices___Server
{
    class Globals
    {
        public volatile static Thread NetworkServerThread;
        public volatile static Thread ADManagerThread;
        public volatile static NetworkServer NetworkServer;
        public volatile static ClientList Clients;
        public volatile static Database DB;
        public volatile static Shield Shield;
        public volatile static AdManager AdManager;

        public volatile static int DelayBrowse = 15000;
        public volatile static int ServerPort = 50;
        public volatile static string ServerIP = "127.0.0.1";//"25.178.71.164";
        public volatile static int HeatBeat = 3000;
        public volatile static int maxDelayBeat = 2 * HeatBeat;
        public volatile static string LogShieldPath = "Log\\Shield\\";
        public static double MaxOneWayRoundTime = 0.500d;
        public volatile static int MaxClients = 5760; // 60*60*24/15 = 4*60*24. 4 Links/min. 5760 Links/day.


        public static void Initialize()
        {
            Clients = new ClientList();
            NetworkServerThread = null;
            NetworkServer = null;
            DB = new Database();
            Shield = new Shield();
            Shield.Load_Log();
            AdManager = new AdManager();
        }

        public static void Load_Settings()
        {

        }
    }
}
