using System;
using System.Linq;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Components.Scripting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Services.Scripting;

public class VolumeService : IVolumeService, IScriptingBase
{
	public bool RegisterNeeded { get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IDotManager Dot { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	public event Action<int> Enter = Util.NullAction;

	public event Action<int> Exit = Util.NullAction;

	public event Action<int> GoLower;

	public event Action<int> GoHigher;

	public event Action<int> CodeAccepted;

	public void OnEnter(int id)
	{
		this.Enter(id);
	}

	public void OnExit(int id)
	{
		this.Exit(id);
	}

	public void OnGoLower(int id)
	{
		if (this.GoLower != null)
		{
			this.GoLower(id);
		}
	}

	public void OnGoHigher(int id)
	{
		if (this.GoHigher != null)
		{
			this.GoHigher(id);
		}
	}

	public void OnCodeAccepted(int id)
	{
		ScriptingHost.ScriptExecuted = false;
		if (this.CodeAccepted != null)
		{
			this.CodeAccepted(id);
		}
		if (ScriptingHost.ScriptExecuted)
		{
			SetEnabled(id, enabled: false, permanent: true);
		}
	}

	public bool get_GomezInside(int id)
	{
		return PlayerManager.CurrentVolumes.Any((Volume x) => x.Id == id);
	}

	public bool get_IsEnabled(int id)
	{
		return LevelManager.Volumes[id].Enabled;
	}

	public LongRunningAction MoveDotWithCamera(int id)
	{
		Volume volume = LevelManager.Volumes[id];
		Vector3 target = (volume.From + volume.To) / 2f;
		PlayerManager.CanControl = false;
		Dot.PreventPoI = true;
		Dot.MoveWithCamera(target, burrowAfter: false);
		return new LongRunningAction((float _, float __) => Dot.Behaviour == DotHost.BehaviourType.WaitAtTarget, delegate
		{
			PlayerManager.CanControl = true;
		});
	}

	public LongRunningAction FocusCamera(int id, int pixelsPerTrixel, bool immediate)
	{
		string levelName = LevelManager.Name;
		Volume volume = LevelManager.Volumes[id];
		BoundingBox boundingBox = volume.BoundingBox;
		bool changedViewpoint = volume.Orientations.Count == 1;
		Viewpoint oldViewpoint = CameraManager.Viewpoint;
		float oldRadius = CameraManager.Radius;
		CameraManager.Constrained = true;
		CameraManager.Center = (boundingBox.Min + boundingBox.Max) / 2f;
		if (pixelsPerTrixel > 0)
		{
			CameraManager.PixelsPerTrixel = pixelsPerTrixel;
		}
		if (changedViewpoint)
		{
			CameraManager.ChangeViewpoint(volume.Orientations.First().AsViewpoint());
		}
		CameraManager.ConstrainedCenter = CameraManager.Center;
		if (immediate)
		{
			CameraManager.SnapInterpolation();
			return null;
		}
		return new LongRunningAction(delegate
		{
			if (!(LevelManager.Name != levelName))
			{
				CameraManager.Constrained = false;
				if (pixelsPerTrixel > 0)
				{
					CameraManager.Radius = oldRadius;
				}
				if (changedViewpoint)
				{
					CameraManager.ChangeViewpoint(oldViewpoint);
				}
			}
		});
	}

	public void SetEnabled(int id, bool enabled, bool permanent)
	{
		LevelManager.Volumes[id].Enabled = enabled;
		if (permanent)
		{
			GameState.SaveData.ThisLevel.InactiveVolumes.Add(id);
			GameState.Save();
		}
	}

	public void SlowFocusOn(int id, float duration, float trixPerPix)
	{
		Volume volume = LevelManager.Volumes[id];
		Vector3 center = (volume.From + volume.To) / 2f;
		CameraManager.Constrained = true;
		Vector3 c = CameraManager.Center;
		float currentTpP = CameraManager.PixelsPerTrixel;
		Waiters.Interpolate(duration, delegate(float s)
		{
			float amount = Easing.EaseInOut(s, EasingType.Sine);
			CameraManager.PixelsPerTrixel = MathHelper.Lerp(currentTpP, trixPerPix, amount);
			CameraManager.Center = Vector3.Lerp(c, center - new Vector3(0f, 1f, 0f), amount);
		}).AutoPause = true;
	}

	public LongRunningAction LoadHexahedronAt(int id, string toLevel)
	{
		Volume volume = LevelManager.Volumes[id];
		Vector3 center = (volume.From + volume.To) / 2f;
		Worker<NowLoadingHexahedron> worker = ThreadPool.Take<NowLoadingHexahedron>(LoadHex);
		NowLoadingHexahedron nowLoadingHexahedron = new NowLoadingHexahedron(ServiceHelper.Game, center, toLevel);
		worker.Start(nowLoadingHexahedron);
		worker.Finished += delegate
		{
			ThreadPool.Return(worker);
		};
		bool disposed = false;
		nowLoadingHexahedron.Disposed += delegate
		{
			disposed = true;
		};
		return new LongRunningAction((float _, float __) => disposed);
	}

	private static void LoadHex(NowLoadingHexahedron nlh)
	{
		ServiceHelper.AddComponent(nlh);
	}

	public LongRunningAction PlaySoundAt(int id, string soundName, bool loop, float initialDelay, float perLoopDelay, bool directional, float pitchVariation)
	{
		SoundEffect sfx = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/" + soundName);
		float duration = (float)sfx.Duration.TotalSeconds;
		Volume volume = LevelManager.Volumes[id];
		Vector3 center = (volume.From + volume.To) / 2f;
		Vector3 direction = Vector3.One;
		if (directional)
		{
			direction = volume.Orientations.Select((FaceOrientation x) => x.AsAxis().GetMask()).Aggregate((Vector3 a, Vector3 b) => a + b);
		}
		if (!loop && initialDelay <= 0f)
		{
			sfx.EmitAt(center, RandomHelper.Centered(pitchVariation)).AxisMask = direction;
			return null;
		}
		float toWait = initialDelay;
		bool perfectLoop = loop && perLoopDelay <= 0f && pitchVariation <= 0f;
		if (perfectLoop)
		{
			return new LongRunningAction(delegate(float elapsed, float total)
			{
				toWait -= elapsed;
				if (toWait <= 0f)
				{
					sfx.EmitAt(center, perfectLoop, RandomHelper.Centered(pitchVariation)).AxisMask = direction;
					return true;
				}
				return false;
			});
		}
		return new LongRunningAction(delegate(float elapsed, float total)
		{
			toWait -= elapsed;
			if (toWait <= 0f)
			{
				sfx.EmitAt(center, RandomHelper.Centered(pitchVariation)).AxisMask = direction;
				if (!loop)
				{
					return true;
				}
				toWait += perLoopDelay + duration;
			}
			return false;
		});
	}

	public LongRunningAction FocusWithPan(int id, int pixelsPerTrixel, float verticalPan, float horizontalPan)
	{
		string levelName = LevelManager.Name;
		Volume volume = LevelManager.Volumes[id];
		BoundingBox boundingBox = volume.BoundingBox;
		bool changedViewpoint = volume.Orientations.Count == 1;
		Viewpoint oldViewpoint = CameraManager.Viewpoint;
		float oldRadius = CameraManager.Radius;
		CameraManager.Constrained = true;
		CameraManager.Center = (boundingBox.Min + boundingBox.Max) / 2f;
		CameraManager.PanningConstraints = new Vector2(horizontalPan, verticalPan);
		if (pixelsPerTrixel > 0)
		{
			CameraManager.PixelsPerTrixel = pixelsPerTrixel;
		}
		if (changedViewpoint)
		{
			CameraManager.ChangeViewpoint(volume.Orientations.First().AsViewpoint());
		}
		CameraManager.ConstrainedCenter = CameraManager.Center;
		return new LongRunningAction(delegate
		{
			if (!(LevelManager.Name != levelName) && (!(levelName == "CRYPT") || !(LevelManager.Name == "CRYPT")))
			{
				CameraManager.Constrained = false;
				CameraManager.PanningConstraints = null;
				if (pixelsPerTrixel > 0)
				{
					CameraManager.Radius = oldRadius;
				}
				if (changedViewpoint)
				{
					CameraManager.ChangeViewpoint(oldViewpoint);
				}
			}
		});
	}

	public void SpawnTrileAt(int id, string actorTypeName)
	{
		BoundingBox boundingBox = LevelManager.Volumes[id].BoundingBox;
		Vector3 vector = (boundingBox.Min + boundingBox.Max) / 2f;
		Trile trile = LevelManager.ActorTriles((ActorType)Enum.Parse(typeof(ActorType), actorTypeName, ignoreCase: true)).FirstOrDefault();
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
			ServiceHelper.AddComponent(new GlitchyRespawner(ServiceHelper.Game, new TrileInstance(vector2, trile.Id)
			{
				OriginalEmplacement = new TrileEmplacement(vector2)
			}));
		}
	}

	public void ResetEvents()
	{
		this.Enter = Util.NullAction;
		this.Exit = Util.NullAction;
		this.GoHigher = (this.GoLower = null);
		this.CodeAccepted = null;
	}
}
