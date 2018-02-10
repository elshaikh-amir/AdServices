using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AdServices___Server.Utils;
using AdServices___Server.Networking;
using System.Runtime.InteropServices;

namespace AdServices___Server
{
    class Program
    {
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            // Put your own handler here

            switch (ctrlType)
            {
                case CtrlTypes.CTRL_CLOSE_EVENT:
                    Close();
                    break;
            }

            return true;
        }

        public static void Close()
        {
            Globals.Shield.LogShield();
            System.Environment.Exit(0);
        }

        public static void Restart()
        {
            string[] x = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.Split(new char[] { '/' });
            System.Diagnostics.Process.Start(x.Last());
            Close();
        }

        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);
            string version = "Beta v0.02";
            printSplash(version);
            Console.Title = "The AdServices Server Platform";
            Console.WindowWidth = Console.LargestWindowWidth - 50;
            Console.WindowHeight = Console.LargestWindowHeight - 20;
            Log.Initialize();
            Globals.Initialize();
            //Log.Write("Server Launching.. 5 Secounds Engine startup time");
            Networking.NetworkServer.Initialize();
            
            Thread.Sleep(1000); // wait server launch, halts only main thread not server

            if (Globals.NetworkServer != null && !Globals.NetworkServer.isOnline)
            {
                Log.Write("Failed Launching Server");
                return;
            }
            Log.Write("Engine Warmup Time UP ==> Server UP - Online");
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
           
            //for (int i = 0; i < 100; i++)
             //   new Networking.Warning(i);
            //Globals.Shield.LogShield();
            //Log.TurnLog(false);
            Log.SYS("This is the Commander, type 'help' for help");

            doCommander();
        }

        public static void doCommander()
        {
            Commander();
            doCommander();
        }

        private static void Commander()
        {
            Console.Beep();
            Log.SYS_NOLINE("Enter your CMD: ");
            string input = Console.ReadLine().ToLower();

            if (input != null)
            {
                string[] dat = null;
                if (input.Contains('='))
                    dat = input.Split(new char[] { '=' });
                else
                    dat = new string[]{input, null};

                Command.ApplyCommand(dat[0], dat[1]);       
            }
        }

        public static void printSplash(string version)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("                         _________________________");
            Console.WriteLine("                 _______/                         \\_______");
            Console.WriteLine("                /                                         \\");
            Console.WriteLine(" +-------------+                                           +-------------+");
            Console.WriteLine(" |               AdServices: Server Application                          |");
            Console.WriteLine(" |                                                                       |");
            Console.WriteLine(" |               Beta Release Date: 06/12/13                             |");
            Console.WriteLine(" |               Written by: zTimeKeeper                                 |");
            Console.WriteLine(" |               Version: " + version + "                                     |");
            Console.WriteLine(" |                                                                       |");
            Console.WriteLine(" |               I love chicken :)                                       |");
            Console.WriteLine(" |               ...Server Application launching...                      |");
            Console.WriteLine(" +-------------+                                           +-------------+");
            Console.WriteLine("                \\_______                           _______/");
            Console.WriteLine("                        \\_________________________/");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("===========================");
            Console.WriteLine("        -SERVER LOG-       ");
            Console.WriteLine("===========================");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
