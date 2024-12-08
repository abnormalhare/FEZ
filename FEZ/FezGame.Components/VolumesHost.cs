using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Components.Scripting;
using FezGame.Services;
using FezGame.Services.Scripting;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class VolumesHost : GameComponent
{
	private Volume[] levelVolumes;

	private readonly List<CodeInput> Input = new List<CodeInput>();

	private TimeSpan SinceInput;

	private bool deferredScripts;

	private bool checkForContainment;

	private bool pendingCheck;

	[ServiceDependency]
	public ILevelService LevelService { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IVolumeService VolumeService { private get; set; }

	public VolumesHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		RegisterVolumes();
		TestVolumes(force: false);
		LevelManager.LevelChanged += DisableTriggeredVolumes;
		LevelManager.LevelChanged += RegisterVolumes;
		LevelManager.LevelChanged += delegate
		{
			TestVolumes(force: true);
		};
	}

	private void DisableTriggeredVolumes()
	{
		foreach (Volume value2 in LevelManager.Volumes.Values)
		{
			if (value2.ActorSettings != null && value2.ActorSettings.NeedsTrigger)
			{
				value2.Enabled = false;
			}
		}
		foreach (int inactiveVolume in GameState.SaveData.ThisLevel.InactiveVolumes)
		{
			if ((!(LevelManager.Name == "ZU_CITY_RUINS") || inactiveVolume != 2) && (!(LevelManager.Name == "TELESCOPE") || inactiveVolume != 4 || GameState.SaveData.ThisLevel.DestroyedTriles.Contains(new TrileEmplacement(18, 36, 20))) && LevelManager.Volumes.TryGetValue(inactiveVolume, out var value))
			{
				value.Enabled = !value.Enabled;
			}
		}
		checkForContainment = LevelManager.Name == "RITUAL";
		Input.Clear();
		pendingCheck = false;
	}

	public override void Update(GameTime gameTime)
	{
		if (!CameraManager.Viewpoint.IsOrthographic() || GameState.InMap || GameState.Paused || GameState.Loading)
		{
			return;
		}
		pendingCheck |= GrabInput();
		if (!LevelManager.IsInvalidatingScreen)
		{
			if (LevelManager.Volumes.Count != levelVolumes.Length || VolumeService.RegisterNeeded)
			{
				RegisterVolumes();
			}
			if (levelVolumes.Length != 0)
			{
				TestVolumes(force: false);
				SinceInput += gameTime.ElapsedGameTime;
			}
		}
	}

	private void HeightCheck()
	{
		SoundService.ImmediateEffect = true;
		Volume[] array = levelVolumes;
		foreach (Volume volume in array)
		{
			bool flag = CameraManager.Center.Y > (volume.From.Y + volume.To.Y) / 2f;
			if (flag)
			{
				VolumeService.OnGoHigher(volume.Id);
			}
			else
			{
				VolumeService.OnGoLower(volume.Id);
			}
			volume.PlayerIsHigher = flag;
		}
	}

	private void RegisterVolumes()
	{
		VolumeService.RegisterNeeded = false;
		levelVolumes = LevelManager.Volumes.Values.ToArray();
	}

	private void TestVolumes(bool force)
	{
		if (!force && GameState.Loading)
		{
			return;
		}
		if (!force && deferredScripts)
		{
			foreach (Volume currentVolume in PlayerManager.CurrentVolumes)
			{
				VolumeService.OnEnter(currentVolume.Id);
			}
			HeightCheck();
			deferredScripts = false;
		}
		else
		{
			SoundService.ImmediateEffect = false;
		}
		if (force)
		{
			deferredScripts = true;
		}
		Vector3 mask = CameraManager.Viewpoint.VisibleAxis().GetMask();
		Vector3 vector = CameraManager.Viewpoint.ForwardVector();
		if (PlayerManager.Background)
		{
			vector *= -1f;
		}
		Ray ray = default(Ray);
		ray.Position = PlayerManager.Center * (Vector3.One - mask) - vector * LevelManager.Size;
		ray.Direction = vector;
		Ray ray2 = ray;
		if (PlayerManager.Action == ActionType.PullUpBack || PlayerManager.Action == ActionType.PullUpFront || PlayerManager.Action == ActionType.PullUpCornerLedge)
		{
			ray2.Position += new Vector3(0f, 0.5f, 0f);
		}
		Volume[] array = levelVolumes;
		foreach (Volume volume in array)
		{
			if (!volume.Enabled)
			{
				continue;
			}
			if (!GameState.FarawaySettings.InTransition)
			{
				bool flag = CameraManager.Center.Y > (volume.From.Y + volume.To.Y) / 2f;
				if (!volume.PlayerIsHigher.HasValue || flag != volume.PlayerIsHigher.Value)
				{
					if (flag)
					{
						VolumeService.OnGoHigher(volume.Id);
					}
					else
					{
						VolumeService.OnGoLower(volume.Id);
					}
					volume.PlayerIsHigher = flag;
				}
			}
			if (checkForContainment && (volume.Id == 1 || volume.Id == 2))
			{
				if (volume.BoundingBox.Contains(PlayerManager.Position) != 0)
				{
					PlayerIsInside(volume, force);
				}
				continue;
			}
			float? num = volume.BoundingBox.Intersects(ray2);
			if (volume.ActorSettings != null && volume.ActorSettings.IsBlackHole)
			{
				if (!num.HasValue)
				{
					num = volume.BoundingBox.Intersects(new Ray(ray2.Position + new Vector3(0f, 0.3f, 0f), ray2.Direction));
				}
				if (!num.HasValue)
				{
					num = volume.BoundingBox.Intersects(new Ray(ray2.Position - new Vector3(0f, 0.3f, 0f), ray2.Direction));
				}
			}
			if (num.HasValue)
			{
				bool flag2 = false;
				bool isBlackHole = volume.ActorSettings != null && volume.ActorSettings.IsBlackHole;
				if (PlayerManager.CarriedInstance != null)
				{
					PlayerManager.CarriedInstance.PhysicsState.UpdatingPhysics = true;
				}
				NearestTriles nearestTriles = LevelManager.NearestTrile(ray2.Position, PlayerManager.Background ? QueryOptions.Background : QueryOptions.None);
				if (LevelManager.Name != "PIVOT_TWO" && nearestTriles.Surface != null)
				{
					flag2 |= TestObstruction(nearestTriles.Surface, num.Value, ray2.Position, isBlackHole);
				}
				if (nearestTriles.Deep != null)
				{
					flag2 |= TestObstruction(nearestTriles.Deep, num.Value, ray2.Position, isBlackHole);
				}
				if (PlayerManager.CarriedInstance != null)
				{
					PlayerManager.CarriedInstance.PhysicsState.UpdatingPhysics = false;
				}
				if (!flag2 && ((volume.ActorSettings != null && volume.ActorSettings.IsBlackHole) || volume.Orientations.Contains(CameraManager.VisibleOrientation)))
				{
					PlayerIsInside(volume, force);
				}
			}
		}
		for (int num2 = PlayerManager.CurrentVolumes.Count - 1; num2 >= 0; num2--)
		{
			Volume volume2 = PlayerManager.CurrentVolumes[num2];
			if (!volume2.PlayerInside)
			{
				if (!force)
				{
					VolumeService.OnExit(volume2.Id);
				}
				PlayerManager.CurrentVolumes.RemoveAt(num2);
			}
			volume2.PlayerInside = false;
		}
		if (PlayerManager.CurrentVolumes.Count <= 0 || GameState.FarawaySettings.InTransition || GameState.DotLoading)
		{
			return;
		}
		if (PlayerManager.Action == ActionType.LesserWarp || PlayerManager.Action == ActionType.GateWarp)
		{
			Input.Clear();
		}
		if (!pendingCheck)
		{
			return;
		}
		foreach (Volume currentVolume2 in PlayerManager.CurrentVolumes)
		{
			if (currentVolume2.ActorSettings != null && currentVolume2.ActorSettings.CodePattern != null && currentVolume2.ActorSettings.CodePattern.Length != 0)
			{
				TestCodePattern(currentVolume2);
			}
		}
	}

	private bool GrabInput()
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
			return false;
		}
		Input.Add(codeInput);
		if (Input.Count > 16)
		{
			Input.RemoveAt(0);
		}
		return true;
	}

	private void TestCodePattern(Volume volume)
	{
		if (PatternTester.Test(Input, volume.ActorSettings.CodePattern))
		{
			Input.Clear();
			Waiters.Wait(() => CameraManager.ViewTransitionReached, delegate
			{
				ScriptingHost.ScriptExecuted = false;
				VolumeService.OnCodeAccepted(volume.Id);
				if (ScriptingHost.ScriptExecuted)
				{
					GameState.SaveData.AnyCodeDeciphered = true;
					LevelService.ResolvePuzzle();
				}
			});
		}
		SinceInput = TimeSpan.Zero;
	}

	private bool TestObstruction(TrileInstance trile, float hitDistance, Vector3 hitStart, bool isBlackHole)
	{
		Vector3 vector = CameraManager.Viewpoint.ForwardVector();
		if (PlayerManager.Background)
		{
			vector *= -1f;
		}
		if (trile != null && trile.Enabled && !trile.Trile.Immaterial && (trile.Trile.ActorSettings.Type != ActorType.Hole || isBlackHole))
		{
			return (trile.Emplacement.AsVector + Vector3.One / 2f + vector * -0.5f - hitStart).Dot(vector) <= hitDistance + 0.25f;
		}
		return false;
	}

	private void PlayerIsInside(Volume volume, bool force)
	{
		volume.PlayerInside = true;
		if (!PlayerManager.CurrentVolumes.Contains(volume))
		{
			PlayerManager.CurrentVolumes.Add(volume);
			if (!force)
			{
				VolumeService.OnEnter(volume.Id);
			}
		}
	}
}
