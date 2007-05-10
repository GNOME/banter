
using System;

namespace Tests
{	
	public delegate void EventFinishHandler (bool error, string message);

	public interface ITest
	{		
		event EventFinishHandler TestFinished;
		bool Run ();
	}
}
