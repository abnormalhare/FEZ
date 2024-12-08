using System;
using System.Linq;
using ContentSerialization.Attributes;
using FezEngine.Services;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure;

public class BackgroundPlane
{
	private IContentManagerProvider CMProvider;

	private ILevelManager LevelManager;

	private ILevelMaterializer LevelMaterializer;

	private Color filter = Color.White;

	private Vector3 position;

	private Vector3 scale = new Vector3(1f);

	private int actualWidth;

	private int actualHeight;

	private Quaternion rotation = Quaternion.Identity;

	private float opacity = 1f;

	private bool lightMap;

	private bool allowOverbrightness;

	private bool fullbright;

	private bool pixelatedLightmap;

	private bool doublesided;

	private bool crosshatch;

	private bool billboard;

	private bool alwaysOnTop;

	private bool xTextureRepeat;

	private bool yTextureRepeat;

	private bool clampTexture;

	private bool drawDirty = true;

	private bool boundsDirty = true;

	private AnimatedTexture animation;

	[Serialization(Ignore = true)]
	public BoundingBox Bounds;

	private bool visible;

	[Serialization(Ignore = true)]
	public int Id { get; set; }

	public Group Group { get; private set; }

	[Serialization(Ignore = true)]
	public ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix> Geometry { get; private set; }

	public Mesh HostMesh { private get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public ActorType ActorType { get; set; }

	[Serialization(Ignore = true)]
	public int InstanceIndex { get; private set; }

	[Serialization(Ignore = true)]
	public bool Visible
	{
		get
		{
			return visible;
		}
		set
		{
			drawDirty |= visible != value;
			visible = value;
		}
	}

	[Serialization(Ignore = true)]
	public bool Hidden { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Animated { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Billboard
	{
		get
		{
			return billboard;
		}
		set
		{
			if (billboard && !value)
			{
				Rotation = Quaternion.Identity;
			}
			billboard = value;
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool SyncWithSamples { get; set; }

	[Serialization(Ignore = true)]
	public AnimationTiming Timing { get; set; }

	[Serialization(Ignore = true)]
	public bool Loop { get; set; }

	[Serialization(Ignore = true)]
	public Vector3 Forward { get; private set; }

	[Serialization(Ignore = true)]
	public Vector3? OriginalPosition { get; set; }

	[Serialization(Ignore = true)]
	public Quaternion OriginalRotation { get; set; }

	public Vector3 Position
	{
		get
		{
			return position;
		}
		set
		{
			position = value;
			drawDirty = (boundsDirty = true);
		}
	}

	[Serialization(Ignore = true)]
	public FaceOrientation Orientation { get; private set; }

	[Serialization(Optional = true)]
	public Quaternion Rotation
	{
		get
		{
			return rotation;
		}
		set
		{
			rotation = value;
			drawDirty = (boundsDirty = true);
			Orientation = FezMath.OrientationFromDirection(FezMath.AlmostClamp(Vector3.Transform(Vector3.UnitZ, rotation)));
			Forward = Vector3.Transform(Vector3.Forward, rotation).Round();
		}
	}

	[Serialization(Optional = true)]
	public Vector3 Scale
	{
		get
		{
			return scale;
		}
		set
		{
			scale = value;
			drawDirty = (boundsDirty = true);
		}
	}

	public Vector3 Size { get; set; }

	public string TextureName { get; set; }

	[Serialization(Ignore = true)]
	public Texture Texture { get; set; }

	[Serialization(Ignore = true)]
	private Texture XboxTexture { get; set; }

	[Serialization(Ignore = true)]
	private Texture SonyTexture { get; set; }

	[Serialization(Optional = true)]
	public float Opacity
	{
		get
		{
			return opacity;
		}
		set
		{
			opacity = value;
			drawDirty = true;
		}
	}

	[Serialization(Optional = true)]
	public float ParallaxFactor { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool LightMap
	{
		get
		{
			return lightMap;
		}
		set
		{
			lightMap = value;
			if (Group != null)
			{
				InitializeGroup();
			}
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool AllowOverbrightness
	{
		get
		{
			return allowOverbrightness;
		}
		set
		{
			allowOverbrightness = value;
			if (Group != null)
			{
				InitializeGroup();
			}
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Doublesided
	{
		get
		{
			return doublesided;
		}
		set
		{
			doublesided = value;
			if (Group != null)
			{
				InitializeGroup();
			}
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Crosshatch
	{
		get
		{
			return crosshatch;
		}
		set
		{
			crosshatch = value;
			if (Group != null)
			{
				InitializeGroup();
			}
		}
	}

	[Serialization(Optional = true)]
	public int? AttachedGroup { get; set; }

	[Serialization(Optional = true)]
	public int? AttachedPlane { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public Color Filter
	{
		get
		{
			return filter;
		}
		set
		{
			filter = value;
			drawDirty = true;
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool AlwaysOnTop
	{
		get
		{
			return alwaysOnTop;
		}
		set
		{
			alwaysOnTop = value;
			if (Group != null)
			{
				InitializeGroup();
			}
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Fullbright
	{
		get
		{
			return fullbright;
		}
		set
		{
			fullbright = value;
			drawDirty = true;
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool PixelatedLightmap
	{
		get
		{
			return pixelatedLightmap;
		}
		set
		{
			pixelatedLightmap = value;
			if (Group != null)
			{
				InitializeGroup();
			}
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool ClampTexture
	{
		get
		{
			return clampTexture;
		}
		set
		{
			clampTexture = value;
			if (Group != null)
			{
				InitializeGroup();
			}
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool XTextureRepeat
	{
		get
		{
			return xTextureRepeat;
		}
		set
		{
			xTextureRepeat = value;
			drawDirty = true;
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool YTextureRepeat
	{
		get
		{
			return yTextureRepeat;
		}
		set
		{
			yTextureRepeat = value;
			drawDirty = true;
		}
	}

	[Serialization(Ignore = true)]
	public bool Disposed { get; set; }

	public BackgroundPlane()
	{
		Loop = true;
		Visible = true;
		Orientation = FaceOrientation.Front;
		OriginalRotation = Quaternion.Identity;
	}

	public BackgroundPlane(Mesh hostMesh, AnimatedTexture animation)
		: this()
	{
		BackgroundPlane backgroundPlane = this;
		HostMesh = hostMesh;
		Timing = animation.Timing.Clone();
		if (animation.Texture == null)
		{
			DrawActionScheduler.Schedule(delegate
			{
				backgroundPlane.Texture = animation.Texture;
			});
		}
		else
		{
			Texture = animation.Texture;
		}
		Animated = true;
		this.animation = animation;
		actualWidth = animation.FrameWidth;
		actualHeight = animation.FrameHeight;
		Initialize();
	}

	public BackgroundPlane(Mesh hostMesh, Texture texture)
		: this()
	{
		HostMesh = hostMesh;
		Texture = texture;
		Animated = false;
		Initialize();
	}

	public BackgroundPlane(Mesh hostMesh, string textureName, bool animated)
		: this()
	{
		HostMesh = hostMesh;
		TextureName = textureName;
		Animated = animated;
		Initialize();
	}

	public void Initialize()
	{
		if (ServiceHelper.IsFull)
		{
			CMProvider = ServiceHelper.Get<IContentManagerProvider>();
			LevelManager = ServiceHelper.Get<ILevelManager>();
			LevelMaterializer = ServiceHelper.Get<ILevelMaterializer>();
		}
		if (Animated)
		{
			if (animation == null)
			{
				AnimatedTexture newAnim = CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/" + TextureName);
				animation = newAnim;
				Timing = animation.Timing.Clone();
				if (newAnim.Texture == null)
				{
					DrawActionScheduler.Schedule(delegate
					{
						Texture = newAnim.Texture;
					});
				}
				else
				{
					Texture = newAnim.Texture;
				}
				actualWidth = animation.FrameWidth;
				actualHeight = animation.FrameHeight;
			}
			Timing.Loop = true;
			Timing.RandomizeStep();
			Size = new Vector3((float)actualWidth / 16f, (float)actualHeight / 16f, 0.125f);
			InitializeGroup();
		}
		else if (Texture == null)
		{
			DrawActionScheduler.Schedule(delegate
			{
				Texture = CMProvider.CurrentLevel.Load<Texture2D>("Background Planes/" + TextureName);
				if (TextureName.StartsWith("ZU_HOUSE_QR_A"))
				{
					XboxTexture = Texture;
					SonyTexture = CMProvider.CurrentLevel.Load<Texture2D>("Background Planes/" + TextureName + "_SONY");
					GamepadState.OnLayoutChanged = (EventHandler)Delegate.Combine(GamepadState.OnLayoutChanged, new EventHandler(UpdateControllerTexture));
					if (GamepadState.Layout != 0)
					{
						Texture = SonyTexture;
					}
				}
				Size = new Vector3((float)(Texture as Texture2D).Width / 16f, (float)(Texture as Texture2D).Height / 16f, 0.125f);
				InitializeGroup();
			});
		}
		else
		{
			Size = new Vector3((float)(Texture as Texture2D).Width / 16f, (float)(Texture as Texture2D).Height / 16f, 0.125f);
			InitializeGroup();
		}
	}

	private void InitializeGroup()
	{
		if (Group != null)
		{
			DestroyInstancedGroup();
		}
		BackgroundPlane backgroundPlane = null;
		lock (LevelManager.BackgroundPlanes)
		{
			backgroundPlane = LevelManager.BackgroundPlanes.Values.FirstOrDefault((BackgroundPlane x) => x != this && x.Animated == Animated && x.doublesided == doublesided && x.crosshatch == crosshatch && ((Texture != null && x.Texture == Texture) || (TextureName != null && x.TextureName == TextureName)) && x.Group != null && x.clampTexture == clampTexture && x.lightMap == lightMap && x.allowOverbrightness == allowOverbrightness && x.pixelatedLightmap == pixelatedLightmap);
		}
		if (backgroundPlane == null)
		{
			Group group2 = (Group = HostMesh.AddFace(Size, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true, doublesided, crosshatch));
			Group groupCopy = group2;
			Geometry = new ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>(PrimitiveType.TriangleList, 58);
			Geometry.Vertices = (Group.Geometry as IndexedUserPrimitives<FezVertexPositionNormalTexture>).Vertices.Select(delegate(FezVertexPositionNormalTexture x)
			{
				VertexPositionNormalTextureInstance result = default(VertexPositionNormalTextureInstance);
				result.Position = x.Position;
				result.Normal = x.Normal;
				result.TextureCoordinate = x.TextureCoordinate;
				return result;
			}).ToArray();
			Geometry.Indices = (Group.Geometry as IndexedUserPrimitives<FezVertexPositionNormalTexture>).Indices.ToArray();
			Geometry.PredictiveBatchSize = 1;
			Group.Geometry = Geometry;
			Geometry.Instances = new Matrix[4];
			if (Texture == null)
			{
				DrawActionScheduler.Schedule(delegate
				{
					groupCopy.Texture = Texture;
				});
			}
			else
			{
				Group.Texture = Texture;
			}
			if (Animated)
			{
				DrawActionScheduler.Schedule(delegate
				{
					groupCopy.CustomData = new Vector2((float)animation.Offsets[0].Width / (float)animation.Texture.Width, (float)animation.Offsets[0].Height / (float)animation.Texture.Height);
				});
			}
		}
		else
		{
			Group = backgroundPlane.Group;
			Geometry = backgroundPlane.Geometry;
		}
		InstanceIndex = Geometry.InstanceCount++;
		UpdateGroupSetings();
	}

	private void DestroyInstancedGroup()
	{
		int num = 0;
		foreach (BackgroundPlane value in LevelManager.BackgroundPlanes.Values)
		{
			if (value != this && value.Group == Group)
			{
				value.InstanceIndex = num++;
				value.drawDirty = true;
				value.Update();
			}
		}
		if (Geometry == null)
		{
			return;
		}
		Geometry.InstanceCount = num;
		if (num == 0 && Group != null)
		{
			Geometry = null;
			if (Animated)
			{
				LevelMaterializer.AnimatedPlanesMesh.RemoveGroup(Group);
			}
			else
			{
				LevelMaterializer.StaticPlanesMesh.RemoveGroup(Group);
			}
		}
		Group = null;
	}

	public void Update()
	{
		if (!drawDirty || Geometry == null)
		{
			return;
		}
		if (Geometry.Instances.Length < Geometry.InstanceCount)
		{
			Array.Resize(ref Geometry.Instances, Geometry.InstanceCount + 4);
		}
		Geometry.UpdateBuffers();
		Vector3 vector = (Visible ? Position : new Vector3(float.MinValue));
		Vector3 vector2 = Filter.ToVector3();
		int num = (fullbright ? 1 : 0) | (clampTexture ? 2 : 0) | (xTextureRepeat ? 4 : 0) | (yTextureRepeat ? 8 : 0);
		Vector2 zero = Vector2.Zero;
		if (Animated)
		{
			if (animation.Texture == null)
			{
				return;
			}
			int frame = Timing.Frame;
			zero.X = (float)animation.Offsets[frame].X / (float)animation.Texture.Width;
			zero.Y = (float)animation.Offsets[frame].Y / (float)animation.Texture.Height;
		}
		if (opacity != 0f && opacity != 1f)
		{
			Group.NoAlphaWrite = true;
		}
		else
		{
			Group.NoAlphaWrite = null;
		}
		Geometry.Instances[InstanceIndex] = new Matrix(vector.X, vector.Y, vector.Z, zero.X, Rotation.X, Rotation.Y, Rotation.Z, Rotation.W, Scale.X, Scale.Y, zero.Y, num, vector2.X, vector2.Y, vector2.Z, Opacity);
		Geometry.InstancesDirty = true;
		drawDirty = false;
	}

	private void UpdateControllerTexture(object sender, EventArgs e)
	{
		if (GamepadState.Layout == GamepadState.GamepadLayout.Xbox360)
		{
			Group.Texture = XboxTexture;
		}
		else
		{
			Group.Texture = SonyTexture;
		}
	}

	public void UpdateBounds()
	{
		if (boundsDirty)
		{
			Vector3 vector = Vector3.Transform(Size / 2f * scale, rotation).Abs();
			Bounds = FezMath.Enclose(position - vector, position + vector);
			boundsDirty = false;
		}
	}

	public void MarkDirty()
	{
		drawDirty = true;
	}

	public void UpdateGroupSetings()
	{
		if (lightMap)
		{
			Group.Blending = ((!allowOverbrightness) ? BlendingMode.Maximum : BlendingMode.Additive);
		}
		else
		{
			Group.Blending = BlendingMode.Alphablending;
		}
		if (lightMap && !pixelatedLightmap)
		{
			Group.SamplerState = ((clampTexture || (!xTextureRepeat && !yTextureRepeat)) ? SamplerState.LinearClamp : SamplerState.LinearWrap);
		}
		else
		{
			Group.SamplerState = ((clampTexture || (!xTextureRepeat && !yTextureRepeat)) ? SamplerState.PointClamp : SamplerState.PointWrap);
		}
		drawDirty = true;
	}

	public void Dispose()
	{
		DestroyInstancedGroup();
		Disposed = true;
	}

	public BackgroundPlane Clone()
	{
		BackgroundPlane backgroundPlane = new BackgroundPlane();
		backgroundPlane.HostMesh = HostMesh;
		backgroundPlane.Animated = Animated;
		backgroundPlane.TextureName = TextureName;
		backgroundPlane.Texture = Texture;
		backgroundPlane.Timing = ((Timing == null) ? null : Timing.Clone());
		backgroundPlane.Position = position;
		backgroundPlane.Rotation = rotation;
		backgroundPlane.Scale = scale;
		backgroundPlane.LightMap = lightMap;
		backgroundPlane.AllowOverbrightness = allowOverbrightness;
		backgroundPlane.Filter = filter;
		backgroundPlane.Doublesided = doublesided;
		backgroundPlane.Opacity = opacity;
		backgroundPlane.Crosshatch = crosshatch;
		backgroundPlane.SyncWithSamples = SyncWithSamples;
		backgroundPlane.AlwaysOnTop = alwaysOnTop;
		backgroundPlane.Fullbright = fullbright;
		backgroundPlane.Loop = Loop;
		backgroundPlane.Billboard = billboard;
		backgroundPlane.AttachedGroup = AttachedGroup;
		backgroundPlane.YTextureRepeat = YTextureRepeat;
		backgroundPlane.XTextureRepeat = XTextureRepeat;
		backgroundPlane.ClampTexture = ClampTexture;
		backgroundPlane.AttachedPlane = AttachedPlane;
		backgroundPlane.ActorType = ActorType;
		backgroundPlane.ParallaxFactor = ParallaxFactor;
		backgroundPlane.Initialize();
		return backgroundPlane;
	}
}
