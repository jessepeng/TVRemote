using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TVRemote
{
    class Server
    {

        private XmlDocument ConfigFile;
        private TcpListener tcpListener;
        private Thread listenThread;

        public Server(string ConfigFilePath)
        {
            ConfigFile = new XmlDocument();
            ConfigFile.Load(ConfigFilePath);
        }

        public int GetServerPort()
        {
            if (ConfigFile != null)
            {
                XmlNode ConfigNode = ConfigFile.SelectSingleNode("/config/server/port");
                return int.Parse(ConfigNode.InnerText);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public IPAddress GetAllowedClientIP()
        {
            if (ConfigFile != null)
            {
                XmlNode ConfigNode = ConfigFile.SelectSingleNode("/config/client/ip");
                return IPAddress.Parse(ConfigNode.InnerText);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Start()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, GetServerPort());
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                TcpClient client = this.tcpListener.AcceptTcpClient();
                try
                {
                    IPAddress clientIPAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                    if (clientIPAddress.Equals(GetAllowedClientIP()))
                    {
                        NetworkStream networkStream = client.GetStream();

                        StreamReader networkStreamReader = new StreamReader(networkStream);
                        StreamWriter networkStreamWriter = new StreamWriter(networkStream);

                        networkStreamWriter.WriteLine("Connection approved.");
                        networkStreamWriter.Flush();

                        string line;
                        while ((line = networkStreamReader.ReadLine()) != null)
                        {
                            switch (line)
                            {
                                case "reset":
                                    new Thread(new ThreadStart(ResetTVCenter)).Start();
                                    break;
                                case "close":
                                    Process.Start("shutdown", "/s /t 0");
                                    break;
                                default:
                                    ActivateTVCenter();
                                    SendKeys.SendWait(line);
                                    break;
                            }
                        }

                    }
                }
                finally
                {
                    client.Close();
                }
            }
        }

        private void ActivateTVCenter()
        {
            Process[] TVCenters = Process.GetProcessesByName("tvcenter");
            if (TVCenters.Length > 0)
            {
                SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                Process TVCenter = TVCenters[0];
                SetForegroundWindow(TVCenter.MainWindowHandle);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void ResetTVCenter()
        {
            try
            {
                string TVCenterPath = "";
                Process[] TVCenters = Process.GetProcessesByName("tvcenter");
                if (TVCenters.Length > 0)
                {
                    Process TVCenter = TVCenters[0];
                    TVCenterPath = TVCenter.MainModule.FileName;
                    TVCenter.Kill();
                    TVCenter.WaitForExit();
                }
                Process[] VideoControls = Process.GetProcessesByName("videocontrol");
                if (VideoControls.Length > 0) 
                {
                    Process VideoControl = VideoControls[0];
                    VideoControl.Kill();
                    VideoControl.WaitForExit();
                }
                if (!TVCenterPath.Equals("")) Process.Start(TVCenterPath);
            }
            finally { }
        }

    }
}
