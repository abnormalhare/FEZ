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

namespace FezGame.Components;

internal class QrCodesHost : GameComponent
{
	private ArtObjectInstance ArtObject;

	private readonly List<VibrationMotor> Input = new List<VibrationMotor>();

	[ServiceDependency]
	public IGomezService GomezService { get; set; }

	[ServiceDependency]
	public IGameService GameService { get; set; }

	[ServiceDependency]
	public ILevelService LevelService { get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	public QrCodesHost(Game game)
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
		ArtObject = LevelManager.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.QrCode && x.ActorSettings.VibrationPattern != null);
		if (ArtObject != null)
		{
			base.Enabled = true;
			if (GameService.IsSewerQrResolved)
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
				LevelManager.Volumes[1].Enabled = false;
				LevelManager.ArtObjects.Remove(ArtObject.Id);
				ArtObject.Dispose();
				LevelMaterializer.RegisterSatellites();
				Vector3 position = ArtObject.Position;
				if (GameState.SaveData.ThisLevel.ScriptingState == "NOT_COLLECTED")
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
						GomezService.CollectedAnti += delegate
						{
							GameState.SaveData.ThisLevel.ScriptingState = null;
						};
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
			ArtObject.ActorSettings.VibrationPattern = Util.JoinArrays(ArtObject.ActorSettings.VibrationPattern, new VibrationMotor[3]);
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
			base.Enabled = false;
			Waiters.Wait(() => CameraManager.ViewTransitionReached, Solve);
		}
	}

	private void Solve()
	{
		GameState.SaveData.ThisLevel.ScriptingState = "NOT_COLLECTED";
		ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, ArtObject, ArtObject.Position)
		{
			FlashOnSpawn = true
		});
		GameState.SaveData.ThisLevel.InactiveArtObjects.Add(ArtObject.Id);
		GameService.ResolveSewerQR();
		LevelService.ResolvePuzzle();
		GomezService.CollectedAnti += delegate
		{
			GameState.SaveData.ThisLevel.ScriptingState = null;
		};
	}

	public override void Update(GameTime gameTime)
	{
		if (!GameState.Paused && !GameState.Loading && !GameState.InMap)
		{
			CameraManager.Viewpoint.IsOrthographic();
		}
	}
}
