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
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class GameWideCodes : GameComponent
{
	private static readonly CodeInput[] AchievementCode = new CodeInput[8]
	{
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinLeft,
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinLeft,
		CodeInput.SpinLeft,
		CodeInput.SpinLeft
	};

	private static readonly CodeInput[] JetpackCode = new CodeInput[5]
	{
		CodeInput.Up,
		CodeInput.Up,
		CodeInput.Up,
		CodeInput.Up,
		CodeInput.Jump
	};

	private static readonly CodeInput[] MapCode = new CodeInput[8]
	{
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinLeft,
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinLeft
	};

	private readonly List<CodeInput> Input = new List<CodeInput>();

	private TimeSpan SinceInput;

	private TrileInstance waitingForTrile;

	private bool isMapQr;

	private bool isAchievementCode;

	[ServiceDependency]
	public IGomezService GomezService { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public ILevelService LevelService { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	public GameWideCodes(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += delegate
		{
			waitingForTrile = null;
			Input.Clear();
			isMapQr = (isAchievementCode = false);
		};
	}

	public override void Update(GameTime gameTime)
	{
		if (CameraManager.Viewpoint.IsOrthographic() && !GameState.InMap && !GameState.Paused && !GameState.Loading && !GameState.InCutscene && !GameState.IsTrialMode && !GameState.FarawaySettings.InTransition && !GameState.DotLoading && PlayerManager.Action != ActionType.OpeningTreasure)
		{
			TestInput();
			SinceInput += gameTime.ElapsedGameTime;
		}
	}

	private void TestInput()
	{
		CodeInput codeInput = CodeInput.None;
		if (InputManager.Jump == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Jump;
		}
		else if (InputManager.RotateRight == FezButtonState.Pressed)
		{
			codeInput = CodeInput.SpinRight;
		}
		else if (InputManager.RotateLeft == FezButtonState.Pressed)
		{
			codeInput = CodeInput.SpinLeft;
		}
		else if (InputManager.Left == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Left;
		}
		else if (InputManager.Right == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Right;
		}
		else if (InputManager.Up == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Up;
		}
		else if (InputManager.Down == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Down;
		}
		if (codeInput == CodeInput.None)
		{
			return;
		}
		Input.Add(codeInput);
		if (Input.Count > 16)
		{
			Input.RemoveAt(0);
		}
		if (!isAchievementCode && !GameState.SaveData.AchievementCheatCodeDone && !GameState.SaveData.FezHidden && PatternTester.Test(Input, AchievementCode) && LevelManager.Name != "ELDERS")
		{
			Input.Clear();
			isAchievementCode = true;
			LevelService.ResolvePuzzleSoundOnly();
			Waiters.Wait(() => CameraManager.ViewTransitionReached && PlayerManager.Grounded && !PlayerManager.Background, delegate
			{
				Vector3 vector = PlayerManager.Center + new Vector3(0f, 2f, 0f);
				Trile trile = LevelManager.ActorTriles(ActorType.SecretCube).FirstOrDefault();
				if (trile != null)
				{
					Vector3 vector2 = vector - Vector3.One / 2f;
					NearestTriles nearestTriles = LevelManager.NearestTrile(vector);
					TrileInstance trileInstance = nearestTriles.Surface ?? nearestTriles.Deep;
					if (trileInstance != null)
					{
						vector2 = CameraManager.Viewpoint.ScreenSpaceMask() * vector2 + trileInstance.Center * CameraManager.Viewpoint.DepthMask() - CameraManager.Viewpoint.ForwardVector() * 2f;
					}
					vector2 = Vector3.Clamp(vector2, Vector3.Zero, LevelManager.Size - Vector3.One);
					ServiceHelper.AddComponent(new GlitchyRespawner(base.Game, waitingForTrile = new TrileInstance(vector2, trile.Id)));
					waitingForTrile.GlobalSpawn = true;
					GomezService.CollectedGlobalAnti += GotTrile;
				}
			});
		}
		if (!isMapQr && !GameState.SaveData.MapCheatCodeDone && GameState.SaveData.Maps.Contains("MAP_MYSTERY") && LevelManager.Name != "WATERTOWER_SECRET" && PatternTester.Test(Input, MapCode))
		{
			Input.Clear();
			GameState.SaveData.AnyCodeDeciphered = true;
			isMapQr = true;
			if (GameState.SaveData.World.ContainsKey("WATERTOWER_SECRET"))
			{
				GameState.SaveData.World["WATERTOWER_SECRET"].FilledConditions.SecretCount = 1;
			}
			LevelService.ResolvePuzzleSoundOnly();
			Waiters.Wait(() => CameraManager.ViewTransitionReached && PlayerManager.Grounded && !PlayerManager.Background, delegate
			{
				Vector3 vector3 = PlayerManager.Center + new Vector3(0f, 2f, 0f);
				Trile trile2 = LevelManager.ActorTriles(ActorType.SecretCube).FirstOrDefault();
				if (trile2 != null)
				{
					Vector3 vector4 = vector3 - Vector3.One / 2f;
					NearestTriles nearestTriles2 = LevelManager.NearestTrile(vector3);
					TrileInstance trileInstance2 = nearestTriles2.Surface ?? nearestTriles2.Deep;
					if (trileInstance2 != null)
					{
						vector4 = CameraManager.Viewpoint.ScreenSpaceMask() * vector4 + trileInstance2.Center * CameraManager.Viewpoint.DepthMask() - CameraManager.Viewpoint.ForwardVector() * 2f;
					}
					vector4 = Vector3.Clamp(vector4, Vector3.Zero, LevelManager.Size - Vector3.One);
					ServiceHelper.AddComponent(new GlitchyRespawner(base.Game, waitingForTrile = new TrileInstance(vector4, trile2.Id)));
					waitingForTrile.GlobalSpawn = true;
					GomezService.CollectedGlobalAnti += GotTrile;
				}
			});
		}
		if (GameState.SaveData.HasNewGamePlus && PatternTester.Test(Input, JetpackCode))
		{
			Input.Clear();
			GameState.JetpackMode = true;
		}
		SinceInput = TimeSpan.Zero;
	}

	private void GotTrile()
	{
		if (waitingForTrile != null && waitingForTrile.Collected)
		{
			waitingForTrile = null;
			GomezService.CollectedGlobalAnti -= GotTrile;
			if (isMapQr)
			{
				GameState.SaveData.MapCheatCodeDone = true;
			}
			else if (isAchievementCode)
			{
				GameState.SaveData.AchievementCheatCodeDone = true;
			}
			isAchievementCode = (isMapQr = false);
		}
	}
}
