using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVRemote
{
    class Program
    {

        static string ConfigFile = "config.xml";

        static void Main(string[] args)
        {

            Server Server = new Server(ConfigFile);
            Server.Start();
        }

       
    }
}
