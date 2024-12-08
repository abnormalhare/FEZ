using System;
using System.Collections.Generic;
using Common;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Components;

public class InputManager : GameComponent, IInputManager
{
	private struct State
	{
		public FezButtonState Up;

		public FezButtonState Down;

		public FezButtonState Left;

		public FezButtonState Right;

		public FezButtonState Cancel;

		public FezButtonState GrabThrow;

		public FezButtonState RotateLeft;

		public FezButtonState RotateRight;

		public FezButtonState Start;

		public FezButtonState Back;

		public FezButtonState Jump;

		public FezButtonState OpenInventory;

		public FezButtonState ExactUp;

		public FezButtonState ClampLook;

		public FezButtonState FpsToggle;

		public FezButtonState MapZoomIn;

		public FezButtonState MapZoomOut;

		public Vector2 Movement;

		public Vector2 FreeLook;
	}

	private readonly bool mouse;

	private readonly bool keyboard;

	private readonly bool gamepad;

	private Vector2 lastMouseCenter;

	private readonly Stack<State> savedStates = new Stack<State>();

	private GamepadState MockGamepad = new GamepadState(PlayerIndex.One);

	public ControllerIndex ActiveControllers { get; private set; }

	public FezButtonState GrabThrow { get; private set; }

	public Vector2 Movement { get; private set; }

	public Vector2 FreeLook { get; private set; }

	public FezButtonState Jump { get; private set; }

	public FezButtonState Back { get; private set; }

	public FezButtonState OpenInventory { get; private set; }

	public FezButtonState Start { get; private set; }

	public FezButtonState RotateLeft { get; private set; }

	public FezButtonState RotateRight { get; private set; }

	public FezButtonState CancelTalk { get; private set; }

	public FezButtonState Up { get; private set; }

	public FezButtonState Down { get; private set; }

	public FezButtonState Left { get; private set; }

	public FezButtonState Right { get; private set; }

	public FezButtonState ClampLook { get; private set; }

	public FezButtonState FpsToggle { get; private set; }

	public FezButtonState ExactUp { get; private set; }

	public FezButtonState MapZoomIn { get; private set; }

	public FezButtonState MapZoomOut { get; private set; }

	public bool StrictRotation { get; set; }

	public GamepadState ActiveGamepad
	{
		get
		{
			if (!gamepad)
			{
				return MockGamepad;
			}
			if (ActiveControllers == ControllerIndex.None)
			{
				return GamepadsManager[PlayerIndex.One];
			}
			return GamepadsManager[ActiveControllers.GetPlayer()];
		}
	}

	[ServiceDependency(Optional = true)]
	public IMouseStateManager MouseState { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency(Optional = true)]
	public IGamepadsManager GamepadsManager { private get; set; }

	public event Action<PlayerIndex> ActiveControllerDisconnected;

	public InputManager(Game game, bool mouse, bool keyboard, bool gamepad)
		: base(game)
	{
		ActiveControllers = ControllerIndex.Any;
		this.mouse = mouse;
		this.keyboard = keyboard;
		this.gamepad = gamepad;
	}

	private FezButtonState GetStateForButton(Buttons button, GamepadState gps)
	{
		return button switch
		{
			Buttons.DPadUp => gps.DPad.Up.State, 
			Buttons.DPadDown => gps.DPad.Down.State, 
			Buttons.DPadLeft => gps.DPad.Left.State, 
			Buttons.DPadRight => gps.DPad.Right.State, 
			Buttons.Start => gps.Start, 
			Buttons.Back => gps.Back, 
			Buttons.LeftStick => gps.LeftStick.Clicked.State, 
			Buttons.RightStick => gps.RightStick.Clicked.State, 
			Buttons.LeftShoulder => gps.LeftShoulder.State, 
			Buttons.RightShoulder => gps.RightShoulder.State, 
			Buttons.BigButton => throw new NotSupportedException("Guide button should not be bindable!"), 
			Buttons.A => gps.A.State, 
			Buttons.B => gps.B.State, 
			Buttons.X => gps.X.State, 
			Buttons.Y => gps.Y.State, 
			Buttons.LeftThumbstickLeft => gps.LeftStick.Left.State, 
			Buttons.RightTrigger => gps.RightTrigger.State, 
			Buttons.LeftTrigger => gps.LeftTrigger.State, 
			Buttons.RightThumbstickUp => gps.RightStick.Up.State, 
			Buttons.RightThumbstickDown => gps.RightStick.Down.State, 
			Buttons.RightThumbstickRight => gps.RightStick.Right.State, 
			Buttons.RightThumbstickLeft => gps.RightStick.Left.State, 
			Buttons.LeftThumbstickUp => gps.LeftStick.Up.State, 
			Buttons.LeftThumbstickDown => gps.LeftStick.Down.State, 
			Buttons.LeftThumbstickRight => gps.LeftStick.Right.State, 
			_ => throw new InvalidOperationException("How did you get here...?"), 
		};
	}

	public override void Update(GameTime gameTime)
	{
		if (!ServiceHelper.Game.IsActive)
		{
			return;
		}
		Reset();
		if (keyboard)
		{
			KeyboardState.Update(Keyboard.GetState(), gameTime);
			Movement = new Vector2(KeyboardState.Right.IsDown() ? 1f : (KeyboardState.Left.IsDown() ? (-1f) : 0f), KeyboardState.Up.IsDown() ? 1f : (KeyboardState.Down.IsDown() ? (-1f) : 0f));
			FreeLook = new Vector2(KeyboardState.LookRight.IsDown() ? 1f : (KeyboardState.LookLeft.IsDown() ? (-1f) : 0f), KeyboardState.LookUp.IsDown() ? 1f : (KeyboardState.LookDown.IsDown() ? (-1f) : 0f));
			Back = KeyboardState.OpenMap;
			Start = KeyboardState.Pause;
			Jump = KeyboardState.Jump;
			GrabThrow = KeyboardState.GrabThrow;
			CancelTalk = KeyboardState.CancelTalk;
			Down = KeyboardState.Down;
			FezButtonState exactUp = (Up = KeyboardState.Up);
			ExactUp = exactUp;
			Left = KeyboardState.Left;
			Right = KeyboardState.Right;
			OpenInventory = KeyboardState.OpenInventory;
			RotateLeft = KeyboardState.RotateLeft;
			RotateRight = KeyboardState.RotateRight;
			MapZoomIn = KeyboardState.MapZoomIn;
			MapZoomOut = KeyboardState.MapZoomOut;
			FpsToggle = KeyboardState.FpViewToggle;
			ClampLook = KeyboardState.ClampLook;
		}
		if (gamepad)
		{
			Dictionary<MappedAction, Buttons> controllerMapping = SettingsManager.Settings.ControllerMapping;
			PlayerIndex[] players = ControllerIndex.Any.GetPlayers();
			for (int i = 0; i < players.Length; i++)
			{
				GamepadState gamepadState = GamepadsManager[players[i]];
				if (!gamepadState.Connected)
				{
					if (gamepadState.NewlyDisconnected && this.ActiveControllerDisconnected != null)
					{
						this.ActiveControllerDisconnected(players[i]);
					}
					continue;
				}
				ClampLook = FezMath.Coalesce(ClampLook, GetStateForButton(controllerMapping[MappedAction.ClampLook], gamepadState), FezButtonStateComparer.Default);
				FpsToggle = FezMath.Coalesce(FpsToggle, GetStateForButton(controllerMapping[MappedAction.FpViewToggle], gamepadState), FezButtonStateComparer.Default);
				Vector2 second = Vector2.Clamp(ThumbstickState.CircleToSquare(gamepadState.LeftStick.Position), -Vector2.One, Vector2.One);
				Vector2 second2 = Vector2.Clamp(ThumbstickState.CircleToSquare(gamepadState.RightStick.Position), -Vector2.One, Vector2.One);
				Movement = FezMath.Coalesce(Movement, second, gamepadState.DPad.Direction);
				FreeLook = FezMath.Coalesce(FreeLook, second2);
				Back = FezMath.Coalesce(Back, GetStateForButton(controllerMapping[MappedAction.OpenMap], gamepadState), FezButtonStateComparer.Default);
				Start = FezMath.Coalesce(Start, GetStateForButton(controllerMapping[MappedAction.Pause], gamepadState), FezButtonStateComparer.Default);
				Jump = FezMath.Coalesce(Jump, GetStateForButton(controllerMapping[MappedAction.Jump], gamepadState), FezButtonStateComparer.Default);
				GrabThrow = FezMath.Coalesce(GrabThrow, GetStateForButton(controllerMapping[MappedAction.GrabThrow], gamepadState), FezButtonStateComparer.Default);
				CancelTalk = FezMath.Coalesce(CancelTalk, GetStateForButton(controllerMapping[MappedAction.CancelTalk], gamepadState), FezButtonStateComparer.Default);
				OpenInventory = FezMath.Coalesce(OpenInventory, GetStateForButton(controllerMapping[MappedAction.OpenInventory], gamepadState), FezButtonStateComparer.Default);
				Up = FezMath.Coalesce(Up, gamepadState.DPad.Up.State, gamepadState.LeftStick.Up.State, FezButtonStateComparer.Default);
				Down = FezMath.Coalesce(Down, gamepadState.DPad.Down.State, gamepadState.LeftStick.Down.State, FezButtonStateComparer.Default);
				Left = FezMath.Coalesce(Left, gamepadState.DPad.Left.State, gamepadState.LeftStick.Left.State, FezButtonStateComparer.Default);
				Right = FezMath.Coalesce(Right, gamepadState.DPad.Right.State, gamepadState.LeftStick.Right.State, FezButtonStateComparer.Default);
				ExactUp = FezMath.Coalesce(ExactUp, gamepadState.ExactUp, FezButtonStateComparer.Default);
				MapZoomIn = FezMath.Coalesce(MapZoomIn, GetStateForButton(controllerMapping[MappedAction.MapZoomIn], gamepadState), FezButtonStateComparer.Default);
				MapZoomOut = FezMath.Coalesce(MapZoomOut, GetStateForButton(controllerMapping[MappedAction.MapZoomOut], gamepadState), FezButtonStateComparer.Default);
				if (StrictRotation)
				{
					RotateLeft = FezMath.Coalesce(RotateLeft, GetStateForButton(controllerMapping[MappedAction.RotateLeft], gamepadState), FezButtonStateComparer.Default);
					RotateRight = FezMath.Coalesce(RotateRight, GetStateForButton(controllerMapping[MappedAction.RotateRight], gamepadState), FezButtonStateComparer.Default);
				}
				else
				{
					RotateLeft = FezMath.Coalesce(RotateLeft, GetStateForButton(controllerMapping[MappedAction.MapZoomOut], gamepadState), GetStateForButton(controllerMapping[MappedAction.RotateLeft], gamepadState), FezButtonStateComparer.Default);
					RotateRight = FezMath.Coalesce(RotateRight, GetStateForButton(controllerMapping[MappedAction.MapZoomIn], gamepadState), GetStateForButton(controllerMapping[MappedAction.RotateRight], gamepadState), FezButtonStateComparer.Default);
				}
				if (SettingsManager.Settings.DeadZone != 0)
				{
					float num = Movement.Length();
					if (num > 0f && num < (float)SettingsManager.Settings.DeadZone / 100f)
					{
						Movement = Vector2.Zero;
					}
					float num2 = FreeLook.Length();
					if (num2 > 0f && num2 < (float)SettingsManager.Settings.DeadZone / 100f)
					{
						FreeLook = Vector2.Zero;
					}
				}
			}
		}
		if (mouse)
		{
			MouseState.Update(gameTime);
			Vector2 second3 = Vector2.Zero;
			switch (MouseState.LeftButton.State)
			{
			case MouseButtonStates.DragStarted:
				lastMouseCenter = new Vector2(MouseState.LeftButton.DragState.Movement.X, -MouseState.LeftButton.DragState.Movement.Y);
				break;
			case MouseButtonStates.Dragging:
			{
				Vector2 vector = new Vector2(MouseState.LeftButton.DragState.Movement.X, -MouseState.LeftButton.DragState.Movement.Y);
				second3 = (lastMouseCenter - vector) / 32f;
				lastMouseCenter = vector;
				break;
			}
			}
			FreeLook = FezMath.Coalesce(FreeLook, second3);
			MapZoomIn = FezMath.Coalesce(MapZoomIn, MouseState.WheelTurnedUp, FezButtonStateComparer.Default);
			MapZoomOut = FezMath.Coalesce(MapZoomOut, MouseState.WheelTurnedDown, FezButtonStateComparer.Default);
		}
	}

	public void SaveState()
	{
		savedStates.Push(new State
		{
			Up = Up,
			Down = Down,
			Left = Left,
			Right = Right,
			ExactUp = ExactUp,
			Cancel = CancelTalk,
			GrabThrow = GrabThrow,
			Jump = Jump,
			RotateLeft = RotateLeft,
			RotateRight = RotateRight,
			Start = Start,
			Back = Back,
			FreeLook = FreeLook,
			Movement = Movement,
			OpenInventory = OpenInventory,
			ClampLook = ClampLook,
			FpsToggle = FpsToggle,
			MapZoomIn = MapZoomIn,
			MapZoomOut = MapZoomOut
		});
	}

	public void RecoverState()
	{
		if (savedStates.Count != 0)
		{
			State state = savedStates.Pop();
			Up = state.Up;
			Down = state.Down;
			Left = state.Left;
			Right = state.Right;
			ExactUp = state.ExactUp;
			CancelTalk = state.Cancel;
			GrabThrow = state.GrabThrow;
			Jump = state.Jump;
			RotateLeft = state.RotateLeft;
			RotateRight = state.RotateRight;
			Start = state.Start;
			Back = state.Back;
			FreeLook = state.FreeLook;
			Movement = state.Movement;
			OpenInventory = state.OpenInventory;
			ClampLook = state.ClampLook;
			FpsToggle = state.FpsToggle;
			MapZoomIn = state.MapZoomIn;
			MapZoomOut = state.MapZoomOut;
		}
	}

	public void Reset()
	{
		FezButtonState fezButtonState2 = (ExactUp = FezButtonState.Up);
		FezButtonState fezButtonState4 = (Right = fezButtonState2);
		FezButtonState fezButtonState6 = (Left = fezButtonState4);
		FezButtonState up = (Down = fezButtonState6);
		Up = up;
		Vector2 movement = (FreeLook = default(Vector2));
		Movement = movement;
		fezButtonState6 = (Jump = FezButtonState.Up);
		up = (GrabThrow = fezButtonState6);
		CancelTalk = up;
		up = (Back = FezButtonState.Up);
		Start = up;
		up = (RotateRight = FezButtonState.Up);
		RotateLeft = up;
		OpenInventory = FezButtonState.Up;
		up = (FpsToggle = FezButtonState.Up);
		ClampLook = up;
		up = (MapZoomOut = FezButtonState.Up);
		MapZoomIn = up;
	}

	public void PressedToDown()
	{
		if (ExactUp == FezButtonState.Pressed)
		{
			ExactUp = FezButtonState.Down;
		}
		if (Up == FezButtonState.Pressed)
		{
			Up = FezButtonState.Down;
		}
		if (Down == FezButtonState.Pressed)
		{
			Down = FezButtonState.Down;
		}
		if (Left == FezButtonState.Pressed)
		{
			Left = FezButtonState.Down;
		}
		if (Right == FezButtonState.Pressed)
		{
			Right = FezButtonState.Down;
		}
		if (CancelTalk == FezButtonState.Pressed)
		{
			CancelTalk = FezButtonState.Down;
		}
		if (GrabThrow == FezButtonState.Pressed)
		{
			GrabThrow = FezButtonState.Down;
		}
		if (Jump == FezButtonState.Pressed)
		{
			Jump = FezButtonState.Down;
		}
		if (Start == FezButtonState.Pressed)
		{
			Start = FezButtonState.Down;
		}
		if (Back == FezButtonState.Pressed)
		{
			Back = FezButtonState.Down;
		}
		if (RotateLeft == FezButtonState.Pressed)
		{
			RotateLeft = FezButtonState.Down;
		}
		if (OpenInventory == FezButtonState.Pressed)
		{
			OpenInventory = FezButtonState.Down;
		}
		if (RotateRight == FezButtonState.Pressed)
		{
			RotateRight = FezButtonState.Down;
		}
		if (ClampLook == FezButtonState.Pressed)
		{
			ClampLook = FezButtonState.Down;
		}
		if (FpsToggle == FezButtonState.Pressed)
		{
			FpsToggle = FezButtonState.Down;
		}
		if (MapZoomIn == FezButtonState.Pressed)
		{
			MapZoomIn = FezButtonState.Down;
		}
		if (MapZoomOut == FezButtonState.Pressed)
		{
			MapZoomOut = FezButtonState.Down;
		}
	}

	public void ForceActiveController(ControllerIndex ci)
	{
		ActiveControllers = ci;
	}

	public void DetermineActiveController()
	{
		if (!gamepad)
		{
			return;
		}
		PlayerIndex[] players = ActiveControllers.GetPlayers();
		foreach (PlayerIndex index in players)
		{
			GamepadState gamepadState = GamepadsManager[index];
			if (gamepadState.Start.IsDown() || gamepadState.A.State.IsDown() || gamepadState.Back.IsDown() || gamepadState.B.State.IsDown())
			{
				ActiveControllers = index.ToControllerIndex();
				return;
			}
		}
		ActiveControllers = ControllerIndex.None;
	}

	public void ClearActiveController()
	{
		ActiveControllers = ControllerIndex.Any;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		foreach (PlayerIndex value in Util.GetValues<PlayerIndex>())
		{
			GamePad.SetVibration(value, 0f, 0f);
		}
	}

	public bool AnyButtonPressed()
	{
		if (GrabThrow != FezButtonState.Pressed && Jump != FezButtonState.Pressed && OpenInventory != FezButtonState.Pressed && Start != FezButtonState.Pressed)
		{
			return CancelTalk == FezButtonState.Pressed;
		}
		return true;
	}
}
