using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using NATUPNPLib;


// .NET 4 command line application that lists UPNP mappings on connected routers.
// Uses Windows Native UPnp Support library (NATUPNPLib).
//     v 1.0
//  (9/14/2010) - Alex Soya, Logan Industries, Inc.
//
//

namespace UpnpStat
{
    class Program
    {
        
        static void Main(string[] args)
        {
            PrintStartUpBanner(); 
            HandleCommandLine(args);

       


        }

        private static void HandleCommandLine(string[] args)
        {
            UpnpHelper Upnp;

            if (args.Length > 0)
            {
                if (string.Compare(args[0].ToLower(), "help") == 0)
                {
                    ShowHelp();
                    return;
                }

                if (string.Compare(args[0].ToLower(), "list") == 0)
                {
                    Upnp = new UpnpHelper();
                    Upnp.ListMappings();
                    return;
                }

                if (string.Compare(args[0].ToLower(), "clear") == 0)
                {
                    Upnp = new UpnpHelper();
                    Upnp.ClearMappings();
                    return;
                }

                if (string.Compare(args[0].ToLower(), "add") == 0)
                {
                    HandleAddCommand(args);
                    return;
                }
            }
            ShowUsage();
        }

        private static void HandleAddCommand(string[] args)
        {
            UpnpHelper Upnp;
            ushort port;
            string Protocol;

            if (args.Length != 4)
            {
                Console.WriteLine("Invalid number of arguments");
                ShowAddUsage();
                return;
            }

            // Get and check port number
            try
            {

                port = ushort.Parse(args[1]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                ShowAddUsage();
                return;
            }
            



            if (port < 1)
            {
                Console.WriteLine("Port number out of range");
            }


            // Get and check protocol

            Protocol = args[2].ToUpper();
            if  (!( (string.Compare(Protocol, "UDP") == 0) ^ (string.Compare(Protocol, "TCP") == 0) ))
            {
                Console.WriteLine("Invalid protocol. Must be TCP or UDP");
                return;
            }



            // Get Description
            string Description = args[3];


            Console.WriteLine("Adding: Port: {0}, Protocol: {1}, Description: {2}", port, Protocol, Description);
            Upnp = new UpnpHelper();
            Upnp.AddMapping(port, Protocol, Description);

        }

        private static void ShowAddUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("add port protocol description");
            Console.WriteLine("    - port           = port number to be mapped [1..65535]");
            Console.WriteLine("    - protocol       = UDP | TCP");
            Console.WriteLine("    - description    = \"a description\"");
            Console.WriteLine("Example:");
            Console.WriteLine("UpnpStat add 80 TCP \"My Web Server\"");
        }




        private static void ShowUsage()
        {
            Console.WriteLine("Usage: upnpstat [help | add | clear | list]");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Valid commands:");
            Console.WriteLine("help                             -- This help screen");
            Console.WriteLine("add port protocol description    -- Add uPnp mapping on router");
            Console.WriteLine("clear                            -- clear all UpNp mappings on router");
            Console.WriteLine("list                             -- list active uPnP mappings on router");
        }




        static void PrintStartUpBanner()
        {
            Console.WriteLine("UpnpStat v1.0");
            Console.WriteLine("(C) 2010 Alex Soya, Logan Industries, Inc.(www.lii.com) akhsoya@gmail.com");
            Console.WriteLine("");
        }
    }


    class UpnpHelper
    {
        UPnPNAT NatMgr;

        public UpnpHelper()
        {
            NatMgr = new UPnPNAT();
        }

        /// <summary>
        /// Returns Local IP used for Internet Communications by opening a socket to www.netsnap.com
        /// If that fails, it tries www.lii.com
        /// Next it reads the LocalEndPoint on connection, and strips of port information.
        /// We do this in case we have multiple NICs to see which one goes out to Internet.
        /// </summary>
        /// <returns></returns>
        private string GetLocalIP()
        {
            bool bConnected;
            EndPoint ep;
            string sip;

            ep = null;
            TcpClient client = new TcpClient();
            try
            {
                client.Connect("www.yahoo.com", 80);
                ep = client.Client.LocalEndPoint;
                client.Close();
                bConnected = true;
            }
            catch (Exception)
            {
                bConnected = false;
            }
            if (!bConnected)
            {
                try
                {
                    client.Connect("www.lii.com", 80);
                    ep = client.Client.LocalEndPoint;
                    client.Close();
                    bConnected = true;
                }
                catch (Exception)
                {
                    bConnected = false;
                }
            }


            if (!bConnected) return null;
            if (ep != null)
            {
                sip = ep.ToString();
                int end = sip.IndexOf(":");
                return (sip.Remove(end));
            }
            return null;
        }

        private bool DeleteMapNat(int port)
        {

            try
            {
                IStaticPortMappingCollection mappings = NatMgr.StaticPortMappingCollection;
                mappings.Remove(port, "TCP");
            }
            catch (Exception)
            {
                return false;
            }
            return true;

        }

        internal void ListMappings()
        {
            if (NatMgr == null)
            {
                Console.WriteLine("Initialization failed creating Windows UPnPNAT interface.");
                return;
            }

            IStaticPortMappingCollection mappings = NatMgr.StaticPortMappingCollection;
            if (mappings == null)
            {
                Console.WriteLine("No mappings found. Do you have a uPnP enabled router as your gateway?");
                return;
            }

            if (mappings.Count == 0)
            {
                Console.WriteLine("Router does not have any active uPnP mappings.");
            }

            foreach (IStaticPortMapping pm in mappings)
            {
                Console.WriteLine("Description:");
                Console.WriteLine(pm.Description);
                Console.WriteLine(" {0}:{1}  --->  {2}:{3} ({4})", pm.ExternalIPAddress, pm.ExternalPort, pm.InternalClient,pm.InternalPort,pm.Protocol);
                Console.WriteLine("");
            }

        }

        internal void ClearMappings()
        {
            if (NatMgr == null)
            {
                Console.WriteLine("Initialization failed creating Windows UPnPNAT interface.");
                return;
            }

            IStaticPortMappingCollection mappings = NatMgr.StaticPortMappingCollection;
            if (mappings == null)
            {
                Console.WriteLine("No mappings found. Do you have a uPnP enabled router as your gateway?");
                return;
            }

            List<IStaticPortMapping> pmsToDelete = new List<IStaticPortMapping>();


            // We need to build our own list as we can not remove from mappings list without altering the list
            // resulting in last entry never being deleted.
            foreach (IStaticPortMapping pm in mappings)
            {
                pmsToDelete.Add(pm);
            }

            foreach (IStaticPortMapping pm in pmsToDelete) 
            {
                Console.WriteLine("Deleting : {0}", pm.Description);
                mappings.Remove(pm.ExternalPort, pm.Protocol);
            }

        }

        internal void AddMapping(ushort Port, string Protocol, string Description)
        {
            if (NatMgr == null)
            {
                Console.WriteLine("Initialization failed creating Windows UPnPNAT interface.");
                return;
            }

            IStaticPortMappingCollection mappings = NatMgr.StaticPortMappingCollection;
            if (mappings == null)
            {
                Console.WriteLine("No mappings found. Do you have a uPnP enabled router as your gateway?");
                return;
            }

            mappings.Add(Port, Protocol, Port, GetLocalIP(), true, Description);
        }
        

    }



}
