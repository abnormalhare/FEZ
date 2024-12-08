using FezEngine;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class GeezerLetterSender : DrawableGameComponent
{
	private int? NpcId;

	private NpcInstance Npc;

	private bool Walking;

	private float SinceStarted;

	private BackgroundPlane Plane;

	private float SinceGotThere;

	private Vector3 OldPosition;

	private Vector3 OldDestinationOffset;

	private bool hooked;

	private bool geezerReset;

	private SoundEffect sLetterInsert;

	[ServiceDependency]
	public IVolumeService VolumeService { get; set; }

	[ServiceDependency]
	public IGomezService GomezService { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	public GeezerLetterSender(Game game, int npcId)
		: base(game)
	{
		NpcId = npcId;
		base.DrawOrder = 99;
	}

	public GeezerLetterSender(Game game)
		: base(game)
	{
		NpcId = null;
		base.DrawOrder = 99;
	}

	public override void Initialize()
	{
		base.Initialize();
		DrawActionScheduler.Schedule(delegate
		{
			IGameLevelManager levelManager = LevelManager;
			BackgroundPlane obj = new BackgroundPlane(LevelMaterializer.StaticPlanesMesh, CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/CAPSULE"))
			{
				Rotation = CameraManager.Rotation,
				Loop = false
			};
			BackgroundPlane plane = obj;
			Plane = obj;
			levelManager.AddPlane(plane);
			if (NpcId.HasValue)
			{
				Npc = LevelManager.NonPlayerCharacters[NpcId.Value];
				OldPosition = Npc.Position;
				Npc.Position = new Vector3(30.4375f, 49f, 10f);
				OldDestinationOffset = Npc.DestinationOffset;
				Npc.DestinationOffset = new Vector3(-3.9375f, 0f, 0f);
				Npc.State.Scripted = true;
				Npc.State.LookingDirection = HorizontalDirection.Right;
				Npc.State.WalkStep = 0f;
				Npc.State.CurrentAction = NpcAction.Idle;
				Npc.State.UpdateAction();
				Npc.State.SyncTextureMatrix();
				Npc.Group.Position = LevelManager.NonPlayerCharacters[NpcId.Value].Position;
				CameraManager.Constrained = true;
				CameraManager.Center = new Vector3(32.5f, 50.5f, 16.5f);
				CameraManager.SnapInterpolation();
				Plane.Position = Npc.Group.Position + new Vector3((float)(Npc.State.LookingDirection.Sign() * 4) / 16f, 0.375f, 0f);
				sLetterInsert = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/LetterTubeInsert");
			}
			else
			{
				Plane.Position = new Vector3(20.5f, 20.75f, 23.5f);
				base.Enabled = false;
				GomezService.ReadMail += Destroy;
			}
		});
		LevelManager.LevelChanged += TryDestroy;
	}

	private void TryDestroy()
	{
		ServiceHelper.RemoveComponent(this);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		GomezService.ReadMail -= Destroy;
		LevelManager.LevelChanged -= TryDestroy;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.TimePaused || GameState.Loading || geezerReset)
		{
			return;
		}
		CameraManager.Constrained = false;
		SinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds;
		if (SinceStarted > 3f && !Walking)
		{
			Walking = true;
			Npc.State.LookingDirection = HorizontalDirection.Left;
			Npc.State.CurrentAction = NpcAction.Walk;
			Npc.State.UpdateAction();
			Npc.State.SyncTextureMatrix();
		}
		if (Npc.State.CurrentAction == NpcAction.Walk && Npc.State.WalkStep == 1f)
		{
			Npc.State.CurrentAction = NpcAction.Idle;
			Npc.State.UpdateAction();
			Npc.State.SyncTextureMatrix();
		}
		if (Npc.State.WalkStep == 1f)
		{
			SinceGotThere += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (SinceGotThere < 0.5f)
			{
				if (sLetterInsert != null)
				{
					sLetterInsert.Emit();
					sLetterInsert = null;
				}
				Plane.Position = Npc.Group.Position + new Vector3((float)(Npc.State.LookingDirection.Sign() * 4) / 16f, 0.375f, 0f) + new Vector3(0f - Easing.EaseIn(SinceGotThere / 0.5f, EasingType.Quadratic), 0.375f, 0f);
			}
			if (SinceGotThere > 12.5f && SinceGotThere < 14.5f)
			{
				Plane.Position = new Vector3(20.5f, 20.75f + Easing.EaseOut(FezMath.Saturate((13.25f - SinceGotThere) * 2f), EasingType.Cubic), 23.5f);
			}
			else if (SinceGotThere > 14.5f && NpcId.HasValue)
			{
				Npc.Position = OldPosition;
				Npc.DestinationOffset = OldDestinationOffset;
				Npc.State.CurrentAction = NpcAction.Walk;
				Npc.State.UpdateAction();
				Npc.State.WalkStep = 0.25f;
				Npc.State.Scripted = false;
				geezerReset = true;
			}
			if (!hooked)
			{
				GomezService.ReadMail += Destroy;
				hooked = true;
			}
		}
		else
		{
			Plane.Position = Npc.Group.Position + new Vector3((float)(Npc.State.LookingDirection.Sign() * 4) / 16f, 0.375f, 0f);
		}
	}

	private void Destroy()
	{
		LevelManager.RemovePlane(Plane);
		ServiceHelper.RemoveComponent(this);
	}
}
