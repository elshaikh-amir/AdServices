using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdServices___Server.Networking
{
    public class Warning
    {
        private DateTime Date;
        private Int16 Level;
        private Int16 keyerrors;

        public Warning(int ip)
        {
            Date = DateTime.Now;
            Level = 0;
            Globals.Shield.AddClientWatch(ip, this);
        }

        public Warning(int ip, DateTime date, short lvl, short keylvl)
        {
            Date = date;
            Level = lvl;
            keyerrors = keylvl;
            Globals.Shield.AddClientWatch(ip, this);
        }

        public static Int16 GetKeyErrors(Warning client)
        {
            return IsReal(client) ? client.keyerrors : (short)0;
        }

        public static void AddWarningToClient(Warning client, bool keyerror = false)
        {
            if (IsReal(client))
            {
                client.Level++;
                client.Date = DateTime.Now;
                if (keyerror)
                    client.keyerrors++;
            }
        }

        public static Int16 GetLevel(Warning client)
        {
            return IsReal(client) ? client.Level : (short)0;
        }

        public static DateTime GetTime(Warning client)
        {
            return IsReal(client) ? client.Date : DateTime.MinValue;
        }

        public static void Reset(Warning client)
        {
            if (IsReal(client))
            {
                client.Level = 0;
                client.keyerrors = 0;
                client.Date = DateTime.MinValue;
            }
        }

        private static bool IsReal(Warning client)
        {
            return client != null;// && Globals.Warnings.IndexOf(client) >= 0;
        }

        public static void Terminate(Warning client)
        {
            if (IsReal(client))
            {
                Globals.Shield.UnBlock(client);
                client = null;//Globals.Warnings.Remove(client);
            }
        }
    }
}
