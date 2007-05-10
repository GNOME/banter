// project created on 3/5/2007 at 11:11 AM
using System;
using System.Collections;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;


namespace TelepathyTest
{

	public class Test
	{
		static IConnection iconn = null;
		static ConnectionInfo conn;
	  	static Bus bus = Bus.Session;
		static bool running = true;
		static string ConnectionPath = "org.freedesktop.Telepathy.ConnectionManager.gabble";
		
		public static void OnSearchResults( uint contact, IDictionary<string, object> values)
		{
		}
		
		public static void OnPresenceUpdate(IDictionary<uint, PresenceUpdateInfo> info )
		{
			Console.WriteLine( "OnPresenceUpdate called" );
		}

		public static void OnConnectionStateChanged (ConnectionStatus status, ConnectionStatusReason reason)
		{
			Console.WriteLine ("Connection state changed, Status: {0}, Reason: {1}", status, reason);
			
			if (status == ConnectionStatus.Connected)
			{
				try
				{
					Console.WriteLine("Connected - trying to get the buddy list");
					string[] args = {"subscribe"};
					uint[] handles = iconn.RequestHandles (HandleType.List, args);
					ObjectPath object_path = iconn.RequestChannel (ChannelType.ContactList, HandleType.List, handles[0], true);
					IChannelGroup contact_list = bus.GetObject<IChannelGroup> (conn.BusName, object_path);
					
					
					string[] members = iconn.InspectHandles (HandleType.Contact, contact_list.Members);

					foreach (string member in members) {
						Console.WriteLine ("Member: {0}", member);
					}	
					
					// Display interfaces on connection				
					foreach (string interfaceName in iconn.Interfaces)
					{
						Console.WriteLine( "interface: {0}", interfaceName );
					}
					
					//uint[] handles = iconn.RequestHandles (HandleType.List, args);
					ObjectPath obPath = iconn.RequestChannel( ChannelType.ContactSearch, HandleType.List, handles[0], true);
					IChannelContactSearch contactSearch = bus.GetObject<IChannelContactSearch>(conn.BusName, obPath );
					contactSearch.SearchResultReceived += OnSearchResults;		
					
					//get connection manager from dbus
					IConnectionPresence connPresence = 
						bus.GetObject<IConnectionPresence> (
							Test.ConnectionPath,
							object_path );
							
					Console.WriteLine( "got a presence object" );
					connPresence.PresenceUpdate += OnPresenceUpdate;
					//connPresence.RequestPresence( members );
				}
				finally
				{
					iconn.Disconnect ();
				}
			}

	    if (status == ConnectionStatus.Disconnected)
	      running = false;
		}

		public static void Main (string [] args)
		{
			// default
			string account = "brady.anderson@gmail.com";
			string password = "";
			string server = "talk.google.com";
			string port = "5223";
			
			// non default
			if (args.Length > 0) {
				account = args[0];
			}
			
			if (args.Length > 1) {
				password = args[1];
			}
			
			if (args.Length > 2) {
				server = args[2];
			}
			
			if (args.Length > 3) {
				port = args[3];
			}
			
			string obPath = "/" + Test.ConnectionPath.Replace('.', '/');
			Console.WriteLine(obPath);

			//get connection manager from dbus
			IConnectionManager connManager = 
				bus.GetObject<IConnectionManager> (
					Test.ConnectionPath,
					new ObjectPath (obPath));

		    if (connManager == null) {
	    	  	Console.WriteLine ("Unable to establish a connection with: {0}", Test.ConnectionPath);
				return;
			}
			
			Console.WriteLine( "Getting protocols for connnection manager: {0}", Test.ConnectionPath );
			string[] protocols;
			try {
				protocols = connManager.ListProtocols();
				foreach( string proto in protocols ) {
					Console.WriteLine( "  " + proto );
				}

			} catch (Exception e1) {
				Console.WriteLine("Exception getting protocols");
				Console.WriteLine( e1.Message );
				return;
			}
			
			Console.WriteLine( "Getting parameters for protocol: {0}", protocols[0] );
			try {
				org.freedesktop.Telepathy.Parameter[] parms = 
					connManager.GetParameters(protocols[0]);
				foreach( org.freedesktop.Telepathy.Parameter parameter in parms ) {
					Console.WriteLine(parameter.Name +": " + parameter.Flags.ToString());
				}

			} catch (Exception e2) {
				Console.WriteLine("Exception getting parameters");
				Console.WriteLine( e2.Message );
				return;
			}
			
			Console.WriteLine("Account: {0}\nPassowrd: ?\nServer: {1}\nPort: {2}", account, server, port);
			Dictionary<string, object> optionList = new Dictionary<string, object>();
			optionList.Add ("account", account);
			optionList.Add ("password", password);
			optionList.Add ("server", server);
			optionList.Add ("port", (uint) UInt32.Parse (port));
			optionList.Add ("old-ssl", true);
			optionList.Add ("ignore-ssl-errors", true);

			try {
				conn = connManager.RequestConnection ("jabber", optionList);
			} catch (Exception e) {
				Console.WriteLine( "Exception while connecting to: " + Test.ConnectionPath );
				Console.WriteLine( e.Message );
				return;
			}

			Console.WriteLine ("Bus Name: " + conn.BusName + ", " + conn.ObjectPath.Value);
			iconn = bus.GetObject<IConnection> (conn.BusName, conn.ObjectPath);

			iconn.StatusChanged += OnConnectionStateChanged;
			iconn.Connect ();

			//connPresence.RequestPresence(
			
			//if (running == true) bus.Iterate();
			Console.ReadLine();
			
//			if (iconn != null) iconn.Disconnect ();

			/*
			while (running)
				bus.Iterate ();
			*/
		}
	}
}