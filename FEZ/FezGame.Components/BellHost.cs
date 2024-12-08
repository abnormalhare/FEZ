using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class BellHost : GameComponent
{
	private ArtObjectInstance BellAo;

	private TimeSpan SinceHit = TimeSpan.FromSeconds(100.0);

	private Vector2 AngularVelocity;

	private Vector2 Angle;

	private Vector3 OriginalPosition;

	private bool Solved;

	private readonly Dictionary<Viewpoint, int> Hits = new Dictionary<Viewpoint, int>(4, ViewpointComparer.Default);

	private readonly Dictionary<Viewpoint, int> ExpectedHits = new Dictionary<Viewpoint, int>(4, ViewpointComparer.Default)
	{
		{
			Viewpoint.Front,
			1
		},
		{
			Viewpoint.Back,
			3
		},
		{
			Viewpoint.Right,
			6
		},
		{
			Viewpoint.Left,
			10
		}
	};

	private Viewpoint LastHit;

	private SoundEffect[] sBellHit;

	private int stackedHits;

	private IWaiter wutex1;

	private IWaiter wutex2;

	[ServiceDependency]
	public ILevelService LevelService { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IInputManager InputManager { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	public BellHost(Game game)
		: base(game)
	{
		base.UpdateOrder = 6;
	}

	public override void Initialize()
	{
		base.Initialize();
		TryInitialize();
		LevelManager.LevelChanged += TryInitialize;
	}

	private void TryInitialize()
	{
		sBellHit = null;
		BellAo = LevelManager.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.Bell);
		base.Enabled = BellAo != null;
		if (!base.Enabled)
		{
			return;
		}
		OriginalPosition = BellAo.Position;
		Hits.Clear();
		Hits.Add(Viewpoint.Front, 0);
		Hits.Add(Viewpoint.Back, 0);
		Hits.Add(Viewpoint.Left, 0);
		Hits.Add(Viewpoint.Right, 0);
		sBellHit = new SoundEffect[4]
		{
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/BellHit1"),
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/BellHit2"),
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/BellHit3"),
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/BellHit4")
		};
		Solved = GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(BellAo.Id);
		if (!Solved)
		{
			return;
		}
		LevelManager.ArtObjects.Remove(BellAo.Id);
		BellAo.Dispose();
		LevelMaterializer.RegisterSatellites();
		if (!GameState.SaveData.ThisLevel.DestroyedTriles.Contains(new TrileEmplacement(OriginalPosition)))
		{
			Trile trile = LevelManager.ActorTriles(ActorType.SecretCube).FirstOrDefault();
			if (trile != null)
			{
				Vector3 position = OriginalPosition - Vector3.One / 2f;
				LevelManager.ClearTrile(new TrileEmplacement(position));
				ILevelManager levelManager = LevelManager;
				TrileInstance obj = new TrileInstance(position, trile.Id)
				{
					OriginalEmplacement = new TrileEmplacement(position)
				};
				TrileInstance trileInstance = obj;
				levelManager.RestoreTrile(obj);
				trileInstance.Foreign = true;
				if (trileInstance.InstanceId == -1)
				{
					LevelMaterializer.CullInstanceIn(trileInstance);
				}
			}
		}
		base.Enabled = false;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || Solved || CameraManager.ProjectionTransition || !CameraManager.Viewpoint.IsOrthographic())
		{
			return;
		}
		TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
		SinceHit += elapsedGameTime;
		Vector3 vector = (PlayerManager.Position - (BellAo.Position - new Vector3(0f, 1f, 0f))) * CameraManager.Viewpoint.ScreenSpaceMask();
		vector.X += vector.Z;
		Vector3 vector2 = vector.Abs();
		bool flag = vector2.X < 2f && vector2.Y < 1.5f;
		if (InputManager.GrabThrow == FezButtonState.Pressed && flag && PlayerManager.CarriedInstance == null && PlayerManager.Grounded && PlayerManager.Action != ActionType.ReadingSign)
		{
			if (wutex1 != null || wutex2 != null)
			{
				if (stackedHits < 10)
				{
					stackedHits++;
				}
			}
			else
			{
				PlayerManager.Action = ActionType.TurnToBell;
				ScheduleTurnTo();
			}
		}
		if (wutex1 == null && wutex2 == null && stackedHits > 0)
		{
			ScheduleTurnTo();
			stackedHits--;
		}
		AngularVelocity *= MathHelper.Clamp(0.995f - (float)SinceHit.TotalSeconds * 0.0025f, 0f, 1f);
		Angle += AngularVelocity * 0.1f;
		AngularVelocity += -Angle * 0.01f;
		(Matrix.CreateTranslation(0f, -3.5f, 0f) * Matrix.CreateFromYawPitchRoll(RandomHelper.Centered(FezMath.Saturate((3.0 - SinceHit.TotalSeconds) / 3.0) * 0.012500000186264515), Angle.X, Angle.Y) * Matrix.CreateTranslation(OriginalPosition.X, OriginalPosition.Y + 3.5f, OriginalPosition.Z)).Decompose(out var _, out var rotation, out var translation);
		BellAo.Position = translation;
		BellAo.Rotation = rotation;
		double num = FezMath.Saturate((1.5 - SinceHit.TotalSeconds) / 1.5) * 0.07500000298023224;
		CameraManager.InterpolatedCenter += new Vector3(RandomHelper.Between(0.0 - num, num), RandomHelper.Between(0.0 - num, num), RandomHelper.Between(0.0 - num, num));
	}

	private void ScheduleTurnTo()
	{
		wutex2 = Waiters.Wait(0.4, (float _) => PlayerManager.Action != ActionType.TurnToBell, ScheduleHit);
		wutex2.AutoPause = true;
	}

	private void ScheduleHit()
	{
		wutex2 = null;
		wutex1 = Waiters.Wait(0.25, delegate
		{
			Waiters.Wait(0.25, delegate
			{
				wutex1 = null;
			});
			PlayerManager.Action = ActionType.HitBell;
			PlayerManager.Animation.Timing.Restart();
			SinceHit = TimeSpan.Zero;
			sBellHit[(int)(CameraManager.Viewpoint - 1)].EmitAt(BellAo.Position);
			SoundManager.FadeVolume(0.25f, 1f, 2f);
			AngularVelocity += new Vector2(0f - CameraManager.Viewpoint.ForwardVector().Dot(Vector3.UnitZ), CameraManager.Viewpoint.ForwardVector().Dot(Vector3.UnitX)) * 0.075f;
			if (!Solved)
			{
				if (LastHit != 0 && LastHit != CameraManager.Viewpoint)
				{
					Hits[CameraManager.Viewpoint] = 0;
				}
				LastHit = CameraManager.Viewpoint;
				Hits[CameraManager.Viewpoint]++;
				if (Hits.All((KeyValuePair<Viewpoint, int> kvp) => kvp.Value == ExpectedHits[kvp.Key]))
				{
					Solved = true;
					GameState.SaveData.ThisLevel.InactiveArtObjects.Add(BellAo.Id);
					LevelService.ResolvePuzzle();
					ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, BellAo, OriginalPosition));
				}
			}
		});
		wutex1.AutoPause = true;
	}
}
