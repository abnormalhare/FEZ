using Common;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezEngine.Structure;

public class SoundEmitter
{
	private readonly float VolumeLevel;

	private bool factorizeVolume;

	private bool pausedForViewTransition;

	public bool New = true;

	private Vector3? position;

	private float pitch;

	private IWaiter deathWaiter;

	private IWaiter fadePauseWaiter;

	public SoundEffectInstance Cue { get; private set; }

	public bool FactorizeVolume
	{
		get
		{
			return factorizeVolume;
		}
		set
		{
			factorizeVolume = value;
		}
	}

	public bool PauseViewTransitions { get; set; }

	public float VolumeMaster { get; set; }

	public float VolumeFactor { get; set; }

	public float FadeDistance { get; set; }

	public float NonFactorizedVolume { get; set; }

	public bool Persistent { get; set; }

	public bool NoAttenuation { get; set; }

	public Vector3 AxisMask { get; set; }

	public bool OverrideMap { get; set; }

	public bool WasPlaying { get; set; }

	public bool LowPass { get; private set; }

	public Vector3 Position
	{
		get
		{
			if (!position.HasValue)
			{
				return Vector3.Zero;
			}
			return position.Value;
		}
		set
		{
			position = value;
		}
	}

	public float Pitch
	{
		get
		{
			return pitch;
		}
		set
		{
			Cue.Pitch = (pitch = value);
		}
	}

	public float Pan { get; set; }

	public bool Dead
	{
		get
		{
			if (Cue != null && !Cue.IsDisposed)
			{
				return Cue.State == SoundState.Stopped;
			}
			return true;
		}
	}

	[ServiceDependency]
	public ISoundManager SM { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	internal SoundEmitter(SoundEffect sound, bool looped, float pitch, float volumeFactor, bool paused, Vector3? position)
	{
		SM = ServiceHelper.Get<ISoundManager>();
		EngineState = ServiceHelper.Get<IEngineStateManager>();
		CameraManager = ServiceHelper.Get<IDefaultCameraManager>();
		ILevelManager levelManager = ServiceHelper.Get<ILevelManager>();
		VolumeLevel = SM.GetVolumeLevelFor(sound.Name);
		VolumeMaster = ((!EngineState.DotLoading) ? 1 : 0);
		FadeDistance = 10f;
		AxisMask = Vector3.One;
		VolumeFactor = volumeFactor;
		this.position = position;
		if (SoundManager.NoMoreSounds)
		{
			return;
		}
		try
		{
			Cue = sound.CreateInstance();
			Pitch = pitch;
			Cue.IsLooped = looped;
			if (!paused)
			{
				Update();
				Cue.Play();
			}
			else
			{
				Cue.Volume = 0f;
				Cue.Play();
				Cue.Pause();
			}
			LowPass = !levelManager.LowPass && !sound.Name.Contains("Ui") && !sound.Name.Contains("Warp") && !sound.Name.Contains("Zoom") && !sound.Name.Contains("Trixel");
			if (LowPass)
			{
				(SM as SoundManager).RegisterLowPass(Cue);
			}
		}
		catch (InstancePlayLimitException)
		{
			Logger.Log("SoundEmitter", LogSeverity.Warning, "Couldn't create sound instance (too many instances)");
		}
	}

	public void Update()
	{
		if (Cue == null)
		{
			return;
		}
		if (PauseViewTransitions)
		{
			if (Cue.State == SoundState.Paused && CameraManager.ViewTransitionReached && pausedForViewTransition)
			{
				pausedForViewTransition = false;
				Cue.Resume();
			}
			else if (Cue.State == SoundState.Playing && !CameraManager.ViewTransitionReached)
			{
				pausedForViewTransition = true;
				Cue.Pause();
			}
		}
		if (Cue.State == SoundState.Paused || (EngineState.InMap && !OverrideMap))
		{
			return;
		}
		if (position.HasValue)
		{
			Vector3 right = CameraManager.InverseView.Right;
			Vector3 interpolatedCenter = CameraManager.InterpolatedCenter;
			Vector2 vector = default(Vector2);
			vector.X = (Position - interpolatedCenter).Dot(right);
			vector.Y = interpolatedCenter.Y - Position.Y;
			Vector2 vector2 = vector;
			float nonFactorizedVolume = 1f;
			if (!NoAttenuation)
			{
				float num = vector2.Length();
				nonFactorizedVolume = ((!(num <= 10f)) ? (0.6f / ((num - 10f) / 5f + 1f)) : (1f - Easing.EaseIn(num / 10f, EasingType.Quadratic) * 0.4f));
			}
			NonFactorizedVolume = nonFactorizedVolume;
			Cue.Volume = FezMath.Saturate(NonFactorizedVolume * VolumeFactor * VolumeLevel * VolumeMaster);
			Cue.Pan = MathHelper.Clamp(vector2.X / SM.LimitDistance.X * 1.5f, -1f, 1f);
		}
		else
		{
			Cue.Volume = FezMath.Saturate(VolumeFactor * VolumeLevel * VolumeMaster);
			Cue.Pan = Pan;
		}
	}

	public void FadeOutAndDie(float forSeconds, bool autoPause)
	{
		if (forSeconds == 0f)
		{
			if (Cue != null && !Cue.IsDisposed && Cue.State != SoundState.Stopped)
			{
				Cue.Stop();
			}
		}
		else
		{
			if (Dead || deathWaiter != null)
			{
				return;
			}
			float volumeFactor = VolumeFactor * VolumeLevel * VolumeMaster;
			deathWaiter = Waiters.Interpolate(forSeconds, delegate(float s)
			{
				VolumeFactor = volumeFactor * (1f - s);
			}, delegate
			{
				if (Cue != null && !Cue.IsDisposed && Cue.State != SoundState.Stopped)
				{
					Cue.Stop();
				}
				deathWaiter = null;
			});
			deathWaiter.AutoPause = autoPause;
		}
	}

	public void FadeOutAndDie(float forSeconds)
	{
		FadeOutAndDie(forSeconds, autoPause: true);
	}

	public void FadeOutAndPause(float forSeconds)
	{
		if (forSeconds == 0f)
		{
			if (Cue != null && !Cue.IsDisposed && Cue.State != SoundState.Paused)
			{
				Cue.Pause();
			}
		}
		else
		{
			if (Dead || fadePauseWaiter != null)
			{
				return;
			}
			float volumeFactor = VolumeFactor * VolumeLevel * VolumeMaster;
			fadePauseWaiter = Waiters.Interpolate(forSeconds, delegate(float s)
			{
				VolumeFactor = volumeFactor * (1f - s);
			}, delegate
			{
				if (Cue != null && !Cue.IsDisposed && Cue.State != SoundState.Paused)
				{
					Cue.Pause();
				}
				fadePauseWaiter = null;
			});
			fadePauseWaiter.AutoPause = true;
		}
	}

	public void Dispose()
	{
		if (Cue != null)
		{
			Cue.Dispose();
		}
		Cue = null;
	}
}
