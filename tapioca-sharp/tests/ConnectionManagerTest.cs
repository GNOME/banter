using System;
using Tapioca;

namespace Tests
{
	public class CMTest : ITest
	{
		event EventFinishHandler TestFinished;

		public CMTest (string [] args)
		{
		}

		public bool Run ()
		{
			ConnectionManagerFactory cm_factory = new ConnectionManagerFactory ();
			ConnectionManager[] cms = cm_factory.AllConnectionManagers;
			foreach (ConnectionManager cm in cms)
			{
				Console.WriteLine ("CM name: {0} Running: {1} ", cm.Name, cm.IsRunning);
				foreach (string proto in cm.SupportedProtocols) {
					Console.WriteLine ("PROTO {0}", proto);
					foreach (ConnectionManagerParameter p in cm.ProtocolConnectionManagerParameters (proto)) {
						Console.WriteLine ("PARAM name: {0} type: {1} val: {2} flag: {3}", p.Name,p.Value.GetType (), p.Value, p.Flags);
					}
				}

			}
			if (TestFinished != null)
				TestFinished (false, "");

			return true;
		}

		event EventFinishHandler ITest.TestFinished
        {
            add { TestFinished += value; }
            remove { TestFinished -= value; }
        }
	}
}
