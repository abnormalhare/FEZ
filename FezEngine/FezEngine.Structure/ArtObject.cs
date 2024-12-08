using System;
using System.Collections.Generic;
using ContentSerialization.Attributes;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure;

public class ArtObject : ITrixelObject, IDisposable
{
	public string Name { get; set; }

	public string CubemapPath { get; set; }

	[Serialization(Ignore = true)]
	public Texture2D Cubemap { get; set; }

	public Vector3 Size { get; set; }

	[Serialization(Optional = true)]
	public ActorType ActorType { get; set; }

	[Serialization(Optional = true)]
	public bool NoSihouette { get; set; }

	[Serialization(Optional = true)]
	public TrixelCluster MissingTrixels { get; set; }

	[Serialization(Optional = true, CollectionItemName = "surface")]
	public List<TrixelSurface> TrixelSurfaces { get; set; }

	[Serialization(Optional = true)]
	public ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix> Geometry { get; set; }

	[Serialization(Ignore = true)]
	public ArtObjectMaterializer Materializer { get; set; }

	[Serialization(Ignore = true)]
	public Group Group { get; set; }

	[Serialization(Ignore = true)]
	public int InstanceCount { get; set; }

	[Serialization(Ignore = true)]
	public Texture2D CubemapSony { get; set; }

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
		if ((float)trixel.X < Size.X * 16f && (float)trixel.Y < Size.Y * 16f && (float)trixel.Z < Size.Z * 16f && trixel.X >= 0 && trixel.Y >= 0)
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
		if (CanContain(traversed))
		{
			return !TrixelExists(traversed);
		}
		return true;
	}

	internal void UpdateControllerTexture(object sender, EventArgs e)
	{
		if (GamepadState.Layout == GamepadState.GamepadLayout.Xbox360)
		{
			Group.Texture = Cubemap;
		}
		else
		{
			Group.Texture = CubemapSony;
		}
	}

	public void Dispose()
	{
		if (Cubemap != null)
		{
			Cubemap.Dispose();
		}
		Cubemap = null;
		if (CubemapSony != null)
		{
			GamepadState.OnLayoutChanged = (EventHandler)Delegate.Remove(GamepadState.OnLayoutChanged, new EventHandler(UpdateControllerTexture));
			CubemapSony.Dispose();
			CubemapSony = null;
		}
		if (Geometry != null)
		{
			Geometry.Dispose();
		}
		Geometry = null;
		Group = null;
		Materializer = null;
	}
}
