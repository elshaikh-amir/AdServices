using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AdServices___Server.Networking
{
    class NetworkHeartBeat
    {
        private Timer timeer;
        private List<Client> dead;

        public NetworkHeartBeat(int delay)
        {
            timeer = new Timer(delay);
            dead = new List<Client>();
            timeer.Elapsed += new System.Timers.ElapsedEventHandler(HeartBeat);
        }

        public void setDelay(int d)
        {
            Utils.Log.SYS("Heart Beat-Freq: {0} per Min. => Changed to: {1} per Min.", Math.Round(60000 / timeer.Interval, 2) + "x", Math.Round(60000 / (double)d, 2) + "x");
            timeer.Interval = d;
            Globals.maxDelayBeat = 3 * d;
        }

        public void Beat()
        {
            Utils.Log.Write("Heart is Beating with Freq: {0} per Min.", Math.Round(60000 / timeer.Interval, 2)+"x");
            timeer.Start();
        }

        public void Kill()
        {
            Utils.Log.Write("Heart is Killed, he is dead Jim!");
            timeer.Stop();
        }

        private void HeartBeat(object sender, EventArgs e)
        {
            int connected = Globals.Clients.Count;
            
            if (connected > 0)
            {
                Globals.Clients.ForEach(ApplyBeat);
                //Globals.Clients.ForEach(c => { if (c != null && dead.Contains(c)) c.ForceKill(); }); // DeadLock here -.-

                if (dead.Count > 0)
                    dead.ForEach(Globals.NetworkServer.Kill);
                dead.Clear();

                Utils.Log.Write("Heart has Beat!, {0} Clients Online", Globals.Clients.Count);
            }
        }

        private void ApplyBeat(Client c)
        {
            if (c == null || !c.tcpclient.Connected)
                return;

            if (DateTime.UtcNow.Subtract(c.lastheartbeat).TotalMilliseconds >= Globals.maxDelayBeat)
                dead.Add(c);
            else
                Globals.NetworkServer.SendCmd(c.tcpclient, CmdPacket.HeartBeat);
        }
    }
}

