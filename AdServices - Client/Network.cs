using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AdServices___Client
{
    class Network
    {
        private enum Packet : byte
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
            adlinkFailed = 10
        }

        private const int buffsize = 4096;
        private const int port = 50;
        private IPAddress server;
        private byte[] buff;
        private int size;
        private TcpClient client;
        private NetworkStream stream;
        private string packetData;
        private Form1 UI;

        public Network(IPAddress ip, Form1 call)
        {
            server = ip;
            buff = new byte[buffsize];
            packetData = string.Empty;
            UI = call;
        }

        public void connect()
        {
            new Thread(new ThreadStart(doListen)).Start();
        }

        private void CalcBeartCheckSum(byte[] arr, int finish)
        {
            for(int i = 0; i < arr.Length - 1; i += 2)
                arr[i] = (byte)(arr[i] + arr[i + 1]);

            if(finish > 0)
                CalcBeartCheckSum(arr, finish - 1);
        }

        private byte[] Sync(byte[] sync, int maxor = 200)
        {
            CalcBeartCheckSum(sync, sync.Length / 2);
            List<byte> sendReady = new List<byte>();
            sendReady.Add((byte)Packet.HeartBeat);
            foreach (byte b in sync)
                if (b >= maxor)
                    sendReady.Add(b);
            if (sendReady.Count == 1)
                return Sync(sync, maxor / 2);
            else
                return sendReady.ToArray();
        }

        private byte[] SyncClock()
        {
            byte[] ticks = BitConverter.GetBytes(DateTime.UtcNow.Ticks);

            List<byte> sendready = new List<byte>();
            sendready.AddRange(ticks);
            Random rnd = new Random();
            int i1 = rnd.Next(201, 255);
            int i2 = rnd.Next(201, i1); // i2 =< i1
            sendready.Add((byte)i1);
            sendready.Add((byte)i2);
            sendready.AddRange(Sync(ticks));
            //Thread.Sleep(100);
            return sendready.ToArray();
        }

        private void HeartBeat()
        {
            byte[] sync = SyncClock();
            stream.Write(sync, 0, sync.Length);
            stream.Flush(); 
        }

        private void doListen()
        {
            try
            {
                client = new TcpClient();
                client.Connect(server, port);
                UI.LockMe();
                UI.postMsg("Connected!");
                stream = client.GetStream();
                while (client.Connected)
                {
                    ReadStream();
                    if (size == 0)
                    {
                        Disconnect("Read Failure");
                        break;
                    }
                    
                    switch ((Packet)buff[0])
                    {
                        case Packet.adlinkFailed:
                            UI.postMsg("Packet: AdLinkFailed Recieved!");
                            break;
                        case Packet.AdLink:
                            UI.postMsg("Packet: AdLink Recieved!");
                            break;
                        case Packet.Disconnect:;
                            Disconnect("Packet: Disconnect Recieved!");
                            break;
                        case Packet.DisconnectIsBanned:
                            Disconnect("Packet: DisconnectIsBanned Recieved!");
                            break;
                        case Packet.DisconnectTooManyIPs:
                            Disconnect("Packet: DisconnectTooManyIPs Recieved!");
                            break;
                        case Packet.HeartBeat:
                            HeartBeat();
                            UI.postMsg("Packet: HeartBeat Recieved!");
                            break;
                        case Packet.Login:
                            UI.postMsg("Packet: Login Recieved!");
                            break;
                        case Packet.LoginFailed:
                            UI.postMsg("Packet: LoginFailed Recieved!");
                            break;
                        case Packet.LoginUserOrPasswordIncorrect:
                            UI.postMsg("Packet: LoginUserOrPasswordIncorrect Recieved!");
                            break;
                        case Packet.ServerRestart:
                            UI.postMsg("Packet: ServerRestart Recieved!");
                            break;
                        default:
                            UI.postMsg("Packet: Unknown: " + buff[0] + " Recieved!");
                            break;
                    }
                }
            }
            catch (SocketException)
            {
                Disconnect("Socket Error, Client Disconnected!");
            }
            catch (Exception)
            {
                Disconnect("Unknown Error, Client Disconnected!");
            }
            UI.UnLockMe();
        }

        private void setBuffSize() // hope this works, if not.. Copy the one from Server YOLO
        {
            byte _f = 0;
            size = Array.FindIndex<byte>(buff, b => b == _f);
            if (size < 0)
                size = buffsize;
        }

        private void ReadPacketData()
        {

        }

        private void ReadStream()
        {
            stream.Read(buff, 0, buffsize);
            setBuffSize();
            /*for(size = 0; size < buffsize; size++)
                if (buff[size] == 0)
                    break;*/
        }


        private void Disconnect(string reason)
        {
            UI.postMsg(reason);
            Disconnect();
        }

        private void Disconnect()
        {
            if (client != null && client.Connected)
                client.Client.Close();
            UI.UnLockMe();
        }
    }
}
