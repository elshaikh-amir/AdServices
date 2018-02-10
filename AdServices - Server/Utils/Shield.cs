using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdServices___Server.Networking
{
    class Shield
    {
        private volatile object _lock;
        private List<Pair<Int32, Warning>> Addresses;
        private int filenumber;

        public Shield()
        {
            _lock = new object();
            Addresses = new List<Pair<Int32, Warning>>();
        }

        private void setMaxFileNum()
        {
            foreach (string filename in Directory.GetFiles(Globals.LogShieldPath, "*.txt"))
                filenumber = Math.Max(filenumber, int.Parse(filename.Substring(filename.LastIndexOf('\\') + 1).Split(new char[] { '.' })[0]));
        }

        public void Load_Log()
        {
            setMaxFileNum();

            if(filenumber > 0)
                LoadCache(filenumber);
        }

        private void LoadCache(int filenumber)
        {
            string[] log = System.IO.File.ReadAllLines(Globals.LogShieldPath+filenumber+".txt");
            string[] row = null;
            char[] sp = new char[] { '=' };
            char[] comma = new char[] { ',' };
            foreach (string s in log)
            {
                if (s == null || s.Length < 5)
                    continue;
                row = s.Split(comma);
                new Warning(int.Parse(row[0].Split(sp)[1]),
                            DateTime.Parse(row[1].Split(sp)[1], CultureInfo.CurrentCulture),
                            short.Parse(row[2].Split(sp)[1]),
                            short.Parse(row[3].Split(sp)[1]));
            }
            
            Utils.Log.Write("Reloaded Server Shield Cache. Items Loaded: {0} from file: {1}.txt", log.Length, filenumber);
        }

        public void AddClientWatch(Int32 c, Warning client)
        {
            lock (_lock)
                Addresses.Add(new Pair<Int32, Warning>(c, client));
        }

        public void Block(Int32 c, Warning client)
        {
            Pair<Int32, Warning> obj = ObjOfIndex(c);
            if (obj == null)
                lock (_lock)
                    Addresses.Add(new Pair<Int32, Warning>(c, client));// != null ? client : null)); // fix bug, object locked & null client
            Warning.AddWarningToClient(client);
        }

        private Pair<Int32, Warning> ObjOfIndex(Int32 c)
        {
            if (c == 0)
                return null;
            lock (_lock)
                return Addresses.Find(a => a != null && a.First.Equals(c));
        }

        public Warning Reference(Int32 IP)
        {
            Pair<Int32, Warning> obj = ObjOfIndex(IP);
            lock(_lock)
                return obj == null ? null : obj.Second;
        }

        private Pair<Int32, Warning> ObjOfWarning(Warning client)
        {
            if (client == null)
                return null;

            lock (_lock)
                return Addresses.Find(a => a != null && a.Second != null && a.Second.Equals(client));
        }

        public void UnBlock(Int32 c)
        {
            Pair<Int32, Warning> obj = ObjOfIndex(c);
            UnBlock(obj);
        }

        public void UnBlock(Warning client)
        {
            Pair<Int32, Warning> obj = ObjOfWarning(client);
            UnBlock(obj);
        }

        private void UnBlock(Pair<Int32, Warning> obj)
        {
            lock (_lock)
                if (obj != null && obj.Second != null)
                    Warning.Reset(obj.Second);
        }

        public bool IsBlocked(Int32 c)
        {
            Pair<Int32, Warning> obj = ObjOfIndex(c);
            lock (_lock)
                if (obj != null && obj.Second != null) // here we must lock first.
                    return Warning.GetLevel(obj.Second) >= 5;
            return false;
        }

        public bool IsBlocked(System.Net.Sockets.TcpClient client)
        {
            if(client !=null)
                try
                {
                    return IsBlocked(Utils.Tools.ipToInt(client.Client.RemoteEndPoint.ToString().Split(new char[] { ':' })[0]));
                }
                catch (System.Net.Sockets.SocketException e)
                {
                }
                catch (ObjectDisposedException e1)
                {
                }
                catch
                {
                }
            return false;
        }

        public bool IsBlocked(Warning client)
        {
            Pair<Int32, Warning> obj = ObjOfWarning(client);
            lock (_lock)
                if (obj != null)
                    return Warning.GetLevel(obj.Second) >= 5;
            return false;
        }

        public void LogShield()
        {
            StringBuilder str = new StringBuilder();
            lock (_lock)
                Addresses.ForEach(a => {
                                        str.Append("IP_INT=").Append(a.First).Append(",")
                                           .Append("LAST_TIME=").Append(Warning.GetTime(a.Second)).Append(",")
                                           .Append("WARN_LVL=").Append(Warning.GetLevel(a.Second)).Append(",")
                                           .Append("WARN_KEYS=").Append(Warning.GetKeyErrors(a.Second)).Append("\n");
                });
            System.IO.File.WriteAllText(Globals.LogShieldPath+(++filenumber)+".txt", str.ToString());
        }
    }

    public class Pair<T1, T2>
    {
        public T1 First; // IP int
        public T2 Second; // Warning obj

        public Pair(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }
}
