using System;
using System.Linq;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Components.Actions;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class GameNpcState : NpcState
{
	private bool SaidFirstLine;

	private int SequentialLineIndex;

	public bool ForceVisible;

	public bool IsNightForOwl;

	private readonly float originalSpeed;

	private bool waitingToSpeak;

	private SoundEffect takeoffSound;

	private SoundEffect hideSound;

	private SoundEffect comeOutSound;

	private SoundEffect burrowSound;

	protected override float AnimationSpeed
	{
		get
		{
			if (CurrentAction != NpcAction.Walk && CurrentAction != NpcAction.Turn)
			{
				return 1f;
			}
			return Npc.WalkSpeed / ((originalSpeed == 0f) ? 1f : originalSpeed);
		}
	}

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public IGraphicsDeviceService GraphicsDeviceService { private get; set; }

	[ServiceDependency]
	public ILevelService LevelService { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IOwlService OwlService { get; set; }

	[ServiceDependency(Optional = true)]
	public IWalkToService WalkTo { protected get; set; }

	public GameNpcState(Game game, NpcInstance npc)
		: base(game, npc)
	{
		originalSpeed = Npc.WalkSpeed;
	}

	public override void Initialize()
	{
		base.Initialize();
		if (CanTakeOff)
		{
			takeoffSound = base.CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Wildlife/BirdTakeoff");
		}
		if (CanHide)
		{
			hideSound = base.CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Wildlife/CritterHide");
			comeOutSound = base.CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Wildlife/CritterComeOut");
		}
		if (CanBurrow)
		{
			burrowSound = base.CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Wildlife/RabbitBurrow");
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (isDisposed)
		{
			return;
		}
		if (base.EngineState.Paused || GameState.InMap)
		{
			if (Emitter != null && !Emitter.Dead)
			{
				Emitter.FadeOutAndDie(0.1f);
			}
		}
		else
		{
			if (base.EngineState.Loading || base.EngineState.SkipRendering)
			{
				return;
			}
			Npc.Visible = base.CameraManager.Frustum.Contains(new BoundingBox(base.Position - Group.Scale, base.Position + Group.Scale)) != ContainmentType.Disjoint;
			if (Npc.Visible)
			{
				UpdateRotation();
			}
			if (!base.CameraManager.Viewpoint.IsOrthographic() || !base.CameraManager.ActionRunning)
			{
				return;
			}
			base.Update(gameTime);
			if (Npc.ActorType == ActorType.LightningGhost)
			{
				if (CurrentAction == NpcAction.Talk)
				{
					Group.Material.Opacity = Math.Min(Group.Material.Opacity + 0.01f, 0.5f);
				}
				else if (Npc.Talking)
				{
					Group.Material.Opacity = Math.Max(Group.Material.Opacity - 0.01f, 0f);
					if (Group.Material.Opacity == 0f)
					{
						Npc.Talking = false;
						Group.Material.Opacity = 1f;
					}
				}
			}
			if (Npc.ActorType != ActorType.Owl)
			{
				return;
			}
			bool flag = base.TimeManager.IsDayPhase(DayPhase.Night) || ForceVisible;
			if (ForceVisible)
			{
				OwlInvisible = false;
			}
			if (flag)
			{
				if (!IsNightForOwl && !GameState.SaveData.ThisLevel.InactiveNPCs.Contains(Npc.Id))
				{
					CurrentAction = NpcAction.Fly;
					UpdateAction();
					FlyingBack = true;
					OwlInvisible = false;
					float num = Npc.Position.Y - base.CameraManager.Center.Y;
					float num2 = Math.Min(base.CameraManager.Radius, 60f * GraphicsDeviceService.GraphicsDevice.GetViewScale()) / base.CameraManager.AspectRatio / 2f - num;
					Vector3 vector = Vector3.Transform(Vector3.Right, Group.Rotation);
					base.Position = Npc.Position + num2 * (-vector * 1.5029511f + Vector3.Up);
				}
			}
			else if (IsNightForOwl)
			{
				Hide();
				UpdateAction();
				MayComeBack = true;
			}
			IsNightForOwl = flag;
		}
	}

	protected override void TryFlee()
	{
		if (PlayerManager.Action == ActionType.IdleSleep)
		{
			return;
		}
		float num = (base.Position - PlayerManager.Position).Abs().Dot(base.CameraManager.Viewpoint.ScreenSpaceMask());
		Npc.WalkSpeed = originalSpeed * (1f + (1f - FezMath.Saturate(num / 3f)));
		switch (CurrentAction)
		{
		case NpcAction.Turn:
		case NpcAction.Burrow:
		case NpcAction.ComeOut:
		case NpcAction.TakeOff:
		case NpcAction.Fly:
			return;
		}
		Vector3 vector = Vector3.UnitY + base.CameraManager.Viewpoint.SideMask() * 3f + base.CameraManager.Viewpoint.DepthMask() * float.MaxValue;
		if (new BoundingBox(base.Position - vector, base.Position + vector).Contains(PlayerManager.Position) == ContainmentType.Disjoint)
		{
			if (CurrentAction == NpcAction.Hide)
			{
				CurrentAction = NpcAction.ComeOut;
				UpdateAction();
			}
			return;
		}
		float num2 = (PlayerManager.Position - base.Position).Dot(base.CameraManager.Viewpoint.RightVector());
		num2 *= (float)Math.Sign(Npc.DestinationOffset.Dot(base.CameraManager.Viewpoint.RightVector()));
		NpcAction currentAction = CurrentAction;
		if (num < 1f)
		{
			Hide();
		}
		else
		{
			if (CurrentAction == NpcAction.Hide)
			{
				CurrentAction = NpcAction.ComeOut;
				comeOutSound.EmitAt(base.Position);
				UpdateAction();
				return;
			}
			HorizontalDirection horizontalDirection = FezMath.DirectionFromMovement(0f - num2);
			if (LookingDirection != horizontalDirection)
			{
				if (CanTurn)
				{
					CurrentAction = NpcAction.Turn;
				}
				else
				{
					LookingDirection = horizontalDirection;
					CurrentAction = ((!CanWalk) ? NpcAction.Idle : NpcAction.Walk);
				}
			}
			else
			{
				CurrentAction = ((!CanWalk) ? NpcAction.Idle : NpcAction.Walk);
			}
		}
		if (currentAction != CurrentAction)
		{
			UpdateAction();
		}
	}

	private void Hide()
	{
		if (CanBurrow)
		{
			CurrentAction = NpcAction.Burrow;
			burrowSound.EmitAt(base.Position);
		}
		else if (CanHide)
		{
			if (CurrentAction != NpcAction.Hide)
			{
				hideSound.EmitAt(base.Position);
			}
			CurrentAction = NpcAction.Hide;
		}
		else if (CanTakeOff)
		{
			CurrentAction = NpcAction.TakeOff;
			takeoffSound.EmitAt(base.Position);
		}
		else
		{
			CurrentAction = (CanIdle ? NpcAction.Idle : NpcAction.Walk);
		}
	}

	protected override void TryTalk()
	{
		switch (PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Sliding:
			if (PlayerManager.Background || !SpeechManager.Hidden || (Npc.ActorType == ActorType.Owl && (OwlInvisible || CurrentAction == NpcAction.TakeOff || CurrentAction == NpcAction.Fly)))
			{
				break;
			}
			if (Npc.CustomSpeechLine == null)
			{
				if (InputManager.CancelTalk != FezButtonState.Pressed)
				{
					break;
				}
				Vector3 vector = Vector3.UnitY + (base.CameraManager.Viewpoint.SideMask() + base.CameraManager.Viewpoint.DepthMask()) * 1.5f;
				BoundingBox boundingBox = new BoundingBox(base.Position - vector, base.Position + vector);
				Vector3 mask = base.CameraManager.Viewpoint.VisibleAxis().GetMask();
				Vector3 vector2 = base.CameraManager.Viewpoint.ForwardVector();
				Ray ray = default(Ray);
				ray.Position = PlayerManager.Center * (Vector3.One - mask) - vector2 * base.LevelManager.Size;
				ray.Direction = vector2;
				Ray ray2 = ray;
				float? num = boundingBox.Intersects(ray2);
				if (!num.HasValue || TestObstruction(ray2.Position, num.Value))
				{
					break;
				}
			}
			Talk();
			break;
		}
	}

	private bool TestObstruction(Vector3 hitStart, float hitDistance)
	{
		Vector3 vector = base.CameraManager.Viewpoint.ForwardVector();
		Vector3 vector2 = base.CameraManager.Viewpoint.ScreenSpaceMask();
		foreach (TrileInstance value in base.LevelManager.Triles.Values)
		{
			if (value.InstanceId != -1 && ((value.Center - hitStart) * vector2).LengthSquared() < 0.5f)
			{
				Trile trile = value.Trile;
				if (value.Enabled && !trile.Immaterial && !trile.SeeThrough)
				{
					return (value.Position + Vector3.One / 2f + vector * -0.5f - hitStart).Dot(vector) <= hitDistance + 0.25f;
				}
			}
		}
		return false;
	}

	private void Talk()
	{
		bool wasTalking = CurrentAction == NpcAction.Talk;
		if (Npc.CustomSpeechLine != null)
		{
			CurrentLine = Npc.CustomSpeechLine;
		}
		else
		{
			SpeechLine currentLine = CurrentLine;
			if (Npc.Speech.Count <= 1 || (Npc.SayFirstSpeechLineOnce && !SaidFirstLine))
			{
				CurrentLine = Npc.Speech.FirstOrDefault();
			}
			else
			{
				do
				{
					if (Npc.RandomizeSpeech)
					{
						CurrentLine = RandomHelper.InList(Npc.Speech);
						continue;
					}
					CurrentLine = Npc.Speech[SequentialLineIndex];
					SequentialLineIndex++;
					if (SequentialLineIndex == Npc.Speech.Count)
					{
						SequentialLineIndex = 0;
					}
				}
				while (currentLine == CurrentLine || (Npc.SayFirstSpeechLineOnce && SaidFirstLine && CurrentLine == Npc.Speech[0]));
			}
			SaidFirstLine = true;
		}
		PlayerManager.Velocity *= Vector3.UnitY;
		Vector3 a = PlayerManager.Position - base.Position;
		Vector3 vector = base.CameraManager.Viewpoint.SideMask();
		Vector3 vector2 = base.CameraManager.Viewpoint.DepthMask();
		float value = a.Dot(vector);
		if (Math.Abs(value) < 1f && !wasTalking)
		{
			Vector3 vector3 = base.Position * vector + PlayerManager.Center * (Vector3.UnitY + vector2);
			_ = PlayerManager.Velocity;
			_ = PlayerManager.Ground;
			Vector3 potentialPosition = vector3 + (float)Math.Sign(value) * 1.125f * base.CameraManager.Viewpoint.SideMask();
			bool flag = true;
			if (!CollisionManager.CollidePoint(potentialPosition, Vector3.Down).Collided)
			{
				a *= -1f;
				value = a.Dot(vector);
				potentialPosition = vector3 + (float)Math.Sign(value) * 1.125f * base.CameraManager.Viewpoint.SideMask();
				if (!CollisionManager.CollidePoint(potentialPosition, Vector3.Down).Collided)
				{
					PlayerManager.Action = ActionType.ReadingSign;
					flag = false;
				}
			}
			if (flag)
			{
				WalkTo.Destination = () => potentialPosition;
				WalkTo.NextAction = ActionType.ReadingSign;
				PlayerManager.Action = ActionType.WalkingTo;
				waitingToSpeak = true;
			}
		}
		else
		{
			PlayerManager.Action = ActionType.ReadingSign;
		}
		LookingDirection = FezMath.DirectionFromMovement(a.Dot(base.CameraManager.Viewpoint.RightVector()));
		PlayerManager.LookingDirection = FezMath.DirectionFromMovement(0f - a.Dot(base.CameraManager.Viewpoint.RightVector()));
		Action action = delegate
		{
			waitingToSpeak = false;
			SpeechManager.Origin = base.Position + Vector3.UnitY * 0.5f;
			string s;
			if (base.LevelManager.SongName == "Majesty")
			{
				SpeechManager.Font = SpeechFont.Zuish;
				string stringRaw = GameText.GetStringRaw(CurrentLine.Text);
				SpeechManager.Origin = base.Position + Vector3.UnitY * 0.5f + base.CameraManager.Viewpoint.RightVector();
				SpeechManager.ChangeText(s = stringRaw);
			}
			else
			{
				SpeechManager.ChangeText(s = GameText.GetString(CurrentLine.Text));
			}
			CurrentAction = NpcAction.Talk;
			Npc.Talking = true;
			if (!wasTalking && Npc.ActorType == ActorType.LightningGhost)
			{
				Group.Material.Opacity = 0f;
			}
			talkWaiter = Waiters.Wait(0.1f + 0.075f * (float)s.StripPunctuation().Length * (float)((!Culture.IsCJK) ? 1 : 2), delegate
			{
				if (talkEmitter != null)
				{
					talkEmitter.FadeOutAndPause(0.1f);
				}
			});
			talkWaiter.AutoPause = true;
			UpdateAction(wasTalking);
			SyncTextureMatrix();
		};
		if (PlayerManager.Action == ActionType.WalkingTo)
		{
			if (CanIdle)
			{
				CurrentAction = NpcAction.Idle;
				UpdateAction();
			}
			else
			{
				CurrentAction = NpcAction.Talk;
				UpdateAction();
			}
			Waiters.Wait(() => PlayerManager.Action != ActionType.WalkingTo, action).AutoPause = true;
		}
		else
		{
			action();
		}
	}

	protected override bool TryStopTalking()
	{
		bool flag = (SpeechManager.Hidden && !waitingToSpeak) || !PlayerManager.Grounded;
		if (flag)
		{
			waitingToSpeak = false;
			if (Npc.CustomSpeechLine == null && !Npc.RandomizeSpeech && !Npc.SayFirstSpeechLineOnce && SequentialLineIndex != 0)
			{
				Talk();
				return false;
			}
			if (talkWaiter != null && talkWaiter.Alive)
			{
				talkWaiter.Cancel();
			}
			if (talkEmitter != null && !talkEmitter.Dead)
			{
				talkEmitter.FadeOutAndPause(0.1f);
			}
			if (!SpeechManager.Hidden)
			{
				SpeechManager.Hide();
			}
			Npc.CustomSpeechLine = null;
			if (Npc.ActorType == ActorType.Owl)
			{
				Vector3 vector = PlayerManager.Position - base.Position;
				LookingDirection = FezMath.DirectionFromMovement((-vector * Npc.DestinationOffset.Sign()).Dot(base.CameraManager.Viewpoint.SideMask()));
				CurrentAction = NpcAction.TakeOff;
				UpdateAction();
				GameState.SaveData.CollectedOwls++;
				OwlService.OnOwlCollected();
				GameState.SaveData.ThisLevel.InactiveNPCs.Add(Npc.Id);
				LevelService.ResolvePuzzle();
			}
		}
		return flag;
	}
}
