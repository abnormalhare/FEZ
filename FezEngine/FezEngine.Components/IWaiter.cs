using System;

namespace FezEngine.Components;

public interface IWaiter
{
	bool Alive { get; }

	object Tag { get; set; }

	bool AutoPause { get; set; }

	Func<bool> CustomPause { get; set; }

	void Cancel();
}
