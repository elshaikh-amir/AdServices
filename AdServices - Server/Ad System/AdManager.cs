using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AdServices___Server.Networking;

namespace AdServices___Server.Ad_System
{
    class AdList
    {
        private volatile object olock;
        private List<AdLink> Links;
        private AdLink Pointer;
        public bool HasRound { get; private set; }

        public AdList()
        {
            olock = new object();
            Links = new List<AdLink>();
            Pointer = null;
            HasRound = false;
        }

        public int Count
        {
            get
            {
                lock (olock)
                    return Links.Count;
            }
        }

        /*public AdLink Last
        {
            get
            {
                lock (olock)
                    return Links.Last();
            }
        }

        public AdLink First
        {
            get
            {
                lock (olock)
                    return Links.First();
            }
        }*/

        public void Add(AdLink link)
        {
            lock (olock)
                Links.Add(link);
        }

        /*public void Remove(AdLink link)
        {            
           // AdLink ad = Find(l => l.linkstring.Equals(link));
            if (link != null)
                Remove(link);
        }*/

        public void Remove(AdLink link)
        {
            // lock here? Yes, Client's Thread might access obj onError Event
            lock(olock)
                link.Remove();
        }

        public AdLink Find(Predicate<AdLink> f)
        {
            lock (olock)
                return Links.Find(f);
        }

        /*public bool Exists(AdLink link)
        {
            return Find(l => l == link) != null;
        }*/

        public void ForEach(Action<AdLink> f)
        {
            lock (olock)
                Links.ForEach(f);
        }

        public AdLink GetNext()
        {
            Next();
            while (Pointer != null && Pointer.isRemoved) //blocks at last, GC is called at size
                Next();
            return Pointer;
        }

        public void AddAll(List<AdLink> list)
        {
            lock (olock)
                Links.AddRange(list);
        }

        public List<AdLink> GetListUnlocked() // error here? pointers are Lost!
        {
            lock (olock)
                return Links.GetRange(0, Links.Count);
        }

       /* private void GC()
        {
            lock(olock)
                Links.RemoveAll(l => l.isRemoved);
            //List<AdLink> newList = new List<AdLink>(Links.FindAll(l => l.isRemoved == false));
        }*/
        public AdLink CurrPointer
        {
            get
            {
                lock (olock)
                    return Pointer;
            }
        }

        private void Next()
        {
            lock (olock)
            {
                if (Pointer == null)
                {
                    if (Links.Count > 0)
                        Pointer = Links.First(); // gets first obj
                }
                else// if(Pointer != null)
                {
                    if (Links.Last() == Pointer) // last index pointer. Call GC and reset pointer
                    {
                        HasRound = true;
                        if (Links.Count > 1)
                            Links.RemoveAll(l => l.isRemoved); // can't call GC inside of locked object, -----> DeadLock Fixed <-----
                            //GC();
                        //Pointer = Links.FirstOrDefault(); // works too, but i dont use it here
                        if (Links.Count > 0)
                            Pointer = Links.First(); // can be null element, but np
                        else
                            Pointer = null;
                    }
                    else
                    {
                        int index = Links.IndexOf(Pointer);
                        if (index < 0 || index + 1 >= Count) // Dafuq.. this case isn't allowed to trigger!
                            return;
                        Pointer = Links[index + 1];
                    }
                }
            }
        }
    }


    class AdManager
    {  
        private AdList AdList;
        private AdList NextList;
        private object olock;

        public void AddToNext(AdLink link) // some sync needed here!
        {
            lock (olock)
                NextList.Add(link);
        }

        public void MarkAsRemoved(AdLink link)
        {
            AdList.Remove(link);
        }

        public void Update(AdLink old, string neww)
        {
            //AdLink item = AdList.Find(a => a != null && !a.isRemoved && a.linkstring.Equals(old));

            if (old != null && neww != null)
                AdLink.Update(old, neww);
                //item = new AdLink(neww);
        }

        private byte[] getNext()
        {
            AdLink item = AdList.GetNext();

            if (item != null)
                return item.linkbytes;
            else
                return null;
        }

        public AdManager()
        {
            this.AdList = new AdList();
            this.NextList = new AdList();
            this.olock = new object();
            Globals.ADManagerThread = new Thread(new ThreadStart(AdService));
            Globals.ADManagerThread.Start();
        }

        private void ShiftNext() // olock here sysncs client threads from addtonext on adlist and nextlist objs
        {
            lock (olock)
            {
                NextList.AddAll(AdList.GetListUnlocked());
                AdList = NextList;
                NextList = new AdList();
            }
        }

        private void HandleRounds()
        {
            if (AdList.HasRound) // pointer == last; =====> and i get Next item here
                ShiftNext();
        }

        private void Browser_Tick()
        {
            if (AdList.Count > 0)
            {
                HandleRounds();
                byte[] b = getNext();
                if (b != null)
                    Globals.NetworkServer.BroadCast(CmdPacket.AdLink, b);
            }
            else if(NextList.Count > 0) // adlist has not rounded, probably first run!
            {
                ShiftNext();
                Browser_Tick();
            }
        }

        private void AdService()
        {
            try
            {
                Utils.Log.Write("Ad Services Browser System UP and Working.");
                while (true)
                {
                    Browser_Tick();
                    if (AdList.CurrPointer != null)
                        Utils.Log.Write("Command Browse Executed with: {0}", AdList.CurrPointer.linkstring);
                    Thread.Sleep(Globals.DelayBrowse);
                }
            }
            catch(Exception e)
            {
                Utils.Log.Write("AdManager Crashed, Restarting Server..{0}", e.StackTrace.ToString());
                Globals.NetworkServer.RestartServer();
            }
        }
    }
}
