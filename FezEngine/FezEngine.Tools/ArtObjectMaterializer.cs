using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

public class ArtObjectMaterializer
{
	private struct InvalidationContext
	{
		public IEnumerable<TrixelEmplacement> Trixels;

		public bool TrixelsExist;

		public ICollection<TrixelFace> Added;

		public ICollection<TrixelFace> Removed;
	}

	public const int InstancesPerBatch = 60;

	private static readonly InvalidTrixelFaceComparer invalidTrixelFaceComparer = new InvalidTrixelFaceComparer();

	private readonly HashSet<TrixelFace> added;

	private readonly HashSet<TrixelFace> removed;

	private readonly ArtObject artObject;

	private readonly TrixelCluster missingTrixels;

	private readonly Vector3 size;

	private readonly List<TrixelSurface> surfaces;

	private InvalidationContext otherThreadContext;

	public bool Dirty { get; set; }

	public ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix> Geometry { get; set; }

	[ServiceDependency(Optional = true)]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency(Optional = true)]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency(Optional = true)]
	public ILevelManager LevelManager { protected get; set; }

	public ArtObjectMaterializer(TrixelCluster missingTrixels, List<TrixelSurface> surfaces, Vector3 size)
	{
		this.missingTrixels = missingTrixels;
		ServiceHelper.InjectServices(this);
		added = new HashSet<TrixelFace>();
		removed = new HashSet<TrixelFace>();
		this.surfaces = surfaces ?? new List<TrixelSurface>();
		this.size = size;
	}

	public ArtObjectMaterializer(ArtObject artObject)
	{
		this.artObject = artObject;
		ServiceHelper.InjectServices(this);
		artObject.Materializer = this;
		size = artObject.Size;
		if (artObject.MissingTrixels != null)
		{
			added = new HashSet<TrixelFace>();
			removed = new HashSet<TrixelFace>();
			missingTrixels = artObject.MissingTrixels;
			if (artObject.TrixelSurfaces == null)
			{
				artObject.TrixelSurfaces = (surfaces = new List<TrixelSurface>());
			}
			else
			{
				surfaces = artObject.TrixelSurfaces;
			}
		}
	}

	public void Rebuild()
	{
		Rebuild(force: false);
	}

	public void Rebuild(bool force)
	{
		if (force || surfaces.Count == 0)
		{
			MarkMissingCells();
			UpdateSurfaces();
		}
		RebuildGeometry();
	}

	public void Update()
	{
		UpdateSurfaces();
		RebuildGeometry();
	}

	public void MarkMissingCells()
	{
		added.Clear();
		removed.Clear();
		InitializeSurfaces();
		MarkRemoved(missingTrixels.Cells);
	}

	public void MarkAdded(IEnumerable<TrixelEmplacement> trixels)
	{
		Invalidate(trixels, trixelsExist: true);
	}

	public void MarkRemoved(IEnumerable<TrixelEmplacement> trixels)
	{
		Invalidate(trixels, trixelsExist: false);
	}

	private void Invalidate(IEnumerable<TrixelEmplacement> trixels, bool trixelsExist)
	{
		int num = trixels.Count();
		InvalidationContext invalidationContext = default(InvalidationContext);
		invalidationContext.Trixels = trixels.Take(num / 2);
		invalidationContext.TrixelsExist = trixelsExist;
		invalidationContext.Added = new List<TrixelFace>();
		invalidationContext.Removed = new List<TrixelFace>();
		InvalidationContext context = invalidationContext;
		otherThreadContext = new InvalidationContext
		{
			Trixels = trixels.Skip(num / 2),
			TrixelsExist = trixelsExist,
			Added = new List<TrixelFace>(),
			Removed = new List<TrixelFace>()
		};
		Thread thread = new Thread(InvalidateOtherThread);
		thread.Start();
		Invalidate(context);
		thread.Join();
		added.UnionWith(context.Added);
		added.UnionWith(otherThreadContext.Added);
		removed.UnionWith(context.Removed);
		removed.UnionWith(otherThreadContext.Removed);
		Dirty = true;
	}

	private void InvalidateOtherThread()
	{
		Invalidate(otherThreadContext);
	}

	private bool TrixelExists(TrixelEmplacement trixelIdentifier)
	{
		if (!missingTrixels.Empty)
		{
			return !missingTrixels.IsFilled(trixelIdentifier);
		}
		return true;
	}

	private bool CanContain(TrixelEmplacement trixel)
	{
		if ((float)trixel.X < size.X * 16f && (float)trixel.Y < size.Y * 16f && (float)trixel.Z < size.Z * 16f && trixel.X >= 0 && trixel.Y >= 0)
		{
			return trixel.Z >= 0;
		}
		return false;
	}

	private bool IsBorderTrixelFace(TrixelEmplacement traversed)
	{
		if (CanContain(traversed))
		{
			return !TrixelExists(traversed);
		}
		return true;
	}

	private void Invalidate(InvalidationContext context)
	{
		foreach (TrixelEmplacement trixel in context.Trixels)
		{
			for (int i = 0; i < 6; i++)
			{
				FaceOrientation face = (FaceOrientation)i;
				TrixelEmplacement traversed = trixel.GetTraversal(face);
				if (IsBorderTrixelFace(traversed))
				{
					if (surfaces.Any((TrixelSurface x) => x.Orientation == face && x.AnyRectangleContains(trixel)))
					{
						context.Removed.Add(new TrixelFace(trixel, face));
					}
					if (context.TrixelsExist)
					{
						context.Added.Add(new TrixelFace(trixel, face));
					}
					continue;
				}
				FaceOrientation oppositeFace = face.GetOpposite();
				if (surfaces.Any((TrixelSurface x) => x.Orientation == oppositeFace && x.AnyRectangleContains(traversed)))
				{
					context.Removed.Add(new TrixelFace(traversed, oppositeFace));
				}
				if (!context.TrixelsExist)
				{
					context.Added.Add(new TrixelFace(traversed, oppositeFace));
				}
			}
		}
	}

	public void UpdateSurfaces()
	{
		TrixelFace[] array = removed.ToArray();
		Array.Sort(array, invalidTrixelFaceComparer);
		TrixelFace[] array2 = array;
		foreach (TrixelFace tf in array2)
		{
			TrixelSurface trixelSurface = null;
			foreach (TrixelSurface item in surfaces.Where((TrixelSurface x) => x.Orientation == tf.Face && x.Trixels.Contains(tf.Id)))
			{
				item.Trixels.Remove(tf.Id);
				if (item.Trixels.Count == 0)
				{
					trixelSurface = item;
				}
				else
				{
					item.MarkAsDirty();
				}
			}
			if (trixelSurface != null)
			{
				surfaces.Remove(trixelSurface);
			}
		}
		removed.Clear();
		TrixelFace[] array3 = added.ToArray();
		Array.Sort(array3, invalidTrixelFaceComparer);
		array2 = array3;
		foreach (TrixelFace tf2 in array2)
		{
			TrixelSurface[] array4 = surfaces.Where((TrixelSurface x) => x.CanContain(tf2.Id, tf2.Face)).ToArray();
			if (array4.Length != 0)
			{
				TrixelSurface trixelSurface2 = array4[0];
				trixelSurface2.Trixels.Add(tf2.Id);
				trixelSurface2.MarkAsDirty();
				if (array4.Length <= 1)
				{
					continue;
				}
				foreach (TrixelSurface item2 in array4.Skip(1))
				{
					trixelSurface2.Trixels.UnionWith(item2.Trixels);
					surfaces.Remove(item2);
				}
			}
			else
			{
				surfaces.Add(new TrixelSurface(tf2.Face, tf2.Id));
			}
		}
		added.Clear();
		RebuildParts();
	}

	private void RebuildParts()
	{
		foreach (TrixelSurface item in surfaces.Where((TrixelSurface x) => x.Dirty))
		{
			item.RebuildParts();
		}
	}

	private void InitializeSurfaces()
	{
		surfaces.Clear();
		foreach (FaceOrientation value in Util.GetValues<FaceOrientation>())
		{
			TrixelEmplacement trixelEmplacement = new TrixelEmplacement(value.IsPositive() ? (value.AsVector() * (size * 16f - Vector3.One)) : Vector3.Zero);
			TrixelSurface trixelSurface = new TrixelSurface(value, trixelEmplacement);
			Vector3 mask = value.GetTangent().AsAxis().GetMask();
			int num = (int)Vector3.Dot(size, mask) * 16;
			Vector3 mask2 = value.GetBitangent().AsAxis().GetMask();
			int num2 = (int)Vector3.Dot(size, mask2) * 16;
			trixelSurface.RectangularParts.Add(new RectangularTrixelSurfacePart
			{
				Orientation = value,
				TangentSize = num,
				BitangentSize = num2,
				Start = trixelEmplacement
			});
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num2; j++)
				{
					trixelSurface.Trixels.Add(new TrixelEmplacement(trixelEmplacement + mask * i + mask2 * j));
				}
			}
			surfaces.Add(trixelSurface);
		}
	}

	public void RebuildGeometry()
	{
		if (surfaces == null)
		{
			return;
		}
		if (Geometry == null)
		{
			Geometry = new ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>(PrimitiveType.TriangleList, 60);
		}
		int num = surfaces.Sum((TrixelSurface x) => x.RectangularParts.Count);
		Dictionary<RectangularTrixelSurfacePart, FaceMaterialization<VertexPositionNormalTextureInstance>> dictionary = new Dictionary<RectangularTrixelSurfacePart, FaceMaterialization<VertexPositionNormalTextureInstance>>(num * 4);
		VertexGroup<VertexPositionNormalTextureInstance> vertexGroup = new VertexGroup<VertexPositionNormalTextureInstance>(num);
		Vector3 vector = size / 2f;
		foreach (RectangularTrixelSurfacePart item in surfaces.SelectMany((TrixelSurface x) => x.RectangularParts))
		{
			if (!dictionary.ContainsKey(item))
			{
				Vector3 vector2 = item.Orientation.AsVector();
				Vector3 vector3 = item.Orientation.GetTangent().AsVector() * item.TangentSize / 16f;
				Vector3 vector4 = item.Orientation.GetBitangent().AsVector() * item.BitangentSize / 16f;
				Vector3 vector5 = item.Start.Position / 16f + (item.Orientation.IsPositive() ? 1 : 0) * vector2 / 16f - vector;
				FaceMaterialization<VertexPositionNormalTextureInstance> faceMaterialization = default(FaceMaterialization<VertexPositionNormalTextureInstance>);
				faceMaterialization.V0 = vertexGroup.Reference(new VertexPositionNormalTextureInstance(vector5.Round(4), vector2));
				faceMaterialization.V1 = vertexGroup.Reference(new VertexPositionNormalTextureInstance((vector5 + vector3).Round(4), vector2));
				faceMaterialization.V2 = vertexGroup.Reference(new VertexPositionNormalTextureInstance((vector5 + vector3 + vector4).Round(4), vector2));
				faceMaterialization.V3 = vertexGroup.Reference(new VertexPositionNormalTextureInstance((vector5 + vector4).Round(4), vector2));
				FaceMaterialization<VertexPositionNormalTextureInstance> value = faceMaterialization;
				value.SetupIndices(item.Orientation);
				dictionary.Add(item, value);
			}
		}
		VertexPositionNormalTextureInstance[] array = new VertexPositionNormalTextureInstance[vertexGroup.Vertices.Count];
		int num2 = 0;
		foreach (SharedVertex<VertexPositionNormalTextureInstance> vertex in vertexGroup.Vertices)
		{
			array[num2] = vertex.Vertex;
			array[num2].TextureCoordinate = array[num2].ComputeTexCoord(size) * new Vector2(1.3333334f, 1f);
			vertex.Index = num2++;
		}
		int[] array2 = new int[dictionary.Count * 6];
		num2 = 0;
		foreach (FaceMaterialization<VertexPositionNormalTextureInstance> value2 in dictionary.Values)
		{
			for (ushort num3 = 0; num3 < 6; num3++)
			{
				array2[num2++] = value2.GetIndex(num3);
			}
		}
		Geometry.Vertices = array;
		Geometry.Indices = array2;
		if (artObject != null)
		{
			PostInitialize();
		}
	}

	public void PostInitialize()
	{
		if (CMProvider != null && artObject.CubemapPath != null && artObject.Cubemap == null)
		{
			string assetName = "Art Objects/" + artObject.CubemapPath;
			artObject.Cubemap = CMProvider.CurrentLevel.Load<Texture2D>(assetName);
		}
		else if (CMProvider != null && artObject.Name == "SEWER_QR_CUBEAO")
		{
			DrawActionScheduler.Schedule(delegate
			{
				ArtObject artObject = CMProvider.CurrentLevel.Load<ArtObject>("Art Objects/SEWER_QR_CUBE_SONYAO");
				this.artObject.CubemapSony = artObject.Cubemap;
			});
			GamepadState.OnLayoutChanged = (EventHandler)Delegate.Combine(GamepadState.OnLayoutChanged, new EventHandler(artObject.UpdateControllerTexture));
		}
		if (artObject.Geometry == null && Geometry != null)
		{
			artObject.Geometry = Geometry;
		}
	}

	public void RecomputeTexCoords(bool widen)
	{
		for (int i = 0; i < artObject.Geometry.Vertices.Length; i++)
		{
			artObject.Geometry.Vertices[i].TextureCoordinate = artObject.Geometry.Vertices[i].ComputeTexCoord(artObject.Size) * ((!widen) ? Vector2.One : new Vector2(1.3333334f, 1f));
		}
	}
}
