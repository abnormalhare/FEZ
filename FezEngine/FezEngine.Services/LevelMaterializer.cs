using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine.Effects;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Services;

public class LevelMaterializer : GameComponent, ILevelMaterializer
{
	private const int OrthographicCullingDistance = 1;

	private const int PerspectiveCullingDistance = 50;

	private readonly List<NpcInstance> levelNPCs = new List<NpcInstance>();

	protected readonly Dictionary<Trile, TrileMaterializer> trileMaterializers;

	private TrileMaterializer fallbackMaterializer;

	private readonly Dictionary<Point, List<TrileInstance>> viewedInstances;

	private Rectangle lastCullingBounds;

	private Rectangle cullingBounds;

	private FaceOrientation xOrientation;

	private FaceOrientation zOrientation;

	private float lastHeight;

	private float lastCullHeight;

	private float lastRadius;

	private TrileUpdateAction lastUpdateAction;

	private readonly Dictionary<int, List<TrileInstance>> trileRows = new Dictionary<int, List<TrileInstance>>();

	private bool rowsCleared;

	private ArtObjectInstance[] aoCache = new ArtObjectInstance[0];

	private BackgroundPlane[] plCache = new BackgroundPlane[0];

	private NpcInstance[] npCache = new NpcInstance[0];

	private Mesh.RenderingHandler drawTrileLights;

	private Mesh.RenderingHandler trileRenderingHandler;

	private RenderPass renderPass;

	public List<ArtObjectInstance> LevelArtObjects { get; private set; }

	public List<BackgroundPlane> LevelPlanes { get; private set; }

	public Mesh TrilesMesh { get; private set; }

	public Mesh ArtObjectsMesh { get; private set; }

	public Mesh StaticPlanesMesh { get; private set; }

	public Mesh AnimatedPlanesMesh { get; private set; }

	public Mesh NpcMesh { get; private set; }

	public IEnumerable<Trile> MaterializedTriles => trileMaterializers.Keys;

	public TrileEffect TrilesEffect => TrilesMesh.Effect as TrileEffect;

	public InstancedArtObjectEffect ArtObjectsEffect => ArtObjectsMesh.Effect as InstancedArtObjectEffect;

	public InstancedStaticPlaneEffect StaticPlanesEffect => StaticPlanesMesh.Effect as InstancedStaticPlaneEffect;

	public InstancedAnimatedPlaneEffect AnimatedPlanesEffect => AnimatedPlanesMesh.Effect as InstancedAnimatedPlaneEffect;

	public AnimatedPlaneEffect NpcEffect => NpcMesh.Effect as AnimatedPlaneEffect;

	public virtual RenderPass RenderPass
	{
		get
		{
			return renderPass;
		}
		set
		{
			renderPass = value;
			switch (renderPass)
			{
			case RenderPass.Ghosts:
			{
				foreach (NpcInstance levelNPC in levelNPCs)
				{
					if (levelNPC.Group != null)
					{
						levelNPC.Group.Enabled = levelNPC.ActorType == ActorType.LightningGhost && levelNPC.Enabled && levelNPC.Visible;
					}
				}
				break;
			}
			case RenderPass.LightInAlphaEmitters:
			{
				AnimatedPlaneEffect npcEffect2 = NpcEffect;
				InstancedStaticPlaneEffect staticPlanesEffect = StaticPlanesEffect;
				InstancedAnimatedPlaneEffect animatedPlanesEffect4 = AnimatedPlanesEffect;
				TrileEffect trilesEffect = TrilesEffect;
				LightingEffectPass lightingEffectPass6 = (ArtObjectsEffect.Pass = LightingEffectPass.Pre);
				LightingEffectPass lightingEffectPass8 = (trilesEffect.Pass = lightingEffectPass6);
				LightingEffectPass lightingEffectPass2 = (animatedPlanesEffect4.Pass = lightingEffectPass8);
				LightingEffectPass pass = (staticPlanesEffect.Pass = lightingEffectPass2);
				npcEffect2.Pass = pass;
				if (LevelManager.TrileSet != null)
				{
					trileRenderingHandler = TrilesMesh.CustomRenderingHandler;
					if (drawTrileLights == null)
					{
						drawTrileLights = DrawTrileLights;
					}
					TrilesMesh.CustomRenderingHandler = drawTrileLights;
				}
				foreach (Group group in StaticPlanesMesh.Groups)
				{
					group.Enabled = false;
				}
				foreach (Group group2 in AnimatedPlanesMesh.Groups)
				{
					group2.Enabled = false;
				}
				{
					foreach (BackgroundPlane levelPlane in LevelPlanes)
					{
						levelPlane.Group.Enabled |= !levelPlane.LightMap && levelPlane.Visible;
					}
					break;
				}
			}
			case RenderPass.WorldspaceLightmaps:
			{
				InstancedAnimatedPlaneEffect animatedPlanesEffect2 = AnimatedPlanesEffect;
				LightingEffectPass pass = (StaticPlanesEffect.Pass = LightingEffectPass.Main);
				animatedPlanesEffect2.Pass = pass;
				InstancedAnimatedPlaneEffect animatedPlanesEffect3 = AnimatedPlanesEffect;
				bool ignoreFog = (StaticPlanesEffect.IgnoreFog = true);
				animatedPlanesEffect3.IgnoreFog = ignoreFog;
				Mesh animatedPlanesMesh4 = AnimatedPlanesMesh;
				ignoreFog = (StaticPlanesMesh.DepthWrites = false);
				animatedPlanesMesh4.DepthWrites = ignoreFog;
				foreach (Group group3 in StaticPlanesMesh.Groups)
				{
					group3.Enabled = false;
				}
				foreach (Group group4 in AnimatedPlanesMesh.Groups)
				{
					group4.Enabled = false;
				}
				{
					foreach (BackgroundPlane levelPlane2 in LevelPlanes)
					{
						levelPlane2.Group.Enabled |= levelPlane2.LightMap && !levelPlane2.AlwaysOnTop && levelPlane2.Visible;
					}
					break;
				}
			}
			case RenderPass.ScreenspaceLightmaps:
			{
				Mesh animatedPlanesMesh3 = AnimatedPlanesMesh;
				bool ignoreFog = (StaticPlanesMesh.AlwaysOnTop = true);
				animatedPlanesMesh3.AlwaysOnTop = ignoreFog;
				foreach (Group group5 in StaticPlanesMesh.Groups)
				{
					group5.Enabled = false;
				}
				foreach (Group group6 in AnimatedPlanesMesh.Groups)
				{
					group6.Enabled = false;
				}
				{
					foreach (BackgroundPlane levelPlane3 in LevelPlanes)
					{
						levelPlane3.Group.Enabled |= levelPlane3.LightMap && levelPlane3.AlwaysOnTop && levelPlane3.Visible;
					}
					break;
				}
			}
			case RenderPass.Normal:
			{
				AnimatedPlaneEffect npcEffect = NpcEffect;
				InstancedArtObjectEffect artObjectsEffect = ArtObjectsEffect;
				LightingEffectPass lightingEffectPass2 = (TrilesEffect.Pass = LightingEffectPass.Main);
				LightingEffectPass pass = (artObjectsEffect.Pass = lightingEffectPass2);
				npcEffect.Pass = pass;
				if (trileRenderingHandler != null)
				{
					TrilesMesh.CustomRenderingHandler = trileRenderingHandler;
					trileRenderingHandler = null;
				}
				InstancedAnimatedPlaneEffect animatedPlanesEffect = AnimatedPlanesEffect;
				bool ignoreFog = (StaticPlanesEffect.IgnoreFog = false);
				animatedPlanesEffect.IgnoreFog = ignoreFog;
				Mesh animatedPlanesMesh = AnimatedPlanesMesh;
				ignoreFog = (StaticPlanesMesh.DepthWrites = true);
				animatedPlanesMesh.DepthWrites = ignoreFog;
				Mesh animatedPlanesMesh2 = AnimatedPlanesMesh;
				ignoreFog = (StaticPlanesMesh.AlwaysOnTop = false);
				animatedPlanesMesh2.AlwaysOnTop = ignoreFog;
				foreach (Group group7 in StaticPlanesMesh.Groups)
				{
					group7.Enabled = false;
				}
				foreach (Group group8 in AnimatedPlanesMesh.Groups)
				{
					group8.Enabled = false;
				}
				foreach (BackgroundPlane levelPlane4 in LevelPlanes)
				{
					levelPlane4.Group.Enabled |= !levelPlane4.LightMap && levelPlane4.Visible;
				}
				{
					foreach (NpcInstance levelNPC2 in levelNPCs)
					{
						if (levelNPC2.Group != null)
						{
							levelNPC2.Group.Enabled = (levelNPC2.ActorType != ActorType.LightningGhost || levelNPC2.Talking) && levelNPC2.Enabled && levelNPC2.Visible;
						}
					}
					break;
				}
			}
			case RenderPass.Occluders:
				break;
			}
		}
	}

	[ServiceDependency]
	public IGraphicsDeviceService GraphicsDeviceService { protected get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { protected get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { protected get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { protected get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { protected get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	public event Action<TrileInstance> TrileInstanceBatched;

	protected LevelMaterializer(Game game)
		: base(game)
	{
		LevelArtObjects = new List<ArtObjectInstance>();
		LevelPlanes = new List<BackgroundPlane>();
		trileMaterializers = new Dictionary<Trile, TrileMaterializer>();
		viewedInstances = new Dictionary<Point, List<TrileInstance>>();
		TrilesMesh = new Mesh
		{
			SamplerState = SamplerState.PointClamp,
			SkipGroupCheck = true
		};
		ArtObjectsMesh = new Mesh
		{
			SamplerState = SamplerState.PointClamp,
			Blending = BlendingMode.Alphablending,
			SkipGroupCheck = true
		};
		StaticPlanesMesh = new Mesh
		{
			SamplerState = SamplerState.PointClamp,
			GroupOrder = delegate(Group a, Group b)
			{
				if (a.Blending == BlendingMode.Additive)
				{
					if (b.Blending == BlendingMode.Additive)
					{
						return 0;
					}
					return -1;
				}
				return (b.Blending == BlendingMode.Additive) ? 1 : 0;
			},
			SkipGroupCheck = true
		};
		AnimatedPlanesMesh = new Mesh
		{
			SamplerState = SamplerState.PointClamp,
			GroupOrder = delegate(Group a, Group b)
			{
				if (a.Blending == BlendingMode.Additive)
				{
					if (b.Blending == BlendingMode.Additive)
					{
						return 0;
					}
					return -1;
				}
				return (b.Blending == BlendingMode.Additive) ? 1 : 0;
			},
			SkipGroupCheck = true
		};
		NpcMesh = new Mesh
		{
			RotateOffCenter = true,
			SamplerState = SamplerState.PointClamp,
			Blending = BlendingMode.Alphablending
		};
	}

	public override void Initialize()
	{
		TrilesMesh.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/FullWhite");
		TrilesMesh.CustomRenderingHandler = DrawTriles;
		ArtObjectsMesh.CustomRenderingHandler = DrawArtObjects;
		CameraManager.ViewChanged += CullEverything;
		CameraManager.ProjectionChanged += CullEverything;
		fallbackMaterializer = new TrileMaterializer(LevelManager.SafeGetTrile(-1), TrilesMesh, mutableSurfaces: true);
		fallbackMaterializer.Rebuild();
		fallbackMaterializer.Group.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/TransparentWhite");
		LevelManager.LevelChanging += delegate
		{
			aoCache = new ArtObjectInstance[0];
			plCache = new BackgroundPlane[0];
			npCache = new NpcInstance[0];
			RegisterSatellites();
			UnRowify(soft: false);
		};
	}

	public void Rowify()
	{
		rowsCleared = false;
		foreach (TrileInstance value2 in LevelManager.Triles.Values)
		{
			value2.InstanceId = -1;
			ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = value2.VisualTrile.Geometry;
			if (geometry == null || !geometry.Empty || value2.Overlaps)
			{
				int y = value2.Emplacement.Y;
				if (!trileRows.TryGetValue(y, out var value))
				{
					trileRows.Add(y, value = new List<TrileInstance>());
				}
				value.Add(value2);
			}
		}
	}

	public void UpdateRow(TrileEmplacement oldEmplacement, TrileInstance instance)
	{
		if (!rowsCleared && lastUpdateAction != TrileUpdateAction.SingleFaceCullFull && lastUpdateAction != TrileUpdateAction.SingleFaceCullPartial)
		{
			if (trileRows.TryGetValue(oldEmplacement.Y, out var value))
			{
				value.Remove(instance);
			}
			int y = instance.Emplacement.Y;
			if (!trileRows.TryGetValue(y, out value))
			{
				trileRows.Add(y, value = new List<TrileInstance>());
			}
			value.Add(instance);
		}
	}

	public void UnRowify()
	{
		UnRowify(soft: false);
	}

	private void UnRowify(bool soft)
	{
		if (soft)
		{
			foreach (KeyValuePair<int, List<TrileInstance>> trileRow in trileRows)
			{
				trileRow.Value.Clear();
			}
		}
		else
		{
			trileRows.Clear();
		}
		rowsCleared = true;
	}

	public void CleanFallbackTriles()
	{
		TrileInstance[] array = fallbackMaterializer.Trile.Instances.ToArray();
		foreach (TrileInstance instance in array)
		{
			LevelManager.ClearTrile(instance, skipRecull: true);
		}
	}

	public void ForceCull()
	{
		CullInstances();
		CullArtObjects();
		CullPlanes();
	}

	public void CullEverything()
	{
		if (!EngineState.SkyRender && !EngineState.Loading && !EngineState.SkipRendering)
		{
			CullInstances(viewChanged: false);
			CullArtObjects();
			CullPlanes();
		}
	}

	private void CullPlanes()
	{
		bool flag = !CameraManager.Viewpoint.IsOrthographic();
		Vector3 forward = CameraManager.InverseView.Forward;
		BoundingFrustum frustum = CameraManager.Frustum;
		if (LevelManager.Loops)
		{
			foreach (BackgroundPlane levelPlane in LevelPlanes)
			{
				levelPlane.UpdateBounds();
				levelPlane.Visible = !levelPlane.Hidden;
				if (levelPlane.Visible)
				{
					levelPlane.Group.Enabled = true;
				}
			}
			return;
		}
		foreach (BackgroundPlane levelPlane2 in LevelPlanes)
		{
			if (!levelPlane2.Disposed)
			{
				levelPlane2.UpdateBounds();
				levelPlane2.Visible = !levelPlane2.Hidden && (flag || levelPlane2.Doublesided || levelPlane2.Crosshatch || levelPlane2.Billboard || forward.Dot(levelPlane2.Forward) > 0f) && frustum.Contains(levelPlane2.Bounds) != ContainmentType.Disjoint;
				if (levelPlane2.Visible && levelPlane2.Group != null)
				{
					levelPlane2.Group.Enabled = true;
				}
			}
		}
	}

	private void CullArtObjects()
	{
		foreach (Group group in ArtObjectsMesh.Groups)
		{
			group.Enabled = false;
		}
		BoundingFrustum frustum = CameraManager.Frustum;
		foreach (ArtObjectInstance levelArtObject in LevelArtObjects)
		{
			levelArtObject.RebuildBounds();
			ContainmentType result = ContainmentType.Disjoint;
			if (!levelArtObject.Hidden)
			{
				frustum.Contains(ref levelArtObject.Bounds, out result);
			}
			levelArtObject.Visible = !levelArtObject.Hidden && result != ContainmentType.Disjoint;
			if (levelArtObject.Visible)
			{
				levelArtObject.ArtObject.Group.Enabled = true;
			}
		}
	}

	public void PrepareFullCull()
	{
		lastUpdateAction = TrileUpdateAction.None;
	}

	public void CullInstances()
	{
		lastUpdateAction = TrileUpdateAction.None;
		CullInstances(viewChanged: false);
	}

	private void CullInstances(bool viewChanged)
	{
		if ((CameraManager.ProjectionTransition && (CameraManager.Viewpoint.IsOrthographic() || lastUpdateAction == TrileUpdateAction.NoCull)) || EngineState.SkipRendering)
		{
			return;
		}
		if (EngineState.LoopRender)
		{
			lastUpdateAction = TrileUpdateAction.None;
		}
		TrileUpdateAction trileUpdateAction = DetermineCullType();
		bool flag = false;
		float num = 1f;
		float num2 = 1f;
		num = GraphicsDeviceService.GraphicsDevice.GetViewScale();
		num2 = GraphicsDeviceService.GraphicsDevice.Viewport.Width;
		switch (trileUpdateAction)
		{
		case TrileUpdateAction.SingleFaceCullPartial:
		case TrileUpdateAction.SingleFaceCullFull:
		{
			Vector3 value2 = CameraManager.Viewpoint.SideMask();
			BoundingFrustum frustum2 = CameraManager.Frustum;
			BoundingBox boundingBox = default(BoundingBox);
			boundingBox.Min.X = (0f - frustum2.Left.D) * frustum2.Left.DotNormal(value2);
			boundingBox.Min.Y = (0f - frustum2.Bottom.D) * frustum2.Bottom.Normal.Y;
			boundingBox.Max.X = (0f - frustum2.Right.D) * frustum2.Right.DotNormal(value2);
			boundingBox.Max.Y = (0f - frustum2.Top.D) * frustum2.Top.Normal.Y;
			BoundingBox boundingBox3 = boundingBox;
			Vector3 vector5 = FezMath.Min(boundingBox3.Min, boundingBox3.Max);
			Vector3 vector6 = FezMath.Max(boundingBox3.Min, boundingBox3.Max);
			cullingBounds = new Rectangle
			{
				X = (int)Math.Floor(vector5.X) - 1,
				Y = (int)Math.Floor(vector5.Y) - 1,
				Width = (int)Math.Ceiling(vector6.X - vector5.X) + 3,
				Height = (int)Math.Ceiling(vector6.Y - vector5.Y) + 3
			};
			if (cullingBounds.Width < 0 || cullingBounds.Height < 0 || CameraManager.Radius > 120f * num2 * num)
			{
				cullingBounds = lastCullingBounds;
			}
			flag |= Math.Abs(lastCullingBounds.X - cullingBounds.X) > 0 || Math.Abs(lastCullingBounds.Y - cullingBounds.Y) > 0;
			break;
		}
		case TrileUpdateAction.TwoFaceCullPartial:
		case TrileUpdateAction.TwoFaceCullFull:
		{
			Vector3 value = CameraManager.Viewpoint.SideMask();
			BoundingFrustum frustum = CameraManager.Frustum;
			Vector3 vector = CameraManager.View.Forward.Sign();
			FaceOrientation faceOrientation = FezMath.OrientationFromDirection(vector.X * Vector3.UnitX);
			FaceOrientation faceOrientation2 = FezMath.OrientationFromDirection((0f - vector.Z) * Vector3.UnitZ);
			flag |= faceOrientation != xOrientation || faceOrientation2 != zOrientation;
			xOrientation = faceOrientation;
			zOrientation = faceOrientation2;
			BoundingBox boundingBox = default(BoundingBox);
			boundingBox.Min.X = (0f - frustum.Left.D) * frustum.Left.DotNormal(value);
			boundingBox.Min.Y = (0f - frustum.Bottom.D) * frustum.Bottom.Normal.Y;
			boundingBox.Max.X = (0f - frustum.Right.D) * frustum.Right.DotNormal(value);
			boundingBox.Max.Y = (0f - frustum.Top.D) * frustum.Top.Normal.Y;
			BoundingBox boundingBox2 = boundingBox;
			Vector3 vector2 = FezMath.Min(boundingBox2.Min, boundingBox2.Max);
			Vector3 vector3 = FezMath.Max(boundingBox2.Min, boundingBox2.Max);
			cullingBounds = new Rectangle
			{
				X = (int)Math.Floor(vector2.X) - 1,
				Y = (int)Math.Floor(vector2.Y) - 1,
				Width = (int)Math.Ceiling(vector3.X - vector2.X) + 3,
				Height = (int)Math.Ceiling(vector3.Y - vector2.Y) + 3
			};
			if (cullingBounds.Width < 0 || cullingBounds.Height < 0 || CameraManager.Radius > 120f * num2 * num)
			{
				cullingBounds = lastCullingBounds;
			}
			flag |= Math.Abs(lastCullingBounds.Y - cullingBounds.Y) > 0;
			break;
		}
		case TrileUpdateAction.TriFaceCull:
		{
			Vector3 vector4 = CameraManager.View.Forward.Sign();
			FaceOrientation faceOrientation3 = FezMath.OrientationFromDirection(vector4.X * Vector3.UnitX);
			FaceOrientation faceOrientation4 = FezMath.OrientationFromDirection((0f - vector4.Z) * Vector3.UnitZ);
			flag |= faceOrientation3 != xOrientation || faceOrientation4 != zOrientation;
			xOrientation = faceOrientation3;
			zOrientation = faceOrientation4;
			if (Math.Abs(CameraManager.InterpolatedCenter.Y - lastCullHeight) >= 1f || lastRadius != CameraManager.Radius)
			{
				flag = true;
			}
			break;
		}
		case TrileUpdateAction.NoCull:
			if (Math.Abs(CameraManager.InterpolatedCenter.Y - lastCullHeight) >= 5f)
			{
				flag = true;
			}
			break;
		}
		lastHeight = CameraManager.InterpolatedCenter.Y;
		lastRadius = CameraManager.Radius;
		flag = flag || viewChanged;
		if (flag | (lastUpdateAction != trileUpdateAction && (lastUpdateAction != TrileUpdateAction.SingleFaceCullFull || trileUpdateAction != TrileUpdateAction.SingleFaceCullPartial) && (lastUpdateAction != TrileUpdateAction.TwoFaceCullFull || trileUpdateAction != TrileUpdateAction.TwoFaceCullPartial)))
		{
			UpdateInstances(trileUpdateAction);
			lastCullingBounds = cullingBounds;
			lastCullHeight = CameraManager.InterpolatedCenter.Y;
		}
		lastUpdateAction = trileUpdateAction;
	}

	private TrileUpdateAction DetermineCullType()
	{
		TrileUpdateAction trileUpdateAction = TrileUpdateAction.None;
		if (CameraManager.Viewpoint == Viewpoint.Perspective)
		{
			return TrileUpdateAction.NoCull;
		}
		Vector3 a = CameraManager.View.Forward.Round(5);
		if (CameraManager.Viewpoint == Viewpoint.Left || CameraManager.Viewpoint == Viewpoint.Right)
		{
			a *= -1f;
		}
		float num = a.Dot(CameraManager.Viewpoint.ForwardVector());
		if (num == 1f)
		{
			if (lastUpdateAction == TrileUpdateAction.SingleFaceCullFull || lastUpdateAction == TrileUpdateAction.SingleFaceCullPartial)
			{
				return TrileUpdateAction.SingleFaceCullPartial;
			}
			return TrileUpdateAction.SingleFaceCullFull;
		}
		if (a.Y == 0f && num != 0f)
		{
			Vector3 vector = CameraManager.View.Forward.Sign();
			FaceOrientation faceOrientation = FezMath.OrientationFromDirection(vector.X * Vector3.UnitX);
			FaceOrientation faceOrientation2 = FezMath.OrientationFromDirection((0f - vector.Z) * Vector3.UnitZ);
			if (faceOrientation != xOrientation || faceOrientation2 != zOrientation)
			{
				return TrileUpdateAction.TwoFaceCullFull;
			}
			if (lastUpdateAction == TrileUpdateAction.TwoFaceCullFull || lastUpdateAction == TrileUpdateAction.TwoFaceCullPartial)
			{
				return TrileUpdateAction.TwoFaceCullPartial;
			}
			return TrileUpdateAction.TwoFaceCullFull;
		}
		if (Math.Abs(a.Dot(Vector3.One)) != 1f && num != 0f)
		{
			return TrileUpdateAction.TriFaceCull;
		}
		return TrileUpdateAction.TwoFaceCullFull;
	}

	public void UpdateInstance(TrileInstance instance)
	{
		if (lastUpdateAction != TrileUpdateAction.SingleFaceCullFull && lastUpdateAction != TrileUpdateAction.SingleFaceCullPartial)
		{
			return;
		}
		if (instance.SkipCulling)
		{
			bool flag = CameraManager.Viewpoint.ForwardVector().Z == 1f;
			Point value = new Point(flag ? instance.Emplacement.X : instance.Emplacement.Z, instance.Emplacement.Y);
			if (cullingBounds.Contains(value))
			{
				RegisterViewedInstance(instance);
			}
			else
			{
				CullInstanceOut(instance, skipUnregister: true);
			}
			return;
		}
		if (UnregisterViewedInstance(instance))
		{
			CullInstanceOut(instance, skipUnregister: true);
		}
		if (RegisterViewedInstance(instance))
		{
			SafeAddToBatch(instance, autoCommit: true);
		}
	}

	public void CullInstanceIn(TrileInstance instance)
	{
		CullInstanceIn(instance, forceAdd: false);
	}

	public void CullInstanceInNoRegister(TrileInstance instance)
	{
		SafeAddToBatch(instance, autoCommit: false);
	}

	public void CullInstanceIn(TrileInstance instance, bool forceAdd)
	{
		if ((lastUpdateAction != TrileUpdateAction.SingleFaceCullFull && lastUpdateAction != TrileUpdateAction.SingleFaceCullPartial) || RegisterViewedInstance(instance) || forceAdd)
		{
			SafeAddToBatch(instance, autoCommit: true);
		}
		if (forceAdd && lastUpdateAction != TrileUpdateAction.SingleFaceCullFull && lastUpdateAction != TrileUpdateAction.SingleFaceCullPartial && !rowsCleared)
		{
			int y = instance.Emplacement.Y;
			if (!trileRows.TryGetValue(y, out var value))
			{
				trileRows.Add(y, value = new List<TrileInstance>());
			}
			value.Add(instance);
		}
	}

	public bool CullInstanceOut(TrileInstance toRemove)
	{
		return CullInstanceOut(toRemove, skipUnregister: false);
	}

	public bool CullInstanceOut(TrileInstance toRemove, bool skipUnregister)
	{
		if (!rowsCleared)
		{
			foreach (List<TrileInstance> value in trileRows.Values)
			{
				value.Remove(toRemove);
			}
		}
		if (!skipUnregister && !UnregisterViewedInstance(toRemove) && lastUpdateAction != TrileUpdateAction.TriFaceCull && lastUpdateAction != TrileUpdateAction.TwoFaceCullFull && lastUpdateAction != TrileUpdateAction.TwoFaceCullPartial && lastUpdateAction != 0 && viewedInstances.Count > 0)
		{
			return false;
		}
		if (toRemove.InstanceId == -1)
		{
			return false;
		}
		TrileMaterializer trileMaterializer = GetTrileMaterializer(toRemove.VisualTrile);
		if (trileMaterializer == null)
		{
			return false;
		}
		trileMaterializer.RemoveFromBatch(toRemove);
		return true;
	}

	public TrileMaterializer GetTrileMaterializer(Trile trile)
	{
		if (trile.Id < 0)
		{
			return fallbackMaterializer;
		}
		if (!trileMaterializers.TryGetValue(trile, out var value))
		{
			return null;
		}
		return value;
	}

	public virtual void InitializeArtObjects()
	{
		foreach (ArtObject item in LevelManager.ArtObjects.Values.Select((ArtObjectInstance x) => x.ArtObject).Distinct())
		{
			ArtObjectMaterializer artObjectMaterializer = new ArtObjectMaterializer(item);
			if (item.MissingTrixels != null)
			{
				artObjectMaterializer.MarkMissingCells();
				artObjectMaterializer.UpdateSurfaces();
			}
			if (item.Geometry == null)
			{
				artObjectMaterializer.RebuildGeometry();
			}
			else
			{
				artObjectMaterializer.PostInitialize();
			}
		}
		VertexGroup<FezVertexPositionNormalTexture>.Deallocate();
		foreach (ArtObjectInstance value in LevelManager.ArtObjects.Values)
		{
			value.Initialize();
		}
	}

	public void RebuildTriles(bool quick)
	{
		RebuildTriles(LevelManager.TrileSet, quick);
	}

	public virtual void RebuildTriles(TrileSet trileSet, bool quick)
	{
		if (!quick)
		{
			DestroyMaterializers(trileSet);
		}
		if (trileSet != null)
		{
			foreach (Trile item in trileSet.Triles.Values.Where((Trile x) => !trileMaterializers.ContainsKey(x)))
			{
				RebuildTrile(item);
			}
			DrawActionScheduler.Schedule(delegate
			{
				TrilesMesh.Texture = trileSet.TextureAtlas;
			});
		}
		VertexGroup<VertexPositionNormalTextureInstance>.Deallocate();
	}

	public virtual void RebuildTrile(Trile trile)
	{
		TrileMaterializer trileMaterializer = new TrileMaterializer(trile, TrilesMesh);
		trileMaterializers.Add(trile, trileMaterializer);
		trileMaterializer.Rebuild();
	}

	public void DestroyMaterializers(TrileSet trileSet)
	{
		if (trileSet != null)
		{
			TrileMaterializer[] array = trileMaterializers.Values.Where((TrileMaterializer x) => trileSet.Triles.ContainsValue(x.Trile)).ToArray();
			foreach (TrileMaterializer trileMaterializer in array)
			{
				trileMaterializer.Dispose();
				trileMaterializers.Remove(trileMaterializer.Trile);
			}
		}
		else
		{
			trileMaterializers.Clear();
		}
	}

	public void ClearBatches()
	{
		foreach (TrileMaterializer value in trileMaterializers.Values)
		{
			value.ClearBatch();
		}
	}

	public void RebuildInstances()
	{
		fallbackMaterializer.Trile.Instances.Clear();
		foreach (Trile key in trileMaterializers.Keys)
		{
			key.Instances.Clear();
		}
		UnregisterAllViewedInstances();
		foreach (TrileInstance value in LevelManager.Triles.Values)
		{
			value.ResetTrile();
			AddInstance(value);
			if (!value.Overlaps)
			{
				continue;
			}
			foreach (TrileInstance overlappedTrile in value.OverlappedTriles)
			{
				overlappedTrile.ResetTrile();
				AddInstance(overlappedTrile);
			}
		}
	}

	public void CleanUp()
	{
		TrileMaterializer[] array = trileMaterializers.Values.ToArray();
		foreach (TrileMaterializer trileMaterializer in array)
		{
			Trile trile = trileMaterializer.Trile;
			ActorType type = trileMaterializer.Trile.ActorSettings.Type;
			if (!trile.ForceKeep && trile.Instances.Count == 0 && !type.IsTreasure() && !type.IsCollectible())
			{
				trileMaterializer.Dispose();
				trileMaterializers.Remove(trileMaterializer.Trile);
			}
		}
	}

	public void AddInstance(TrileInstance instance)
	{
		if (instance.TrileId == -1 || LevelManager.TrileSet == null || !LevelManager.TrileSet.Triles.ContainsKey(instance.TrileId))
		{
			instance.TrileId = -1;
			instance.RefreshTrile();
			fallbackMaterializer.Trile.Instances.Add(instance);
		}
		else
		{
			instance.VisualTrile.Instances.Add(instance);
		}
	}

	public void RemoveInstance(TrileInstance instance)
	{
		if (instance.TrileId == -1 || LevelManager.TrileSet == null || !LevelManager.TrileSet.Triles.ContainsKey(instance.TrileId))
		{
			fallbackMaterializer.Trile.Instances.Remove(instance);
		}
		else
		{
			instance.VisualTrile.Instances.Remove(instance);
		}
	}

	private void UnregisterAllViewedInstances()
	{
		foreach (List<TrileInstance> value in viewedInstances.Values)
		{
			foreach (TrileInstance item in value)
			{
				item.InstanceId = -1;
			}
		}
		viewedInstances.Clear();
	}

	private void UpdateInstances(TrileUpdateAction action)
	{
		switch (action)
		{
		case TrileUpdateAction.None:
			return;
		case TrileUpdateAction.SingleFaceCullFull:
		{
			foreach (TrileMaterializer value6 in trileMaterializers.Values)
			{
				value6.ResetBatch();
			}
			fallbackMaterializer.ResetBatch();
			foreach (TrileInstance value7 in LevelManager.Triles.Values)
			{
				if (value7.SkipCulling)
				{
					SafeAddToBatch(value7, autoCommit: false);
				}
				else
				{
					value7.InstanceId = -1;
				}
			}
			viewedInstances.Clear();
			if (!rowsCleared)
			{
				UnRowify(soft: true);
			}
			LevelManager.WaitForScreenInvalidation();
			for (int num18 = cullingBounds.Left; num18 < cullingBounds.Right; num18++)
			{
				for (int num19 = cullingBounds.Top; num19 < cullingBounds.Bottom; num19++)
				{
					FillScreenSpace(num18, num19);
				}
			}
			break;
		}
		case TrileUpdateAction.SingleFaceCullPartial:
		{
			LevelManager.WaitForScreenInvalidation();
			for (int i = cullingBounds.Right; i < lastCullingBounds.Right; i++)
			{
				for (int j = lastCullingBounds.Top; j < lastCullingBounds.Bottom; j++)
				{
					FreeScreenSpace(i, j);
				}
			}
			for (int k = lastCullingBounds.Right; k < cullingBounds.Right; k++)
			{
				for (int l = cullingBounds.Top; l < cullingBounds.Bottom; l++)
				{
					FillScreenSpace(k, l);
				}
			}
			for (int m = lastCullingBounds.Left; m < cullingBounds.Left; m++)
			{
				for (int n = lastCullingBounds.Top; n < lastCullingBounds.Bottom; n++)
				{
					FreeScreenSpace(m, n);
				}
			}
			for (int num = cullingBounds.Left; num < lastCullingBounds.Left; num++)
			{
				for (int num2 = cullingBounds.Top; num2 < cullingBounds.Bottom; num2++)
				{
					FillScreenSpace(num, num2);
				}
			}
			for (int num3 = lastCullingBounds.Top; num3 < cullingBounds.Top; num3++)
			{
				for (int num4 = lastCullingBounds.Left; num4 < lastCullingBounds.Right; num4++)
				{
					FreeScreenSpace(num4, num3);
				}
			}
			for (int num5 = cullingBounds.Top; num5 < lastCullingBounds.Top; num5++)
			{
				for (int num6 = cullingBounds.Left; num6 < cullingBounds.Right; num6++)
				{
					FillScreenSpace(num6, num5);
				}
			}
			for (int num7 = cullingBounds.Bottom; num7 < lastCullingBounds.Bottom; num7++)
			{
				for (int num8 = lastCullingBounds.Left; num8 < lastCullingBounds.Right; num8++)
				{
					FreeScreenSpace(num8, num7);
				}
			}
			for (int num9 = lastCullingBounds.Bottom; num9 < cullingBounds.Bottom; num9++)
			{
				for (int num10 = cullingBounds.Left; num10 < cullingBounds.Right; num10++)
				{
					FillScreenSpace(num10, num9);
				}
			}
			break;
		}
		case TrileUpdateAction.TwoFaceCullFull:
		{
			foreach (TrileMaterializer value8 in trileMaterializers.Values)
			{
				value8.ResetBatch();
			}
			fallbackMaterializer.ResetBatch();
			if (rowsCleared)
			{
				Rowify();
			}
			viewedInstances.Clear();
			for (int num17 = cullingBounds.Top; num17 <= cullingBounds.Bottom; num17++)
			{
				if (!trileRows.TryGetValue(num17, out var value5))
				{
					continue;
				}
				foreach (TrileInstance item in value5)
				{
					TrileEmplacement id5 = item.Emplacement;
					if (item.Enabled && !item.Hidden && (item.ForceSeeThrough || LevelManager.IsCornerTrile(ref id5, ref xOrientation, ref zOrientation)) && item.Trile.Id >= 0)
					{
						SafeAddToBatchWithOverlaps(item, autoCommit: false);
					}
				}
			}
			break;
		}
		case TrileUpdateAction.TwoFaceCullPartial:
		{
			int top = lastCullingBounds.Top;
			int top2 = cullingBounds.Top;
			int bottom = cullingBounds.Bottom;
			int bottom2 = lastCullingBounds.Bottom;
			for (int num13 = top; num13 < top2; num13++)
			{
				if (!trileRows.TryGetValue(num13, out var value))
				{
					continue;
				}
				foreach (TrileInstance item2 in value)
				{
					if (item2.InstanceId != -1)
					{
						SafeRemoveFromBatchWithOverlaps(item2);
					}
				}
			}
			for (int num14 = top2; num14 < top; num14++)
			{
				if (!trileRows.TryGetValue(num14, out var value2))
				{
					continue;
				}
				foreach (TrileInstance item3 in value2)
				{
					TrileEmplacement id3 = item3.Emplacement;
					if (item3.Enabled && !item3.Hidden && (item3.ForceSeeThrough || LevelManager.IsCornerTrile(ref id3, ref xOrientation, ref zOrientation)) && item3.Trile.Id >= 0)
					{
						SafeAddToBatchWithOverlaps(item3, autoCommit: false);
					}
				}
			}
			for (int num15 = bottom; num15 < bottom2; num15++)
			{
				if (!trileRows.TryGetValue(num15, out var value3))
				{
					continue;
				}
				foreach (TrileInstance item4 in value3)
				{
					if (item4.InstanceId != -1)
					{
						SafeRemoveFromBatchWithOverlaps(item4);
					}
				}
			}
			for (int num16 = bottom2; num16 < bottom; num16++)
			{
				if (!trileRows.TryGetValue(num16, out var value4))
				{
					continue;
				}
				foreach (TrileInstance item5 in value4)
				{
					TrileEmplacement id4 = item5.Emplacement;
					if (item5.Enabled && !item5.Hidden && (item5.ForceSeeThrough || LevelManager.IsCornerTrile(ref id4, ref xOrientation, ref zOrientation)) && item5.Trile.Id >= 0)
					{
						SafeAddToBatchWithOverlaps(item5, autoCommit: false);
					}
				}
			}
			break;
		}
		case TrileUpdateAction.TriFaceCull:
		{
			foreach (TrileMaterializer value9 in trileMaterializers.Values)
			{
				value9.ResetBatch();
			}
			fallbackMaterializer.ResetBatch();
			UnregisterAllViewedInstances();
			float num11 = CameraManager.Radius / CameraManager.AspectRatio / 2f + 1f;
			int num12 = (EngineState.InEditor ? 8 : 0);
			foreach (TrileInstance value10 in LevelManager.Triles.Values)
			{
				TrileEmplacement id2 = value10.Emplacement;
				FaceOrientation face = FaceOrientation.Top;
				Vector3 position = value10.Position;
				if (value10.Enabled && !value10.Hidden && (value10.VisualTrile.Geometry == null || !value10.VisualTrile.Geometry.Empty || value10.Overlaps) && position.Y > lastHeight - num11 - 1f - (float)num12 && position.Y < lastHeight + num11 + (float)num12 && (LevelManager.IsBorderTrileFace(ref id2, ref xOrientation) || LevelManager.IsBorderTrileFace(ref id2, ref zOrientation) || LevelManager.IsBorderTrileFace(ref id2, ref face)))
				{
					SafeAddToBatchWithOverlaps(value10, autoCommit: false);
				}
			}
			break;
		}
		case TrileUpdateAction.NoCull:
			foreach (TrileMaterializer value11 in trileMaterializers.Values)
			{
				value11.ResetBatch();
			}
			fallbackMaterializer.ResetBatch();
			UnregisterAllViewedInstances();
			foreach (TrileInstance value12 in LevelManager.Triles.Values)
			{
				TrileEmplacement id = value12.Emplacement;
				if (value12.Enabled && !value12.Hidden && value12.Position.Y > lastHeight - 50f && value12.Position.Y < lastHeight + 50f && LevelManager.IsBorderTrile(ref id) && value12.VisualTrile.Geometry != null)
				{
					SafeAddToBatchWithOverlaps(value12, autoCommit: false);
				}
			}
			break;
		}
		CommitBatchesIfNeeded();
	}

	public void CommitBatchesIfNeeded()
	{
		foreach (TrileMaterializer value in trileMaterializers.Values)
		{
			if (value.BatchNeedsCommit)
			{
				value.CommitBatch();
			}
		}
		if (fallbackMaterializer.BatchNeedsCommit)
		{
			fallbackMaterializer.CommitBatch();
		}
	}

	public void FreeScreenSpace(int i, int j)
	{
		Point key = new Point(i, j);
		if (viewedInstances.TryGetValue(key, out var value))
		{
			foreach (TrileInstance item in value)
			{
				if (item.InstanceId != -1)
				{
					SafeRemoveFromBatch(item);
				}
			}
			value.Clear();
		}
		viewedInstances.Remove(key);
	}

	public void FillScreenSpace(int i, int j)
	{
		Vector3 vector = CameraManager.Viewpoint.ForwardVector();
		bool flag = vector.Z != 0f;
		bool flag2 = flag;
		int num = (flag ? ((int)vector.Z) : ((int)vector.X));
		if (!LevelManager.ScreenSpaceLimits.TryGetValue(new Point(i, j), out var value))
		{
			return;
		}
		value.End += num;
		TrileEmplacement id = new TrileEmplacement(flag2 ? i : value.Start, j, flag2 ? value.Start : i);
		bool flag3 = (flag ? (id.Z != value.End) : (id.X != value.End));
		flag3 &= Math.Sign(flag ? (value.End - id.Z) : (value.End - id.X)) == num;
		bool flag4 = true;
		while (flag3)
		{
			TrileInstance trileInstance = LevelManager.TrileInstanceAt(ref id);
			if (trileInstance != null && trileInstance.Enabled && !trileInstance.Hidden && (flag4 || trileInstance.PhysicsState != null) && !trileInstance.SkipCulling)
			{
				Point ssPos = new Point(i, j);
				RegisterViewedInstance(ssPos, trileInstance);
				SafeAddToBatch(trileInstance, autoCommit: false);
				flag4 &= trileInstance.VisualTrile.SeeThrough || id.AsVector != trileInstance.Position || trileInstance.ForceSeeThrough;
				if (trileInstance.Overlaps)
				{
					foreach (TrileInstance overlappedTrile in trileInstance.OverlappedTriles)
					{
						RegisterViewedInstance(ssPos, overlappedTrile);
						SafeAddToBatch(overlappedTrile, autoCommit: false);
						flag4 &= overlappedTrile.VisualTrile.SeeThrough || id.AsVector != overlappedTrile.Position || trileInstance.ForceSeeThrough;
					}
				}
			}
			if (flag)
			{
				id.Z += num;
			}
			else
			{
				id.X += num;
			}
			flag3 = (flag ? (id.Z != value.End) : (id.X != value.End));
		}
	}

	public bool UnregisterViewedInstance(TrileInstance instance)
	{
		Vector3 b = CameraManager.Viewpoint.SideMask();
		Vector3 a = instance.LastUpdatePosition.Round();
		Point key = new Point((int)a.Dot(b), (int)a.Y);
		bool flag = false;
		if (viewedInstances.Count > 0)
		{
			if (viewedInstances.TryGetValue(key, out var value))
			{
				while (value.Remove(instance))
				{
					flag = true;
				}
			}
			else if (instance.OldSsEmplacement.HasValue && viewedInstances.TryGetValue(instance.OldSsEmplacement.Value, out value))
			{
				while (value.Remove(instance))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			instance.OldSsEmplacement = null;
		}
		return flag;
	}

	private bool RegisterViewedInstance(TrileInstance instance)
	{
		Vector3 b = CameraManager.Viewpoint.SideMask();
		TrileEmplacement emplacement = instance.Emplacement;
		Point ssPos = new Point((int)emplacement.AsVector.Dot(b), emplacement.Y);
		return RegisterViewedInstance(ssPos, instance);
	}

	private bool RegisterViewedInstance(Point ssPos, TrileInstance instance)
	{
		if (!cullingBounds.Contains(ssPos))
		{
			return false;
		}
		if (!viewedInstances.TryGetValue(ssPos, out var value))
		{
			List<TrileInstance> list2 = (viewedInstances[ssPos] = new List<TrileInstance>());
			value = list2;
		}
		value.Add(instance);
		instance.OldSsEmplacement = ssPos;
		return true;
	}

	private void SafeRemoveFromBatchWithOverlaps(TrileInstance instance)
	{
		SafeRemoveFromBatch(instance);
		if (!instance.Overlaps)
		{
			return;
		}
		foreach (TrileInstance overlappedTrile in instance.OverlappedTriles)
		{
			SafeRemoveFromBatch(overlappedTrile);
		}
	}

	private void SafeRemoveFromBatch(TrileInstance instance)
	{
		if (instance.InstanceId >= 0)
		{
			GetTrileMaterializer(instance.VisualTrile)?.RemoveFromBatch(instance);
		}
	}

	private void SafeAddToBatchWithOverlaps(TrileInstance instance, bool autoCommit)
	{
		SafeAddToBatch(instance, autoCommit);
		if (!instance.Overlaps)
		{
			return;
		}
		foreach (TrileInstance overlappedTrile in instance.OverlappedTriles)
		{
			SafeAddToBatch(overlappedTrile, autoCommit);
		}
	}

	private void SafeAddToBatch(TrileInstance instance, bool autoCommit)
	{
		TrileMaterializer trileMaterializer = GetTrileMaterializer(instance.VisualTrile);
		if (trileMaterializer != null)
		{
			trileMaterializer.AddToBatch(instance);
			if (autoCommit)
			{
				trileMaterializer.CommitBatch();
			}
			if (this.TrileInstanceBatched != null)
			{
				this.TrileInstanceBatched(instance);
			}
		}
	}

	private void DrawArtObjects(Mesh m, BaseEffect effect)
	{
		GraphicsDevice graphicsDevice = GraphicsDeviceService.GraphicsDevice;
		foreach (ArtObjectInstance levelArtObject in LevelArtObjects)
		{
			levelArtObject.ArtObject.Geometry.InstanceCount = levelArtObject.ArtObject.InstanceCount;
			levelArtObject.Update();
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.NoSilhouette);
		foreach (Group group in ArtObjectsMesh.Groups)
		{
			ArtObjectCustomData artObjectCustomData = (ArtObjectCustomData)group.CustomData;
			if (group.Enabled && artObjectCustomData.ArtObject.NoSihouette)
			{
				group.Draw(effect);
			}
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.Level);
		foreach (Group group2 in ArtObjectsMesh.Groups)
		{
			ArtObjectCustomData artObjectCustomData2 = (ArtObjectCustomData)group2.CustomData;
			if (group2.Enabled && !artObjectCustomData2.ArtObject.NoSihouette)
			{
				group2.Draw(effect);
			}
		}
	}

	protected virtual void DrawTriles(Mesh m, BaseEffect effect)
	{
		GraphicsDevice graphicsDevice = GraphicsDeviceService.GraphicsDevice;
		foreach (TrileMaterializer value in trileMaterializers.Values)
		{
			if (value.Geometry == null || value.Geometry.InstanceCount == 0 || value.Geometry.Empty)
			{
				continue;
			}
			Trile trile = value.Trile;
			ActorType type = trile.ActorSettings.Type;
			bool flag = false;
			bool flag2 = false;
			if (type.IsBomb())
			{
				flag = true;
				graphicsDevice.PrepareStencilWrite(StencilMask.Bomb);
			}
			else
			{
				switch (type)
				{
				case ActorType.LightningPlatform:
					graphicsDevice.PrepareStencilWrite(StencilMask.Ghosts);
					flag = true;
					graphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
					graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = false;
					flag2 = true;
					break;
				case ActorType.Hole:
					flag = true;
					graphicsDevice.PrepareStencilWrite(StencilMask.Hole);
					break;
				default:
					if (trile.Immaterial || trile.Thin || type.IsPickable())
					{
						flag = true;
						graphicsDevice.PrepareStencilWrite(StencilMask.NoSilhouette);
					}
					break;
				}
			}
			value.Group.Draw(effect);
			if (flag)
			{
				graphicsDevice.PrepareStencilWrite(StencilMask.Level);
			}
			if (flag2)
			{
				graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
				graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = true;
			}
		}
		fallbackMaterializer.Group.Draw(effect);
	}

	public void RegisterSatellites()
	{
		int count = LevelManager.ArtObjects.Count;
		if (aoCache.Length < count)
		{
			Array.Resize(ref aoCache, count);
		}
		LevelManager.ArtObjects.Values.CopyTo(aoCache, 0);
		LevelArtObjects.Clear();
		for (int i = 0; i < count; i++)
		{
			LevelArtObjects.Add(aoCache[i]);
		}
		int count2 = LevelManager.BackgroundPlanes.Count;
		if (plCache.Length < count2)
		{
			Array.Resize(ref plCache, count2);
		}
		LevelManager.BackgroundPlanes.Values.CopyTo(plCache, 0);
		LevelPlanes.Clear();
		for (int j = 0; j < count2; j++)
		{
			LevelPlanes.Add(plCache[j]);
		}
		int count3 = LevelManager.NonPlayerCharacters.Count;
		if (npCache.Length < count3)
		{
			Array.Resize(ref npCache, count3);
		}
		LevelManager.NonPlayerCharacters.Values.CopyTo(npCache, 0);
		levelNPCs.Clear();
		for (int k = 0; k < count3; k++)
		{
			levelNPCs.Add(npCache[k]);
		}
	}

	private void DrawTrileLights(Mesh m, BaseEffect effect)
	{
		GraphicsDevice graphicsDevice = TrilesMesh.GraphicsDevice;
		foreach (TrileMaterializer value in trileMaterializers.Values)
		{
			if (value.Geometry == null || value.Geometry.InstanceCount == 0)
			{
				continue;
			}
			ActorType type = value.Trile.ActorSettings.Type;
			if (type != ActorType.LightningPlatform)
			{
				if (type == ActorType.GoldenCube)
				{
					graphicsDevice.PrepareStencilWrite(StencilMask.Sky);
				}
				if (LevelManager.BlinkingAlpha)
				{
					TrilesEffect.Blink = type != ActorType.Crystal;
				}
				value.Group.Draw(effect);
				if (type == ActorType.GoldenCube)
				{
					graphicsDevice.PrepareStencilWrite(StencilMask.Level);
				}
			}
		}
		TrilesEffect.Blink = false;
	}
}
