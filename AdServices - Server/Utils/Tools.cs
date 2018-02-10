using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdServices___Server.Utils
{
    class Tools
    {
        public static int ipToInt(string addr)
        {
            string[] addrArray = addr.Split(new char[] { '.' });
            short[] ips = new short[4];
            for (int i = 0; i < 4; i++)
                if (!Int16.TryParse(addrArray[i], out ips[i]))
                    return 0;
            int num = 0, power = 3;
            for (int i = 0; i < 4; i++)
            {
                num += (int)(ips[i] % 256 * Math.Pow(256, power));
                power = 3 - i;
            }
            return num;
        }
    }
}
