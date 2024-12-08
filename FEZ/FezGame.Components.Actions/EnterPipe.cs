using System;
using System.Linq;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.Actions;

internal class EnterPipe : PlayerAction
{
	private enum States
	{
		None,
		Sucked,
		FadeOut,
		LevelChange,
		FadeIn,
		SpitOut
	}

	private const float SuckedSeconds = 0.75f;

	private const float FadeSeconds = 1.25f;

	private SoundEffect EnterSound;

	private SoundEffect ExitSound;

	private Volume PipeVolume;

	private States State;

	private bool Descending;

	private TimeSpan SinceChanged;

	private string NextLevel;

	private float Depth;

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	public EnterPipe(Game game)
		: base(game)
	{
		base.DrawOrder = 101;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		EnterSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Sewer/PipeDown");
		ExitSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Sewer/PipeUp");
	}

	protected override void TestConditions()
	{
		if (base.PlayerManager.Action != ActionType.WalkingTo && !IsActionAllowed(base.PlayerManager.Action))
		{
			if (base.PlayerManager.Grounded && base.PlayerManager.PipeVolume.HasValue && base.InputManager.Down == FezButtonState.Pressed)
			{
				PipeVolume = base.LevelManager.Volumes[base.PlayerManager.PipeVolume.Value];
				base.PlayerManager.Action = ActionType.EnteringPipe;
				Descending = true;
			}
			if (!base.PlayerManager.Grounded && base.PlayerManager.PipeVolume.HasValue && base.InputManager.Up.IsDown() && base.PlayerManager.Ceiling.AnyCollided())
			{
				PipeVolume = base.LevelManager.Volumes[base.PlayerManager.PipeVolume.Value];
				base.PlayerManager.Action = ActionType.EnteringPipe;
				Descending = false;
				Vector3 vector = base.CameraManager.Viewpoint.ScreenSpaceMask();
				Vector3 vector2 = (PipeVolume.From + PipeVolume.To) / 2f;
				base.PlayerManager.Position = base.PlayerManager.Position * vector + vector2 * (Vector3.One - vector);
			}
		}
	}

	protected override void Begin()
	{
		NextLevel = base.PlayerManager.NextLevel;
		State = States.Sucked;
		SinceChanged = TimeSpan.Zero;
		base.PlayerManager.Velocity = Vector3.Zero;
		EnterSound.EmitAt(base.PlayerManager.Position);
	}

	protected override void End()
	{
		State = States.None;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		switch (State)
		{
		case States.Sucked:
			base.PlayerManager.Position += (float)elapsed.TotalSeconds * Vector3.UnitY * ((!Descending) ? 1 : (-1)) * 0.75f;
			SinceChanged += elapsed;
			if (SinceChanged.TotalSeconds > 0.75)
			{
				State = States.FadeOut;
				SinceChanged = TimeSpan.Zero;
			}
			break;
		case States.FadeOut:
			base.PlayerManager.Position += (float)elapsed.TotalSeconds * Vector3.UnitY * ((!Descending) ? 1 : (-1)) * 0.75f;
			SinceChanged += elapsed;
			if (SinceChanged.TotalSeconds > 1.25)
			{
				State = States.LevelChange;
				SinceChanged = TimeSpan.Zero;
				base.GameState.Loading = true;
				Worker<bool> worker = ThreadPool.Take<bool>(DoLoad);
				worker.Finished += delegate
				{
					ThreadPool.Return(worker);
				};
				worker.Start(context: false);
			}
			break;
		case States.FadeIn:
		{
			if (SinceChanged == TimeSpan.Zero)
			{
				ExitSound.EmitAt(base.PlayerManager.Position);
			}
			Vector3 vector = base.CameraManager.Viewpoint.ScreenSpaceMask();
			base.PlayerManager.Position = base.PlayerManager.Position * vector + Depth * (Vector3.One - vector);
			base.PlayerManager.Position += (float)elapsed.TotalSeconds * Vector3.UnitY * (Descending ? (-1.1f) : 1f) * 0.75f;
			SinceChanged += elapsed;
			if (SinceChanged.TotalSeconds > 1.25)
			{
				State = States.SpitOut;
				SinceChanged = TimeSpan.Zero;
			}
			break;
		}
		case States.SpitOut:
		{
			base.PlayerManager.Position += (float)elapsed.TotalSeconds * Vector3.UnitY * (Descending ? (-1.1f) : 1f) * 0.875f;
			SinceChanged += elapsed;
			bool flag = true;
			PointCollision[] cornerCollision = base.PlayerManager.CornerCollision;
			for (int i = 0; i < cornerCollision.Length; i++)
			{
				PointCollision pointCollision = cornerCollision[i];
				flag &= pointCollision.Instances.Deep == null;
			}
			if ((!Descending && flag) || SinceChanged.TotalSeconds > 0.75)
			{
				State = States.None;
				SinceChanged = TimeSpan.Zero;
				if (!Descending)
				{
					base.PlayerManager.Position += 0.5f * Vector3.UnitY;
					base.PlayerManager.Velocity -= Vector3.UnitY;
					base.PhysicsManager.Update(base.PlayerManager);
				}
				base.PlayerManager.Action = ActionType.Idle;
			}
			break;
		}
		}
		return false;
	}

	private void DoLoad(bool dummy)
	{
		base.LevelManager.ChangeLevel(NextLevel);
		base.GameState.ScheduleLoadEnd = true;
		State = States.FadeIn;
		Volume volume = base.LevelManager.Volumes.Values.FirstOrDefault((Volume v) => v.Id == base.LevelManager.Scripts.Values.First((Script s) => s.Actions.Any((ScriptAction a) => a.Operation == "AllowPipeChangeLevel")).Triggers[0].Object.Identifier);
		if (volume == null)
		{
			throw new InvalidOperationException("Missing pipe volume in destination level!");
		}
		Vector3 vector = (volume.From + volume.To) / 2f;
		base.PlayerManager.Action = ActionType.EnteringPipe;
		base.PlayerManager.Position = vector + Vector3.UnitY * 1.25f * (Descending ? 1 : (-1));
		base.PlayerManager.Velocity = Vector3.Zero;
		base.PlayerManager.RecordRespawnInformation();
		Depth = vector.Dot(base.CameraManager.Viewpoint.DepthMask());
	}

	public override void Draw(GameTime gameTime)
	{
		if (State == States.FadeOut || State == States.FadeIn || State == States.LevelChange)
		{
			double num = SinceChanged.TotalSeconds / 1.25;
			if (State == States.FadeIn)
			{
				num = 1.0 - num;
			}
			float alpha = FezMath.Saturate(Easing.EaseIn(num, EasingType.Cubic));
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, alpha));
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.EnteringPipe)
		{
			return State != States.None;
		}
		return true;
	}
}
