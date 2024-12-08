using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

public class TrileMaterializer : IDisposable
{
	public const int InstancesPerBatch = 200;

	private static readonly InvalidTrixelFaceComparer InvalidTrixelFaceComparer = new InvalidTrixelFaceComparer();

	protected ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry;

	protected readonly List<Vector4> tempInstances;

	protected readonly List<TrileInstance> tempInstanceIds;

	private readonly List<TrixelSurface> surfaces;

	private readonly HashSet<TrixelFace> added;

	private readonly HashSet<TrixelFace> removed;

	protected readonly Trile trile;

	protected readonly Group group;

	private static readonly Vector4 OutOfSight = new Vector4(float.MinValue);

	private const int PredictiveBatchSize = 16;

	public static bool NoTrixelsMode { get; set; }

	public ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> Geometry
	{
		get
		{
			return geometry;
		}
		set
		{
			group.Geometry = (geometry = value);
		}
	}

	public bool BatchNeedsCommit { get; private set; }

	public Group Group => group;

	internal Trile Trile => trile;

	public IEnumerable<TrixelSurface> TrixelSurfaces => surfaces;

	[ServiceDependency(Optional = true)]
	public IContentManagerProvider CMProvider { protected get; set; }

	[ServiceDependency(Optional = true)]
	public ILevelManager LevelManager { protected get; set; }

	[ServiceDependency(Optional = true)]
	public ILevelMaterializer LevelMaterializer { protected get; set; }

	[ServiceDependency(Optional = true)]
	public IDefaultCameraManager CameraManager { protected get; set; }

	[ServiceDependency(Optional = true)]
	public IDebuggingBag DebuggingBag { protected get; set; }

	[ServiceDependency(Optional = true)]
	public IEngineStateManager EngineState { protected get; set; }

	public TrileMaterializer(Trile trile)
		: this(trile, null)
	{
	}

	public TrileMaterializer(Trile trile, Mesh levelMesh)
		: this(trile, levelMesh, mutableSurfaces: false)
	{
	}

	public TrileMaterializer(Trile trile, Mesh levelMesh, bool mutableSurfaces)
	{
		ServiceHelper.InjectServices(this);
		this.trile = trile;
		if (mutableSurfaces)
		{
			surfaces = new List<TrixelSurface>();
			added = new HashSet<TrixelFace>();
			removed = new HashSet<TrixelFace>();
		}
		if (levelMesh != null)
		{
			group = levelMesh.AddGroup();
			tempInstances = new List<Vector4>();
			tempInstanceIds = new List<TrileInstance>();
			group.Geometry = geometry;
		}
	}

	public void Rebuild()
	{
		MarkMissingCells();
		UpdateSurfaces();
		RebuildGeometry();
	}

	public void MarkMissingCells()
	{
		added.Clear();
		removed.Clear();
		InitializeSurfaces();
		if (!NoTrixelsMode)
		{
			MarkRemoved(trile.MissingTrixels.Cells);
		}
	}

	private void InitializeSurfaces()
	{
		foreach (FaceOrientation value in Util.GetValues<FaceOrientation>())
		{
			TrixelEmplacement trixelEmplacement = new TrixelEmplacement(value.IsPositive() ? (value.AsVector() * (new Vector3(16f) - Vector3.One)) : Vector3.Zero);
			TrixelSurface trixelSurface = new TrixelSurface(value, trixelEmplacement);
			Vector3 mask = value.GetTangent().AsAxis().GetMask();
			int num = (int)Vector3.Dot(Vector3.One, mask) * 16;
			Vector3 mask2 = value.GetBitangent().AsAxis().GetMask();
			int num2 = (int)Vector3.Dot(Vector3.One, mask2) * 16;
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

	public void MarkAdded(IEnumerable<TrixelEmplacement> trixels)
	{
		Invalidate(trixels, trixelExists: true);
	}

	public void MarkRemoved(IEnumerable<TrixelEmplacement> trixels)
	{
		Invalidate(trixels, trixelExists: false);
	}

	private void Invalidate(IEnumerable<TrixelEmplacement> trixels, bool trixelExists)
	{
		foreach (TrixelEmplacement trixel in trixels)
		{
			for (int i = 0; i < 6; i++)
			{
				FaceOrientation face = (FaceOrientation)i;
				TrixelEmplacement traversed = trixel.GetTraversal(face);
				if (Trile.IsBorderTrixelFace(traversed))
				{
					if (surfaces.Any((TrixelSurface x) => x.Orientation == face && x.Trixels.Contains(trixel)))
					{
						removed.Add(new TrixelFace(trixel, face));
					}
					if (trixelExists)
					{
						added.Add(new TrixelFace(trixel, face));
					}
					continue;
				}
				FaceOrientation oppositeFace = face.GetOpposite();
				if (surfaces.Any((TrixelSurface x) => x.Orientation == oppositeFace && x.Trixels.Contains(traversed)))
				{
					removed.Add(new TrixelFace(traversed, oppositeFace));
				}
				if (!trixelExists)
				{
					added.Add(new TrixelFace(traversed, oppositeFace));
				}
			}
		}
	}

	public void UpdateSurfaces()
	{
		TrixelFace[] array = removed.ToArray();
		Array.Sort(array, InvalidTrixelFaceComparer);
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
		Array.Sort(array3, InvalidTrixelFaceComparer);
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
		foreach (TrixelSurface item3 in surfaces.Where((TrixelSurface x) => x.Dirty))
		{
			item3.RebuildParts();
		}
	}

	public void RebuildGeometry()
	{
		int num = surfaces.Sum((TrixelSurface x) => x.RectangularParts.Count);
		VertexGroup<VertexPositionNormalTextureInstance> vertexGroup = new VertexGroup<VertexPositionNormalTextureInstance>(num * 4);
		Dictionary<RectangularTrixelSurfacePart, FaceMaterialization<VertexPositionNormalTextureInstance>> dictionary = new Dictionary<RectangularTrixelSurfacePart, FaceMaterialization<VertexPositionNormalTextureInstance>>(num);
		Vector3 vector = new Vector3(0.5f);
		foreach (RectangularTrixelSurfacePart item in surfaces.SelectMany((TrixelSurface x) => x.RectangularParts))
		{
			Vector3 vector2 = item.Orientation.AsVector();
			Vector3 vector3 = item.Orientation.GetTangent().AsVector() * item.TangentSize / 16f;
			Vector3 vector4 = item.Orientation.GetBitangent().AsVector() * item.BitangentSize / 16f;
			Vector3 vector5 = item.Start.Position / 16f + ((item.Orientation >= FaceOrientation.Right) ? 1 : 0) * vector2 / 16f - vector;
			if (!dictionary.ContainsKey(item))
			{
				FaceMaterialization<VertexPositionNormalTextureInstance> faceMaterialization = default(FaceMaterialization<VertexPositionNormalTextureInstance>);
				faceMaterialization.V0 = vertexGroup.Reference(new VertexPositionNormalTextureInstance(vector5, vector2));
				faceMaterialization.V1 = vertexGroup.Reference(new VertexPositionNormalTextureInstance(vector5 + vector3, vector2));
				faceMaterialization.V2 = vertexGroup.Reference(new VertexPositionNormalTextureInstance(vector5 + vector3 + vector4, vector2));
				faceMaterialization.V3 = vertexGroup.Reference(new VertexPositionNormalTextureInstance(vector5 + vector4, vector2));
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
			array[num2].TextureCoordinate = array[num2].ComputeTexCoord() * ((EngineState != null && EngineState.InEditor) ? new Vector2(1.3333334f, 1f) : Vector2.One);
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
		if (geometry == null)
		{
			geometry = new ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4>(PrimitiveType.TriangleList, 200, appendIndex: true);
			if (group != null)
			{
				group.Geometry = geometry;
			}
		}
		geometry.Vertices = array;
		geometry.Indices = array2;
		geometry.InstancesDirty = true;
		DetermineFlags();
	}

	public void DetermineFlags()
	{
		if (group != null)
		{
			ActorType type = trile.ActorSettings.Type;
			group.CustomData = new TrileCustomData
			{
				Unstable = (type == ActorType.GoldenCube),
				TiltTwoAxis = (type == ActorType.CubeShard || type == ActorType.SecretCube || type == ActorType.PieceOfHeart),
				Shiny = (type == ActorType.CubeShard || type == ActorType.SkeletonKey || type == ActorType.SecretCube || type == ActorType.PieceOfHeart)
			};
			(group.CustomData as TrileCustomData).DetermineCustom();
		}
	}

	public void ClearBatch()
	{
		tempInstances.Clear();
		tempInstanceIds.Clear();
		tempInstances.TrimExcess();
		tempInstanceIds.TrimExcess();
		if (geometry != null)
		{
			geometry.ResetBuffers();
		}
	}

	public void ResetBatch()
	{
		BatchNeedsCommit = true;
		foreach (TrileInstance tempInstanceId in tempInstanceIds)
		{
			tempInstanceId.InstanceId = -1;
		}
		tempInstances.Clear();
		tempInstanceIds.Clear();
	}

	public void AddToBatch(TrileInstance instance)
	{
		BatchNeedsCommit = true;
		instance.InstanceId = tempInstances.Count;
		tempInstances.Add(instance.Enabled ? instance.Data.PositionPhi : OutOfSight);
		tempInstanceIds.Add(instance);
	}

	public void RemoveFromBatch(TrileInstance instance)
	{
		int instanceId = instance.InstanceId;
		if (instance != tempInstanceIds[instanceId])
		{
			int instanceId2;
			while ((instanceId2 = tempInstanceIds.IndexOf(instance)) != -1)
			{
				instance.InstanceId = instanceId2;
				RemoveFromBatch(instance);
			}
			return;
		}
		BatchNeedsCommit = true;
		for (int i = instanceId + 1; i < tempInstanceIds.Count; i++)
		{
			TrileInstance trileInstance = tempInstanceIds[i];
			if (trileInstance.InstanceId >= 0)
			{
				trileInstance.InstanceId--;
			}
		}
		tempInstances.RemoveAt(instanceId);
		tempInstanceIds.RemoveAt(instanceId);
		instance.InstanceId = -1;
	}

	public void CommitBatch()
	{
		if (geometry != null)
		{
			BatchNeedsCommit = false;
			int num = (int)Math.Ceiling((double)tempInstances.Count / 16.0) * 16;
			if (geometry.Instances == null || num > geometry.Instances.Length)
			{
				geometry.Instances = new Vector4[num];
			}
			tempInstances.CopyTo(geometry.Instances, 0);
			geometry.InstanceCount = tempInstances.Count;
			geometry.InstancesDirty = true;
			geometry.UpdateBuffers();
		}
	}

	public void UpdateInstance(TrileInstance instance)
	{
		int instanceId = instance.InstanceId;
		if (instanceId != -1 && geometry != null)
		{
			Vector4 vector = (instance.Enabled ? instance.Data.PositionPhi : OutOfSight);
			if (instanceId < geometry.Instances.Length)
			{
				geometry.Instances[instanceId] = vector;
				geometry.InstancesDirty = true;
			}
			if (instanceId < tempInstances.Count)
			{
				tempInstances[instanceId] = vector;
			}
		}
	}

	public void FakeUpdate(int instanceId, Vector4 data)
	{
		if (instanceId != -1 && geometry.Instances != null && instanceId < geometry.Instances.Length)
		{
			geometry.Instances[instanceId] = data;
			geometry.InstancesDirty = true;
		}
	}

	public void Dispose()
	{
		ClearBatch();
		group.Mesh.RemoveGroup(group, skipDispose: true);
	}
}
