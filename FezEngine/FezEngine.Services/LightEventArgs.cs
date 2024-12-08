using System;

namespace FezEngine.Services;

public class LightEventArgs : EventArgs
{
	private readonly int lightNumber;

	public int LightNumber => lightNumber;

	public LightEventArgs(int lightNumber)
	{
		this.lightNumber = lightNumber;
	}
}
