using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AdServices___Server.Utils;
using System.Threading;
using System.Globalization;
using AdServices___Server.Ad_System;

namespace AdServices___Server.Networking
{
    class ClientList // need to ref. AdLink, Solution: Pair Obj of <Client, AdLink>
    {
        private volatile object olock;
        private List<Client> Clients;

        public ClientList()
        {
            olock = new object();
            Clients = new List<Client>();
        }

        public void Add(Client c)
        {
            lock (olock)
                Clients.Add(c);
        }

        public void Remove(Client c)
        {
            lock (olock)
                Clients.Remove(c);
        }


        public void ForEach(Action<Client> f)
        {
            lock (olock)
                Clients.ForEach(f);
        }

        public Client Find(Predicate<Client> f)
        {
            lock (olock)
                return Clients.Find(f);
        }

        public int Count
        {
            get 
            {
                lock (olock)
                    return Clients.Count;
            }
        }
    }

    class Client
    {
        public string ipport { get; private set; }
        public string ip { get; private set; }
        public EndPoint endpoint { get; private set; }
        public TcpClient tcpclient { get; private set; }
        public AdLink adlink { get; private set; }
        private byte[] buff;
        private Thread mythread;
        private int size;
        public string userid { get; private set; }
        public DateTime lastheartbeat { get; private set; }
        public Warning warning { get; private set; }
        private const int buffsize = 4096;
        public string lastPingOneWay { get; private set; }

        public Client(TcpClient client)
        {
            if (client == null)
            {
                return;
            }

            this.tcpclient = client;
            try
            {
                endpoint = tcpclient.Client.RemoteEndPoint;
                ipport = endpoint.ToString();
                ip = ipport.Split(new char[] { ':' })[0];
            }
            catch (SocketException e)
            {
                Disconnect("Client Error, Disconnecting client: {0}", e.SocketErrorCode.ToString());
                return;
            }
            catch (ObjectDisposedException e1)
            {
                Disconnect("Client Error, Disconnecting client: {0}", e1.ToString());
                return;
            }
            catch
            {
                Disconnect("Client Error, Disconnecting client");
                return;
            }
            mythread = new Thread(new ThreadStart(ProcessStream));
            mythread.Start();
        }

        private void iniBuff()
        {
            if (buff != null)
            {
                for (int i = 0; i < size; i++)
                    buff[i] = 0;
                size = 0;
            }
            else
                buff = new byte[buffsize];
        }

        private void setBuffSize()
        {
            for (size = buffsize - 1; size >= 0; size--)
                if (buff[size] != 0)
                    break;
            if(size > 0)
                size++;
            /*
                for (size = 0; size < buff.Length; size++)
                    if (buff[size] == 0 && size + 1 < buff.Length && buff[size + 1] == 0)
                        return;

            if (size < 0)
                size = buffsize;
             */
        }

        private void processWarning()
        {
            int ipint = Tools.ipToInt(ip);
            warning = Globals.Shield.Reference(ipint);
            if (warning == null)
                warning = new Warning(ipint);
        }

        private void readClientStream()
        {
            iniBuff();
            try
            {
                tcpclient.GetStream().Read(buff, 0, buff.Length);
                setBuffSize();
            }
            catch(Exception)
            {
                size = 0;
                //Disconnect("Client Read Error");
                //return;
            }   
        }

        private void HandleCommand() // fix required here!
        {
            switch ((CmdPacket)buff[0])
            {
                case CmdPacket.AdLink:
                    if(Array.LastIndexOf<byte>(buff, (byte)CmdPacket.HeartBeat) > 0)
                    {
                        HandleHeartBeat();
                        break;
                    }
                    Log.Write("AdLink-Cmd recieved from {0}", ipport);
                    string t = getAdLinkFromBuffData();
                    if (t == null)
                        Globals.NetworkServer.SendCmd(tcpclient, CmdPacket.adlinkFailed);
                    else
                    {
                        Globals.AdManager.Update(adlink, t);
                       // adlink = t; 
                    }
                    break;
                    // more commands here
                default:
                    HandleHeartBeat();
                    break;
            }
        }

        private void HandleHeartBeat()
        {
            int indexOfPacketByte = Array.LastIndexOf<byte>(buff, (byte)CmdPacket.HeartBeat);
            if(indexOfPacketByte < 10) // This is a limited check for some.. years or time, needs to be specific
            {
                if(indexOfPacketByte < 0)
                    Disconnect("Spoof Packet recieved from: {0}", ipport);
                else
                    Disconnect("Spoof Packet recieved, Beat found at: {0}", indexOfPacketByte);
            }
            else
            {
                DateTime now = DateTime.UtcNow;
                byte[] sync = SyncClock(indexOfPacketByte);
                if(sync == null)
                {
                    Disconnect("Fake SyncClock from: {0}", ipport);
                    return;
                }

                DateTime PacketSyncData = new DateTime(BitConverter.ToInt64(sync, 0));
                //Log.Write("OneWayPing (Milliseconds): {0} Of Client: {1}", now.Subtract(testme).Milliseconds.ToString(), ip);

                if (PacketSyncData.AddSeconds(Globals.MaxOneWayRoundTime) <= now)
                {
                    Warning.AddWarningToClient(warning);
                    string msg = "Client Lagging or Faking SyncClock: {0}, Now: {1}, Client: {2}";
                    if(Warning.GetLevel(warning) > 3)
                        Disconnect(msg, PacketSyncData.ToString(), now.ToString(), ipport);
                    else
                        Log.Write(msg, PacketSyncData.ToString(), now.ToString(), ipport);
                }
                else
                {
                    lastPingOneWay = (now - PacketSyncData).Milliseconds.ToString();
                    lastheartbeat = DateTime.UtcNow;
                }   
            }
        }

        private void ProcessStream()
        {
            try
            {
                if (!Login())
                {
                    Disconnect("Failed Login, Stream Processor Terminating..");
                    return;
                }
                processWarning();

                Globals.Clients.Add(this);
                Log.Write("New Client Loggedin with IP: {0}, User: {1}, AdLink: {2}", ipport, userid, adlink);
                lastheartbeat = DateTime.UtcNow;

                while (mythread.IsAlive)
                {
                    readClientStream(); // blocks till stream is read

                    if (size == 0)
                        Disconnect(); // normal client close,"Buff size = 0 after read - Client read error"
                    else
                        HandleCommand();
                }
            }
            catch (ThreadAbortException)
            {
                if (tcpclient.Client.IsBound)
                    Disconnect("Disconnected Client: {0}", ipport);
            }
        }

        private bool Login()
        {
            Globals.AdManager.AddToNext(new AdLink("http://MYLINKYOLO.COM"));
            return true;
            // stop watch needed here or not?
            readClientStream();
            if (!IsValidCmdID() || (CmdPacket)buff[0] != CmdPacket.Login)
                return false;

            string[] userdata = GetUserPassFromBuffData();
            if (userdata == null || userdata.Length <= 1)
                return false;
            
            foreach (string s in userdata)
                if (s == null || s.Length <= 1)
                    return false;

            if (!Globals.DB.LoginUserPass(userdata[0], userdata[1]))
                return false;

            this.userid = userdata[0];
            string link = Globals.DB.getAdLink(userdata[0]);
            if (link != null && link != "")
                //adlink = new AdLink(link);
                Globals.AdManager.AddToNext(adlink = new AdLink(link));
            return true;
        }

        private string getAdLinkFromBuffData()
        {
            string buffdata = readBuffData();
            if (buffdata == null || !buffdata.Contains('$'))
                return null;

            string[] splited = buffdata.Split(new char[] { '$' });
            if (splited.Length != 2)
                return null;

            return splited[0];
            //return "http://yahoo.de";
        }

        private string[] GetUserPassFromBuffData()
        {
            string buffdata = readBuffData();
            if (buffdata == null || !buffdata.Contains('$'))
                return null;

            string[] splited = buffdata.Split(new char[] { '$' });
            if (splited.Length != 2)
                return null;

            return splited;
        }

        private string readBuffData()
        {
            if (size <= 1)
                return null;
            try
            {
                return System.Text.Encoding.ASCII.GetString(readBuffByter());
            }
            catch
            {
                return null;
            }
        }

        public byte[] readFullBuffbyter()
        {
            if (size <= 1)
                return null;
            return read(0, size);
        }

        public byte[] readBuffByter()
        {
            if (size <= 1)
                return null;
            return read(1, size - 1); //  - 1
        }

        private byte[] read(int start, int end)
        {
            if (end > size)
                return null;
            byte[] _buff = new byte[end - start];
            Buffer.BlockCopy(buff, start, _buff, 0, _buff.Length);
            return _buff;
        }

        public void Disconnect(string reason, params object[] objs)
        {
            Log.Write(reason, objs);
            Disconnect();
        }

        public void Disconnect()
        {
            Globals.Clients.Remove(this);
            if(adlink != null)
                Globals.AdManager.MarkAsRemoved(adlink); // fixed

            if (tcpclient != null && tcpclient.Client.IsBound) // can never be null, but for feature
            {
                Globals.NetworkServer.SendCmd(tcpclient, CmdPacket.Disconnect);
                tcpclient.Close();
            }
            ForceKill();
        }

        public void ForceKill()
        {
            if (mythread.IsAlive)
                try
                {
                    mythread.Abort();
                } 
                catch
                {
                }
        }

        private bool IsValidCmdID()
        {
            return Enum.IsDefined(typeof(CmdPacket), buff[0]);
        }

        private void CalcBeartCheckSum(byte[] arr, int finish)
        {
            for (int i = 0; i < arr.Length - 1; i += 2)
                arr[i] = (byte)(arr[i] + arr[i + 1]); // moded auto

            if (finish > 0)
                CalcBeartCheckSum(arr, finish - 1);
        }

        private byte[] Sync(byte[] sync, int maxor = 200)
        {
            CalcBeartCheckSum(sync, sync.Length / 2);
            List<byte> sendReady = new List<byte>();
            sendReady.Add((byte)CmdPacket.HeartBeat);
            foreach (byte b in sync)
                if (b >= maxor)
                    sendReady.Add(b);
            if (sendReady.Count == 1)
                return Sync(sync, maxor / 2);
            else
                return sendReady.ToArray();
        }

        private byte[] SyncClock(int indexOfBeat) // need some checks, for spoof lengths, prevent System Collapse/Crash.
        {
            byte[] buffbyter = readFullBuffbyter();
            byte[] beatBuff = new byte[indexOfBeat - 2];
            byte[] copyofBeatBuff = new byte[indexOfBeat - 2];
            Buffer.BlockCopy(buffbyter, 0, beatBuff, 0, indexOfBeat - 2);
            Buffer.BlockCopy(beatBuff, 0, copyofBeatBuff, 0, beatBuff.Length);

            byte[] Sync_Beatbuff = Sync(copyofBeatBuff);

            if (buffbyter.Length - indexOfBeat != Sync_Beatbuff.Length)// Check Lens HERE
                return null;
            
            // copyofBeatBuff is here edited and contains elements not picked by maxor

            if (buffbyter[indexOfBeat - 1] <= buffbyter[indexOfBeat - 2] &&
                buffbyter[indexOfBeat - 1] >= 201)
            {
                int j = 1;
                for(int i = indexOfBeat + 1; i < buffbyter.Length; i++)
                    if (Sync_Beatbuff[j++] != buffbyter[i])
                        return null;

                return beatBuff;
            }
            else
                return null;
        }
    }
}
