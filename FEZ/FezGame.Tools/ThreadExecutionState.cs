using System;
using System.Runtime.InteropServices;

namespace FezGame.Tools;

internal static class ThreadExecutionState
{
	[Flags]
	public enum EXECUTION_STATE : uint
	{
		ES_AWAYMODE_REQUIRED = 0x40u,
		ES_CONTINUOUS = 0x80000000u,
		ES_DISPLAY_REQUIRED = 2u,
		ES_SYSTEM_REQUIRED = 1u
	}

	public enum SPI : uint
	{
		SPI_GETSCREENSAVEACTIVE = 16u,
		SPI_SETSCREENSAVEACTIVE
	}

	public enum SPIF : uint
	{
		None = 0u,
		SPIF_UPDATEINIFILE = 1u,
		SPIF_SENDCHANGE = 2u,
		SPIF_SENDWININICHANGE = 2u
	}

	private static bool screenSaverWasEnabled;

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, ref uint pvParam, SPIF fWinIni);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SystemParametersInfo(SPI uiAction, uint uiParam, uint pvParam, SPIF fWinIni);

	public static void SetUp()
	{
		SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
		uint pvParam = 0u;
		if (SystemParametersInfo(SPI.SPI_GETSCREENSAVEACTIVE, 0u, ref pvParam, SPIF.None))
		{
			if (pvParam == 1)
			{
				screenSaverWasEnabled = true;
			}
			else
			{
				screenSaverWasEnabled = false;
			}
		}
		if (screenSaverWasEnabled)
		{
			SystemParametersInfo(SPI.SPI_SETSCREENSAVEACTIVE, 0u, 0u, SPIF.None);
		}
	}

	public static void TearDown()
	{
		if (screenSaverWasEnabled)
		{
			SystemParametersInfo(SPI.SPI_SETSCREENSAVEACTIVE, 1u, 0u, SPIF.None);
		}
	}
}
