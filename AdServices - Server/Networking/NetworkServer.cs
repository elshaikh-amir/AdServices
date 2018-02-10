using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using AdServices___Server.Utils;

namespace AdServices___Server.Networking
{
    class NetworkServer
    {
        //private char sp = ':';
        private TcpListener Server;
        public bool isOnline { get; private set; }
        private NetworkHeartBeat Heart;
        private CmdPacket BroadCastInErrorCase;

        public static void Initialize()
        {
            if (Globals.NetworkServer != null && Globals.NetworkServer.isOnline)
                Globals.NetworkServer.ShutDown(CmdPacket.ServerReini);
           
            Globals.NetworkServer = new NetworkServer();
            Globals.NetworkServer.Server = new TcpListener(IPAddress.Parse(Globals.ServerIP), Globals.ServerPort);
            Globals.NetworkServer.Run();
        }

        public void HeartKill()
        {
            Heart.Kill();
            Log.SYS("HeartKill Command Executed Successfully, Heart is Dead!");
        }

        public void HeartBeat()
        {
            Heart.Beat();
            Log.SYS("HeartBeat Command Executed Successfully, Heart is Beating!");
        }

        public void SetHeartDelay(int d)
        {
            Heart.setDelay(d);
        }

        private void Run()
        {
            /*if (Globals.NetworkServerThread != null && Globals.NetworkServerThread.IsAlive)
            {
                Log.Write("Failed starting Server, ServerThread is: {0}", Globals.NetworkServerThread.ThreadState.ToString());
                return;
            }
            else if (Server != null && Server.Server.IsBound)
            {
                Log.Write("Failed starting Server, Server is Bound at(Remote): {0} - at(Local): {1}", Server.Server.RemoteEndPoint.ToString(), Server.Server.LocalEndPoint.ToString());
                return;
            }*/
            Heart = new NetworkHeartBeat(Globals.HeatBeat);
            Globals.NetworkServerThread = new Thread(new ThreadStart(Networker));
            Globals.NetworkServerThread.Start();
        }

        private void Networker()
        {
            Server.Start();
            Heart.Beat();
            this.isOnline = true;
            BroadCastInErrorCase = CmdPacket.ServerRestart;
            Log.Write("NetworkServer started, Bound at: {0}", Server.LocalEndpoint.ToString());
            try
            {
                while (this.isOnline)
                {
                    TcpClient NewClient = Server.AcceptTcpClient();

                    if (Globals.Shield.IsBlocked(NewClient))
                    {
                        Globals.NetworkServer.SendCmd(NewClient, CmdPacket.DisconnectIsBanned);
                        NewClient.Close();
                        continue;
                    }

                    if (!isClientWithSameIP(NewClient))
                    {
                        Log.Write("New Client incomming Connection at: {0}", getIP(NewClient));
                        new Client(NewClient);
                    }
                    else
                    {
                        Log.Write("New Client incomming Connection with duplicated IPAddress at: {0} - Blocked!", getIP(NewClient));
                        SendCmd(NewClient, CmdPacket.DisconnectTooManyIPs);
                        NewClient.Close();
                        //continue;
                    }
                }
                PutOff();
            }
            catch// (ThreadAbortException)
            {
                PutOff();
            }
        }

        private void PutOff()
        {
            Heart.Kill();
            BroadCast(BroadCastInErrorCase);
            Server.Stop();
            //DisconnectAll();
            //this.isOnline = false;
            Log.Write("Server has been brought Offline!");
        }

        private string getIP(TcpClient c)
        {
            try
            {
                return c.Client.RemoteEndPoint.ToString();
            }
            catch { return "Client.IP.ERROR"; }
        }

        public void DisconnectAll()
        {
            Globals.Clients.ForEach(c => { if (c != null) c.tcpclient.Close(); });
        }

        public void SendCmd(TcpClient client, CmdPacket cmd)
        {
            SendCmd(client, new byte[] { (byte)cmd });
        }

        public void SendCmd(TcpClient client, byte[] cmd)
        {
            try
            {
                if(client != null && client.Connected)
                    client.Client.Send(cmd);
            }
            catch (SocketException e)
            {
                Log.Write("Error at: SendCmd(Tcpclient, Command), Exception: SocketException, StackTrace: {0}", e.StackTrace);
            }
            catch (ObjectDisposedException e1)
            {
                Log.Write("Error at: SendCmd(Tcpclient, Command), Exception: ObjectDisposedException, StackTrace: {0} ", e1.StackTrace);
            }
            catch (ArgumentNullException e2)
            {
                Log.Write("Error at: SendCmd(Tcpclient, Command), Exception: ArgumentNullException, StackTrace: {0}", e2.StackTrace);
            }
        }

        public void BroadCast(CmdPacket cmd)
        {
            byte[] _cmd = new byte[]{(byte)cmd};
            Globals.Clients.ForEach(c => SendCmd(c.tcpclient, _cmd));
        }

        public void BroadCast(CmdPacket cmd, byte[] buffdata) // check later
        {
            //byte[] _cmd = new byte[] { (byte)cmd };
            byte[] newbuff = new byte[buffdata.Length + 1];
            newbuff[0] = (byte)cmd;
            Buffer.BlockCopy(buffdata, 0, newbuff, 1, buffdata.Length);
            //var buff = _cmd.Concat(buffdata).ToArray<byte>();
            Globals.Clients.ForEach(c => SendCmd(c.tcpclient, newbuff));
        }

        private bool isClientWithSameIP(TcpClient client)
        {
            string findIP = getIP(client);
            if (findIP.Equals("Client.IP.ERROR"))
                return false;
            return null != Globals.Clients.Find(c => c.ip.Equals(findIP));
        }

        public void ShutDown(CmdPacket packet)
        {
            BroadCastInErrorCase = packet;
            Server.Server.Close();
            Globals.NetworkServerThread.Abort();
            isOnline = false;
        }

        public void ShutDown()
        {
            ShutDown(CmdPacket.Disconnect);             
        }

        public void RestartServer(string reason)
        {
            Log.Write(reason);
            RestartServer();
        }

        public void RestartServer() // still under construction, this is shit code
        {
            ShutDown(CmdPacket.ServerRestart); 
            //Send Restat Command
            //Initialize();
            Program.Restart();
        }

        public void DisconnectIP(string ip)
        {
            if (ip == null || !ip.Contains(".") || ip.Split(new char[] { '.' }).Length != 4)
                Log.SYS("DisconnectIP: {0} failed, wrong parameter", ip);
            else
            {
                Client client = getClientbyIP(ip);
                if (client == null)
                    Log.SYS("Client with IP: {0} not found!, Command Execution Failed!", ip);
                else
                    try
                    {
                        client.Disconnect("Command Disconnect.IP Executed Successfully!, Client: {0} Disconnected", ip);
                    }
                    catch { Log.SYS("Fatal Error, Threads Multi Access on Obj. Client, Command Failed!"); }
            }
        }

        public void Kill(Client client) // this can be called of an outer function, not for Heart.
        {
            try
            {
                if (client != null)
                    client.ForceKill();
            }
            catch { }
        }

        public Client getClientbyIP(string ip)
        {
            return Globals.Clients.Find(c => c != null && c.ip.Equals(ip));
        }
    }
}
