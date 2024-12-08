using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class RumblerHost : GameComponent
{
	private static readonly TimeSpan SignalDuration = TimeSpan.FromSeconds(0.25);

	private static readonly TimeSpan SilenceDuration = TimeSpan.FromSeconds(0.4);

	private ArtObjectInstance ArtObject;

	private int CurrentIndex;

	private VibrationMotor CurrentSignal;

	private TimeSpan SinceChanged;

	private SoundEmitter eForkRumble;

	private readonly List<VibrationMotor> Input = new List<VibrationMotor>();

	private SoundEffect ActivateSound;

	private TimeSpan CurrentDuration
	{
		get
		{
			if (CurrentSignal != 0)
			{
				return SignalDuration;
			}
			return SilenceDuration;
		}
	}

	[ServiceDependency]
	public ILevelService LevelService { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IInputManager InputManager { get; set; }

	[ServiceDependency]
	public ICodePatternService RumblerService { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	public RumblerHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		CameraManager.ViewpointChanged += CheckForPattern;
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		base.Enabled = false;
		ArtObject = LevelManager.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.Rumbler && x.ActorSettings.VibrationPattern != null);
		if (ArtObject != null)
		{
			base.Enabled = true;
			if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(ArtObject.Id))
			{
				if (ArtObject.ActorSettings.AttachedGroup.HasValue)
				{
					int value = ArtObject.ActorSettings.AttachedGroup.Value;
					TrileInstance[] array = LevelManager.Groups[ArtObject.ActorSettings.AttachedGroup.Value].Triles.ToArray();
					foreach (TrileInstance instance in array)
					{
						LevelManager.ClearTrile(instance);
					}
					LevelManager.Groups.Remove(value);
				}
				LevelManager.ArtObjects.Remove(ArtObject.Id);
				ArtObject.Dispose();
				LevelMaterializer.RegisterSatellites();
				Vector3 position = ArtObject.Position;
				if (!GameState.SaveData.ThisLevel.DestroyedTriles.Contains(new TrileEmplacement(position - Vector3.One / 2f)))
				{
					Trile trile = LevelManager.ActorTriles(ActorType.SecretCube).FirstOrDefault();
					if (trile != null)
					{
						Vector3 position2 = position - Vector3.One / 2f;
						LevelManager.ClearTrile(new TrileEmplacement(position2));
						IGameLevelManager levelManager = LevelManager;
						TrileInstance obj = new TrileInstance(position2, trile.Id)
						{
							OriginalEmplacement = new TrileEmplacement(position2)
						};
						TrileInstance trileInstance = obj;
						levelManager.RestoreTrile(obj);
						if (trileInstance.InstanceId == -1)
						{
							LevelMaterializer.CullInstanceIn(trileInstance);
						}
					}
				}
				base.Enabled = false;
			}
		}
		if (base.Enabled)
		{
			eForkRumble = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Zu/ForkRumble").Emit(loop: true, 0f, 0f);
			ActivateSound = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/ForkActivate");
			ArtObject.ActorSettings.VibrationPattern = Util.JoinArrays(ArtObject.ActorSettings.VibrationPattern, new VibrationMotor[3]);
		}
		else
		{
			ActivateSound = null;
			eForkRumble = null;
		}
	}

	private void CheckForPattern()
	{
		if (!base.Enabled || GameState.Loading)
		{
			return;
		}
		int distance = CameraManager.Viewpoint.GetDistance(CameraManager.LastViewpoint);
		Input.Add((distance == 1) ? VibrationMotor.LeftLow : VibrationMotor.RightHigh);
		if (Input.Count > 16)
		{
			Input.RemoveAt(0);
		}
		if (PatternTester.Test(Input, ArtObject.ActorSettings.VibrationPattern))
		{
			Input.Clear();
			RumblerService.OnActivate(ArtObject.Id);
			base.Enabled = false;
			Waiters.Wait(() => CameraManager.ViewTransitionReached, Solve);
		}
	}

	private void Solve()
	{
		foreach (Volume value in LevelManager.Volumes.Values)
		{
			if (value.ActorSettings != null && value.ActorSettings.IsPointOfInterest && value.BoundingBox.Contains(ArtObject.Bounds) != 0 && value.Enabled)
			{
				value.Enabled = false;
				GameState.SaveData.ThisLevel.InactiveVolumes.Add(value.Id);
			}
		}
		SoundManager.MusicVolumeFactor = 1f;
		eForkRumble.Cue.Stop();
		ActivateSound.EmitAt(ArtObject.Position);
		ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, ArtObject, ArtObject.Position)
		{
			FlashOnSpawn = true
		});
		GameState.SaveData.ThisLevel.InactiveArtObjects.Add(ArtObject.Id);
		LevelService.ResolvePuzzle();
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.Loading || GameState.InMap || !CameraManager.Viewpoint.IsOrthographic())
		{
			return;
		}
		SinceChanged += gameTime.ElapsedGameTime;
		if (SinceChanged >= CurrentDuration)
		{
			CurrentIndex++;
			if (CurrentIndex >= ArtObject.ActorSettings.VibrationPattern.Length || CurrentIndex < 0)
			{
				CurrentIndex = 0;
			}
			SinceChanged -= CurrentDuration;
			if (ArtObject.ActorSettings.VibrationPattern.Length == 0)
			{
				CurrentSignal = VibrationMotor.None;
			}
			else
			{
				CurrentSignal = ArtObject.ActorSettings.VibrationPattern[CurrentIndex];
			}
		}
		Vector3 vector = ((PlayerManager.Center - ArtObject.Position) * CameraManager.Viewpoint.ScreenSpaceMask()).Abs();
		vector -= new Vector3(0.5f, 2f, 0.5f);
		vector /= 3f;
		float num = FezMath.Saturate(1f - vector.Saturate().Length());
		if (CurrentSignal == VibrationMotor.None)
		{
			eForkRumble.VolumeFactor *= Math.Max(1f - (float)Math.Pow(SinceChanged.TotalSeconds / CurrentDuration.TotalSeconds, 4.0), 0.75f);
			SoundManager.MusicVolumeFactor = 1f - num * 0.6f - eForkRumble.VolumeFactor * 0.2f;
			return;
		}
		eForkRumble.VolumeFactor = num;
		eForkRumble.Pan = ((CurrentSignal == VibrationMotor.RightHigh) ? 1 : (-1));
		SoundManager.MusicVolumeFactor = 1f - num * 0.8f;
		if (num != 1f)
		{
			num *= 0.5f;
		}
		if (CurrentSignal == VibrationMotor.LeftLow)
		{
			num *= 0.5f;
		}
		if (num > 0f)
		{
			InputManager.ActiveGamepad.Vibrate(CurrentSignal, num, CurrentDuration - SinceChanged, EasingType.None);
		}
		else
		{
			Input.Clear();
		}
	}
}
