using System;
using System.Collections.Generic;
using Tapioca;


namespace Tests
{
	public class ConnectionTest : ITest
	{
		Connection conn;
		string account, password;
		event EventFinishHandler TestFinished;
		bool param_ok;

		private void LoadContacts ()
		{
			foreach (Contact c in conn.ContactList.KnownContacts)
			{
				Console.WriteLine ("Contact Retrieved\n\t{0}/{4} - {1}/{2} - {3}",
					c.Uri, c.Presence, c.PresenceMessage, c.SubscriptionStatus, c.Alias);
			}
		}

		private bool Disconnect ()
		{
			Console.WriteLine ("Calling Disconnect");
			conn.Disconnect ();
			return false;
		}

		private void OnStatusChanged (Connection connection, ConnectionStatus status, ConnectionStatusReason reason)
		{

			Console.WriteLine ("STATUS {0} REASON {1}", status, reason);

			if (status == ConnectionStatus.Connected)
			{
				Console.WriteLine ("Connected {0}", connection.Name);
				LoadContacts ();
				Disconnect ();
			}


			if (status == ConnectionStatus.Disconnected) {
				conn.Dispose ();
				Console.WriteLine ("Connection disconnected: OK");
				if (TestFinished != null)
					TestFinished (false, "");
			}
		}

		public ConnectionTest (string[] args)
		{
			if (args.Length != 3) {
				param_ok = false;
				Console.WriteLine ("invalid param use [google_user_account] [password]");
			 	return;
			}
			param_ok = true;
			account = args[1];
			password = args[2];
		}

		public bool Run ()
		{
			if (!param_ok) return false;

			ConnectionManagerFactory cm_factory = new ConnectionManagerFactory ();

			Console.WriteLine ("Service is created");

			System.Collections.ArrayList ps = new System.Collections.ArrayList ();
			ps.Add (new ConnectionManagerParameter ("account", account));
			ps.Add (new ConnectionManagerParameter ("password", password));
			ps.Add (new ConnectionManagerParameter ("server", "talk.google.com"));
			ps.Add (new ConnectionManagerParameter ("old-ssl", true));
			ps.Add (new ConnectionManagerParameter ("ignore-ssl-errors", true));
			ps.Add (new ConnectionManagerParameter ("port", (uint) 5223));

			ConnectionManagerParameter[] parameters = (ConnectionManagerParameter[]) ps.ToArray (typeof (ConnectionManagerParameter));

			Console.WriteLine ("Creating connection");
			ConnectionManager cm = cm_factory.GetConnectionManager ("jabber");
			if (cm == null) {
				Console.WriteLine ("Error geting CM");
				return false;
			}
			Console.WriteLine ("Connection created");

			conn = cm.RequestConnection ("jabber", parameters);
			if (conn == null) {
				Console.WriteLine ("Error on RequestConnection");
				return false;
			}

			Console.WriteLine ("Connection jabber requested");

			conn.StatusChanged += OnStatusChanged;
			conn.Connect (ContactPresence.Available);

			Console.WriteLine ("Started connection");
			return true;
		}

		event EventFinishHandler ITest.TestFinished
        {
            add { TestFinished += value; }
            remove { TestFinished -= value; }
        }

	}
}
