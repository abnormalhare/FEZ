using System;
using System.Collections.Generic;
using Common;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Services;

public class DefaultCameraManager : CameraManager, IDefaultCameraManager, ICameraProvider
{
	protected class PredefinedView
	{
		public Vector3 Center;

		public float Radius;

		public Vector3 Direction;

		public PredefinedView()
		{
		}

		public PredefinedView(Vector3 center, float radius, Vector3 direction)
		{
			Center = center;
			Radius = radius;
			Direction = direction;
		}
	}

	public const float TrixelsPerTrile = 16f;

	protected const float TransitionTiltFactor = 0f;

	public static float TransitionSpeed = 0.45f;

	public static bool NoInterpolation;

	protected static readonly float DefaultFov = MathHelper.ToRadians(45f);

	protected const float DefaultNearPlane = 0.25f;

	protected const float DefaultFarPlane = 500f;

	protected readonly Dictionary<Viewpoint, PredefinedView> predefinedViews;

	protected PredefinedView current = new PredefinedView();

	protected float defaultViewableWidth = 26.666666f;

	protected readonly PredefinedView interpolated = new PredefinedView();

	protected Vector3SplineInterpolation directionTransition;

	protected Viewpoint viewpoint;

	protected Viewpoint lastViewpoint;

	protected Viewpoint olderViewpoint;

	protected bool viewNewlyReached;

	protected bool projNewlyReached;

	protected bool projReached;

	protected float radiusBeforeTransition;

	private GameTime dummyGt = new GameTime();

	private bool transitionNewlyReached;

	private Vector3 viewOffset;

	private float pixelsPerTrixel;

	private bool constrained;

	public virtual float InterpolationSpeed { get; set; }

	public bool ViewTransitionCancelled { get; protected set; }

	public bool ForceTransition { get; set; }

	public Matrix InverseView { get; private set; }

	public Vector3 Center
	{
		get
		{
			return current.Center;
		}
		set
		{
			current.Center = value;
		}
	}

	public Vector3 InterpolatedCenter
	{
		get
		{
			return interpolated.Center;
		}
		set
		{
			interpolated.Center = value;
		}
	}

	public virtual float Radius
	{
		get
		{
			if (!ProjectionTransition)
			{
				return interpolated.Radius;
			}
			return current.Radius;
		}
		set
		{
			bool num = !FezMath.AlmostEqual(current.Radius, value);
			current.Radius = value;
			if (num && viewpoint.IsOrthographic())
			{
				RebuildProjection();
			}
		}
	}

	public Vector3 Direction
	{
		get
		{
			return current.Direction;
		}
		set
		{
			current.Direction = value;
		}
	}

	public float DefaultViewableWidth
	{
		get
		{
			return defaultViewableWidth;
		}
		set
		{
			defaultViewableWidth = value;
			foreach (PredefinedView value2 in predefinedViews.Values)
			{
				value2.Radius = defaultViewableWidth;
			}
			RebuildProjection();
		}
	}

	public Viewpoint Viewpoint => viewpoint;

	public FaceOrientation VisibleOrientation => Viewpoint.VisibleOrientation();

	public Viewpoint LastViewpoint
	{
		get
		{
			if (lastViewpoint != Viewpoint.Perspective)
			{
				return lastViewpoint;
			}
			return olderViewpoint;
		}
	}

	public bool ProjectionTransitionNewlyReached
	{
		get
		{
			if (transitionNewlyReached)
			{
				return lastViewpoint == Viewpoint.Perspective;
			}
			return false;
		}
	}

	public bool DollyZoomOut { protected get; set; }

	public bool ViewTransitionReached
	{
		get
		{
			if (directionTransition == null || directionTransition.Reached)
			{
				return !ForceTransition;
			}
			return false;
		}
	}

	public virtual bool ActionRunning => ViewTransitionReached;

	public float ViewTransitionStep
	{
		get
		{
			if (directionTransition != null && !ViewTransitionReached)
			{
				return directionTransition.TotalStep;
			}
			return 0f;
		}
	}

	public Quaternion Rotation { get; protected set; }

	public bool InterpolationReached { get; protected set; }

	public float FieldOfView { get; protected set; }

	public BoundingFrustum Frustum { get; protected set; }

	public Vector3 Position { get; protected set; }

	public float NearPlane { get; protected set; }

	public float FarPlane { get; protected set; }

	public float AspectRatio { get; set; }

	public bool ProjectionTransition { get; set; }

	public bool ForceInterpolation { get; set; }

	public Vector3 ViewOffset
	{
		get
		{
			return viewOffset;
		}
		set
		{
			Vector3 vector = viewOffset;
			if (ProjectionTransition)
			{
				current.Center -= viewOffset;
				viewOffset = value;
				current.Center += viewOffset;
				if (!(viewOffset == Vector3.Zero) || !(vector == Vector3.Zero))
				{
					DollyZoom();
				}
			}
			else
			{
				interpolated.Center -= viewOffset;
				viewOffset = value;
				interpolated.Center += viewOffset;
				if (!(viewOffset == Vector3.Zero) || !(vector == Vector3.Zero))
				{
					RebuildInterpolatedView();
				}
			}
		}
	}

	public float PixelsPerTrixel
	{
		get
		{
			return pixelsPerTrixel;
		}
		set
		{
			pixelsPerTrixel = value;
			DefaultViewableWidth = (float)base.Game.GraphicsDevice.Viewport.Width / (PixelsPerTrixel * 16f);
		}
	}

	public bool StickyCam { get; set; }

	public Vector2? PanningConstraints { get; set; }

	public Vector3 ConstrainedCenter { get; set; }

	public bool Constrained
	{
		get
		{
			return constrained;
		}
		set
		{
			constrained = value;
		}
	}

	[ServiceDependency]
	public IGraphicsDeviceService GraphicsService { protected get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { protected get; set; }

	[ServiceDependency]
	public IFogManager FogManager { protected get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { protected get; set; }

	public event Action ViewpointChanged = Util.NullAction;

	public event Action PreViewpointChanged = Util.NullAction;

	protected DefaultCameraManager(Game game)
		: base(game)
	{
		predefinedViews = new Dictionary<Viewpoint, PredefinedView>(ViewpointComparer.Default);
		FieldOfView = DefaultFov;
		NearPlane = 0.25f;
		FarPlane = 500f;
		Frustum = new BoundingFrustum(Matrix.Identity);
	}

	public override void Initialize()
	{
		InterpolationSpeed = 10f;
		ResetViewpoints();
		viewpoint = Viewpoint.Right;
		current = predefinedViews[Viewpoint.Right];
		SnapInterpolation();
		GraphicsService.DeviceReset += delegate
		{
			PixelsPerTrixel = pixelsPerTrixel;
			RebuildProjection();
		};
		TimeInterpolation.RegisterCallback(InterpolationCallback, 100);
	}

	private void RebuildFrustum()
	{
		Frustum.Matrix = view * projection;
	}

	public bool ChangeViewpoint(Viewpoint newView)
	{
		return ChangeViewpoint(newView, 1f);
	}

	public virtual bool ChangeViewpoint(Viewpoint newViewpoint, float speedFactor)
	{
		bool flag = newViewpoint.IsOrthographic() != viewpoint.IsOrthographic();
		if (flag && ProjectionTransition)
		{
			return false;
		}
		ProjectionTransition = flag && speedFactor > 0f;
		radiusBeforeTransition = (viewpoint.IsOrthographic() ? current.Radius : predefinedViews[lastViewpoint].Radius);
		if (speedFactor > 0f)
		{
			float num = (float)(Math.Abs(newViewpoint.GetDistance(Viewpoint)) - 1) / 2f + 1f;
			if (newViewpoint == Viewpoint.Perspective || Viewpoint == Viewpoint.Perspective)
			{
				num = 1f;
			}
			Vector3 direction = current.Direction;
			Vector3 direction2 = predefinedViews[newViewpoint].Direction;
			directionTransition = new Vector3SplineInterpolation(TimeSpan.FromSeconds(TransitionSpeed * num * speedFactor), direction, GetIntemediateVector(direction, direction2), direction2);
			directionTransition.Start();
		}
		if (viewpoint.IsOrthographic())
		{
			current.Direction = -viewpoint.ForwardVector();
			current.Radius = DefaultViewableWidth;
		}
		olderViewpoint = lastViewpoint;
		lastViewpoint = viewpoint;
		viewpoint = newViewpoint;
		Vector3 center = Center;
		current = predefinedViews[newViewpoint];
		current.Center = center;
		if (lastViewpoint != 0)
		{
			this.PreViewpointChanged();
			if (!ViewTransitionCancelled)
			{
				this.ViewpointChanged();
			}
		}
		if (speedFactor == 0f && !ViewTransitionCancelled)
		{
			RebuildView();
		}
		bool viewTransitionCancelled = ViewTransitionCancelled;
		ViewTransitionCancelled = false;
		if (speedFactor > 0f && !viewTransitionCancelled)
		{
			directionTransition.Update(dummyGt);
			current.Direction = directionTransition.Current;
		}
		return !viewTransitionCancelled;
	}

	public void AlterTransition(Vector3 newDestinationDirection)
	{
		directionTransition.Points[1] = GetIntemediateVector(directionTransition.Points[0], newDestinationDirection);
		directionTransition.Points[2] = newDestinationDirection;
	}

	public void AlterTransition(Viewpoint newTo)
	{
		Viewpoint fromView = FezMath.OrientationFromDirection(directionTransition.Points[0]).AsViewpoint();
		int distance = FezMath.OrientationFromDirection(directionTransition.Points[2]).AsViewpoint().GetDistance(newTo);
		Viewpoint rotatedView = fromView.GetRotatedView(distance);
		Vector3 direction = predefinedViews[rotatedView].Direction;
		Vector3 direction2 = predefinedViews[newTo].Direction;
		directionTransition.Points[0] = direction;
		directionTransition.Points[1] = GetIntemediateVector(direction, direction2);
		directionTransition.Points[2] = direction2;
		current = predefinedViews[newTo];
		lastViewpoint = rotatedView;
		viewpoint = newTo;
	}

	private static Vector3 GetIntemediateVector(Vector3 from, Vector3 to)
	{
		Vector3 vector = ((!FezMath.AlmostEqual(FezMath.AngleBetween(from, to), (float)Math.PI)) ? FezMath.Slerp(from, to, 0.5f) : Vector3.Cross(Vector3.Normalize(to - from), Vector3.UnitY));
		return vector + Vector3.UnitY * 0f;
	}

	public void SnapInterpolation()
	{
		interpolated.Center = current.Center;
		interpolated.Direction = current.Direction;
		interpolated.Radius = current.Radius;
		InterpolationReached = true;
		RebuildView();
		(LevelManager as LevelManager).PrepareFullCull();
		RebuildProjection();
	}

	public virtual void ResetViewpoints()
	{
		predefinedViews.Clear();
		predefinedViews.Add(Viewpoint.Perspective, new PredefinedView(Vector3.Zero, defaultViewableWidth, Vector3.One));
		predefinedViews.Add(Viewpoint.Front, new PredefinedView(Vector3.Zero, defaultViewableWidth, -Viewpoint.Front.ForwardVector()));
		predefinedViews.Add(Viewpoint.Right, new PredefinedView(Vector3.Zero, defaultViewableWidth, -Viewpoint.Right.ForwardVector()));
		predefinedViews.Add(Viewpoint.Back, new PredefinedView(Vector3.Zero, defaultViewableWidth, -Viewpoint.Back.ForwardVector()));
		predefinedViews.Add(Viewpoint.Left, new PredefinedView(Vector3.Zero, defaultViewableWidth, -Viewpoint.Left.ForwardVector()));
		if (viewpoint != 0)
		{
			current = predefinedViews[viewpoint];
		}
	}

	public void RebuildProjection()
	{
		AspectRatio = GraphicsService.GraphicsDevice.Viewport.AspectRatio;
		float num = interpolated.Radius / GraphicsService.GraphicsDevice.GetViewScale();
		if (viewpoint.IsOrthographic())
		{
			projection = Matrix.CreateOrthographic(num, num / AspectRatio, NearPlane, FarPlane);
		}
		else
		{
			projection = Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
		}
		OnProjectionChanged();
	}

	public void RebuildView()
	{
		Vector3 cameraPosition = (viewpoint.IsOrthographic() ? ((FarPlane - NearPlane) / 2f) : current.Radius) * current.Direction + current.Center;
		view = Matrix.CreateLookAt(cameraPosition, current.Center, Vector3.UnitY);
		OnViewChanged();
	}

	public override void Update(GameTime gameTime)
	{
		if (!ViewTransitionReached && !ForceTransition)
		{
			transitionNewlyReached = true;
			directionTransition.Update(gameTime);
			current.Direction = directionTransition.Current;
		}
		else if (directionTransition != null && transitionNewlyReached)
		{
			transitionNewlyReached = false;
			current.Direction = directionTransition.Current;
		}
		if (ProjectionTransition)
		{
			DollyZoom();
		}
		else
		{
			Interpolate(dummyGt);
		}
	}

	public void InterpolationCallback(GameTime gameTime)
	{
		if (!ProjectionTransition)
		{
			Interpolate(gameTime);
		}
	}

	protected virtual void DollyZoom()
	{
		bool flag = viewpoint.IsOrthographic();
		float num = ((directionTransition.TotalStep == 0f) ? 0.001f : directionTransition.TotalStep);
		float num2 = MathHelper.Lerp(flag ? DefaultFov : 0f, flag ? 0f : DefaultFov, num);
		float num3 = radiusBeforeTransition;
		if (DollyZoomOut)
		{
			num3 = radiusBeforeTransition + (1f - Easing.EaseIn(num, EasingType.Quadratic)) * 15f;
		}
		float num4 = num3 / AspectRatio / (2f * (float)Math.Tan(num2 / 2f));
		if (directionTransition.Reached)
		{
			ProjectionTransition = false;
			if (!flag)
			{
				predefinedViews[lastViewpoint].Direction = -lastViewpoint.ForwardVector();
				current.Radius = num4;
			}
			else
			{
				current.Radius = radiusBeforeTransition;
				NearPlane = 0.25f;
				FarPlane = 500f;
			}
			FogManager.Density = ((LevelManager.Sky == null) ? 0f : LevelManager.Sky.FogDensity);
			DollyZoomOut = false;
			RebuildProjection();
			SnapInterpolation();
		}
		else
		{
			FogManager.Density = ((LevelManager.Sky == null) ? 0f : LevelManager.Sky.FogDensity) * Easing.EaseIn(flag ? (1f - num) : num, EasingType.Quadratic);
			NearPlane = Math.Max(0.25f, 0.25f + num4 - num3);
			FarPlane = Math.Max(num4 + NearPlane, 499.75f);
			FieldOfView = num2;
			projection = Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
			OnProjectionChanged();
			current.Radius = num4;
			view = Matrix.CreateLookAt(current.Radius * current.Direction + current.Center, current.Center, Vector3.UnitY);
			OnViewChanged();
		}
	}

	private void Interpolate(GameTime gameTime)
	{
		float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
		float reachFactor = FezMath.GetReachFactor(0.017f * InterpolationSpeed, dt);
		reachFactor = FezMath.Saturate(reachFactor);
		if (NoInterpolation)
		{
			reachFactor = 0f;
		}
		if (!(current.Direction == Vector3.Zero))
		{
			current.Direction = Vector3.Normalize(current.Direction);
			interpolated.Center = Vector3.Lerp(interpolated.Center, current.Center, reachFactor);
			interpolated.Radius = MathHelper.Lerp(interpolated.Radius, current.Radius, reachFactor);
			interpolated.Direction = FezMath.Slerp(interpolated.Direction, current.Direction, reachFactor);
			bool flag = TransitionSpeed < 2f && FezMath.AlmostEqual(interpolated.Direction, current.Direction) && FezMath.AlmostEqual(interpolated.Center, current.Center);
			bool flag2 = FezMath.AlmostEqual(interpolated.Radius, current.Radius);
			if (ForceInterpolation)
			{
				flag = false;
				flag2 = false;
			}
			viewNewlyReached = flag && !InterpolationReached;
			if (viewNewlyReached)
			{
				interpolated.Direction = current.Direction;
				interpolated.Center = current.Center;
			}
			projNewlyReached = flag2 && !projReached;
			if (projNewlyReached)
			{
				interpolated.Radius = current.Radius;
			}
			InterpolationReached = flag && flag2;
			projReached = flag2;
			if (!flag || viewNewlyReached || reachFactor == 1f)
			{
				RebuildInterpolatedView();
			}
			if (!flag2 || projNewlyReached || reachFactor == 1f)
			{
				RebuildInterpolatedProj();
			}
			PostUpdate();
		}
	}

	protected virtual void PostUpdate()
	{
	}

	private void RebuildInterpolatedView()
	{
		Vector3 cameraPosition = (viewpoint.IsOrthographic() ? 249.875f : interpolated.Radius) * interpolated.Direction + interpolated.Center;
		view = Matrix.CreateLookAt(cameraPosition, interpolated.Center, Vector3.UnitY);
		OnViewChanged();
	}

	private void RebuildInterpolatedProj()
	{
		if (viewpoint.IsOrthographic() && (interpolated.Radius != current.Radius || projNewlyReached) && !ProjectionTransition)
		{
			float num = interpolated.Radius / GraphicsService.GraphicsDevice.GetViewScale();
			projection = Matrix.CreateOrthographic(num, num / AspectRatio, 0.25f, 500f);
			OnProjectionChanged();
		}
	}

	protected override void OnViewChanged()
	{
		InverseView = Matrix.Invert(view);
		if (!float.IsNaN(InverseView.M11) && !float.IsNaN(InverseView.M12) && !float.IsNaN(InverseView.M13) && !float.IsNaN(InverseView.M14) && !float.IsNaN(InverseView.M21) && !float.IsNaN(InverseView.M22) && !float.IsNaN(InverseView.M23) && !float.IsNaN(InverseView.M24) && !float.IsNaN(InverseView.M31) && !float.IsNaN(InverseView.M32) && !float.IsNaN(InverseView.M33) && !float.IsNaN(InverseView.M34))
		{
			InverseView.Decompose(out var _, out var rotation, out var translation);
			Position = translation;
			Rotation = rotation;
		}
		RebuildFrustum();
		base.OnViewChanged();
	}

	protected override void OnProjectionChanged()
	{
		RebuildFrustum();
		base.OnProjectionChanged();
	}
}
