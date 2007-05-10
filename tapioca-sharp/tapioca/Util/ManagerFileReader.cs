using System;
using Tapioca;

namespace Tapioca.Util
{
	public class ManagerFileReader
	{
		ConfigFileReader ini;
		System.Collections.Hashtable protocols;

		public ManagerFileReader(string file_name)
		{
			ini = new ConfigFileReader (file_name);
			protocols = new System.Collections.Hashtable ();
			Load ();
		}

		public bool IsValid ()
		{
			return ((BusName != null) && (ObjectPath != null) && (BusName.Length > 0) && (ObjectPath.Length > 0));
		}

		public string BusName
		{
			get {
				return ini.GetValue ("ConnectionManager", "BusName");
			}
		}

		public string ObjectPath
		{
			get {
				return ini.GetValue ("ConnectionManager", "ObjectPath");
			}
		}

		public string[] Protocols
		{
			get {
				string [] names = new string [protocols.Keys.Count];
				int i = 0;
				foreach (string s in protocols.Keys)
				{
					names[i] = s;
					i++;
				}
				return names;
			}
		}

		public ConnectionManagerParameter[] GetProtolsParameters (string protocol)
		{
			if (protocols.Contains (protocol)) {
				System.Collections.ArrayList vals = (System.Collections.ArrayList) protocols[protocol];
				ConnectionManagerParameter [] p = new ConnectionManagerParameter [vals.Count];
				int i = 0;
				foreach (ConnectionManagerParameter param in vals)
				{
					p[i] = param;
					i++;
				}
				return p;
			}
			return new ConnectionManagerParameter [0];
		}

		private object GetInitValue (char type)
		{
			switch (type)
			{
				case 'y':
					return (byte) 0;
				case 'b':
					return false;
				case 'n':
					return (System.Int16) 0;
				case 'q':
					return (System.UInt16) 0;
				case 'i':
					return (System.Int32) 0;
				case 'u':
					return (System.UInt32) 0;
				case 'x':
					return (System.Int64) 0;
				case 't':
					return (System.UInt64) 0;
				case 'd':
					return (System.Double) 0.0;
				case 's':
					return (System.String) "";
				default:
					return (object) "";
			}
		}

		private ConnectionManagerParameterFlags ParseFlags (string [] flags)
		{
			ConnectionManagerParameterFlags f = ConnectionManagerParameterFlags.None;

			for (int i=1; flags.Length > i; i++)
			{
				switch (flags[i])
				{
					case "required":
						f |= ConnectionManagerParameterFlags.Required;
						break;
					case "register":
						f |= ConnectionManagerParameterFlags.Register;
						break;
					default:
						break;
				}
			}
			return f;
		}

		private ConnectionManagerParameter CreateConnectionManagerParameter (string name, string flags)
		{
			string [] args = flags.Split (' ');
			return new ConnectionManagerParameter (name, GetInitValue (args[0][0]), ParseFlags (args));
		}

		private System.Collections.ArrayList ParseProtocol (string key, out string name)
		{
			name = key.Replace ("Protocol ", "");

			string [] values = ini.GetValue (key);
			System.Collections.ArrayList param_list = new System.Collections.ArrayList ();
			foreach (string s in values) {
				if (s.IndexOf ("param-") != -1) {
					string p_name = s.Replace ("param-", "").Trim ();
					param_list.Add (CreateConnectionManagerParameter (p_name, ini.GetValue (key, s)));
				}
			}

			//populate default values
			foreach (string s in values) {
				if (s.IndexOf ("default-") != -1) {
					string p_name = s.Replace ("default-", "").Trim ();
					foreach (ConnectionManagerParameter p in param_list) {
						if (p.Name == p_name) { 
							p.Value = ini.GetValue (key, s);						
							p.AddFlag (ConnectionManagerParameterFlags.HasDefault);
						}
					}
				}
			}

			return param_list;
		}

		private void Load ()
		{
			System.Collections.ArrayList details = null;
			string [] keys = ini.GetKeys ();
			string name = "";
			foreach (string v in keys) {
				if (v.IndexOf ("Protocol") != -1)
					details = ParseProtocol (v, out name);
					if (details != null) {
						if (!protocols.Contains (name))
							protocols.Add (name, details);
					}
			}
		}
	}
}
