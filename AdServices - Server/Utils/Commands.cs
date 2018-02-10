using AdServices___Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdServices___Server.Networking
{
    public enum CmdPacket : byte
    {
        Login = 1,
        Disconnect = 2,
        DisconnectTooManyIPs = 3,
        DisconnectIsBanned = 4,
        LoginUserOrPasswordIncorrect = 5,
        LoginFailed = 6,
        ServerRestart = 7,
        AdLink = 8,
        HeartBeat = 9,
        adlinkFailed = 10,
        ServerReini = 11
    }

    public class Command
    {
        private static Action<string> BanIP;
        private static Action<string> BanEmail;
        private static Action<string> BlockIP;
        private static Action<string> UnBlockIP;
       
        private static Action<string> lookupEmail;
        private static Action<string> LookupWarnlvlIP;
        private static Action<string> LookupLastWarnDate;

        private static Action<string> DisconnectEmail;

        private static Action<string> ReloadShieldFromLog;
        private static Action<string> ResertShield;

        private static Action<string> ResetKeyErrorIP;
        private static Action<string> ResetWarnLevel;
        private static Action<string> ResetLastWarnDate;


        // ------------------ Commands working --------------------
        private static Action StartServer = () => NetworkServer.Initialize();
        private static Action HeartBeat = () => Globals.NetworkServer.HeartBeat();
        private static Action<int> SetHeatDelay = Globals.NetworkServer.SetHeartDelay;
        private static Action HeartKiller = () => Globals.NetworkServer.HeartKill();
        private static Func<string, bool> isValidIP = s => (s != null && s.Contains(".") && s.Split(new char[] { '.' }).Length == 4);
        private static Func<string, Warning> GetWarning = parm => isValidIP(parm) ? Globals.Shield.Reference(Tools.ipToInt(parm)) : null;
        private static Action<string> Log = p => Utils.Log.TurnLog(p == null ? false: p.Equals("on"));
        private static Action Shutdown = () => Globals.NetworkServer.ShutDown();
        private static Action Restart = () => Globals.NetworkServer.RestartServer();
        private static Action Exit = () => { Globals.Shield.LogShield(); System.Environment.Exit(0); };
        private static Action Clear = () => Console.Clear();
        private static Action<string> DisconnectIP = Globals.NetworkServer.DisconnectIP;
        private static void printLastPingOneWay(string ip)
        {
            if (isValidIP(ip))
            {
                Client c = Globals.NetworkServer.getClientbyIP(ip);
                if (c == null)
                    Utils.Log.SYS("Client with IP: {0} Not Found!", ip);
                else if (c.lastPingOneWay == null)
                    Utils.Log.SYS("Client with IP: {0} Still has No Ping Records, please wait for HeartBeat", c.ipport);
                else
                    Utils.Log.SYS("OneWayPing (Milliseconds): {0} Of Client: {1}", c.lastPingOneWay, c.ipport);
            }
            else
                Utils.Log.SYS("Invalid Parameter: {0}, Command Execution failed", ip==null?"NULL":ip);
        }

        private static Action Help = () =>
        {
            for (int i = 0; i < Commandos.Count; i++)
                Utils.Log.SYS("ID: {0}, Name: {1}, Parm: {2}", i, Commandos[i].First, Commandos[i].First.Contains("=") ? "<Parameter>" : "No Parameter");
        };

        private static void ListOnClients()
        {
            if (Globals.Clients.Count < 1)
            {
                Utils.Log.SYS("No Clients Online!");
                return;
            }
            Globals.Clients.ForEach(c => Utils.Log.SYS("IP: {0}, LastTime: {1}, User: {2}, WarningLevel: {3}, KeyLevel: {4}, PingOneWay: {5} Milli. Seconds", c.ipport, c.lastheartbeat, c.userid, Warning.GetLevel(c.warning), Warning.GetKeyErrors(c.warning), c.lastPingOneWay));
        }

        private static Action<string> LookupKeyErrorsIP = ip =>
        {
            if (!isValidIP(ip))
            {
                Utils.Log.SYS("Command Failed, Wrong Parameter: {0}", ip);
                return;
            }
            Warning w = GetWarning(ip);
            if (w == null)
                Utils.Log.SYS("IP: {0} Warning Object not Found!, Command failed!", ip);
            else
                Utils.Log.SYS("IP: {0} - has Keyerrors: {1}", ip, Warning.GetKeyErrors(w));
        };

        private static Action<string> lookupIP = parm =>
        {
            if (isValidIP(parm)) //127.0.0.1
            {
                if (Globals.Shield.Reference(Tools.ipToInt(parm)) == null)
                    Utils.Log.SYS("LookupIP Command Failed, Client: {0} is NOT Connected!", parm);
                else
                    Utils.Log.SYS("LookupIP Command Executed Successfully, Client: {0} is Connected!", parm);
            }
            else
                Utils.Log.SYS("LookupIP Command Failed, Wrong param: {0}", parm);
        };
        // ---------------------- Command List here -------------------------------------------------------//
        private static List<Pair<string, Action<string>>> Commandos = new List<Pair<string,Action<string>>>( new Pair<string, Action<string>>[] 
        
        { new Pair<string, Action<string>>("ban.ip", BanIP), 
          new Pair<string, Action<string>>("ban.email=", BanEmail),
          new Pair<string, Action<string>>("block.ip=", BlockIP),
          new Pair<string, Action<string>>("unblock.ip=", UnBlockIP),
          new Pair<string, Action<string>>("lookup.email=", lookupEmail),
          new Pair<string, Action<string>>("lookup.ip=", lookupIP),
          new Pair<string, Action<string>>("lookup.warninglevel.ip=", LookupWarnlvlIP),
          new Pair<string, Action<string>>("lookup.lastwarndate.ip=", LookupLastWarnDate),
          new Pair<string, Action<string>>("lookup.keyerrors.ip=", LookupKeyErrorsIP),
          new Pair<string, Action<string>>("dc.ip=", DisconnectIP),
          new Pair<string, Action<string>>("dc.email=", DisconnectEmail),
          new Pair<string, Action<string>>("shield.reloadlog", ReloadShieldFromLog),
          new Pair<string, Action<string>>("shield.reset", ResertShield),
          new Pair<string, Action<string>>("resetkeyerror.ip=", ResetKeyErrorIP),
          new Pair<string, Action<string>>("resetwarninglevel.ip=", ResetWarnLevel),
          new Pair<string, Action<string>>("resetlastwarningdate.ip=", ResetLastWarnDate),
          new Pair<string, Action<string>>("heart.beat", _ => HeartBeat.Invoke()),
          new Pair<string, Action<string>>("heart.kill", _ => HeartKiller.Invoke()),
          new Pair<string, Action<string>>("heart.delay=", i => {int o; if(!int.TryParse(i, out o)) Utils.Log.SYS("Invalid Parameter for Heart.delay Execution"); else SetHeatDelay.Invoke(o);}),
          new Pair<string, Action<string>>("help", _ => Help.Invoke()),
          new Pair<string, Action<string>>("server.shutdown", _ => Shutdown.Invoke()),
          new Pair<string, Action<string>>("server.start", _ => StartServer.Invoke()),
          new Pair<string, Action<string>>("server.restart", _ => Restart.Invoke()),
          new Pair<string, Action<string>>("exit", _ => Exit.Invoke()),
          new Pair<string, Action<string>>("clear", _ => Clear.Invoke()),
          new Pair<string, Action<string>>("log", Log),
          new Pair<string, Action<string>>("list.clients", _ => ListOnClients()),
          new Pair<string, Action<string>>("show.onewayping=", printLastPingOneWay),
          new Pair<string, Action<string>>("reload.settings", _ => Globals.Load_Settings()) // lift to local method
        }

        );
        // ------------------------------ End Command list here -----------------------------------//
        public static void ApplyCommand(string c, string parm)
        {
            Pair<string, Action<string>> cmd;
            int intcmd;
            if (int.TryParse(c, out intcmd))
                cmd = Commandos.Count <= intcmd || intcmd < 0 ? null : Commandos[intcmd];
            else
                cmd = Commandos.Find(p => { if (p.First.Contains("=")) return p.First.Substring(0, p.First.IndexOf("=")).Equals(c); else return p.First.Equals(c); });

            if (cmd == null)
                Utils.Log.SYS("Command Execution Failed, Command Not Found!");
            else
                cmd.Second.Invoke(parm);
        }
    }
}
