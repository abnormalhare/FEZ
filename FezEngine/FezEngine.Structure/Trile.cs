using System.Collections.Generic;
using ContentSerialization.Attributes;
using FezEngine.Structure.Geometry;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class Trile : ITrixelObject
{
	[Serialization(Ignore = true)]
	public int Id { get; set; }

	public string Name { get; set; }

	public string CubemapPath { get; set; }

	[Serialization(Ignore = true)]
	public List<TrileInstance> Instances { get; set; }

	public Dictionary<FaceOrientation, CollisionType> Faces { get; set; }

	[Serialization(Optional = true)]
	public ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> Geometry { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public TrixelCluster MissingTrixels { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public TrileActorSettings ActorSettings { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Immaterial { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool SeeThrough { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Thin { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool ForceHugging { get; set; }

	[Serialization(Ignore = true)]
	public TrileSet TrileSet { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public Vector3 Size { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public Vector3 Offset { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public SurfaceType SurfaceType { get; set; }

	[Serialization(Ignore = true)]
	public Vector2 AtlasOffset { get; set; }

	[Serialization(Ignore = true)]
	public bool ForceKeep { get; set; }

	public Trile()
	{
		MissingTrixels = new TrixelCluster();
		Instances = new List<TrileInstance>();
		ActorSettings = new TrileActorSettings();
		Name = "Untitled";
		Size = Vector3.One;
		Faces = new Dictionary<FaceOrientation, CollisionType>(4, FaceOrientationComparer.Default);
	}

	public Trile(CollisionType faceType)
		: this()
	{
		Faces.Add(FaceOrientation.Back, faceType);
		Faces.Add(FaceOrientation.Front, faceType);
		Faces.Add(FaceOrientation.Left, faceType);
		Faces.Add(FaceOrientation.Right, faceType);
		MissingTrixels.OnDeserialization();
	}

	public override string ToString()
	{
		return Name;
	}

	public bool TrixelExists(TrixelEmplacement trixelIdentifier)
	{
		if (!MissingTrixels.Empty)
		{
			return !MissingTrixels.IsFilled(trixelIdentifier);
		}
		return true;
	}

	public bool CanContain(TrixelEmplacement trixel)
	{
		return TrixelInRange(trixel);
	}

	public static bool TrixelInRange(TrixelEmplacement trixel)
	{
		if (trixel.X < 16 && trixel.Y < 16 && trixel.Z < 16 && trixel.X >= 0 && trixel.Y >= 0)
		{
			return trixel.Z >= 0;
		}
		return false;
	}

	public bool IsBorderTrixelFace(TrixelEmplacement id, FaceOrientation face)
	{
		return IsBorderTrixelFace(id.GetTraversal(face));
	}

	public bool IsBorderTrixelFace(TrixelEmplacement traversed)
	{
		if (TrixelInRange(traversed))
		{
			return !TrixelExists(traversed);
		}
		return true;
	}

	public Trile Clone()
	{
		return new Trile
		{
			CubemapPath = CubemapPath,
			Faces = new Dictionary<FaceOrientation, CollisionType>(Faces),
			Id = Id,
			Immaterial = Immaterial,
			Name = Name,
			SeeThrough = SeeThrough,
			Thin = Thin,
			ForceHugging = ForceHugging,
			ActorSettings = new TrileActorSettings(ActorSettings),
			SurfaceType = SurfaceType
		};
	}

	public void CopyFrom(Trile copy)
	{
		ActorSettings = new TrileActorSettings(copy.ActorSettings);
		Faces = new Dictionary<FaceOrientation, CollisionType>(copy.Faces);
		Immaterial = copy.Immaterial;
		Thin = copy.Thin;
		Name = copy.Name;
		ForceHugging = copy.ForceHugging;
		SeeThrough = copy.SeeThrough;
		SurfaceType = copy.SurfaceType;
	}

	public void Dispose()
	{
		if (Geometry != null)
		{
			Geometry.Dispose();
			Geometry = null;
		}
		TrileSet = null;
	}
}
