using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class PlayerCameraControl : GameComponent
{
	private const float TrilesBeforeScreenMove = 1.5f;

	public const float VerticalOffset = 4f;

	private Vector3 StickyCenter;

	private float MinimumStickDistance;

	private SoundEffect swooshLeft;

	private SoundEffect swooshRight;

	private SoundEffect slowSwooshLeft;

	private SoundEffect slowSwooshRight;

	private Vector2 lastFactors;

	[ServiceDependency]
	public IGraphicsDeviceService GraphicsDeviceService { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IMouseStateManager MouseState { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public PlayerCameraControl(Game game)
		: base(game)
	{
		base.UpdateOrder = 10;
	}

	public override void Initialize()
	{
		swooshLeft = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateLeft");
		swooshRight = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateRight");
		slowSwooshLeft = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateLeftHalfSpeed");
		slowSwooshRight = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateRightHalfSpeed");
		CameraManager.Radius = CameraManager.DefaultViewableWidth;
		LevelManager.LevelChanged += delegate
		{
			CameraManager.StickyCam = false;
		};
		CameraManager.ViewpointChanged += delegate
		{
			MinimumStickDistance = 2f;
			if (CameraManager.Viewpoint == Viewpoint.Perspective || GameState.InMap)
			{
				CameraManager.OriginalDirection = CameraManager.Direction;
			}
		};
		TimeInterpolation.RegisterCallback(delegate
		{
			PerFrameFollowGomez();
		}, 50);
	}

	private void TrackBeforeRotation()
	{
		if (!GameState.Paused && (!CameraManager.Constrained || CameraManager.PanningConstraints.HasValue || GameState.InMap) && !PlayerManager.Hidden && (CameraManager.ActionRunning || CameraManager.ForceTransition) && CameraManager.Viewpoint != Viewpoint.Perspective)
		{
			FollowGomez();
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.InCutscene)
		{
			return;
		}
		if (!PlayerManager.CanControl && !GameState.InMenuCube && !GameState.InMap)
		{
			InputManager.SaveState();
			InputManager.Reset();
		}
		if ((!PlayerManager.Action.PreventsRotation() || GameState.InMap || GameState.InMenuCube || GameState.InFpsMode) && PlayerManager.CanRotate && (!LevelManager.Flat || PlayerManager.Action == ActionType.GrabTombstone || GameState.InMap || GameState.InFpsMode || GameState.InMenuCube) && !GameState.Paused && !GameState.InCutscene)
		{
			bool flag = PlayerManager.Action == ActionType.GrabTombstone;
			if (!GameState.InFpsMode && !GameState.DisallowRotation)
			{
				if (InputManager.RotateLeft == FezButtonState.Pressed || (flag && InputManager.RotateLeft == FezButtonState.Down))
				{
					TrackBeforeRotation();
					RotateViewLeft();
				}
				else if (InputManager.RotateRight == FezButtonState.Pressed || (flag && InputManager.RotateRight == FezButtonState.Down))
				{
					TrackBeforeRotation();
					RotateViewRight();
				}
			}
			if (CameraManager.Viewpoint == Viewpoint.Perspective || CameraManager.RequestedViewpoint == Viewpoint.Perspective || GameState.InMap)
			{
				if (CameraManager.Viewpoint == Viewpoint.Perspective && !GameState.InMenuCube && !GameState.InMap)
				{
					float num = 4f * (float)((!LevelManager.Descending) ? 1 : (-1)) / CameraManager.PixelsPerTrixel;
					Vector3 stickyCenter = (CameraManager.Center = Vector3.Lerp(CameraManager.Center, PlayerManager.Position + num * Vector3.UnitY, 0.075f));
					StickyCenter = stickyCenter;
				}
				if (GameState.InFpsMode)
				{
					if (InputManager.FreeLook != Vector2.Zero)
					{
						int num2 = (SettingsManager.Settings.InvertLookX ? 1 : (-1));
						int num3 = (SettingsManager.Settings.InvertLookY ? 1 : (-1));
						if (MouseState.LeftButton.State != MouseButtonStates.Dragging)
						{
							num2 *= -1;
							num3 *= -1;
						}
						Vector3 value = Vector3.Transform(CameraManager.Direction, Quaternion.CreateFromAxisAngle(CameraManager.InverseView.Right, InputManager.FreeLook.Y * 0.4f * (float)num3));
						if ((double)value.Y > 0.7 || (double)value.Y < -0.7)
						{
							float num4 = 0.7f / new Vector2(value.X, value.Z).Length();
							value = new Vector3(value.X * num4, 0.7f * (float)Math.Sign(value.Y), value.Z * num4);
						}
						value = Vector3.Transform(value, Quaternion.CreateFromAxisAngle(CameraManager.InverseView.Up, (0f - InputManager.FreeLook.X) * 0.5f * (float)num2));
						if (!CameraManager.ActionRunning)
						{
							CameraManager.AlterTransition(FezMath.Slerp(CameraManager.Direction, value, 0.1f));
						}
						else
						{
							CameraManager.Direction = FezMath.Slerp(CameraManager.Direction, value, 0.1f);
						}
						if (CameraManager.Direction.Y < -0.625f)
						{
							CameraManager.Direction = new Vector3(CameraManager.Direction.X, -0.625f, CameraManager.Direction.Z);
						}
					}
				}
				else if (InputManager.FreeLook != Vector2.Zero || GameState.InMap)
				{
					Vector3 vector2 = Vector3.Transform(CameraManager.OriginalDirection, Matrix.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 2f));
					Vector3 to = Vector3.Transform(CameraManager.OriginalDirection, Matrix.CreateFromAxisAngle(vector2, -(float)Math.PI / 2f));
					Vector2 vector3 = InputManager.FreeLook / (GameState.MenuCubeIsZoomed ? 1.75f : 6.875f);
					float step = 0.1f;
					if (GameState.InMap && MouseState.LeftButton.State == MouseButtonStates.Dragging)
					{
						vector3 = Vector2.Clamp(new Vector2(-MouseState.LeftButton.DragState.Movement.X, MouseState.LeftButton.DragState.Movement.Y) / (300f * GraphicsDeviceService.GraphicsDevice.GetViewScale()), -Vector2.One, Vector2.One) / 3.4375f;
						step = 0.2f;
						lastFactors = vector3;
					}
					if (GameState.InMap && MouseState.LeftButton.State == MouseButtonStates.DragEnded)
					{
						if (lastFactors.X > 0.175f)
						{
							RotateViewRight();
						}
						else if (lastFactors.X < -0.175f)
						{
							RotateViewLeft();
						}
					}
					if (GameState.InMap)
					{
						vector3 *= new Vector2(3.425f, 1.725f);
						vector3.Y += 0.25f;
						vector3.X += 0.5f;
					}
					Vector3 to2 = FezMath.Slerp(FezMath.Slerp(CameraManager.OriginalDirection, vector2, vector3.X), to, vector3.Y);
					if (!CameraManager.ActionRunning)
					{
						CameraManager.AlterTransition(FezMath.Slerp(CameraManager.Direction, to2, step));
					}
					else
					{
						CameraManager.Direction = FezMath.Slerp(CameraManager.Direction, to2, step);
					}
				}
				else if (!CameraManager.ActionRunning)
				{
					CameraManager.AlterTransition(FezMath.Slerp(CameraManager.Direction, CameraManager.OriginalDirection, 0.1f));
				}
				else
				{
					CameraManager.Direction = FezMath.Slerp(CameraManager.Direction, CameraManager.OriginalDirection, 0.1f);
				}
			}
		}
		if (CameraManager.RequestedViewpoint != 0)
		{
			if (CameraManager.RequestedViewpoint != CameraManager.Viewpoint)
			{
				RotateTo(CameraManager.RequestedViewpoint);
			}
			CameraManager.RequestedViewpoint = Viewpoint.None;
		}
		if (!GameState.Paused && (!CameraManager.Constrained || CameraManager.PanningConstraints.HasValue || GameState.InMap) && !PlayerManager.Hidden)
		{
			if ((CameraManager.ActionRunning || CameraManager.ForceTransition) && CameraManager.Viewpoint != Viewpoint.Perspective)
			{
				if (InputManager.FreeLook != Vector2.Zero)
				{
					if (!CameraManager.StickyCam)
					{
						StickyCenter = CameraManager.Center;
					}
					MinimumStickDistance = float.MaxValue;
					Vector2 vector4 = InputManager.FreeLook;
					if (MouseState.LeftButton.State == MouseButtonStates.Dragging)
					{
						vector4 = -vector4;
					}
					CameraManager.StickyCam = true;
					float viewScale = GraphicsDeviceService.GraphicsDevice.GetViewScale();
					Vector2 vector5 = new Vector2(CameraManager.Radius, CameraManager.Radius / CameraManager.AspectRatio) * 0.4f / viewScale;
					StickyCenter = Vector3.Lerp(StickyCenter, PlayerManager.Position + (vector4.X * CameraManager.Viewpoint.RightVector() * vector5.X + vector4.Y * Vector3.UnitY * vector5.Y), 0.05f);
				}
				if (InputManager.ClampLook == FezButtonState.Pressed)
				{
					CameraManager.StickyCam = false;
				}
			}
		}
		else
		{
			CameraManager.StickyCam = false;
		}
		if (!PlayerManager.CanControl && !GameState.InMenuCube && !GameState.InMap)
		{
			InputManager.RecoverState();
		}
	}

	private void PerFrameFollowGomez()
	{
		if (!GameState.Loading && !GameState.InCutscene && EndCutscene32Host.Instance == null && !GameState.Paused && (!CameraManager.Constrained || CameraManager.PanningConstraints.HasValue || GameState.InMap) && !PlayerManager.Hidden && (CameraManager.ActionRunning || CameraManager.ForceTransition) && CameraManager.Viewpoint != Viewpoint.Perspective)
		{
			FollowGomez();
		}
	}

	private void FollowGomez()
	{
		float num = CameraManager.PixelsPerTrixel;
		if (GameState.FarawaySettings.InTransition && FezMath.AlmostEqual(GameState.FarawaySettings.DestinationCrossfadeStep, 1f))
		{
			num = MathHelper.Lerp(CameraManager.PixelsPerTrixel, GameState.FarawaySettings.DestinationPixelsPerTrixel, (GameState.FarawaySettings.TransitionStep - 0.875f) / 0.125f);
		}
		float num2 = 4f * (float)((!LevelManager.Descending) ? 1 : (-1)) / num;
		Vector3 interpolatedPosition = GomezHost.Instance.InterpolatedPosition;
		Vector3 center = new Vector3(CameraManager.Center.X, interpolatedPosition.Y + num2, CameraManager.Center.Z);
		Vector3 center2 = CameraManager.Center;
		if (CameraManager.StickyCam)
		{
			Vector3 vector = interpolatedPosition + Vector3.UnitY * num2;
			Vector3 vector2 = StickyCenter * CameraManager.Viewpoint.ScreenSpaceMask() - vector * CameraManager.Viewpoint.ScreenSpaceMask();
			float num3 = vector2.Length() + 1f;
			if (InputManager.FreeLook == Vector2.Zero)
			{
				MinimumStickDistance = Math.Min(num3, MinimumStickDistance);
				float viewScale = GraphicsDeviceService.GraphicsDevice.GetViewScale();
				if (Math.Abs(vector2.X + vector2.Z) > CameraManager.Radius * 0.4f / viewScale || Math.Abs(vector2.Y) > CameraManager.Radius * 0.4f / CameraManager.AspectRatio / viewScale)
				{
					MinimumStickDistance = 2.5f;
				}
				if (MinimumStickDistance < 4f)
				{
					StickyCenter = Vector3.Lerp(StickyCenter, vector, (float)Math.Pow(1f / MinimumStickDistance, 4.0));
				}
			}
			center = StickyCenter;
			if ((double)num3 <= 1.1)
			{
				CameraManager.StickyCam = false;
			}
		}
		else
		{
			if (MathHelper.Clamp(interpolatedPosition.X, center2.X - 1.5f, center2.X + 1.5f) != interpolatedPosition.X)
			{
				float num4 = interpolatedPosition.X - center2.X;
				center.X += num4 - 1.5f * (float)Math.Sign(num4);
			}
			if (MathHelper.Clamp(interpolatedPosition.Z, center2.Z - 1.5f, center2.Z + 1.5f) != interpolatedPosition.Z)
			{
				float num5 = interpolatedPosition.Z - CameraManager.Center.Z;
				center.Z += num5 - 1.5f * (float)Math.Sign(num5);
			}
		}
		if (CameraManager.PanningConstraints.HasValue && WorldMap.Instance == null)
		{
			Vector3 vector3 = CameraManager.Viewpoint.DepthMask();
			Vector3 vector4 = CameraManager.Viewpoint.SideMask();
			Vector3 vector5 = new Vector3(MathHelper.Lerp(CameraManager.ConstrainedCenter.X, center.X, CameraManager.PanningConstraints.Value.X), MathHelper.Lerp(CameraManager.ConstrainedCenter.Y, center.Y, CameraManager.PanningConstraints.Value.Y), MathHelper.Lerp(CameraManager.ConstrainedCenter.Z, center.Z, CameraManager.PanningConstraints.Value.X));
			CameraManager.Center = CameraManager.Center * vector3 + vector4 * vector5 + Vector3.UnitY * vector5;
		}
		else if (!GameState.InMenuCube && WorldMap.Instance == null && Intro.Instance == null && !FezMath.In(PlayerManager.Action, ActionType.PullUpCornerLedge, ActionType.LowerToCornerLedge, ActionType.PullUpFront, ActionType.LowerToLedge, ActionType.PullUpBack, ActionType.Victory, ActionTypeComparer.Default))
		{
			CameraManager.Center = center;
		}
	}

	private void RotateViewLeft()
	{
		bool flag = PlayerManager.Action == ActionType.GrabTombstone;
		if (CameraManager.Viewpoint == Viewpoint.Perspective || GameState.InMap)
		{
			CameraManager.OriginalDirection = Vector3.Transform(CameraManager.OriginalDirection, Quaternion.CreateFromAxisAngle(Vector3.Up, -(float)Math.PI / 2f));
			if (!GameState.InMenuCube && !GameState.InMap)
			{
				EmitLeft();
			}
		}
		else if (CameraManager.ChangeViewpoint(CameraManager.Viewpoint.GetRotatedView(-1), (float)((!flag) ? 1 : 2) * Math.Abs(1f / CollisionManager.GravityFactor)) && !flag)
		{
			EmitLeft();
		}
		if (LevelManager.NodeType == LevelNodeType.Lesser && PlayerManager.AirTime != TimeSpan.Zero)
		{
			PlayerManager.Velocity *= CameraManager.Viewpoint.ScreenSpaceMask();
		}
	}

	private void RotateViewRight()
	{
		bool flag = PlayerManager.Action == ActionType.GrabTombstone;
		if (CameraManager.Viewpoint == Viewpoint.Perspective || GameState.InMap)
		{
			CameraManager.OriginalDirection = Vector3.Transform(CameraManager.OriginalDirection, Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 2f));
			if (!GameState.InMenuCube && !GameState.InMap)
			{
				EmitRight();
			}
		}
		else if (CameraManager.ChangeViewpoint(CameraManager.Viewpoint.GetRotatedView(1), (float)((!flag) ? 1 : 2) * Math.Abs(1f / CollisionManager.GravityFactor)) && !flag)
		{
			EmitRight();
		}
		if (LevelManager.NodeType == LevelNodeType.Lesser && PlayerManager.AirTime != TimeSpan.Zero)
		{
			PlayerManager.Velocity *= CameraManager.Viewpoint.ScreenSpaceMask();
		}
	}

	private void EmitLeft()
	{
		if (!Fez.LongScreenshot)
		{
			if (CollisionManager.GravityFactor == 1f)
			{
				swooshLeft.Emit();
			}
			else
			{
				slowSwooshLeft.Emit();
			}
		}
	}

	private void EmitRight()
	{
		if (!Fez.LongScreenshot)
		{
			if (CollisionManager.GravityFactor == 1f)
			{
				swooshRight.Emit();
			}
			else
			{
				slowSwooshRight.Emit();
			}
		}
	}

	private void RotateTo(Viewpoint view)
	{
		if (Math.Abs(CameraManager.Viewpoint.GetDistance(view)) > 1)
		{
			EmitRight();
		}
		CameraManager.ChangeViewpoint(view);
	}
}
