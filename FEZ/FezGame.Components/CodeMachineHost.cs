using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class CodeMachineHost : GameComponent
{
	private class BitState
	{
		public TimeSpan SinceOn;

		public TimeSpan SinceOff;

		public TimeSpan SinceIdle;

		public bool On;
	}

	private static readonly TimeSpan FadeOutDuration = TimeSpan.FromSeconds(0.10000000149011612);

	private static readonly TimeSpan FadeInDuration = TimeSpan.FromSeconds(0.20000000298023224);

	private static readonly TimeSpan Delay = TimeSpan.FromSeconds(0.03333333507180214);

	private static readonly TimeSpan TimeOut = TimeSpan.FromSeconds(2.0);

	private static readonly Dictionary<CodeInput, int[]> BitPatterns = new Dictionary<CodeInput, int[]>(CodeInputComparer.Default)
	{
		{
			CodeInput.Down,
			new int[36]
			{
				1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
				1, 1, 0, 0, 1, 1, 0, 0, 0, 0,
				1, 1, 0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0
			}
		},
		{
			CodeInput.Up,
			new int[36]
			{
				0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 1, 1, 0, 0, 0, 0,
				1, 1, 0, 0, 1, 1, 1, 1, 1, 1,
				1, 1, 1, 1, 1, 1
			}
		},
		{
			CodeInput.Left,
			new int[36]
			{
				0, 0, 0, 0, 1, 1, 0, 0, 0, 0,
				1, 1, 0, 0, 1, 1, 1, 1, 0, 0,
				1, 1, 1, 1, 0, 0, 0, 0, 1, 1,
				0, 0, 0, 0, 1, 1
			}
		},
		{
			CodeInput.Right,
			new int[36]
			{
				1, 1, 0, 0, 0, 0, 1, 1, 0, 0,
				0, 0, 1, 1, 1, 1, 0, 0, 1, 1,
				1, 1, 0, 0, 1, 1, 0, 0, 0, 0,
				1, 1, 0, 0, 0, 0
			}
		},
		{
			CodeInput.SpinRight,
			new int[36]
			{
				0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 1, 1, 1, 1, 0, 0,
				1, 1, 1, 1, 1, 1, 1, 1, 0, 0,
				1, 1, 1, 1, 0, 0
			}
		},
		{
			CodeInput.SpinLeft,
			new int[36]
			{
				0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 1, 1, 1, 1, 0, 0, 1, 1,
				1, 1, 0, 0, 0, 0, 1, 1, 1, 1,
				0, 0, 1, 1, 1, 1
			}
		},
		{
			CodeInput.Jump,
			new int[36]
			{
				0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 1, 1, 1, 1, 0, 0,
				1, 1, 1, 1, 0, 0, 1, 1, 1, 1,
				0, 0, 1, 1, 1, 1
			}
		}
	};

	private ArtObjectInstance CodeMachineAO;

	private BackgroundPlane[] BitPlanes;

	private BitState[] BitStates;

	private readonly List<CodeInput> Input = new List<CodeInput>();

	private TimeSpan SinceInput;

	private SoundEffect inputSound;

	private SoundEmitter inputEmitter;

	private bool needsInitialize;

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ICodePatternService CPService { private get; set; }

	public CodeMachineHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		inputSound = CMProvider.Global.Load<SoundEffect>("Sounds/Zu/CodeMachineInput");
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		CodeMachineAO = null;
		BitPlanes = null;
		BitStates = null;
		base.Enabled = false;
		Input.Clear();
		base.Enabled = (CodeMachineAO = LevelManager.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.CodeMachine)) != null;
		if (base.Enabled)
		{
			BitPlanes = new BackgroundPlane[144];
			BitStates = new BitState[144];
			needsInitialize = true;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMenuCube || GameState.InMap)
		{
			return;
		}
		if (needsInitialize)
		{
			Texture2D texture = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/glow/code_machine_glowbit");
			for (int i = 0; i < 36; i++)
			{
				BackgroundPlane backgroundPlane = new BackgroundPlane(LevelMaterializer.StaticPlanesMesh, texture)
				{
					Fullbright = true,
					Opacity = 0f,
					Rotation = Quaternion.Identity
				};
				BitPlanes[i * 4] = backgroundPlane;
				BackgroundPlane backgroundPlane2 = backgroundPlane.Clone();
				backgroundPlane2.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 2f);
				BitPlanes[i * 4 + 1] = backgroundPlane2;
				BackgroundPlane backgroundPlane3 = backgroundPlane.Clone();
				backgroundPlane3.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI);
				BitPlanes[i * 4 + 2] = backgroundPlane3;
				BackgroundPlane backgroundPlane4 = backgroundPlane.Clone();
				backgroundPlane4.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, 4.712389f);
				BitPlanes[i * 4 + 3] = backgroundPlane4;
				int num = i % 6;
				int num2 = i / 6;
				for (int j = 0; j < 4; j++)
				{
					BackgroundPlane backgroundPlane5 = BitPlanes[i * 4 + j];
					BitStates[i * 4 + j] = new BitState();
					Vector3 vector = Vector3.Transform(Vector3.UnitZ, backgroundPlane5.Rotation);
					Vector3 vector2 = Vector3.Transform(Vector3.Right, backgroundPlane5.Rotation);
					backgroundPlane5.Position = CodeMachineAO.Position + vector * 1.5f + vector2 * (-20 + num * 8) / 16f + Vector3.Up * (35 - num2 * 8) / 16f;
					LevelManager.AddPlane(backgroundPlane5);
				}
			}
			needsInitialize = false;
		}
		if (CameraManager.ViewTransitionReached)
		{
			CheckInput();
		}
		UpdateBits(gameTime.ElapsedGameTime);
		SinceInput += gameTime.ElapsedGameTime;
		if (SinceInput > TimeOut)
		{
			Input.Clear();
		}
	}

	private void UpdateBits(TimeSpan elapsed)
	{
		for (int i = 0; i < 36; i++)
		{
			int num = i % 6;
			int num2 = i / 6;
			for (int j = 0; j < 4; j++)
			{
				BackgroundPlane backgroundPlane = BitPlanes[i * 4 + j];
				BitState bitState = BitStates[i * 4 + j];
				TimeSpan timeSpan = TimeSpan.FromTicks(Delay.Ticks * (num + num2));
				if (bitState.On)
				{
					if (bitState.SinceOn < FadeInDuration)
					{
						bitState.SinceOn += elapsed;
						backgroundPlane.Opacity = FezMath.Saturate((float)bitState.SinceOn.Ticks / (float)FadeInDuration.Ticks);
						bitState.SinceOff = TimeSpan.FromSeconds((double)(1f - backgroundPlane.Opacity) * FadeOutDuration.TotalSeconds) - timeSpan;
					}
					else if (bitState.SinceIdle < TimeOut)
					{
						bitState.SinceIdle += elapsed;
					}
					else
					{
						bitState.On = false;
					}
				}
				else if (bitState.SinceOff < FadeOutDuration)
				{
					bitState.SinceOff += elapsed;
					backgroundPlane.Opacity = FezMath.Saturate(1f - (float)bitState.SinceOff.Ticks / (float)FadeOutDuration.Ticks);
					bitState.SinceOn = TimeSpan.FromSeconds((double)backgroundPlane.Opacity * FadeInDuration.TotalSeconds) - timeSpan;
				}
			}
		}
	}

	private void CheckInput()
	{
		Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
		Vector3 vector2 = CameraManager.Viewpoint.DepthMask();
		Vector3 vector3 = CodeMachineAO.ArtObject.Size * vector;
		Vector3 vector4 = CodeMachineAO.Position * vector;
		if (new BoundingBox(vector4 - vector3 - Vector3.UnitY * 2f, vector4 + vector3 + vector2).Contains(PlayerManager.Position * vector + vector2 / 2f) != 0)
		{
			if (InputManager.Jump == FezButtonState.Pressed)
			{
				OnInput(CodeInput.Jump);
			}
			else if (InputManager.RotateRight == FezButtonState.Pressed)
			{
				OnInput(CodeInput.SpinRight);
			}
			else if (InputManager.RotateLeft == FezButtonState.Pressed)
			{
				OnInput(CodeInput.SpinLeft);
			}
			else if (InputManager.Left == FezButtonState.Pressed)
			{
				OnInput(CodeInput.Left);
			}
			else if (InputManager.Right == FezButtonState.Pressed)
			{
				OnInput(CodeInput.Right);
			}
			else if (InputManager.Up == FezButtonState.Pressed)
			{
				OnInput(CodeInput.Up);
			}
			else if (InputManager.Down == FezButtonState.Pressed)
			{
				OnInput(CodeInput.Down);
			}
		}
	}

	private void OnInput(CodeInput newInput)
	{
		int[] array = BitPatterns[newInput];
		if (inputEmitter != null && !inputEmitter.Dead)
		{
			inputEmitter.Cue.Stop();
		}
		inputEmitter = inputSound.EmitAt(CodeMachineAO.Position, RandomHelper.Between(-0.05, 0.05));
		for (int i = 0; i < 36; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				BitState bitState = BitStates[i * 4 + j];
				bitState.On = array[i] == 1;
				if (bitState.On)
				{
					bitState.SinceIdle = TimeSpan.Zero;
				}
			}
		}
		SinceInput = TimeSpan.Zero;
	}
}
