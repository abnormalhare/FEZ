using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class SpinBlocksHost : GameComponent
{
	private class SpinBlockState
	{
		private const float WarnTime = 0.1f;

		private const float SpinTime = 0.5f;

		private const float Charge = 0.1f;

		private readonly List<TrileInstance> Triles;

		private readonly ArtObjectInstance ArtObject;

		private readonly Vector3 OriginalPosition;

		private readonly Vector3 RotationOffset;

		private readonly bool IsRotato;

		private SpinState State;

		private TimeSpan SinceChanged;

		private Quaternion OriginalRotation;

		private Quaternion SpinAccumulatedRotation = Quaternion.Identity;

		private Vector3 OriginalPlayerPosition;

		private readonly SoundEffect SoundEffect;

		private SoundEmitter Emitter;

		private bool hasRotated;

		[ServiceDependency]
		public ILevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IDefaultCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public ISoundManager SoundManager { private get; set; }

		[ServiceDependency]
		public ITrixelParticleSystems TrixelParticleSystems { private get; set; }

		public SpinBlockState(List<TrileInstance> triles, ArtObjectInstance aoInstance, SoundEffect soundEffect)
		{
			ServiceHelper.InjectServices(this);
			Triles = triles;
			ArtObject = aoInstance;
			OriginalPosition = ArtObject.Position;
			if (ArtObject.ActorSettings.OffCenter)
			{
				RotationOffset = ArtObject.ActorSettings.RotationCenter - ArtObject.Position;
			}
			if (ArtObject.ActorSettings.SpinView == Viewpoint.None)
			{
				ArtObject.ActorSettings.SpinView = Viewpoint.Front;
			}
			foreach (TrileInstance trile in Triles)
			{
				trile.Unsafe = true;
			}
			SoundEffect = soundEffect;
			SinceChanged -= TimeSpan.FromSeconds(ArtObject.ActorSettings.SpinOffset);
			IsRotato = ArtObject.ActorSettings.SpinView == Viewpoint.Up || ArtObject.ActorSettings.SpinView == Viewpoint.Down;
		}

		public void Update(TimeSpan elapsed)
		{
			if (ArtObject.ActorSettings.Inactive && State == SpinState.Idle)
			{
				return;
			}
			SinceChanged += elapsed;
			switch (State)
			{
			case SpinState.Idle:
			{
				if (!(SinceChanged.TotalSeconds >= (double)(ArtObject.ActorSettings.SpinEvery - 0.5f - 0.1f)))
				{
					break;
				}
				OriginalRotation = ArtObject.Rotation;
				SinceChanged -= TimeSpan.FromSeconds(ArtObject.ActorSettings.SpinEvery - 0.5f - 0.1f);
				State = SpinState.Warning;
				Vector3 right = CameraManager.InverseView.Right;
				Vector3 interpolatedCenter = CameraManager.InterpolatedCenter;
				Vector2 vector2 = default(Vector2);
				vector2.X = (ArtObject.Position - interpolatedCenter).Dot(right);
				vector2.Y = interpolatedCenter.Y - ArtObject.Position.Y;
				Vector2 vector3 = vector2;
				float num3 = vector3.Length();
				float num4 = 1f;
				num4 = ((!(num3 <= 10f)) ? (0.6f / ((num3 - 10f) / 5f + 1f)) : (1f - Easing.EaseIn(num3 / 10f, EasingType.Quadratic) * 0.4f));
				if (num4 > 0.05f)
				{
					Emitter = SoundEffect.EmitAt(ArtObject.Position, RandomHelper.Centered(0.07999999821186066));
					if (IsRotato)
					{
						Emitter.PauseViewTransitions = false;
					}
				}
				break;
			}
			case SpinState.Warning:
			{
				float num5 = (float)Math.Sin(FezMath.Saturate(SinceChanged.TotalSeconds / 0.10000000149011612) * 0.7853981852531433);
				Quaternion quaternion2 = Quaternion.CreateFromAxisAngle(ArtObject.ActorSettings.SpinView.ForwardVector(), -(float)Math.PI / 2f * num5 * 0.1f);
				ArtObject.Rotation = quaternion2 * OriginalRotation;
				ArtObject.Position = OriginalPosition + Vector3.Transform(-RotationOffset, SpinAccumulatedRotation * quaternion2) + RotationOffset;
				if (SinceChanged.TotalSeconds >= 0.10000000149011612)
				{
					SinceChanged -= TimeSpan.FromSeconds(0.10000000149011612);
					State = SpinState.Spinning;
				}
				break;
			}
			case SpinState.Spinning:
			{
				double num = FezMath.Saturate(SinceChanged.TotalSeconds / 0.5);
				float num2;
				if (!IsRotato)
				{
					num2 = (float)((num < 0.75) ? (num / 0.75) : (1.0 + Math.Sin((num - 0.75) / 0.25 * 6.2831854820251465) * 0.014999999664723873));
					num2 = Easing.EaseIn(num2, EasingType.Quintic);
				}
				else
				{
					num2 = Easing.EaseInOut(FezMath.Saturate(num / 0.75), EasingType.Quartic, EasingType.Quadratic);
				}
				bool flag = PlayerManager.Grounded && Triles.Contains(PlayerManager.Ground.First);
				if (flag)
				{
					if (!IsRotato)
					{
						PlayerManager.Velocity += ArtObject.ActorSettings.SpinView.RightVector() * num2 * 0.1f;
						if ((double)num2 > 0.25)
						{
							PlayerManager.Position -= 0.010000001f * Vector3.UnitY;
						}
					}
					else if (num2 > 0f && !hasRotated)
					{
						PlayerManager.IsOnRotato = true;
						Rotate();
						OriginalPlayerPosition = PlayerManager.Position;
						int distance = ((ArtObject.ActorSettings.SpinView == Viewpoint.Up) ? 1 : (-1));
						CameraManager.ChangeViewpoint(CameraManager.Viewpoint.GetRotatedView(distance), 0.5f);
						hasRotated = true;
					}
				}
				if (!IsRotato)
				{
					foreach (TrileInstance trile in Triles)
					{
						trile.Enabled = (double)num2 <= 0.25;
					}
				}
				bool flag2 = PlayerManager.Action.IsOnLedge() && Triles.Contains(PlayerManager.HeldInstance);
				if (flag2)
				{
					if (!IsRotato)
					{
						PlayerManager.Velocity += ArtObject.ActorSettings.SpinView.RightVector() * num2 * 0.1f;
						if ((double)num2 > 0.25)
						{
							PlayerManager.Action = ActionType.Falling;
							PlayerManager.HeldInstance = null;
						}
					}
					else if (num2 > 0f && (double)num2 < 0.5 && !hasRotated)
					{
						PlayerManager.IsOnRotato = true;
						Rotate();
						OriginalPlayerPosition = PlayerManager.Position;
						int distance2 = ((ArtObject.ActorSettings.SpinView == Viewpoint.Up) ? 1 : (-1));
						CameraManager.ChangeViewpoint(CameraManager.Viewpoint.GetRotatedView(distance2), 0.5f);
						hasRotated = true;
					}
				}
				TrixelParticleSystems.PropagateEnergy(ArtObject.Position - ArtObject.ActorSettings.SpinView.RightVector(), num2 * 0.1f);
				Quaternion quaternion = Quaternion.CreateFromAxisAngle(ArtObject.ActorSettings.SpinView.ForwardVector(), (float)Math.PI / 2f * num2 * 1.1f - (float)Math.PI / 20f);
				ArtObject.Rotation = quaternion * OriginalRotation;
				ArtObject.Position = OriginalPosition + Vector3.Transform(-RotationOffset, SpinAccumulatedRotation * quaternion) + RotationOffset;
				if (IsRotato && (flag || flag2))
				{
					Vector3 vector = ArtObject.ActorSettings.RotationCenter;
					if (!ArtObject.ActorSettings.OffCenter)
					{
						vector = ArtObject.Position;
					}
					PlayerManager.Position = Vector3.Transform(OriginalPlayerPosition - vector, quaternion) + vector;
				}
				if (!(SinceChanged.TotalSeconds >= 0.5))
				{
					break;
				}
				foreach (TrileInstance trile2 in Triles)
				{
					trile2.Enabled = true;
				}
				if ((!IsRotato || !hasRotated) && (float)Triles.Count != ArtObject.ArtObject.Size.X * ArtObject.ArtObject.Size.Y * ArtObject.ArtObject.Size.Z)
				{
					Rotate();
				}
				SpinAccumulatedRotation *= quaternion;
				State = SpinState.Idle;
				hasRotated = false;
				SinceChanged -= TimeSpan.FromSeconds(0.5);
				if (IsRotato && (flag || flag2))
				{
					PlayerManager.IsOnRotato = false;
				}
				break;
			}
			}
		}

		private void Rotate()
		{
			Vector3 vector = ArtObject.ActorSettings.RotationCenter;
			if (!ArtObject.ActorSettings.OffCenter)
			{
				vector = ArtObject.Position;
			}
			Quaternion rotation = Quaternion.CreateFromAxisAngle(ArtObject.ActorSettings.SpinView.ForwardVector(), (float)Math.PI / 2f);
			TrileInstance[] array = Triles.ToArray();
			TrileInstance[] array2 = array;
			foreach (TrileInstance trileInstance in array2)
			{
				Vector3 vector2 = Vector3.Transform(trileInstance.Position + FezMath.HalfVector - vector, rotation) + vector - FezMath.HalfVector;
				if (!FezMath.AlmostEqual(vector2, trileInstance.Position))
				{
					LevelManager.ClearTrile(trileInstance, skipRecull: true);
					trileInstance.Position = vector2;
				}
			}
			array2 = array;
			foreach (TrileInstance instance in array2)
			{
				LevelManager.UpdateInstance(instance);
			}
		}
	}

	private enum SpinState
	{
		Idle,
		Warning,
		Spinning
	}

	private readonly List<SpinBlockState> TrackedBlocks = new List<SpinBlockState>();

	private SoundEffect smallSound;

	private SoundEffect largeSound;

	private SoundEffect rotatoSound;

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public SpinBlocksHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		smallSound = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/SmallSpinblock");
		largeSound = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/LargeSpinblock");
		rotatoSound = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/RotatoSpinblock");
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		TrackedBlocks.Clear();
		if (LevelManager.TrileSet == null)
		{
			return;
		}
		TrileInstance[] array = LevelManager.TrileSet.Triles.Values.Where((Trile x) => x.Geometry != null && x.Geometry.Empty).SelectMany((Trile x) => x.Instances).ToArray();
		foreach (ArtObjectInstance value in LevelManager.ArtObjects.Values)
		{
			if (value.ArtObject.ActorType != ActorType.SpinBlock)
			{
				continue;
			}
			Vector3 vector = (value.ActorSettings.OffCenter ? value.ActorSettings.RotationCenter : value.Position);
			BoundingBox box = new BoundingBox((vector - value.ArtObject.Size / 2f).Floor(), (vector + value.ArtObject.Size / 2f).Floor());
			List<TrileInstance> list = new List<TrileInstance>();
			TrileInstance[] array2 = array;
			foreach (TrileInstance trileInstance in array2)
			{
				Vector3 center = trileInstance.Center;
				Vector3 vector2 = trileInstance.TransformedSize / 2f;
				if (new BoundingBox(center - vector2, center + vector2).Intersects(box))
				{
					list.Add(trileInstance);
					trileInstance.ForceTopMaybe = true;
				}
			}
			if (list.Count > 0)
			{
				SoundEffect soundEffect = ((value.ActorSettings.SpinView == Viewpoint.Up || value.ActorSettings.SpinView == Viewpoint.Down) ? rotatoSound : ((list.Count < 4) ? smallSound : largeSound));
				TrackedBlocks.Add(new SpinBlockState(list, value, soundEffect));
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || GameState.InMenuCube || !CameraManager.Viewpoint.IsOrthographic() || (!CameraManager.ActionRunning && !PlayerManager.IsOnRotato))
		{
			return;
		}
		foreach (SpinBlockState trackedBlock in TrackedBlocks)
		{
			trackedBlock.Update(gameTime.ElapsedGameTime);
		}
	}
}
