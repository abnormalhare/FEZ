using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Input;

namespace FezGame.Tools;

public struct LocalizedKeyboardState
{
	public class KeyboardLayout : IDisposable
	{
		public static KeyboardLayout US_English = new KeyboardLayout("00000409");

		public readonly IntPtr Handle;

		public bool IsDisposed { get; private set; }

		public static KeyboardLayout Active => new KeyboardLayout(GetKeyboardLayout(IntPtr.Zero));

		public KeyboardLayout(IntPtr handle)
		{
			Handle = handle;
		}

		public KeyboardLayout(string keyboardLayoutID)
			: this(LoadKeyboardLayout(keyboardLayoutID, 128u))
		{
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				UnloadKeyboardLayout(Handle);
				IsDisposed = true;
			}
		}

		~KeyboardLayout()
		{
			Dispose(disposing: false);
		}
	}

	internal enum MAPVK : uint
	{
		VK_TO_VSC,
		VSC_TO_VK,
		VK_TO_CHAR
	}

	internal const uint KLF_NOTELLSHELL = 128u;

	public readonly KeyboardState Native;

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern uint MapVirtualKeyEx(uint key, MAPVK mappingType, IntPtr keyboardLayout);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern IntPtr LoadKeyboardLayout(string keyboardLayoutID, uint flags);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern bool UnloadKeyboardLayout(IntPtr handle);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern IntPtr GetKeyboardLayout(IntPtr threadId);

	public static Keys USEnglishToLocal(Keys key)
	{
		return (Keys)MapVirtualKeyEx(MapVirtualKeyEx((uint)key, MAPVK.VK_TO_VSC, KeyboardLayout.US_English.Handle), MAPVK.VSC_TO_VK, KeyboardLayout.Active.Handle);
	}
}
