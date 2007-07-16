////***********************************************************************
//// *   /home/calvin/code/giver/src/UriList.cs
//// *
//// *  Copyright (C) 2007 Novell, Inc.
//// *
//// *  This program is free software; you can redistribute it and/or
//// *  modify it under the terms of the GNU General Public
//// *  License as published by the Free Software Foundation; either
//// *  version 2 of the License, or (at your option) any later version.
//// *
//// *  This program is distributed in the hope that it will be useful,
//// *  but WITHOUT ANY WARRANTY; without even the implied warranty of
//// *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//// *  General Public License for more details.
//// *
//// *  You should have received a copy of the GNU General Public
//// *  License along with this program; if not, write to the Free
//// *  Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
//// *
//// **********************************************************************

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Gtk;
using Mono.Unix;

namespace Banter
{	
	public class UriList : ArrayList
	{
		private void LoadFromString (string data) {
			//string [] items = System.Text.RegularExpressions.Regex.Split ("\n", data);
			string [] items = data.Split ('\n');
			
			foreach (String i in items) {
				if (!i.StartsWith ("#")) {
					Uri uri;
					String s = i;

					if (i.EndsWith ("\r")) {
						s = i.Substring (0, i.Length - 1);
	//					Debug.PrintLine ("uri = {0}", s);
					}
					
					try {
						uri = new Uri (s);
					} catch {
						continue;
					}
					Add (uri);
				}
			}
		}

		static char[] CharsToQuote = { ';', '?', ':', '@', '&', '=', '$', ',', '#' };

		public static Uri PathToFileUri (string path)
		{
			path = Path.GetFullPath (path);

			StringBuilder builder = new StringBuilder ();
			builder.Append (Uri.UriSchemeFile);
			builder.Append (Uri.SchemeDelimiter);

			int i;
			while ((i = path.IndexOfAny (CharsToQuote)) != -1) {
				if (i > 0)
					builder.Append (path.Substring (0, i));
				builder.Append (Uri.HexEscape (path [i]));
				path = path.Substring (i+1);
			}
			builder.Append (path);

			return new Uri (builder.ToString (), true);
		}

		public UriList (string [] uris)
		{	
			// FIXME this is so lame do real chacking at some point
			foreach (string str in uris) {
				Uri uri;

				if (File.Exists (str) || Directory.Exists (str))
					uri = PathToFileUri (str);
				else 
					uri = new Uri (str);
				
				Add (uri);
			}
		}

		public UriList (string data) {
			LoadFromString (data);
		}
		
		public UriList (Gtk.SelectionData selection) 
		{
			// FIXME this should check the atom etc.
			LoadFromString (System.Text.Encoding.UTF8.GetString (selection.Data));
		}

		public override string ToString () {
			StringBuilder list = new StringBuilder ();

			foreach (Uri uri in this) {
				if (uri == null)
					break;

				list.Append (uri.ToString () + "\r\n");
			}

			return list.ToString ();
		}

		public string [] ToLocalPaths () {
			int count = 0;
			foreach (Uri uri in this) {
				if (uri.IsFile)
					count++;
			}
			
			String [] paths = new String [count];
			count = 0;
			foreach (Uri uri in this) {
				if (uri.IsFile)
					paths[count++] = uri.LocalPath;
			}
			return paths;
		}
	}
}
