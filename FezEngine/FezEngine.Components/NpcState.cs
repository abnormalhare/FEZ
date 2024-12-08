using System;
using System.Linq;
using Common;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezEngine.Components;

public class NpcState : GameComponent
{
	private const int MinSecondsActionToggle = 2;

	private const int MaxSecondsActionToggle = 5;

	public readonly NpcInstance Npc;

	public NpcAction CurrentAction;

	public HorizontalDirection LookingDirection;

	protected SpeechLine CurrentLine;

	protected readonly bool CanIdle2;

	protected readonly bool CanIdle3;

	protected readonly bool CanTurn;

	protected readonly bool CanBurrow;

	protected readonly bool CanHide;

	protected readonly bool CanTakeOff;

	protected bool CanIdle;

	protected bool CanWalk;

	protected bool CanTalk;

	public float WalkStep;

	public bool Scripted;

	protected Group Group;

	protected AnimationTiming CurrentTiming;

	protected AnimatedTexture CurrentAnimation;

	protected SoundEmitter Emitter;

	protected SoundEmitter talkEmitter;

	protected IWaiter talkWaiter;

	private float WalkedDistance;

	private TimeSpan TimeUntilActionChange;

	private TimeSpan TimeSinceActionChange;

	protected SoundEffect flySound;

	protected bool FlyingBack;

	protected bool MayComeBack;

	protected bool OwlInvisible;

	protected Vector2 flySpeed;

	protected bool InBackground;

	private bool initialized;

	protected bool isDisposed;

	private Quaternion oldRotation;

	private HorizontalDirection oldDirection;

	private int lastFrame;

	private Vector3? flyRight;

	protected virtual float AnimationSpeed => 1f;

	protected Vector3 Position
	{
		get
		{
			return Group.Position;
		}
		set
		{
			Group.Position = value;
		}
	}

	[ServiceDependency]
	public ITimeManager TimeManager { protected get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { protected get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { protected get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { protected get; set; }

	public NpcState(Game game, NpcInstance npc)
		: base(game)
	{
		Npc = npc;
		Npc.State = this;
		foreach (NpcAction value in Util.GetValues<NpcAction>())
		{
			if (value != 0 && value != NpcAction.Walk && value != NpcAction.Idle && value != NpcAction.Talk && !Npc.Actions.ContainsKey(value) && MemoryContentManager.AssetExists("Character Animations\\" + Npc.Name + "\\" + value))
			{
				Npc.Actions.Add(value, new NpcActionContent
				{
					AnimationName = value.ToString()
				});
			}
		}
		if (MemoryContentManager.AssetExists("Character Animations\\" + Npc.Name + "\\Metadata"))
		{
			npc.FillMetadata(ServiceHelper.Get<IContentManagerProvider>().CurrentLevel.Load<NpcMetadata>("Character Animations/" + npc.Name + "/Metadata"));
		}
		CanIdle = Npc.Actions.ContainsKey(NpcAction.Idle);
		CanIdle2 = Npc.Actions.ContainsKey(NpcAction.Idle2);
		CanIdle3 = Npc.Actions.ContainsKey(NpcAction.Idle3);
		CanTalk = Npc.Actions.ContainsKey(NpcAction.Talk);
		CanWalk = Npc.Actions.ContainsKey(NpcAction.Walk);
		CanTurn = Npc.Actions.ContainsKey(NpcAction.Turn);
		CanBurrow = Npc.Actions.ContainsKey(NpcAction.Burrow);
		CanHide = Npc.Actions.ContainsKey(NpcAction.Hide);
		CanTakeOff = Npc.Actions.ContainsKey(NpcAction.TakeOff);
	}

	public override void Initialize()
	{
		if (!initialized)
		{
			LoadContent();
			Npc.Group = (Group = LevelMaterializer.NpcMesh.AddFace(Vector3.One, Vector3.UnitY / 2f, FaceOrientation.Front, centeredOnOrigin: true, doublesided: true));
			Group.Material = new Material();
			WalkStep = RandomHelper.Between(0.0, 1.0);
			LookingDirection = HorizontalDirection.Right;
			UpdatePath();
			OwlInvisible = !TimeManager.IsDayPhase(DayPhase.Night);
			Npc.Enabled = Npc.ActorType != ActorType.Owl || !OwlInvisible;
			Walk(TimeSpan.Zero);
			ToggleAction();
			CameraManager.ViewpointChanged += UpdateRotation;
			CameraManager.ViewpointChanged += UpdatePath;
			UpdateRotation();
			UpdateScale();
			DrawActionScheduler.Schedule(SyncTextureMatrix);
			initialized = true;
		}
	}

	public void SyncTextureMatrix()
	{
		int width = CurrentAnimation.Texture.Width;
		int height = CurrentAnimation.Texture.Height;
		int frame = CurrentTiming.Frame;
		Rectangle rectangle = CurrentAnimation.Offsets[frame];
		Group.TextureMatrix.Set(new Matrix((float)rectangle.Width / (float)width, 0f, 0f, 0f, 0f, (float)rectangle.Height / (float)height, 0f, 0f, (float)rectangle.X / (float)width, (float)rectangle.Y / (float)height, 1f, 0f, 0f, 0f, 0f, 0f));
	}

	protected void UpdatePath()
	{
		float walkedDistance = WalkedDistance;
		WalkedDistance = Math.Abs(Npc.DestinationOffset.Dot(CameraManager.Viewpoint.SideMask()));
		if (WalkedDistance == 0f)
		{
			WalkedDistance = walkedDistance;
		}
	}

	protected void LoadContent()
	{
		foreach (NpcAction key in Npc.Actions.Keys)
		{
			NpcActionContent npcActionContent = Npc.Actions[key];
			npcActionContent.Animation = LoadAnimation(npcActionContent.AnimationName);
			npcActionContent.Animation.Timing.Loop = key.Loops();
			if (npcActionContent.SoundName != null)
			{
				npcActionContent.Sound = LoadSound(npcActionContent.SoundName);
			}
			else if (Npc.Metadata.SoundActions.Contains(key))
			{
				npcActionContent.Sound = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/" + Npc.Metadata.SoundPath);
			}
		}
		foreach (NpcActionContent item in from x in Npc.Speech
			where x.OverrideContent != null
			select x.OverrideContent)
		{
			if (item.AnimationName != null)
			{
				item.Animation = LoadAnimation(item.AnimationName);
			}
			if (item.SoundName != null)
			{
				item.Sound = LoadSound(item.SoundName);
			}
		}
		if (CanTakeOff)
		{
			flySound = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Wildlife/BirdFly");
		}
	}

	private AnimatedTexture LoadAnimation(string name)
	{
		string assetName = "Character Animations/" + Npc.Name + "/" + name;
		return CMProvider.CurrentLevel.Load<AnimatedTexture>(assetName);
	}

	private SoundEffect LoadSound(string name)
	{
		string assetName = "Sounds/Npc/" + name;
		return CMProvider.CurrentLevel.Load<SoundEffect>(assetName);
	}

	protected override void Dispose(bool disposing)
	{
		CameraManager.ViewpointChanged -= UpdateRotation;
		CameraManager.ViewpointChanged -= UpdatePath;
		LevelMaterializer.NpcMesh.RemoveGroup(Group);
		if (Emitter != null && !Emitter.Dead)
		{
			Emitter.FadeOutAndDie(0f);
		}
		if (talkEmitter != null)
		{
			if (!talkEmitter.Dead)
			{
				talkEmitter.FadeOutAndDie(0f);
			}
			talkEmitter = null;
		}
		isDisposed = true;
	}

	protected void UpdateRotation()
	{
		Quaternion rotation = CameraManager.Rotation;
		if (oldRotation == rotation && oldDirection == LookingDirection)
		{
			return;
		}
		oldRotation = rotation;
		oldDirection = LookingDirection;
		if (LookingDirection == HorizontalDirection.Left)
		{
			rotation *= FezMath.QuaternionFromPhi((float)Math.PI);
		}
		Group.Rotation = rotation;
		if (CameraManager.Viewpoint.IsOrthographic())
		{
			if (LevelManager.IsInvalidatingScreen)
			{
				LevelManager.ScreenInvalidated += UpdateBackgroundState;
			}
			else
			{
				UpdateBackgroundState();
			}
		}
	}

	private void UpdateBackgroundState()
	{
		InBackground = false;
		Vector3 vector = CameraManager.Viewpoint.ForwardVector();
		NearestTriles nearestTriles = LevelManager.NearestTrile(Position, QueryOptions.Simple);
		TrileInstance trileInstance = nearestTriles.Surface ?? nearestTriles.Deep;
		if (trileInstance != null)
		{
			Vector3 a = trileInstance.Center + trileInstance.TransformedSize / 2f * -vector;
			InBackground = Position.Dot(vector) > a.Dot(vector);
		}
	}

	public override void Update(GameTime gameTime)
	{
		TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
		Npc.Enabled = Npc.ActorType != ActorType.Owl || !OwlInvisible;
		if (!Scripted && CurrentAction.AllowsRandomChange())
		{
			TimeSinceActionChange += elapsedGameTime;
			if (TimeSinceActionChange > TimeUntilActionChange)
			{
				ToggleAction();
			}
		}
		else if (!CurrentAction.Loops() && CurrentTiming.Ended && CurrentAction != NpcAction.Hide)
		{
			ToggleAction();
		}
		if (CurrentAction != NpcAction.Talk)
		{
			if (CanTalk && (Npc.Speech.Count > 0 || Npc.CustomSpeechLine != null))
			{
				TryTalk();
			}
			if (CurrentAction == NpcAction.Walk)
			{
				Walk(elapsedGameTime);
			}
			if (CurrentAction == NpcAction.Fly || CurrentAction == NpcAction.TakeOff)
			{
				Fly(elapsedGameTime);
			}
		}
		else if (TryStopTalking() && CurrentAction != NpcAction.TakeOff)
		{
			ToggleAction();
		}
		if (Npc.AvoidsGomez && Npc.Visible)
		{
			TryFlee();
		}
		if (CurrentTiming != null)
		{
			CurrentTiming.Update(elapsedGameTime, AnimationSpeed);
			SyncTextureMatrix();
		}
	}

	private void Fly(TimeSpan elapsed)
	{
		if (!flyRight.HasValue)
		{
			flyRight = Vector3.Transform(Vector3.Right, Group.Rotation);
		}
		Vector3 vector = Vector3.Transform(Vector3.Right, Group.Rotation);
		if ((!FlyingBack || Npc.ActorType != ActorType.Owl) && FezMath.AlmostEqual(vector.Abs().Dot(flyRight.Value.Abs()), 0f, 0.1f))
		{
			flyRight += Vector3.Transform(Vector3.Right, Group.Rotation);
		}
		flySpeed = Vector2.Lerp(flySpeed, new Vector2(4f, 3f), 1f / 30f);
		float num = 1f - FezMath.Frac(CurrentTiming.Step + 0.75f);
		Vector2 vector2 = flySpeed * (num * new Vector2(0.4f, 0.6f) + new Vector2(0.6f, 0.4f));
		Position += (float)elapsed.TotalSeconds * (vector2.X * flyRight.Value + Vector3.Up * vector2.Y * ((!FlyingBack) ? 1 : (-1)));
		LookingDirection = ((!(flyRight.Value.Dot(CameraManager.InverseView.Right) > 0f)) ? HorizontalDirection.Left : HorizontalDirection.Right);
		if (CameraManager.Viewpoint.IsOrthographic() && CameraManager.ViewTransitionReached)
		{
			Vector3 vector3 = CameraManager.Viewpoint.ForwardVector();
			if (InBackground)
			{
				vector3 *= -1f;
			}
			NearestTriles nearestTriles = LevelManager.NearestTrile(Position, QueryOptions.Simple);
			TrileInstance trileInstance = nearestTriles.Surface ?? nearestTriles.Deep;
			if (trileInstance != null)
			{
				Vector3 vector4 = trileInstance.Center + trileInstance.TransformedSize / 2f * -vector3;
				Vector3 vector5 = CameraManager.Viewpoint.DepthMask();
				if (Position.Dot(vector3) > vector4.Dot(vector3))
				{
					Position = Position * CameraManager.Viewpoint.ScreenSpaceMask() + vector4 * vector5 - vector3;
				}
			}
		}
		if (CurrentTiming.Frame == 0 && lastFrame != 0)
		{
			flySound.EmitAt(Position);
		}
		lastFrame = CurrentTiming.Frame;
		if (FlyingBack)
		{
			if (Position.Y <= Npc.Position.Y)
			{
				Position = Npc.Position;
				CurrentAction = NpcAction.Land;
				flySpeed = Vector2.Zero;
				UpdateAction();
				FlyingBack = false;
				flyRight = null;
			}
		}
		else if (CameraManager.Frustum.Contains(new BoundingBox(Position - Vector3.One, Position + Vector3.One)) == ContainmentType.Disjoint)
		{
			if (MayComeBack)
			{
				OwlInvisible = true;
				MayComeBack = false;
				CurrentAction = NpcAction.Idle;
				flySpeed = Vector2.Zero;
				flyRight = null;
				UpdateAction();
			}
			else
			{
				ServiceHelper.RemoveComponent(this);
			}
		}
	}

	protected virtual void TryTalk()
	{
	}

	protected virtual bool TryStopTalking()
	{
		return false;
	}

	protected virtual void TryFlee()
	{
	}

	private void Walk(TimeSpan elapsed)
	{
		float num = (LookingDirection.Sign() * CameraManager.InverseView.Right).Dot(Npc.DestinationOffset.Sign());
		WalkStep += num / ((WalkedDistance == 0f) ? 1f : WalkedDistance) * (float)elapsed.TotalSeconds * Npc.WalkSpeed;
		if (!Scripted && (WalkStep > 1f || WalkStep < 0f))
		{
			WalkStep = FezMath.Saturate(WalkStep);
			ToggleAction();
		}
		else
		{
			WalkStep = FezMath.Saturate(WalkStep);
			Position = Vector3.Lerp(Npc.Position, Npc.Position + Npc.DestinationOffset, WalkStep);
		}
	}

	private void ToggleAction()
	{
		NpcAction currentAction = CurrentAction;
		if (initialized)
		{
			RandomizeAction();
		}
		else
		{
			CurrentAction = (CanIdle ? NpcAction.Idle : NpcAction.Walk);
		}
		TimeUntilActionChange = new TimeSpan(0, 0, RandomHelper.Random.Next(2, 5));
		TimeSinceActionChange = TimeSpan.Zero;
		if (!initialized || CurrentAction != currentAction)
		{
			UpdateAction();
		}
	}

	private void RandomizeAction()
	{
		switch (CurrentAction)
		{
		case NpcAction.Burrow:
			ServiceHelper.RemoveComponent(this);
			return;
		case NpcAction.Turn:
			Turn();
			return;
		case NpcAction.TakeOff:
			CurrentAction = NpcAction.Fly;
			UpdateAction();
			return;
		case NpcAction.Land:
			CurrentAction = NpcAction.Idle;
			UpdateAction();
			return;
		}
		if ((RandomHelper.Probability(0.5) || !CanWalk) && CanIdle)
		{
			if (CanWalk || RandomHelper.Probability(0.5))
			{
				ChooseIdle();
			}
			else if (CanTurn)
			{
				CurrentAction = NpcAction.Turn;
			}
			else
			{
				Turn();
			}
			return;
		}
		if (CanWalk)
		{
			if (WalkStep == 1f || WalkStep == 0f)
			{
				if (CanIdle && RandomHelper.Probability(0.5))
				{
					ChooseIdle();
				}
				else if (CanTurn)
				{
					CurrentAction = NpcAction.Turn;
				}
				else
				{
					Turn();
				}
			}
			else if (CanTurn && RandomHelper.Probability(0.5))
			{
				CurrentAction = NpcAction.Turn;
			}
			else
			{
				CurrentAction = NpcAction.Walk;
			}
			return;
		}
		throw new InvalidOperationException("This NPC can't walk or idle!");
	}

	private void Turn()
	{
		LookingDirection = LookingDirection.GetOpposite();
		UpdateRotation();
		CurrentAction = ((!CanWalk) ? NpcAction.Idle : NpcAction.Walk);
	}

	private void ChooseIdle()
	{
		if (CurrentAction.IsSpecialIdle())
		{
			CurrentAction = NpcAction.Idle;
			return;
		}
		float num = RandomHelper.Unit();
		float num2 = 1 + CanIdle2.AsNumeric() + CanIdle3.AsNumeric();
		if (num < 1f / num2)
		{
			CurrentAction = NpcAction.Idle;
		}
		else if (num2 > 1f && num < 2f / num2)
		{
			CurrentAction = (CanIdle2 ? NpcAction.Idle2 : NpcAction.Idle3);
		}
		else if (num2 > 2f && num < 3f / num2)
		{
			CurrentAction = NpcAction.Idle3;
		}
	}

	public void UpdateAction(bool resumeTiming = false)
	{
		if (Emitter != null)
		{
			if (Emitter.Cue != null && Emitter.Cue.IsLooped)
			{
				Emitter.Cue.Stop(immediate: true);
			}
			Emitter = null;
		}
		NpcActionContent npcActionContent = Npc.Actions[CurrentAction];
		AnimatedTexture animation = npcActionContent.Animation;
		SoundEffect sound = Npc.Actions[CurrentAction].Sound;
		if (CurrentAction == NpcAction.Talk && CurrentLine != null && CurrentLine.OverrideContent != null)
		{
			if (CurrentLine.OverrideContent.Animation != null)
			{
				animation = CurrentLine.OverrideContent.Animation;
			}
			if (CurrentLine.OverrideContent.Sound != null)
			{
				sound = CurrentLine.OverrideContent.Sound;
			}
		}
		if (!resumeTiming)
		{
			CurrentTiming = animation.Timing.Clone();
		}
		CurrentAnimation = animation;
		if (!initialized)
		{
			DrawActionScheduler.Schedule(delegate
			{
				Group.Texture = animation.Texture;
			});
		}
		else
		{
			Group.Texture = animation.Texture;
		}
		UpdateScale();
		if (sound == null || (Npc.ActorType == ActorType.Owl && OwlInvisible) || !initialized)
		{
			return;
		}
		if (CurrentAction == NpcAction.Talk)
		{
			if (talkEmitter == null || talkEmitter.Dead)
			{
				talkEmitter = sound.EmitAt(Position, loop: true, RandomHelper.Centered(0.05));
				return;
			}
			talkEmitter.Position = Position;
			Waiters.Wait(0.10000000149011612, delegate
			{
				talkEmitter.Cue.Resume();
				talkEmitter.VolumeFactor = 1f;
			}).AutoPause = true;
		}
		else
		{
			Emitter = sound.EmitAt(Position, loop: true, RandomHelper.Centered(0.05));
		}
	}

	private void UpdateScale()
	{
		if (CurrentAnimation != null)
		{
			Group.Scale = new Vector3((float)CurrentAnimation.Offsets[0].Width / 16f, (float)CurrentAnimation.Offsets[0].Height / 16f, 1f);
		}
	}
}
