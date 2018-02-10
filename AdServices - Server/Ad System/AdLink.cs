using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdServices___Server.Ad_System
{
    class AdLink
    {
        public byte[] linkbytes { get; private set; }
        public string linkstring { get; private set; }
        public bool isRemoved { get; private set; }

        public void Remove()
        {
            isRemoved = true;
        }

        public AdLink(string linkstr)
        {
            if (linkstr != null && linkstr.Length > 3)
            {
                linkbytes = System.Text.ASCIIEncoding.UTF8.GetBytes(linkstr);
                linkstring = linkstr;
                isRemoved = false;
            }
            else
                isRemoved = true;
        }

        public static void Update(AdLink old, string nw)
        {
            old.linkstring = nw;
            old.linkbytes = System.Text.ASCIIEncoding.UTF8.GetBytes(nw);
        }
    }
}
