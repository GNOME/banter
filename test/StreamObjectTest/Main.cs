// project created on 3/15/2007 at 10:31 AM
using System;
using Gtk;

namespace StreamObjectTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Show ();
			Application.Run ();
		}
	}
}