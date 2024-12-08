using System;
using FezEngine;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Services;

public class GameCameraManager : DefaultCameraManager, IGameCameraManager, IDefaultCameraManager, ICameraProvider
{
	private static readonly float FirstPersonFov = MathHelper.ToRadians(75f);

	private int concurrentChanges;

	private float originalCarriedPhi;

	private bool shouldRotateInstance;

	public override float InterpolationSpeed
	{
		get
		{
			return base.InterpolationSpeed * (1.5f + Math.Abs(CollisionManager.GravityFactor) * 0.5f) / 2f;
		}
		set
		{
			base.InterpolationSpeed = value;
		}
	}

	public override bool ActionRunning
	{
		get
		{
			if (!Fez.LongScreenshot)
			{
				if (!base.ActionRunning && PlayerManager.Action != ActionType.PivotTombstone)
				{
					return PlayerManager.Action == ActionType.GrabTombstone;
				}
				return true;
			}
			return true;
		}
	}

	public Viewpoint RequestedViewpoint { get; set; }

	public Vector3 OriginalDirection { get; set; }

	public override float Radius
	{
		get
		{
			if (!GameState.MenuCubeIsZoomed)
			{
				return base.Radius;
			}
			return 18f;
		}
		set
		{
			base.Radius = value;
		}
	}

	[ServiceDependency]
	public IGraphicsDeviceService GraphicsDeviceService { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ICameraService CameraService { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	public GameCameraManager(Game game)
		: base(game)
	{
	}

	public void CancelViewTransition()
	{
		directionTransition = null;
		viewpoint = lastViewpoint;
		current = predefinedViews[viewpoint];
		current.Direction = -viewpoint.ForwardVector();
		base.ViewTransitionCancelled = true;
	}

	public override void Initialize()
	{
		base.Initialize();
		base.LevelManager.LevelChanged += delegate
		{
			lastViewpoint = viewpoint;
		};
	}

	public override bool ChangeViewpoint(Viewpoint newView, float speedFactor)
	{
		if (speedFactor != 0f && (newView == viewpoint || concurrentChanges >= 1))
		{
			return false;
		}
		if (PlayerManager.Action == ActionType.GrabTombstone && !PlayerManager.Animation.Timing.Ended)
		{
			return false;
		}
		if (!base.ViewTransitionReached && speedFactor != 0f)
		{
			concurrentChanges++;
		}
		shouldRotateInstance = newView.IsOrthographic() && viewpoint.IsOrthographic();
		if (newView == Viewpoint.Perspective)
		{
			predefinedViews[newView].Direction = current.Direction;
		}
		base.ChangeViewpoint(newView, speedFactor);
		if (PlayerManager.CarriedInstance != null && concurrentChanges == 0)
		{
			originalCarriedPhi = PlayerManager.CarriedInstance.Phi;
		}
		CameraService.OnRotate();
		return true;
	}

	public void RecordNewCarriedInstancePhi()
	{
		PlayerManager.CarriedInstance.Phi = FezMath.SnapPhi(PlayerManager.CarriedInstance.Phi);
		TrileMaterializer trileMaterializer = LevelMaterializer.GetTrileMaterializer(PlayerManager.CarriedInstance.VisualTrile);
		trileMaterializer.UpdateInstance(PlayerManager.CarriedInstance);
		trileMaterializer.CommitBatch();
		originalCarriedPhi = PlayerManager.CarriedInstance.Phi;
		shouldRotateInstance = false;
	}

	protected override void PostUpdate()
	{
		if (!GameState.Loading)
		{
			if (concurrentChanges == 1 && (double)base.ViewTransitionStep > 0.8)
			{
				concurrentChanges = 0;
			}
			if (base.ViewTransitionReached)
			{
				concurrentChanges = 0;
			}
			if (PlayerManager.CarriedInstance != null && shouldRotateInstance)
			{
				float num = FezMath.WrapAngle((float)Math.Atan2(base.LastViewpoint.ForwardVector().Z, base.LastViewpoint.ForwardVector().X));
				float num2 = FezMath.WrapAngle((float)Math.PI - (float)Math.Atan2(base.View.Forward.Z, base.View.Forward.X));
				PlayerManager.CarriedInstance.Phi = FezMath.WrapAngle(originalCarriedPhi + (num - num2));
				TrileMaterializer trileMaterializer = LevelMaterializer.GetTrileMaterializer(PlayerManager.CarriedInstance.VisualTrile);
				trileMaterializer.UpdateInstance(PlayerManager.CarriedInstance);
				trileMaterializer.CommitBatch();
			}
		}
	}

	protected override void DollyZoom()
	{
		float viewScale = GraphicsDeviceService.GraphicsDevice.GetViewScale();
		if (!GameState.InFpsMode)
		{
			base.DollyZoom();
			return;
		}
		bool flag = viewpoint.IsOrthographic();
		float num = ((directionTransition.TotalStep == 0f) ? 0.001f : directionTransition.TotalStep);
		if (num == 1f && !directionTransition.Reached)
		{
			num -= 0.001f;
		}
		float firstPersonFov = FirstPersonFov;
		float num2 = MathHelper.Lerp(flag ? firstPersonFov : 0f, flag ? 0f : firstPersonFov, num);
		float num3 = radiusBeforeTransition;
		if (base.DollyZoomOut)
		{
			num3 = radiusBeforeTransition + (1f - Easing.EaseIn(num, EasingType.Quadratic)) * 15f;
		}
		float num4 = num3 / base.AspectRatio / (2f * (float)Math.Tan(num2 / 2f)) / viewScale;
		if (directionTransition.Reached)
		{
			base.ProjectionTransition = false;
			if (!flag)
			{
				predefinedViews[lastViewpoint].Direction = -lastViewpoint.ForwardVector();
				current.Radius = 0.1f;
			}
			else
			{
				current.Radius = radiusBeforeTransition;
				base.NearPlane = 0.25f;
				base.FarPlane = 500f;
				GameState.InFpsMode = false;
			}
			base.FogManager.Density = ((base.LevelManager.Sky == null) ? 0f : base.LevelManager.Sky.FogDensity);
			base.DollyZoomOut = false;
			RebuildProjection();
			SnapInterpolation();
		}
		else
		{
			base.FogManager.Density = ((base.LevelManager.Sky == null) ? 0f : base.LevelManager.Sky.FogDensity) * Easing.EaseIn(flag ? (1f - num) : num, EasingType.Quadratic);
			float num5 = num4 * (flag ? num : (1f - num)) + 0.1f;
			base.NearPlane = Math.Max(0.25f, 0.25f + num5 - num3);
			base.FarPlane = Math.Max(num5 + base.NearPlane, 499.75f);
			base.FieldOfView = num2;
			projection = Matrix.CreatePerspectiveFieldOfView(base.FieldOfView, base.AspectRatio, base.NearPlane, base.FarPlane);
			OnProjectionChanged();
			current.Radius = num5;
			view = Matrix.CreateLookAt(current.Radius * current.Direction + current.Center, current.Center, Vector3.UnitY);
			OnViewChanged();
		}
	}
}
