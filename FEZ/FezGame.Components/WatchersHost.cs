using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class WatchersHost : DrawableGameComponent
{
	private class WatcherState
	{
		public WatcherAction Action { get; set; }

		public TimeSpan StartTime { get; set; }

		public Vector3 OriginalCenter { get; set; }

		public Vector3 CrushDirection { get; set; }

		public Mesh Eyes { get; set; }

		public SoundEmitter MoveEmitter { get; set; }

		public SoundEmitter WithdrawEmitter { get; set; }

		public Vector3 EyeOffset { get; set; }

		public float CrashAttenuation { get; set; }

		public bool SkipNextSound { get; set; }
	}

	private enum WatcherAction
	{
		Idle,
		Spotted,
		Crushing,
		Wait,
		Withdrawing,
		Cooldown
	}

	private const float WatchRange = 8f;

	private const float SpotDelay = 1f;

	private const float CrushSpeed = 15f;

	private const float WithdrawSpeed = 2f;

	private const float Acceleration = 0.025f;

	private const float CooldownTime = 0.5f;

	private const float CrushWaitTime = 1.5f;

	private Dictionary<TrileInstance, WatcherState> watchers;

	private SoundEffect seeSound;

	private SoundEffect moveSound;

	private SoundEffect collideSound;

	private SoundEffect withdrawSound;

	private readonly List<Vector3> lastCrushDirections = new List<Vector3>();

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IPhysicsManager PhysicsManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public WatchersHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		base.UpdateOrder = -2;
		base.DrawOrder = 6;
		LevelManager.LevelChanged += InitializeWatchers;
		InitializeWatchers();
		LightingPostProcess.DrawGeometryLights += PreDraw;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		seeSound = CMProvider.Global.Load<SoundEffect>("Sounds/Zu/WatcherSee");
		moveSound = CMProvider.Global.Load<SoundEffect>("Sounds/Zu/WatcherMove");
		collideSound = CMProvider.Global.Load<SoundEffect>("Sounds/Zu/WatcherCollide");
		withdrawSound = CMProvider.Global.Load<SoundEffect>("Sounds/Zu/WatcherWithdraw");
	}

	private void InitializeWatchers()
	{
		watchers = new Dictionary<TrileInstance, WatcherState>();
		if (LevelManager.TrileSet == null)
		{
			return;
		}
		foreach (TrileInstance item in LevelManager.TrileSet.Triles.Values.Where((Trile t) => t.ActorSettings.Type == ActorType.Watcher).SelectMany((Trile t) => t.Instances))
		{
			item.Trile.Size = new Vector3(31f / 32f);
			item.PhysicsState = new InstancePhysicsState(item);
			watchers.Add(item, new WatcherState
			{
				Eyes = new Mesh(),
				OriginalCenter = item.Center,
				CrashAttenuation = 1f
			});
			watchers[item].Eyes.AddColoredBox(Vector3.One / 16f, Vector3.Zero, new Color(255, 127, 0), centeredOnOrigin: false);
			watchers[item].Eyes.AddColoredBox(Vector3.One / 16f, Vector3.Zero, new Color(255, 127, 0), centeredOnOrigin: false);
			watchers[item].Eyes.AddColoredBox(Vector3.One / 16f, Vector3.Zero, new Color(255, 127, 0), centeredOnOrigin: false);
			watchers[item].Eyes.AddColoredBox(Vector3.One / 16f, Vector3.Zero, new Color(255, 127, 0), centeredOnOrigin: false);
		}
		DrawActionScheduler.Schedule(delegate
		{
			foreach (WatcherState value in watchers.Values)
			{
				value.Eyes.Effect = new DefaultEffect.VertexColored
				{
					Fullbright = true
				};
			}
		});
	}

	public override void Update(GameTime gameTime)
	{
		if (CameraManager.Viewpoint == Viewpoint.Perspective || GameState.InMap || GameState.Paused || GameState.Loading || watchers.Count == 0)
		{
			return;
		}
		Vector3 vector = CameraManager.Viewpoint.RightVector();
		Vector3 vector2 = vector.Abs();
		Vector3 vector3 = CameraManager.Viewpoint.ForwardVector();
		foreach (TrileInstance key in watchers.Keys)
		{
			WatcherState watcherState = watchers[key];
			Vector3 vector4 = key.PhysicsState.Center + vector2 * -5f / 16f + Vector3.UnitY * -2f / 16f - 0.5f * vector3;
			watcherState.Eyes.Groups[0].Position = vector4 + watcherState.EyeOffset;
			watcherState.Eyes.Groups[1].Position = vector4 + vector2 * 9f / 16f + watcherState.EyeOffset;
			watcherState.Eyes.Groups[0].Enabled = true;
			watcherState.Eyes.Groups[1].Enabled = true;
		}
		if (!CameraManager.ActionRunning || !CameraManager.ViewTransitionReached)
		{
			return;
		}
		Vector3 center = PlayerManager.Center;
		BoundingBox box = FezMath.Enclose(center - PlayerManager.Size / 2f, center + PlayerManager.Size / 2f);
		Vector3 vector5 = vector * 8f;
		Vector3 vector6 = vector3 * LevelManager.Size;
		Vector3 vector7 = Vector3.Up * 8f;
		lastCrushDirections.Clear();
		bool flag = false;
		foreach (TrileInstance key2 in watchers.Keys)
		{
			WatcherState watcherState2 = watchers[key2];
			Vector3 vector8 = (center - key2.Position).Sign() * vector2;
			Vector3 vector9 = (center - key2.Position).Sign() * Vector3.UnitY;
			BoundingBox boundingBox = ((Vector3.Dot(vector8, vector) > 0f) ? FezMath.Enclose(key2.Position + Vector3.UnitY * 0.05f - vector6, key2.Position + vector5 + vector6 + new Vector3(0.9f)) : FezMath.Enclose(key2.Position + Vector3.UnitY * 0.05f - vector6 - vector5, key2.Position + vector6 + new Vector3(0.9f)));
			BoundingBox boundingBox2 = FezMath.Enclose(key2.Position + Vector3.UnitY * 0.05f - vector7 - vector6, key2.Position + vector7 + new Vector3(0.9f) + vector6);
			switch (watcherState2.Action)
			{
			case WatcherAction.Idle:
			{
				bool flag4 = boundingBox.Intersects(box);
				bool flag5 = boundingBox2.Intersects(box);
				if (flag4)
				{
					watcherState2.EyeOffset = Vector3.Lerp(watcherState2.EyeOffset, vector8 * 1f / 16f, 0.25f);
				}
				else if (flag5)
				{
					watcherState2.EyeOffset = Vector3.Lerp(watcherState2.EyeOffset, vector9 * 1f / 16f, 0.25f);
				}
				else
				{
					watcherState2.EyeOffset = Vector3.Lerp(watcherState2.EyeOffset, Vector3.Zero, 0.1f);
				}
				watcherState2.CrushDirection = (flag4 ? vector8 : (flag5 ? vector9 : Vector3.Zero));
				watcherState2.Eyes.Material.Opacity = 1f;
				WatcherState watcherState3;
				if (LevelManager.NearestTrile(key2.Position + new Vector3(0.5f)).Deep == key2 && (flag4 || flag5) && !FezMath.In(PlayerManager.Action, ActionType.GrabCornerLedge, ActionType.Suffering, ActionType.Dying, ActionTypeComparer.Default) && (watcherState3 = HasPair(key2)) != null)
				{
					watcherState2.Action = WatcherAction.Spotted;
					TimeSpan startTime = (watcherState2.StartTime = gameTime.TotalGameTime);
					watcherState3.StartTime = startTime;
					if (!watcherState2.SkipNextSound)
					{
						seeSound.EmitAt(key2.Center);
						watcherState3.SkipNextSound = true;
					}
				}
				break;
			}
			case WatcherAction.Spotted:
				watcherState2.EyeOffset = Vector3.Lerp(watcherState2.EyeOffset, watcherState2.CrushDirection * 1f / 16f, 0.25f);
				if ((gameTime.TotalGameTime - watcherState2.StartTime).TotalSeconds > 1.0)
				{
					watcherState2.Action = WatcherAction.Crushing;
					watcherState2.StartTime = gameTime.TotalGameTime;
					key2.PhysicsState.Velocity = watcherState2.OriginalCenter - key2.Center;
					PhysicsManager.Update(key2.PhysicsState, simple: true, keepInFront: false);
					key2.PhysicsState.UpdateInstance();
					LevelManager.UpdateInstance(key2);
					if (!watcherState2.SkipNextSound)
					{
						watcherState2.MoveEmitter = moveSound.EmitAt(key2.Center);
					}
					else
					{
						watcherState2.MoveEmitter = null;
					}
				}
				else
				{
					Vector3 vector12 = watcherState2.CrushDirection * RandomHelper.Unit() * 0.5f / 16f;
					key2.PhysicsState.Sticky = true;
					key2.PhysicsState.Velocity = watcherState2.OriginalCenter + vector12 - key2.Center;
					PhysicsManager.Update(key2.PhysicsState, simple: true, keepInFront: false);
					key2.PhysicsState.UpdateInstance();
					LevelManager.UpdateInstance(key2);
				}
				break;
			case WatcherAction.Crushing:
			{
				if (key2.PhysicsState.Sticky)
				{
					key2.PhysicsState.Sticky = false;
					key2.PhysicsState.Velocity = Vector3.Zero;
				}
				watcherState2.EyeOffset = watcherState2.CrushDirection * 1f / 16f;
				Vector3 value = watcherState2.CrushDirection * (float)gameTime.ElapsedGameTime.TotalSeconds * 15f;
				Vector3 vector10 = Vector3.Lerp(key2.PhysicsState.Velocity, value, 0.025f);
				key2.PhysicsState.Velocity = vector10 * watcherState2.CrashAttenuation;
				if (CameraManager.Viewpoint.VisibleAxis() != FezMath.OrientationFromDirection(watcherState2.CrushDirection).AsAxis())
				{
					PhysicsManager.Update(key2.PhysicsState, simple: false, keepInFront: false);
				}
				Vector3 vector11 = vector10 * watcherState2.CrashAttenuation - key2.PhysicsState.Velocity;
				if (watcherState2.MoveEmitter != null)
				{
					watcherState2.MoveEmitter.Position = key2.Center;
				}
				key2.PhysicsState.UpdateInstance();
				LevelManager.UpdateInstance(key2);
				PlayerManager.ForceOverlapsDetermination();
				bool flag2 = PlayerManager.HeldInstance == key2 || PlayerManager.WallCollision.FarHigh.Destination == key2 || PlayerManager.WallCollision.NearLow.Destination == key2 || PlayerManager.Ground.NearLow == key2 || PlayerManager.Ground.FarHigh == key2;
				if (!flag2)
				{
					PointCollision[] cornerCollision = PlayerManager.CornerCollision;
					for (int i = 0; i < cornerCollision.Length; i++)
					{
						if (cornerCollision[i].Instances.Deep == key2)
						{
							flag2 = true;
							break;
						}
					}
				}
				if (flag && flag2 && lastCrushDirections.Contains(-watcherState2.CrushDirection))
				{
					PlayerManager.Position = key2.Center + Vector3.One / 2f * watcherState2.CrushDirection + -CameraManager.Viewpoint.SideMask() * watcherState2.CrushDirection.Abs() * 1.5f / 16f;
					PlayerManager.Velocity = Vector3.Zero;
					PlayerManager.Action = ((watcherState2.CrushDirection.Y == 0f) ? ActionType.CrushHorizontal : ActionType.CrushVertical);
					watcherState2.CrashAttenuation = ((PlayerManager.Action == ActionType.CrushVertical) ? 0.5f : 0.75f);
				}
				flag = flag || flag2;
				if (flag2 && PlayerManager.Action != ActionType.CrushHorizontal && PlayerManager.Action != ActionType.CrushVertical)
				{
					lastCrushDirections.Add(watcherState2.CrushDirection);
					if (watcherState2.CrushDirection.Y == 0f)
					{
						PlayerManager.Position += key2.PhysicsState.Velocity;
					}
				}
				if (vector11.LengthSquared() > 5E-05f || Math.Abs(Vector3.Dot(key2.Center - watcherState2.OriginalCenter, watcherState2.CrushDirection.Abs())) >= 8f)
				{
					if (watcherState2.MoveEmitter != null && !watcherState2.MoveEmitter.Dead)
					{
						watcherState2.MoveEmitter.Cue.Stop();
					}
					watcherState2.MoveEmitter = null;
					if (!watcherState2.SkipNextSound)
					{
						collideSound.EmitAt(key2.Center);
					}
					watcherState2.Action = WatcherAction.Wait;
					key2.PhysicsState.Velocity = Vector3.Zero;
					watcherState2.StartTime = TimeSpan.Zero;
					watcherState2.CrashAttenuation = 1f;
				}
				break;
			}
			case WatcherAction.Wait:
				watcherState2.StartTime += gameTime.ElapsedGameTime;
				if (watcherState2.StartTime.TotalSeconds > 1.5)
				{
					watcherState2.Action = WatcherAction.Withdrawing;
					watcherState2.StartTime = gameTime.TotalGameTime;
					if (!watcherState2.SkipNextSound)
					{
						watcherState2.WithdrawEmitter = withdrawSound.EmitAt(key2.Center, loop: true);
					}
					else
					{
						watcherState2.WithdrawEmitter = null;
					}
				}
				break;
			case WatcherAction.Withdrawing:
			{
				watcherState2.EyeOffset = Vector3.Lerp(watcherState2.EyeOffset, -watcherState2.CrushDirection * 0.5f / 16f, 0.05f);
				Vector3 value = -watcherState2.CrushDirection * (float)gameTime.ElapsedGameTime.TotalSeconds * 2f;
				key2.PhysicsState.Velocity = Vector3.Lerp(key2.PhysicsState.Velocity, value, 0.025f);
				if (watcherState2.WithdrawEmitter != null)
				{
					watcherState2.WithdrawEmitter.VolumeFactor = 0f;
				}
				bool flag3 = false;
				if (CameraManager.Viewpoint.DepthMask() == FezMath.OrientationFromDirection(watcherState2.CrushDirection).AsAxis().GetMask())
				{
					flag3 = true;
				}
				if (watcherState2.WithdrawEmitter != null)
				{
					watcherState2.WithdrawEmitter.VolumeFactor = 1f;
				}
				Vector3 center2 = key2.PhysicsState.Center;
				Vector3 velocity = key2.PhysicsState.Velocity;
				PhysicsManager.Update(key2.PhysicsState, simple: true, keepInFront: false);
				key2.PhysicsState.Center = center2 + velocity;
				if (watcherState2.WithdrawEmitter != null)
				{
					watcherState2.WithdrawEmitter.Position = key2.Center;
				}
				if (flag3 ? (Math.Abs(Vector3.Dot(key2.Center - watcherState2.OriginalCenter, vector + Vector3.Up)) <= 1f / 32f) : (Vector3.Dot(key2.Center - watcherState2.OriginalCenter, watcherState2.CrushDirection) <= 0.001f))
				{
					if (watcherState2.WithdrawEmitter != null)
					{
						watcherState2.WithdrawEmitter.FadeOutAndDie(0.1f);
						watcherState2.WithdrawEmitter = null;
					}
					watcherState2.SkipNextSound = false;
					watcherState2.Action = WatcherAction.Cooldown;
					watcherState2.CrushDirection = Vector3.Zero;
					watcherState2.StartTime = TimeSpan.Zero;
				}
				key2.PhysicsState.UpdateInstance();
				LevelManager.UpdateInstance(key2);
				break;
			}
			case WatcherAction.Cooldown:
				key2.PhysicsState.Velocity = watcherState2.OriginalCenter - key2.Center;
				PhysicsManager.Update(key2.PhysicsState, simple: true, keepInFront: false);
				key2.PhysicsState.UpdateInstance();
				LevelManager.UpdateInstance(key2);
				watcherState2.EyeOffset = Vector3.Lerp(watcherState2.EyeOffset, Vector3.Zero, 0.05f);
				watcherState2.Eyes.Material.Opacity = 0.5f;
				watcherState2.StartTime += gameTime.ElapsedGameTime;
				if (watcherState2.StartTime.TotalSeconds > 0.5)
				{
					key2.PhysicsState.Velocity = Vector3.Zero;
					watcherState2.Action = WatcherAction.Idle;
				}
				break;
			}
			Vector3 vector13 = key2.PhysicsState.Center + vector2 * -5f / 16f + Vector3.UnitY * -2f / 16f - 0.5f * vector3;
			watcherState2.Eyes.Groups[0].Position = vector13 + watcherState2.EyeOffset;
			watcherState2.Eyes.Groups[1].Position = vector13 + vector2 * 9f / 16f + watcherState2.EyeOffset;
			watcherState2.Eyes.Groups[2].Position = watcherState2.Eyes.Groups[0].Position;
			watcherState2.Eyes.Groups[3].Position = watcherState2.Eyes.Groups[1].Position;
			watcherState2.Eyes.Groups[0].Enabled = false;
			watcherState2.Eyes.Groups[1].Enabled = false;
		}
	}

	private WatcherState HasPair(TrileInstance watcher)
	{
		WatcherState watcherState = watchers[watcher];
		Vector3 b = CameraManager.Viewpoint.ScreenSpaceMask();
		foreach (TrileInstance key in watchers.Keys)
		{
			if (key != watcher)
			{
				WatcherState watcherState2 = watchers[key];
				if (watcherState.CrushDirection == -watcherState2.CrushDirection && watcherState2.Action != WatcherAction.Cooldown && watcherState2.Action != WatcherAction.Withdrawing && watcherState2.Action != WatcherAction.Crushing && Math.Abs((watcherState.OriginalCenter - watcherState2.OriginalCenter).Dot(b)) > 2f && Math.Abs((watcher.Center - key.Center).Dot(b)) > 2f && LevelManager.NearestTrile(key.Position + new Vector3(0.5f)).Deep == key)
				{
					return watcherState2;
				}
			}
		}
		return null;
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading || watchers.Count == 0)
		{
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.PrepareStencilWrite(StencilMask.Level);
		foreach (WatcherState value in watchers.Values)
		{
			value.Eyes.Draw();
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
	}

	private void PreDraw(GameTime gameTime)
	{
		if (GameState.Loading || watchers.Count == 0)
		{
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.PrepareStencilWrite(StencilMask.Level);
		foreach (WatcherState value in watchers.Values)
		{
			(value.Eyes.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
			value.Eyes.Draw();
			(value.Eyes.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
	}
}
