using System;
using System.Linq;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class OwlHeadHost : GameComponent
{
	private ArtObjectInstance OwlHeadAo;

	private ArtObjectInstance AttachedCandlesAo;

	private SoundEffect sRumble;

	private SoundEmitter eRumble;

	private Quaternion InterpolatedRotation;

	private Quaternion OriginalRotation;

	private Vector3 OriginalTranslation;

	private bool IsBig;

	private bool IsInverted;

	private float SinceStarted;

	private float lastAngle;

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	public OwlHeadHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		sRumble = null;
		eRumble = null;
		OwlHeadAo = LevelManager.ArtObjects.Values.SingleOrDefault((ArtObjectInstance x) => x.ArtObjectName == "OWL_STATUE_HEADAO" || x.ArtObjectName == "BIG_OWL_HEADAO" || x.ArtObjectName == "OWL_STATUE_DRAPES_BAO");
		AttachedCandlesAo = null;
		IsBig = OwlHeadAo != null && OwlHeadAo.ArtObjectName == "BIG_OWL_HEADAO";
		base.Enabled = OwlHeadAo != null;
		SinceStarted = 0f;
		if (base.Enabled)
		{
			sRumble = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/Rumble");
			eRumble = sRumble.EmitAt(OwlHeadAo.Position, loop: true, paused: true);
			eRumble.VolumeFactor = 0f;
			IsInverted = false;
			if (OwlHeadAo.ArtObjectName == "OWL_STATUE_DRAPES_BAO")
			{
				AttachedCandlesAo = LevelManager.ArtObjects[14];
				OriginalRotation = Quaternion.Identity;
				IsInverted = true;
			}
			else
			{
				OriginalRotation = OwlHeadAo.Rotation * Quaternion.CreateFromAxisAngle(Vector3.Up, CameraManager.Viewpoint.ToPhi());
			}
			OriginalTranslation = OwlHeadAo.Position;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || GameState.InFpsMode)
		{
			return;
		}
		InterpolatedRotation = Quaternion.Slerp(InterpolatedRotation, OriginalRotation * CameraManager.Rotation, 0.075f);
		if (InterpolatedRotation == CameraManager.Rotation)
		{
			if (!eRumble.Dead && eRumble.Cue.State != SoundState.Paused)
			{
				eRumble.Cue.Pause();
			}
			return;
		}
		ToAxisAngle(ref InterpolatedRotation, out var _, out var angle);
		float value = lastAngle - angle;
		if (eRumble.Cue.State == SoundState.Paused)
		{
			eRumble.Cue.Resume();
		}
		eRumble.VolumeFactor = Math.Min(Easing.EaseOut(FezMath.Saturate(Math.Abs(value) * 10f), EasingType.Quadratic), 0.5f) * FezMath.Saturate(SinceStarted);
		SinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds;
		lastAngle = angle;
		if (FezMath.AlmostEqual(InterpolatedRotation, CameraManager.Rotation) || FezMath.AlmostEqual(-InterpolatedRotation, CameraManager.Rotation))
		{
			InterpolatedRotation = CameraManager.Rotation;
		}
		Matrix matrix;
		if (IsInverted)
		{
			matrix = Matrix.CreateTranslation(0.25f, 0f, -0.75f) * Matrix.CreateFromQuaternion(InterpolatedRotation) * Matrix.CreateTranslation(-0.75f + OriginalTranslation.X, OriginalTranslation.Y, -0.25f + OriginalTranslation.Z);
			AttachedCandlesAo.Rotation = InterpolatedRotation;
		}
		else
		{
			matrix = Matrix.CreateTranslation((float)(IsBig ? 8 : 4) / 16f, 0f, (float)(-(IsBig ? 24 : 12)) / 16f) * Matrix.CreateFromQuaternion(InterpolatedRotation) * Matrix.CreateTranslation((float)(-(IsBig ? 8 : 4)) / 16f + OriginalTranslation.X, OriginalTranslation.Y, (float)(IsBig ? 24 : 12) / 16f + OriginalTranslation.Z);
		}
		matrix.Decompose(out var _, out var rotation, out var translation);
		OwlHeadAo.Position = translation;
		OwlHeadAo.Rotation = rotation;
	}

	private static void ToAxisAngle(ref Quaternion q, out Vector3 axis, out float angle)
	{
		angle = (float)Math.Acos(MathHelper.Clamp(q.W, -1f, 1f));
		float num = (float)Math.Sin(angle);
		float num2 = 1f / ((num == 0f) ? 1f : num);
		angle *= 2f;
		axis = new Vector3((0f - q.X) * num2, (0f - q.Y) * num2, (0f - q.Z) * num2);
	}
}
