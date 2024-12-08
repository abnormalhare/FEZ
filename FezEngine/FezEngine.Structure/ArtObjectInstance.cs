using System;
using ContentSerialization.Attributes;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class ArtObjectInstance
{
	[Serialization(Ignore = true)]
	public BoundingBox Bounds;

	private static readonly ILevelMaterializer LevelMaterializer;

	private Mesh hostMesh;

	private Vector3 position;

	private Vector3 scale = Vector3.One;

	private Quaternion rotation = Quaternion.Identity;

	private bool forceShading;

	private bool visible = true;

	private bool drawDirty = true;

	private bool boundsDirty = true;

	private string artObjectName;

	[Serialization(Ignore = true)]
	public int Id { get; set; }

	[Serialization(Ignore = true)]
	public bool Enabled { get; set; }

	[Serialization(Ignore = true)]
	public bool Hidden { get; set; }

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
	public int InstanceIndex { get; private set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public ArtObjectActorSettings ActorSettings { get; set; }

	public Vector3 Position
	{
		get
		{
			return position;
		}
		set
		{
			position = value;
			boundsDirty = (drawDirty = true);
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
			boundsDirty = (drawDirty = true);
		}
	}

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
			boundsDirty = (drawDirty = true);
		}
	}

	[Serialization(Ignore = true)]
	public Material Material { get; set; }

	[Serialization(Ignore = true)]
	public bool ForceShading
	{
		get
		{
			return forceShading;
		}
		set
		{
			forceShading = value;
			drawDirty = true;
		}
	}

	public string ArtObjectName
	{
		get
		{
			if (ArtObject != null)
			{
				return ArtObject.Name;
			}
			return artObjectName;
		}
		set
		{
			artObjectName = value;
		}
	}

	[Serialization(Ignore = true)]
	public ArtObject ArtObject { get; set; }

	static ArtObjectInstance()
	{
		if (ServiceHelper.IsFull)
		{
			LevelMaterializer = ServiceHelper.Get<ILevelMaterializer>();
		}
	}

	public ArtObjectInstance()
	{
		ActorSettings = new ArtObjectActorSettings();
		Enabled = true;
	}

	public ArtObjectInstance(string artObjectName)
		: this()
	{
		this.artObjectName = artObjectName;
	}

	public ArtObjectInstance(ArtObject artObject)
		: this()
	{
		ArtObject = artObject;
	}

	public void Initialize()
	{
		hostMesh = LevelMaterializer.ArtObjectsMesh;
		if (ArtObject.Group == null)
		{
			ArtObject.Group = hostMesh.AddGroup();
			ArtObject.Group.Geometry = ArtObject.Geometry;
			DrawActionScheduler.Schedule(delegate
			{
				if (ArtObject != null)
				{
					if (ArtObject.CubemapSony != null && GamepadState.Layout != 0)
					{
						ArtObject.Group.Texture = ArtObject.CubemapSony;
					}
					else
					{
						ArtObject.Group.Texture = ArtObject.Cubemap;
					}
				}
			});
			ArtObject.Group.CustomData = new ArtObjectCustomData
			{
				ArtObject = ArtObject
			};
		}
		InstanceIndex = ArtObject.InstanceCount++;
		ArtObject.Geometry.PredictiveBatchSize = 1;
		if (ArtObject.Geometry.Instances == null)
		{
			ArtObject.Geometry.Instances = new Matrix[4];
		}
	}

	public void RebuildBounds()
	{
		if (boundsDirty)
		{
			Vector3 vector = ArtObject.Size / 2f * scale;
			Bounds = new BoundingBox(position - vector, position + vector);
			FezMath.RotateOnCenter(ref Bounds, ref rotation);
			boundsDirty = false;
		}
	}

	public void Update()
	{
		if (drawDirty)
		{
			if (ArtObject.Geometry.Instances.Length < ArtObject.InstanceCount)
			{
				Array.Resize(ref ArtObject.Geometry.Instances, ArtObject.InstanceCount + 4);
			}
			ArtObject.Geometry.UpdateBuffers();
			Vector3 vector = (visible ? position : new Vector3(float.MinValue));
			ArtObject.Geometry.Instances[InstanceIndex] = new Matrix(vector.X, vector.Y, vector.Z, (Material == null) ? 1f : Material.Opacity, Scale.X, Scale.Y, Scale.Z, ForceShading ? 1 : 0, rotation.X, rotation.Y, rotation.Z, rotation.W, ActorSettings.InvisibleSides.Contains(FaceOrientation.Front) ? 1 : 0, ActorSettings.InvisibleSides.Contains(FaceOrientation.Right) ? 1 : 0, ActorSettings.InvisibleSides.Contains(FaceOrientation.Back) ? 1 : 0, ActorSettings.InvisibleSides.Contains(FaceOrientation.Left) ? 1 : 0);
			ArtObject.Geometry.InstancesDirty = true;
			drawDirty = false;
		}
	}

	public void MarkDirty()
	{
		boundsDirty = (drawDirty = true);
	}

	public void Dispose(bool final)
	{
		int num = 0;
		if (!final)
		{
			foreach (ArtObjectInstance levelArtObject in LevelMaterializer.LevelArtObjects)
			{
				if (levelArtObject != this && levelArtObject.ArtObject == ArtObject)
				{
					levelArtObject.InstanceIndex = num++;
					levelArtObject.drawDirty = true;
					levelArtObject.Update();
				}
			}
			LevelMaterializer.LevelArtObjects.Remove(this);
			ArtObject.InstanceCount = num;
		}
		else
		{
			ArtObject.InstanceCount = 0;
		}
		if (num == 0 && ArtObject.Group != null)
		{
			ArtObject.Geometry.ResetBuffers();
			LevelMaterializer.ArtObjectsMesh.RemoveGroup(ArtObject.Group);
			ArtObject.Group = null;
			ArtObject = null;
		}
	}

	public void Dispose()
	{
		Dispose(final: false);
	}

	public void SoftDispose()
	{
		LevelMaterializer.LevelArtObjects.Remove(this);
		if (ArtObject != null)
		{
			if (ArtObject.Group != null)
			{
				LevelMaterializer.ArtObjectsMesh.RemoveGroup(ArtObject.Group, skipDispose: true);
			}
			ArtObject.InstanceCount = 0;
			ArtObject.Group = null;
			if (ArtObject.Geometry != null)
			{
				ArtObject.Geometry.ResetBuffers();
			}
		}
	}
}
