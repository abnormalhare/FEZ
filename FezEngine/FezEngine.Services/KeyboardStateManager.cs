using System.Collections.Generic;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Services;

public class KeyboardStateManager : IKeyboardStateManager
{
	private readonly Dictionary<Keys, FezButtonState> keyStates = new Dictionary<Keys, FezButtonState>(KeysEqualityComparer.Default);

	private readonly List<Keys> registeredKeys = new List<Keys>();

	private Dictionary<MappedAction, Keys> lastMapping;

	private bool enterDown;

	private readonly List<Keys> activeKeys = new List<Keys>();

	public bool IgnoreMapping { get; set; }

	public FezButtonState Up => GetUIMapping(MappedAction.Up);

	public FezButtonState Down => GetUIMapping(MappedAction.Down);

	public FezButtonState Left => GetUIMapping(MappedAction.Left);

	public FezButtonState Right => GetUIMapping(MappedAction.Right);

	public FezButtonState Jump => GetUIMapping(MappedAction.Jump);

	public FezButtonState CancelTalk => GetUIMapping(MappedAction.CancelTalk);

	public FezButtonState Pause => GetUIMapping(MappedAction.Pause);

	public FezButtonState OpenMap => GetUIMapping(MappedAction.OpenMap);

	public FezButtonState GrabThrow => GetKeyState(lastMapping[MappedAction.GrabThrow]);

	public FezButtonState LookUp => GetKeyState(lastMapping[MappedAction.LookUp]);

	public FezButtonState LookDown => GetKeyState(lastMapping[MappedAction.LookDown]);

	public FezButtonState LookRight => GetKeyState(lastMapping[MappedAction.LookRight]);

	public FezButtonState LookLeft => GetKeyState(lastMapping[MappedAction.LookLeft]);

	public FezButtonState MapZoomIn => GetKeyState(lastMapping[MappedAction.MapZoomIn]);

	public FezButtonState MapZoomOut => GetKeyState(lastMapping[MappedAction.MapZoomOut]);

	public FezButtonState OpenInventory => GetKeyState(lastMapping[MappedAction.OpenInventory]);

	public FezButtonState RotateLeft => GetKeyState(lastMapping[MappedAction.RotateLeft]);

	public FezButtonState RotateRight => GetKeyState(lastMapping[MappedAction.RotateRight]);

	public FezButtonState FpViewToggle => GetKeyState(lastMapping[MappedAction.FpViewToggle]);

	public FezButtonState ClampLook => GetKeyState(lastMapping[MappedAction.ClampLook]);

	public KeyboardStateManager()
	{
		UpdateMapping();
	}

	public FezButtonState GetKeyState(Keys key)
	{
		if (!keyStates.TryGetValue(key, out var value))
		{
			return FezButtonState.Up;
		}
		return value;
	}

	public void RegisterKey(Keys key)
	{
		lock (this)
		{
			if (!registeredKeys.Contains(key))
			{
				registeredKeys.Add(key);
			}
		}
	}

	public void UpdateMapping()
	{
		Dictionary<MappedAction, Keys> keyboardMapping = SettingsManager.Settings.KeyboardMapping;
		if (lastMapping != null)
		{
			foreach (Keys value in lastMapping.Values)
			{
				registeredKeys.Remove(value);
			}
		}
		foreach (Keys value2 in keyboardMapping.Values)
		{
			RegisterKey(value2);
		}
		RegisterKey(Keys.Down);
		RegisterKey(Keys.Up);
		RegisterKey(Keys.Right);
		RegisterKey(Keys.Left);
		RegisterKey(Keys.Enter);
		RegisterKey(Keys.Escape);
		lastMapping = keyboardMapping;
	}

	private FezButtonState GetUIMapping(MappedAction action)
	{
		return GetKeyState(IgnoreMapping ? SettingsManager.Settings.UiKeyboardMapping[action] : lastMapping[action]);
	}

	public void Update(KeyboardState state, GameTime time)
	{
		KeyboardState state2 = Keyboard.GetState();
		activeKeys.Clear();
		lock (this)
		{
			activeKeys.AddRange(registeredKeys);
		}
		foreach (Keys activeKey in activeKeys)
		{
			bool flag = state2.IsKeyDown(activeKey);
			if (keyStates.TryGetValue(activeKey, out var value))
			{
				if (flag || value != 0)
				{
					FezButtonState fezButtonState = value.NextState(flag);
					if (fezButtonState != value)
					{
						keyStates.Remove(activeKey);
						keyStates.Add(activeKey, fezButtonState);
					}
				}
			}
			else
			{
				keyStates.Add(activeKey, value.NextState(flag));
			}
		}
		bool flag2 = state.IsKeyDown(Keys.Enter);
		if (state.IsKeyDown(Keys.LeftAlt) && flag2)
		{
			if (keyStates.ContainsKey(Keys.Enter))
			{
				keyStates[Keys.Enter] = FezButtonState.Up;
			}
			if (!enterDown)
			{
				SettingsManager.DeviceManager.ToggleFullScreen();
				SettingsManager.Settings.ScreenMode = (SettingsManager.DeviceManager.IsFullScreen ? ScreenMode.Fullscreen : ScreenMode.Windowed);
			}
		}
		enterDown = flag2;
	}
}
