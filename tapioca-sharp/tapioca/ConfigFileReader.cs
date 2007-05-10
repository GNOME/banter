/***************************************************************************
 *  ConfigFileReader.cs
 *
 *  Copyright (C) 2006 INdT
 *  Written by
 *      Andre Moreira Magalhaes <andre.magalhaes@indt.org.br>
 *      Kenneth Christiansen <kenneth.christiansen@gmail.com>
 *      Renato Araujo Oliveira Filho <renato.filho@indt.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the Software),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */


using System;

namespace Tapioca
{
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
			file = new System.IO.StreamReader(file_name);
			keys = new System.Collections.Hashtable();
			Load ();
		}
		
		public string GetValue (string key, string name)
		{
			System.Collections.Hashtable aux = (System.Collections.Hashtable) keys[key];
			if(aux != null)
				if(aux.ContainsKey(name))
					return (string) aux[name];
			return null;
		}
		
		private void Load ()
		{
			string line;
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
				if (line[0] == '[') {					
					end = line.IndexOf (']', i);
					if (end == -1) 
						throw new Exceptions.InvalidFile();
					string name = line.Substring (i+1,end-i-1).Trim();
					if(!keys.ContainsKey(name)) {
						System.Collections.Hashtable aux = new System.Collections.Hashtable();
						keys[name] = aux;
						current_key = aux;
					} 
					else {
						current_key = (System.Collections.Hashtable) keys[name];
					}
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
				
				if (curren_key != null)
					current_key[name] = val;
			}
		}
	}
}
