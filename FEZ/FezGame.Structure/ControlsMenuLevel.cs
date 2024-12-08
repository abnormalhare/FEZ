using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Services;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FezGame.Structure;

internal class ControlsMenuLevel : MenuLevel
{
	private enum ArrowKeyMapping
	{
		WASD,
		ZQSD,
		IJKL,
		ESDF,
		Arrows
	}

	private readonly MenuBase menuBase;

	private SoundEffect sSliderValueIncrease;

	private SoundEffect sSliderValueDecrease;

	private int chosen;

	private MenuItem keyGrabFor;

	private HashSet<Keys> lastPressed = new HashSet<Keys>();

	private HashSet<Keys> thisPressed = new HashSet<Keys>();

	private readonly HashSet<Keys> keysDown = new HashSet<Keys>();

	private GamePadButtons lastButton = new GamePadButtons((Buttons)0);

	private GamePadButtons thisButton = new GamePadButtons((Buttons)0);

	private bool forGamepad;

	private bool noArrows;

	private bool mappedButton;

	private MenuItem gamepadFPItem;

	private MenuItem keyboardFPItem;

	private int keyboardStart;

	private int selectorStart;

	private Rectangle? leftSliderRect;

	private Rectangle? rightSliderRect;

	private static readonly MappedAction[] KeyboardActionOrder = new MappedAction[14]
	{
		MappedAction.Jump,
		MappedAction.GrabThrow,
		MappedAction.CancelTalk,
		MappedAction.Up,
		MappedAction.LookUp,
		MappedAction.OpenMap,
		MappedAction.OpenInventory,
		MappedAction.Pause,
		MappedAction.RotateLeft,
		MappedAction.RotateRight,
		MappedAction.ClampLook,
		MappedAction.MapZoomIn,
		MappedAction.MapZoomOut,
		MappedAction.FpViewToggle
	};

	private static readonly MappedAction[] GamepadActionOrder = new MappedAction[12]
	{
		MappedAction.Jump,
		MappedAction.GrabThrow,
		MappedAction.CancelTalk,
		MappedAction.OpenInventory,
		MappedAction.RotateLeft,
		MappedAction.RotateRight,
		MappedAction.MapZoomIn,
		MappedAction.MapZoomOut,
		MappedAction.OpenMap,
		MappedAction.Pause,
		MappedAction.ClampLook,
		MappedAction.FpViewToggle
	};

	private static readonly string[] GamepadButtonOrder = new string[12]
	{
		"{A}", "{X}", "{B}", "{Y}", "{LT}", "{RT}", "{RB}", "{LB}", "{BACK}", "{START}",
		"{RS}", "{LS}"
	};

	public override string AButtonString
	{
		get
		{
			if (!base.SelectedItem.IsSlider && base.SelectedIndex != chosen)
			{
				if (base.SelectedItem.SuffixText == null)
				{
					return StaticText.GetString("MenuApplyWithGlyph");
				}
				return StaticText.GetString("ChangeWithGlyph");
			}
			return null;
		}
	}

	public IFontManager FontManager { private get; set; }

	public IInputManager InputManager { private get; set; }

	public IKeyboardStateManager KeyboardManager { private get; set; }

	public IMouseStateManager MouseState { private get; set; }

	public IGameStateManager GameState { private get; set; }

	private void ToggleVibration()
	{
		SettingsManager.Settings.Vibration = !SettingsManager.Settings.Vibration;
	}

	public ControlsMenuLevel(MenuBase menuBase)
	{
		InputManager = ServiceHelper.Get<IInputManager>();
		GameState = ServiceHelper.Get<IGameStateManager>();
		KeyboardManager = ServiceHelper.Get<IKeyboardStateManager>();
		MouseState = ServiceHelper.Get<IMouseStateManager>();
		this.menuBase = menuBase;
		IsDynamic = true;
		Dictionary<MappedAction, Keys> kmap = SettingsManager.Settings.KeyboardMapping;
		MenuItem gjmi = AddItem("ControlsJump");
		gjmi.Selected = delegate
		{
			ChangeButton(gjmi);
		};
		gjmi.SuffixText = () => " : {A}";
		MenuItem gami = AddItem("ControlsAction");
		gami.Selected = delegate
		{
			ChangeButton(gami);
		};
		gami.SuffixText = () => " : {X}";
		MenuItem gtmi = AddItem("ControlsTalk");
		gtmi.Selected = delegate
		{
			ChangeButton(gtmi);
		};
		gtmi.SuffixText = () => " : {B}";
		MenuItem gimi = AddItem("ControlsInventory");
		gimi.Selected = delegate
		{
			ChangeButton(gimi);
		};
		gimi.SuffixText = () => " : {Y}";
		MenuItem grlmi = AddItem("ControlsRotateLeft");
		grlmi.Selected = delegate
		{
			ChangeButton(grlmi);
		};
		grlmi.SuffixText = () => " : {LT}";
		MenuItem grrmi = AddItem("ControlsRotateRight");
		grrmi.Selected = delegate
		{
			ChangeButton(grrmi);
		};
		grrmi.SuffixText = () => " : {RT}";
		MenuItem gmzimi = AddItem("ControlsMapZoomIn");
		gmzimi.Selected = delegate
		{
			ChangeButton(gmzimi);
		};
		gmzimi.SuffixText = () => " : {RB}";
		MenuItem gmzomi = AddItem("ControlsZoomOut");
		gmzomi.Selected = delegate
		{
			ChangeButton(gmzomi);
		};
		gmzomi.SuffixText = () => " : {LB}";
		MenuItem gmami = AddItem("Map_Title");
		gmami.Selected = delegate
		{
			ChangeButton(gmami);
		};
		gmami.SuffixText = () => " : {BACK}";
		MenuItem gpmi = AddItem("ControlsPause");
		gpmi.Selected = delegate
		{
			ChangeButton(gpmi);
		};
		gpmi.SuffixText = () => " : {START}";
		MenuItem gclmi = AddItem("ControlsClampLook");
		gclmi.Selected = delegate
		{
			ChangeButton(gclmi);
		};
		gclmi.SuffixText = () => " : {RS}";
		gamepadFPItem = AddItem(null);
		gamepadFPItem.Selected = delegate
		{
			ChangeButton(gamepadFPItem);
		};
		gamepadFPItem.Selectable = false;
		AddItem(null, MenuBase.SliderAction).Selectable = false;
		AddItem("Vibration", delegate
		{
		}, defaultItem: false, () => (!SettingsManager.Settings.Vibration) ? StaticText.GetString("Off") : StaticText.GetString("On"), delegate
		{
			ToggleVibration();
		});
		AddItem("DeadZone", delegate
		{
		}, defaultItem: false, () => SettingsManager.Settings.DeadZone + "%", delegate(string _, int diff)
		{
			SettingsManager.Settings.DeadZone += 10 * diff;
			if (SettingsManager.Settings.DeadZone < 0)
			{
				SettingsManager.Settings.DeadZone = 0;
			}
			else if (SettingsManager.Settings.DeadZone > 90)
			{
				SettingsManager.Settings.DeadZone = 90;
			}
		});
		AddItem("ResetToDefault", delegate
		{
			ResetToDefault(forKeyboard: false, forGamepad: true);
		});
		keyboardStart = Items.Count;
		MenuItem jmi = AddItem("ControlsJump");
		jmi.Selected = delegate
		{
			ChangeKey(jmi);
		};
		jmi.SuffixText = () => " : " + Localize(kmap[MappedAction.Jump]);
		MenuItem ami = AddItem("ControlsAction");
		ami.Selected = delegate
		{
			ChangeKey(ami);
		};
		ami.SuffixText = () => " : " + Localize(kmap[MappedAction.GrabThrow]);
		MenuItem tmi = AddItem("ControlsTalk");
		tmi.Selected = delegate
		{
			ChangeKey(tmi);
		};
		tmi.SuffixText = () => " : " + Localize(kmap[MappedAction.CancelTalk]);
		AddItem("ControlsMove", MenuBase.SliderAction, defaultItem: false, () => UpToAKM(kmap[MappedAction.Up]), delegate(ArrowKeyMapping lastValue, int change)
		{
			ArrowKeyMapping arrowKeyMapping = UpToAKM(kmap[MappedAction.Up]);
			arrowKeyMapping += change;
			if (arrowKeyMapping == (ArrowKeyMapping)5)
			{
				arrowKeyMapping = ArrowKeyMapping.WASD;
			}
			if (arrowKeyMapping < ArrowKeyMapping.WASD)
			{
				arrowKeyMapping = ArrowKeyMapping.Arrows;
			}
			kmap[MappedAction.Up] = AKMToKey(arrowKeyMapping, 0);
			kmap[MappedAction.Left] = AKMToKey(arrowKeyMapping, 1);
			kmap[MappedAction.Down] = AKMToKey(arrowKeyMapping, 2);
			kmap[MappedAction.Right] = AKMToKey(arrowKeyMapping, 3);
			KeyboardManager.UpdateMapping();
			ValidateKeyCollision();
		}).SuffixText = () => " : " + Localize(UpToAKM(kmap[MappedAction.Up]));
		AddItem("ControlsLook", MenuBase.SliderAction, defaultItem: false, () => UpToAKM(kmap[MappedAction.LookUp]), delegate(ArrowKeyMapping lastValue, int change)
		{
			ArrowKeyMapping arrowKeyMapping2 = UpToAKM(kmap[MappedAction.LookUp]);
			arrowKeyMapping2 += change;
			if (arrowKeyMapping2 == (ArrowKeyMapping)5)
			{
				arrowKeyMapping2 = ArrowKeyMapping.WASD;
			}
			if (arrowKeyMapping2 < ArrowKeyMapping.WASD)
			{
				arrowKeyMapping2 = ArrowKeyMapping.Arrows;
			}
			kmap[MappedAction.LookUp] = AKMToKey(arrowKeyMapping2, 0);
			kmap[MappedAction.LookLeft] = AKMToKey(arrowKeyMapping2, 1);
			kmap[MappedAction.LookDown] = AKMToKey(arrowKeyMapping2, 2);
			kmap[MappedAction.LookRight] = AKMToKey(arrowKeyMapping2, 3);
			KeyboardManager.UpdateMapping();
			ValidateKeyCollision();
		}).SuffixText = () => " : " + Localize(UpToAKM(kmap[MappedAction.LookUp]));
		MenuItem mami = AddItem("Map_Title");
		mami.Selected = delegate
		{
			ChangeKey(mami);
		};
		mami.SuffixText = () => " : " + Localize(kmap[MappedAction.OpenMap]);
		MenuItem imi = AddItem("ControlsInventory");
		imi.Selected = delegate
		{
			ChangeKey(imi);
		};
		imi.SuffixText = () => " : " + Localize(kmap[MappedAction.OpenInventory]);
		MenuItem pmi = AddItem("ControlsPause");
		pmi.Selected = delegate
		{
			ChangeKey(pmi);
		};
		pmi.SuffixText = () => " : " + Localize(kmap[MappedAction.Pause]);
		MenuItem rlmi = AddItem("ControlsRotateLeft");
		rlmi.Selected = delegate
		{
			ChangeKey(rlmi);
		};
		rlmi.SuffixText = () => " : " + Localize(kmap[MappedAction.RotateLeft]);
		MenuItem rrmi = AddItem("ControlsRotateRight");
		rrmi.Selected = delegate
		{
			ChangeKey(rrmi);
		};
		rrmi.SuffixText = () => " : " + Localize(kmap[MappedAction.RotateRight]);
		MenuItem clmi = AddItem("ControlsClampLook");
		clmi.Selected = delegate
		{
			ChangeKey(clmi);
		};
		clmi.SuffixText = () => " : " + Localize(kmap[MappedAction.ClampLook]);
		MenuItem mzimi = AddItem("ControlsMapZoomIn");
		mzimi.Selected = delegate
		{
			ChangeKey(mzimi);
		};
		mzimi.SuffixText = () => " : " + Localize(kmap[MappedAction.MapZoomIn]);
		MenuItem mzomi = AddItem("ControlsZoomOut");
		mzomi.Selected = delegate
		{
			ChangeKey(mzomi);
		};
		mzomi.SuffixText = () => " : " + Localize(kmap[MappedAction.MapZoomOut]);
		keyboardFPItem = AddItem(null);
		keyboardFPItem.Selected = delegate
		{
			ChangeKey(keyboardFPItem);
		};
		keyboardFPItem.Selectable = false;
		AddItem(null, MenuBase.SliderAction).Selectable = false;
		AddItem("ResetToDefault", delegate
		{
			ResetToDefault(forKeyboard: true, forGamepad: false);
		});
		selectorStart = Items.Count;
		AddItem("Controller", MenuBase.SliderAction, defaultItem: true).UpperCase = true;
		AddItem("Keyboard", MenuBase.SliderAction, defaultItem: true).UpperCase = true;
	}

	public override void Reset()
	{
		base.Reset();
		Items[selectorStart].Hidden = true;
		Items[selectorStart].Selectable = false;
		Items[selectorStart + 1].Hidden = true;
		Items[selectorStart + 1].Selectable = false;
		chosen = Items.Count - 1;
		FakeSlideRight(silent: true);
		noArrows = !GamepadState.AnyConnected;
		if (GameState.SaveData.HasFPView)
		{
			gamepadFPItem.Text = "ControlsToggleFpView";
			keyboardFPItem.Text = "ControlsToggleFpView";
			gamepadFPItem.SuffixText = () => " : {LS}";
			keyboardFPItem.SuffixText = () => " : " + Localize(SettingsManager.Settings.KeyboardMapping[MappedAction.FpViewToggle]);
			gamepadFPItem.Selectable = true;
			keyboardFPItem.Selectable = true;
		}
	}

	private void ResetToDefault(bool forKeyboard, bool forGamepad)
	{
		SettingsManager.Settings.ResetMapping(forKeyboard, forGamepad);
		if (forGamepad)
		{
			SettingsManager.Settings.Vibration = false;
			ToggleVibration();
			SettingsManager.Settings.DeadZone = 40;
		}
		if (forKeyboard)
		{
			ValidateKeyCollision();
		}
	}

	private void ChangeButton(MenuItem mi)
	{
		if (base.TrapInput)
		{
			return;
		}
		if (mi != gamepadFPItem || GameState.SaveData.HasFPView)
		{
			mi.SuffixText = () => " : " + StaticText.GetString("ChangeGamepadMapping");
		}
		keyGrabFor = mi;
		base.TrapInput = true;
		forGamepad = true;
	}

	private void ChangeKey(MenuItem mi)
	{
		if (base.TrapInput)
		{
			return;
		}
		if (mi != keyboardFPItem || GameState.SaveData.HasFPView)
		{
			mi.SuffixText = () => " : " + StaticText.GetString("ChangeMapping");
		}
		keyGrabFor = mi;
		base.TrapInput = true;
		forGamepad = false;
		lastPressed.Clear();
		keysDown.Clear();
		Keys[] pressedKeys = Keyboard.GetState().GetPressedKeys();
		foreach (Keys item in pressedKeys)
		{
			lastPressed.Add(item);
		}
	}

	private string Localize(object input)
	{
		string text = input.ToString();
		if (text.ToCharArray().All(char.IsUpper))
		{
			return text;
		}
		if (text.StartsWith("D") && text.Length == 2 && char.IsNumber(text[1]))
		{
			return text[1].ToString();
		}
		text = text.Replace("Oem", string.Empty);
		if (StaticText.TryGetString("Keyboard" + text, out var text2))
		{
			return text2;
		}
		return Regex.Replace(text, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
	}

	private ArrowKeyMapping UpToAKM(Keys key)
	{
		return key switch
		{
			Keys.W => ArrowKeyMapping.WASD, 
			Keys.E => ArrowKeyMapping.ESDF, 
			Keys.I => ArrowKeyMapping.IJKL, 
			Keys.Z => ArrowKeyMapping.ZQSD, 
			_ => ArrowKeyMapping.Arrows, 
		};
	}

	private Keys AKMToKey(ArrowKeyMapping akm, int i)
	{
		return akm switch
		{
			ArrowKeyMapping.WASD => i switch
			{
				0 => Keys.W, 
				1 => Keys.A, 
				2 => Keys.S, 
				_ => Keys.D, 
			}, 
			ArrowKeyMapping.ESDF => i switch
			{
				0 => Keys.E, 
				1 => Keys.S, 
				2 => Keys.D, 
				_ => Keys.F, 
			}, 
			ArrowKeyMapping.IJKL => i switch
			{
				0 => Keys.I, 
				1 => Keys.J, 
				2 => Keys.K, 
				_ => Keys.L, 
			}, 
			ArrowKeyMapping.ZQSD => i switch
			{
				0 => Keys.Z, 
				1 => Keys.Q, 
				2 => Keys.S, 
				_ => Keys.D, 
			}, 
			_ => i switch
			{
				0 => Keys.Up, 
				1 => Keys.Left, 
				2 => Keys.Down, 
				_ => Keys.Right, 
			}, 
		};
	}

	public override void Update(TimeSpan elapsed)
	{
		base.Update(elapsed);
		lastButton = thisButton;
		if (GamepadState.AnyConnected)
		{
			noArrows = false;
			thisButton = GamePad.GetState(InputManager.ActiveGamepad.PlayerIndex).Buttons;
		}
		else
		{
			noArrows = true;
			if (base.SelectedIndex == selectorStart)
			{
				FakeSlideRight(silent: true);
			}
		}
		if (mappedButton)
		{
			base.TrapInput = false;
			mappedButton = false;
		}
		if (base.TrapInput)
		{
			if (forGamepad)
			{
				if (KeyboardManager.GetKeyState(Keys.Escape) == FezButtonState.Pressed)
				{
					if (keyGrabFor != gamepadFPItem && GameState.SaveData.HasFPView)
					{
						keyGrabFor.SuffixText = () => " : " + GamepadButtonOrder[Items.IndexOf(keyGrabFor)];
					}
					base.TrapInput = false;
					forGamepad = false;
				}
				else if (thisButton != lastButton)
				{
					int num = thisButton.GetHashCode() & 0xC0F3F0;
					if (num != 0)
					{
						int j = Items.IndexOf(keyGrabFor);
						MappedAction mappedAction = GamepadActionOrder[j];
						Dictionary<MappedAction, Buttons> controllerMapping = SettingsManager.Settings.ControllerMapping;
						Buttons value = controllerMapping[mappedAction];
						if (thisButton.Start == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.Start;
						}
						else if (thisButton.Back == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.Back;
						}
						else if (thisButton.LeftStick == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.LeftStick;
						}
						else if (thisButton.RightStick == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.RightStick;
						}
						else if (thisButton.LeftShoulder == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.LeftShoulder;
						}
						else if (thisButton.RightShoulder == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.RightShoulder;
						}
						else if (thisButton.A == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.A;
						}
						else if (thisButton.B == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.B;
						}
						else if (thisButton.X == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.X;
						}
						else if (thisButton.Y == ButtonState.Pressed)
						{
							controllerMapping[mappedAction] = Buttons.Y;
						}
						else if ((num & 0x400000) == 4194304)
						{
							controllerMapping[mappedAction] = Buttons.RightTrigger;
						}
						else
						{
							if ((num & 0x800000) != 8388608)
							{
								throw new InvalidOperationException("How did you get here...?");
							}
							controllerMapping[mappedAction] = Buttons.LeftTrigger;
						}
						if (keyGrabFor != gamepadFPItem || GameState.SaveData.HasFPView)
						{
							keyGrabFor.SuffixText = () => " : " + GamepadButtonOrder[j];
						}
						MappedAction? mappedAction2 = null;
						foreach (KeyValuePair<MappedAction, Buttons> item2 in controllerMapping)
						{
							if (item2.Value == controllerMapping[mappedAction] && item2.Key != mappedAction)
							{
								mappedAction2 = item2.Key;
								break;
							}
						}
						if (mappedAction2.HasValue)
						{
							controllerMapping[mappedAction2.Value] = value;
							int k;
							for (k = 0; k < GamepadActionOrder.Length; k++)
							{
								if (GamepadActionOrder[k] != mappedAction2.Value)
								{
									continue;
								}
								if (GamepadActionOrder[k] != MappedAction.FpViewToggle || GameState.SaveData.HasFPView)
								{
									Items[k].SuffixText = () => " : " + GamepadButtonOrder[k];
								}
								break;
							}
						}
						mappedButton = true;
						forGamepad = false;
					}
				}
			}
			else
			{
				thisPressed.Clear();
				Keys[] pressedKeys = Keyboard.GetState().GetPressedKeys();
				foreach (Keys item in pressedKeys)
				{
					thisPressed.Add(item);
				}
				foreach (Keys item3 in keysDown)
				{
					if (thisPressed.Contains(item3))
					{
						continue;
					}
					Keys value2 = item3;
					int num2 = Items.IndexOf(keyGrabFor);
					MappedAction mappedAction3 = KeyboardActionOrder[num2 - keyboardStart];
					Dictionary<MappedAction, Keys> kMap = SettingsManager.Settings.KeyboardMapping;
					kMap[mappedAction3] = value2;
					if (keyGrabFor != keyboardFPItem || GameState.SaveData.HasFPView)
					{
						keyGrabFor.SuffixText = () => " : " + Localize(kMap[mappedAction3]);
					}
					KeyboardManager.UpdateMapping();
					ValidateKeyCollision();
					base.TrapInput = false;
					break;
				}
				foreach (Keys item4 in thisPressed)
				{
					if (!lastPressed.Contains(item4))
					{
						keysDown.Add(item4);
					}
				}
				HashSet<Keys> hashSet = thisPressed;
				thisPressed = lastPressed;
				lastPressed = hashSet;
			}
		}
		if (base.SelectedIndex < Items.Count - 2)
		{
			return;
		}
		Point position = MouseState.Position;
		if (leftSliderRect.HasValue && leftSliderRect.Value.Contains(position))
		{
			menuBase.CursorSelectable = true;
			if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
			{
				FakeSlideLeft();
			}
		}
		else if (rightSliderRect.HasValue && rightSliderRect.Value.Contains(position))
		{
			menuBase.CursorSelectable = true;
			if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
			{
				FakeSlideRight();
			}
		}
		if (InputManager.Right == FezButtonState.Pressed)
		{
			FakeSlideRight();
		}
		else if (InputManager.Left == FezButtonState.Pressed)
		{
			FakeSlideLeft();
		}
	}

	private void ValidateKeyCollision()
	{
		Dictionary<MappedAction, Keys> keyboardMapping = SettingsManager.Settings.KeyboardMapping;
		for (int i = 0; i < KeyboardActionOrder.Length; i++)
		{
			Items[i + keyboardStart].InError = false;
		}
		for (int j = 0; j < KeyboardActionOrder.Length; j++)
		{
			for (int k = 0; k < KeyboardActionOrder.Length; k++)
			{
				if (j != k)
				{
					MenuItem menuItem = Items[j + keyboardStart];
					MenuItem menuItem2 = Items[k + keyboardStart];
					Keys keys = keyboardMapping[KeyboardActionOrder[j]];
					Keys keys2 = keyboardMapping[KeyboardActionOrder[k]];
					if (keys == keys2)
					{
						menuItem.InError = true;
						menuItem2.InError = true;
					}
				}
			}
		}
	}

	private void FakeSlideRight(bool silent = false)
	{
		Items[chosen].Hidden = true;
		Items[chosen].Selectable = false;
		int num = chosen;
		chosen++;
		if (chosen == Items.Count)
		{
			chosen = selectorStart;
		}
		if (!GamepadState.AnyConnected && chosen == selectorStart)
		{
			chosen++;
		}
		int num2 = chosen - selectorStart;
		for (int i = 0; i < keyboardStart; i++)
		{
			Items[i].Hidden = num2 != 0;
		}
		for (int j = keyboardStart; j < selectorStart; j++)
		{
			Items[j].Hidden = num2 != 1;
		}
		Items[chosen].Hidden = false;
		Items[chosen].Selectable = true;
		base.SelectedIndex = chosen;
		if (!silent && num != chosen)
		{
			sSliderValueIncrease.Emit();
		}
	}

	private void FakeSlideLeft(bool silent = false)
	{
		Items[chosen].Hidden = true;
		Items[chosen].Selectable = false;
		int num = chosen;
		chosen--;
		if (!GamepadState.AnyConnected && chosen == selectorStart)
		{
			chosen++;
		}
		if (chosen == selectorStart - 1)
		{
			chosen = Items.Count - 1;
		}
		int num2 = chosen - selectorStart;
		for (int i = 0; i < keyboardStart; i++)
		{
			Items[i].Hidden = num2 != 0;
		}
		for (int j = keyboardStart; j < selectorStart; j++)
		{
			Items[j].Hidden = num2 != 1;
		}
		Items[chosen].Hidden = false;
		Items[chosen].Selectable = true;
		base.SelectedIndex = chosen;
		if (!silent && num != chosen)
		{
			sSliderValueDecrease.Emit();
		}
	}

	public override void Initialize()
	{
		ContentManager contentManager = base.CMProvider.Get(CM.Menu);
		sSliderValueDecrease = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/SliderValueDecrease");
		sSliderValueIncrease = contentManager.Load<SoundEffect>("Sounds/Ui/Menu/SliderValueIncrease");
		base.Initialize();
	}

	public override void PostDraw(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha)
	{
		float viewScale = batch.GraphicsDevice.GetViewScale();
		int num = batch.GraphicsDevice.Viewport.Height / 2;
		float num2 = Items[chosen].Size.X + 70f;
		if (Culture.IsCJK)
		{
			num2 *= 0.5f;
		}
		if (base.SelectedIndex >= Items.Count - 2)
		{
			float num3 = 25f;
			if (!Culture.IsCJK)
			{
				num3 *= viewScale;
			}
			else
			{
				num2 *= 0.4f;
				num2 += 25f;
				num3 = 5f * viewScale;
				if (Culture.Language == Language.Chinese)
				{
					num3 = 10f + 25f * viewScale;
				}
			}
			int num4 = ServiceHelper.Game.GraphicsDevice.Viewport.Width / 2;
			Vector2 offset = new Vector2(0f - num2 - 40f * (viewScale - 1f), (float)num + 180f * viewScale + num3);
			int num5 = ServiceHelper.Game.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - num4;
			int num6 = (ServiceHelper.Game.GraphicsDevice.PresentationParameters.BackBufferHeight - ServiceHelper.Game.GraphicsDevice.Viewport.Height) / 2;
			if (!noArrows)
			{
				tr.DrawCenteredString(batch, FontManager.Big, "{LA}", new Color(1f, 1f, 1f, alpha), offset, (Culture.IsCJK ? 0.2f : 1f) * viewScale);
				leftSliderRect = new Rectangle((int)((float)num5 + offset.X + (float)num4 - 25f * viewScale), (int)((float)num6 + offset.Y), (int)(40f * viewScale), (int)(25f * viewScale));
			}
			else
			{
				leftSliderRect = null;
			}
			offset = new Vector2(num2 + 40f * (viewScale - 1f), (float)num + 180f * viewScale + num3);
			if (!noArrows)
			{
				tr.DrawCenteredString(batch, FontManager.Big, "{RA}", new Color(1f, 1f, 1f, alpha), offset, (Culture.IsCJK ? 0.2f : 1f) * viewScale);
				rightSliderRect = new Rectangle((int)((float)num5 + offset.X + (float)num4 - 30f * viewScale), (int)((float)num6 + offset.Y), (int)(40f * viewScale), (int)(25f * viewScale));
			}
			else
			{
				rightSliderRect = null;
			}
		}
		else
		{
			leftSliderRect = (rightSliderRect = null);
		}
	}

	private void DrawLeftAligned(GlyphTextRenderer tr, SpriteBatch batch, SpriteFont font, string text, float alpha, Vector2 offset, float size)
	{
		float num = font.MeasureString(text).X * size;
		tr.DrawShadowedText(batch, font, text, offset - num * Vector2.UnitX, new Color(1f, 1f, 1f, alpha), size);
	}
}
