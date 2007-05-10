
using System;
using System.Collections;

namespace Tapioca.Util
{
	internal class ConfigEntry 
	{
		public object Value;
		public uint Order;
		
		public ConfigEntry (object val, uint order)
		{
			this.Value = val;
			this.Order = order;
		}
	}

	namespace Exceptions
	{
		class InvalidFile : System.Exception { }
	}
	
	public class ConfigFileReader
	{
		System.IO.TextReader file;
		System.Collections.Hashtable keys;
		
		public ConfigFileReader(string file_name)
		{	
			if (!System.IO.File.Exists (file_name))
				throw new Exceptions.InvalidFile();
				
			file = new System.IO.StreamReader(file_name);
			keys = new System.Collections.Hashtable();
			Load ();
		}
		
		public string GetValue (string key, string name)
		{
			if (!keys.ContainsKey (key)) return null;
			
			ConfigEntry entry = (ConfigEntry) keys[key];
			System.Collections.Hashtable list = (System.Collections.Hashtable) entry.Value;
			
			if(list != null)
				if(list.ContainsKey(name)) {
					ConfigEntry val = (ConfigEntry) list[name];
					return (string) val.Value;
				}
			return null;
		}
		
		public string [] GetValue (string key) 
		{
			ConfigEntry entry = (ConfigEntry) keys[key];
			if(entry != null) {
				System.Collections.Hashtable hash = (System.Collections.Hashtable) entry.Value; 
				string [] ret = new string [hash.Count];			
				foreach (DictionaryEntry item in hash) {					
					ConfigEntry v = (ConfigEntry) item.Value;
					ret[v.Order] = (string) item.Key;
				}
				return ret;
			}
			return new string [0];
		}
	
		
		public string [] GetKeys ()
		{
			string [] ret = new string [keys.Count];
			
			foreach (DictionaryEntry item in keys) {
				ConfigEntry v = (ConfigEntry) item.Value;
				ret[v.Order] = (string) item.Key;
			}
			return ret;
		}
		
		
		private void Load ()
		{
			string line;
			uint key_count = 0;
			uint entry_count = 0;
			System.Collections.Hashtable current_key = null;
			
			while ((line = file.ReadLine ()) != null)
			{				
				if (line.Length == 0)
					continue;
				if (line[0] == ';')
					continue;
				
				//remove white spaces
				int i = 0;		
				while (Char.IsWhiteSpace (line, i)) i++;				
				
				int end = 0;
				string name = "";
				if (line[0] == '[') {					
					end = line.IndexOf (']', i);
					if (end == -1) 
						throw new Exceptions.InvalidFile();
					name = line.Substring (i+1,end-i-1).Trim();
					if(!keys.ContainsKey(name)) {
						System.Collections.Hashtable aux = new System.Collections.Hashtable();
						keys[name] = new ConfigEntry (aux, key_count);
						key_count++;
						current_key = aux;
					} 
					else {
						ConfigEntry entry =  (ConfigEntry) keys[name];
						current_key = (System.Collections.Hashtable) entry.Value;
					}
					entry_count = 0;
					continue;
				}
				end = line.IndexOf ('=', i);
				if (end == -1)
					throw new Exceptions.InvalidFile();
				name = line.Substring (i, end-i).Trim();
				
				string val = "";
				if((i = line.IndexOf (';', end + 1)) != -1)
					val = line.Substring (end + 1, i - (end + 1)).Trim();
				else 
					val = line.Substring (end + 1).Trim();
				
				if (current_key != null) {
					current_key[name] = new ConfigEntry (val, entry_count);
					entry_count++;
				}
			}
		}
	}
}
