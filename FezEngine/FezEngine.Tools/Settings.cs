using System.Collections.Generic;
using ContentSerialization.Attributes;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Tools;

public class Settings
{
	public bool UseCurrentMode { get; set; }

	public ScreenMode ScreenMode { get; set; }

	[Serialization(Optional = true)]
	public ScaleMode ScaleMode { get; set; }

	public int Width { get; set; }

	public int Height { get; set; }

	[Serialization(Optional = true)]
	public bool HighDPI { get; set; }

	public Language Language { get; set; }

	public float SoundVolume { get; set; }

	public float MusicVolume { get; set; }

	[Serialization(Optional = true)]
	public bool Vibration { get; set; }

	[Serialization(Optional = true)]
	public bool PauseOnLostFocus { get; set; }

	[Serialization(Optional = true)]
	public bool Singlethreaded { get; set; }

	[Serialization(Optional = true)]
	public float Brightness { get; set; }

	public Dictionary<MappedAction, Keys> KeyboardMapping { get; set; }

	public Dictionary<MappedAction, Buttons> ControllerMapping { get; set; }

	[Serialization(Optional = true)]
	public Dictionary<MappedAction, Keys> UiKeyboardMapping { get; set; }

	[Serialization(Optional = true)]
	public int DeadZone { get; set; }

	[Serialization(Optional = true)]
	public bool DisableController { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool InvertMouse
	{
		get
		{
			return false;
		}
		set
		{
			InvertLook = value;
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool InvertLook
	{
		get
		{
			return false;
		}
		set
		{
			bool invertLookX = (InvertLookY = value);
			InvertLookX = invertLookX;
		}
	}

	[Serialization(Optional = true)]
	public bool InvertLookX { get; set; }

	[Serialization(Optional = true)]
	public bool InvertLookY { get; set; }

	[Serialization(Optional = true)]
	public bool VSync { get; set; }

	[Serialization(Optional = true)]
	public bool HardwareInstancing { get; set; }

	[Serialization(Optional = true)]
	public int MultiSampleCount { get; set; }

	[Serialization(Optional = true)]
	public bool MultiSampleOption { get; set; }

	[Serialization(Optional = true)]
	public bool Lighting { get; set; }

	[Serialization(Optional = true)]
	public bool DisableSteamworks { get; set; }

	public Settings()
	{
		KeyboardMapping = new Dictionary<MappedAction, Keys>();
		ControllerMapping = new Dictionary<MappedAction, Buttons>();
		UiKeyboardMapping = new Dictionary<MappedAction, Keys>();
		RevertToDefaults();
	}

	public void RevertToDefaults()
	{
		DisplayMode currentDisplayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
		ScreenMode = ScreenMode.Fullscreen;
		ScaleMode = ScaleMode.FullAspect;
		Width = currentDisplayMode.Width;
		Height = currentDisplayMode.Height;
		HighDPI = false;
		Language = Culture.Language;
		Brightness = 0.5f;
		SoundVolume = 1f;
		MusicVolume = 1f;
		Vibration = true;
		PauseOnLostFocus = true;
		Singlethreaded = false;
		InvertMouse = false;
		DeadZone = 40;
		DisableController = false;
		VSync = true;
		HardwareInstancing = true;
		MultiSampleCount = 0;
		MultiSampleOption = false;
		Lighting = true;
		DisableSteamworks = false;
		ResetMapping();
	}

	public void ResetMapping(bool forKeyboard = true, bool forGamepad = true)
	{
		if (forKeyboard)
		{
			KeyboardMapping[MappedAction.Jump] = Keyboard.GetKeyFromScancodeEXT(Keys.Space);
			KeyboardMapping[MappedAction.GrabThrow] = Keyboard.GetKeyFromScancodeEXT(Keys.LeftControl);
			KeyboardMapping[MappedAction.CancelTalk] = Keyboard.GetKeyFromScancodeEXT(Keys.LeftShift);
			KeyboardMapping[MappedAction.Up] = Keyboard.GetKeyFromScancodeEXT(Keys.Up);
			KeyboardMapping[MappedAction.Down] = Keyboard.GetKeyFromScancodeEXT(Keys.Down);
			KeyboardMapping[MappedAction.Left] = Keyboard.GetKeyFromScancodeEXT(Keys.Left);
			KeyboardMapping[MappedAction.Right] = Keyboard.GetKeyFromScancodeEXT(Keys.Right);
			KeyboardMapping[MappedAction.LookUp] = Keyboard.GetKeyFromScancodeEXT(Keys.I);
			KeyboardMapping[MappedAction.LookDown] = Keyboard.GetKeyFromScancodeEXT(Keys.K);
			KeyboardMapping[MappedAction.LookRight] = Keyboard.GetKeyFromScancodeEXT(Keys.L);
			KeyboardMapping[MappedAction.LookLeft] = Keyboard.GetKeyFromScancodeEXT(Keys.J);
			KeyboardMapping[MappedAction.OpenMap] = Keyboard.GetKeyFromScancodeEXT(Keys.Escape);
			KeyboardMapping[MappedAction.OpenInventory] = Keyboard.GetKeyFromScancodeEXT(Keys.Tab);
			KeyboardMapping[MappedAction.MapZoomIn] = Keyboard.GetKeyFromScancodeEXT(Keys.W);
			KeyboardMapping[MappedAction.MapZoomOut] = Keyboard.GetKeyFromScancodeEXT(Keys.S);
			KeyboardMapping[MappedAction.Pause] = Keyboard.GetKeyFromScancodeEXT(Keys.Enter);
			KeyboardMapping[MappedAction.RotateLeft] = Keyboard.GetKeyFromScancodeEXT(Keys.A);
			KeyboardMapping[MappedAction.RotateRight] = Keyboard.GetKeyFromScancodeEXT(Keys.D);
			KeyboardMapping[MappedAction.FpViewToggle] = Keyboard.GetKeyFromScancodeEXT(Keys.RightAlt);
			KeyboardMapping[MappedAction.ClampLook] = Keyboard.GetKeyFromScancodeEXT(Keys.RightShift);
		}
		if (forGamepad)
		{
			ControllerMapping[MappedAction.Jump] = Buttons.A;
			ControllerMapping[MappedAction.GrabThrow] = Buttons.X;
			ControllerMapping[MappedAction.CancelTalk] = Buttons.B;
			ControllerMapping[MappedAction.OpenMap] = Buttons.Back;
			ControllerMapping[MappedAction.OpenInventory] = Buttons.Y;
			ControllerMapping[MappedAction.MapZoomIn] = Buttons.RightShoulder;
			ControllerMapping[MappedAction.MapZoomOut] = Buttons.LeftShoulder;
			ControllerMapping[MappedAction.Pause] = Buttons.Start;
			ControllerMapping[MappedAction.RotateLeft] = Buttons.LeftTrigger;
			ControllerMapping[MappedAction.RotateRight] = Buttons.RightTrigger;
			ControllerMapping[MappedAction.FpViewToggle] = Buttons.LeftStick;
			ControllerMapping[MappedAction.ClampLook] = Buttons.RightStick;
		}
		UiKeyboardMapping[MappedAction.Up] = Keyboard.GetKeyFromScancodeEXT(Keys.Up);
		UiKeyboardMapping[MappedAction.Down] = Keyboard.GetKeyFromScancodeEXT(Keys.Down);
		UiKeyboardMapping[MappedAction.Left] = Keyboard.GetKeyFromScancodeEXT(Keys.Left);
		UiKeyboardMapping[MappedAction.Right] = Keyboard.GetKeyFromScancodeEXT(Keys.Right);
		UiKeyboardMapping[MappedAction.Jump] = Keyboard.GetKeyFromScancodeEXT(Keys.Enter);
		UiKeyboardMapping[MappedAction.CancelTalk] = Keyboard.GetKeyFromScancodeEXT(Keys.Escape);
		UiKeyboardMapping[MappedAction.Pause] = Keyboard.GetKeyFromScancodeEXT(Keys.Enter);
		UiKeyboardMapping[MappedAction.OpenMap] = Keyboard.GetKeyFromScancodeEXT(Keys.Escape);
		UiKeyboardMapping[MappedAction.GrabThrow] = Keyboard.GetKeyFromScancodeEXT(Keys.LeftControl);
		UiKeyboardMapping[MappedAction.LookUp] = Keyboard.GetKeyFromScancodeEXT(Keys.I);
		UiKeyboardMapping[MappedAction.LookDown] = Keyboard.GetKeyFromScancodeEXT(Keys.K);
		UiKeyboardMapping[MappedAction.LookRight] = Keyboard.GetKeyFromScancodeEXT(Keys.L);
		UiKeyboardMapping[MappedAction.LookLeft] = Keyboard.GetKeyFromScancodeEXT(Keys.J);
		UiKeyboardMapping[MappedAction.OpenInventory] = Keyboard.GetKeyFromScancodeEXT(Keys.Tab);
		UiKeyboardMapping[MappedAction.MapZoomIn] = Keyboard.GetKeyFromScancodeEXT(Keys.W);
		UiKeyboardMapping[MappedAction.MapZoomOut] = Keyboard.GetKeyFromScancodeEXT(Keys.S);
		UiKeyboardMapping[MappedAction.RotateLeft] = Keyboard.GetKeyFromScancodeEXT(Keys.A);
		UiKeyboardMapping[MappedAction.RotateRight] = Keyboard.GetKeyFromScancodeEXT(Keys.D);
		UiKeyboardMapping[MappedAction.FpViewToggle] = Keyboard.GetKeyFromScancodeEXT(Keys.RightAlt);
		UiKeyboardMapping[MappedAction.ClampLook] = Keyboard.GetKeyFromScancodeEXT(Keys.RightShift);
	}
}
