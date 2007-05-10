using System;
using System.Collections.Generic;
using Tapioca;
using Gtk;
using GLib;
using NDesk.DBus;
using Tests;

public class MainClass
{

	public static bool OnEnd ()
	{
		Application.Quit ();
		return false;		
	}

	public static void OnTestFinished (bool error, string message)
	{
		if (error)
			Console.WriteLine ("Erro: {0}", message);
		
		GLib.Idle.Add (OnEnd);		
	}


	public static void Main (string[] args)
	{
		if (args.Length < 1)
		{
			Console.WriteLine ("Usage\n program [test_name]");
			Console.WriteLine ("Avaliables tests:\n CMF - Connection Manager Factory\n CO - Connection");
			return;
		}
		Application.Init ();
    NDesk.DBus.BusG.Init ();

		ITest test;
		switch (args[0])
		{			
			case "CMF":
			{
				test = new CMTest (args);
				break;
			}
			case "CO":
			{
				test = new ConnectionTest (args);
				break;
			}
			default:
				Console.WriteLine ("Invalid test name");
				return;
		}
		test.TestFinished += OnTestFinished;
		if (!test.Run ()) {
			Console.WriteLine ("Test Fail");
			return;
		}

		Application.Run ();
	}
}
