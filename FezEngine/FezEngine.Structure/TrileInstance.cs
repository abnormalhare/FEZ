using System;
using System.Collections.Generic;
using ContentSerialization.Attributes;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class TrileInstance
{
	private static readonly ILevelManager LevelManager;

	private Vector3 cachedPosition;

	private TrileEmplacement cachedEmplacement;

	private Vector3 lastUpdatePosition = -Vector3.One;

	private TrileInstanceData data;

	private Quaternion phiQuat;

	private FaceOrientation phiOri;

	private bool hasPhi;

	private static readonly Quaternion[] QuatLookup;

	private static readonly FaceOrientation[] OrientationLookup;

	[Serialization(Ignore = true)]
	public Trile Trile;

	[Serialization(Ignore = true)]
	public Trile VisualTrile;

	[Serialization(Name = "Id", UseAttribute = true)]
	public int TrileId { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public InstanceActorSettings ActorSettings { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float Phi
	{
		get
		{
			return data.PositionPhi.W;
		}
		set
		{
			data.PositionPhi.W = value;
			phiQuat = FezMath.QuaternionFromPhi(value);
			phiOri = FezMath.OrientationFromPhi(value);
			hasPhi = value != 0f;
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public List<TrileInstance> OverlappedTriles { get; set; }

	[Serialization(Optional = true)]
	public Vector3 Position
	{
		get
		{
			return cachedPosition;
		}
		set
		{
			data.PositionPhi.X = value.X;
			data.PositionPhi.Y = value.Y;
			data.PositionPhi.Z = value.Z;
			cachedPosition = value;
			if (PhysicsState != null)
			{
				PhysicsState.Center = Center;
			}
			cachedEmplacement = new TrileEmplacement(cachedPosition);
		}
	}

	[Serialization(Ignore = true)]
	public int? VisualTrileId { get; set; }

	[Serialization(Ignore = true)]
	public int InstanceId { get; set; }

	[Serialization(Ignore = true)]
	public bool Enabled { get; set; }

	[Serialization(Ignore = true)]
	public bool Removed { get; set; }

	[Serialization(Ignore = true)]
	public bool Collected { get; set; }

	[Serialization(Ignore = true)]
	public bool Hidden { get; set; }

	[Serialization(Ignore = true)]
	public bool Foreign { get; set; }

	[Serialization(Ignore = true)]
	public bool GlobalSpawn { get; set; }

	[Serialization(Ignore = true)]
	public bool ForceSeeThrough { get; set; }

	[Serialization(Ignore = true)]
	public bool ForceTopMaybe { get; set; }

	[Serialization(Ignore = true)]
	public bool ForceClampToGround { get; set; }

	[Serialization(Ignore = true)]
	public bool SkipCulling { get; set; }

	[Serialization(Ignore = true)]
	public bool NeedsRandomCleanup { get; set; }

	[Serialization(Ignore = true)]
	public bool RandomTracked { get; set; }

	[Serialization(Ignore = true)]
	public float LastTreasureSin { get; set; }

	[Serialization(Ignore = true)]
	internal Vector3 LastUpdatePosition => lastUpdatePosition;

	[Serialization(Ignore = true)]
	public InstancePhysicsState PhysicsState { get; set; }

	[Serialization(Ignore = true)]
	public TrileEmplacement Emplacement
	{
		get
		{
			return cachedEmplacement;
		}
		set
		{
			data.PositionPhi.X = value.X;
			data.PositionPhi.Y = value.Y;
			data.PositionPhi.Z = value.Z;
			cachedPosition = new Vector3(value.X, value.Y, value.Z);
			if (PhysicsState != null)
			{
				PhysicsState.Center = Center;
			}
			cachedEmplacement = value;
		}
	}

	public TrileInstanceData Data => data;

	[Serialization(Ignore = true)]
	public bool IsMovingGroup { get; set; }

	[Serialization(Ignore = true)]
	public TrileEmplacement OriginalEmplacement { get; set; }

	[Serialization(Ignore = true)]
	public bool Unsafe { get; set; }

	[Serialization(Ignore = true)]
	public Point? OldSsEmplacement { get; set; }

	public bool Overlaps
	{
		get
		{
			if (OverlappedTriles != null)
			{
				return OverlappedTriles.Count > 0;
			}
			return false;
		}
	}

	public Vector3 TransformedSize
	{
		get
		{
			if (phiOri == FaceOrientation.Left || phiOri == FaceOrientation.Right)
			{
				return Trile.Size.ZYX();
			}
			return Trile.Size;
		}
	}

	public Vector3 Center
	{
		get
		{
			Vector3 vector = (hasPhi ? Vector3.Transform(Trile.Offset, phiQuat) : Trile.Offset);
			return new Vector3(vector.X * 0.5f + 0.5f + cachedPosition.X, vector.Y * 0.5f + 0.5f + cachedPosition.Y, vector.Z * 0.5f + 0.5f + cachedPosition.Z);
		}
	}

	static TrileInstance()
	{
		QuatLookup = new Quaternion[4]
		{
			FezMath.QuaternionFromPhi(-(float)Math.PI),
			FezMath.QuaternionFromPhi(-(float)Math.PI / 2f),
			FezMath.QuaternionFromPhi(0f),
			FezMath.QuaternionFromPhi((float)Math.PI / 2f)
		};
		OrientationLookup = new FaceOrientation[4]
		{
			FaceOrientation.Back,
			FaceOrientation.Left,
			FaceOrientation.Front,
			FaceOrientation.Right
		};
		if (ServiceHelper.IsFull)
		{
			LevelManager = ServiceHelper.Get<ILevelManager>();
		}
	}

	public TrileInstance()
	{
		ActorSettings = new InstanceActorSettings();
		Enabled = true;
		InstanceId = -1;
	}

	public TrileInstance(TrileEmplacement emplacement, int trileId)
		: this(emplacement.AsVector, trileId)
	{
	}

	public TrileInstance(Vector3 position, int trileId)
		: this()
	{
		data.PositionPhi = new Vector4(position, 0f);
		cachedPosition = position;
		cachedEmplacement = new TrileEmplacement(position);
		Update();
		TrileId = trileId;
		RefreshTrile();
	}

	public void SetPhiLight(float phi)
	{
		data.PositionPhi.W = phi;
	}

	public void SetPhiLight(byte orientation)
	{
		data.PositionPhi.W = (float)(orientation - 2) * ((float)Math.PI / 2f);
		phiQuat = QuatLookup[orientation];
		phiOri = OrientationLookup[orientation];
		hasPhi = orientation != 2;
	}

	public void Update()
	{
		lastUpdatePosition = Position;
	}

	public TrileInstance PopOverlap()
	{
		if (OverlappedTriles == null || OverlappedTriles.Count == 0)
		{
			throw new InvalidOperationException();
		}
		int num = OverlappedTriles.Count - 1;
		TrileInstance trileInstance = OverlappedTriles[num];
		OverlappedTriles.RemoveAt(num);
		int num2 = num - 1;
		while (num2 >= 0 && OverlappedTriles.Count > num2)
		{
			trileInstance.PushOverlap(OverlappedTriles[num2]);
			num2--;
		}
		OverlappedTriles.Clear();
		return trileInstance;
	}

	public void PushOverlap(TrileInstance instance)
	{
		if (OverlappedTriles == null)
		{
			OverlappedTriles = new List<TrileInstance>();
		}
		OverlappedTriles.Add(instance);
		if (instance.Overlaps)
		{
			OverlappedTriles.AddRange(instance.OverlappedTriles);
			instance.OverlappedTriles.Clear();
		}
	}

	public void ResetTrile()
	{
		VisualTrile = (Trile = null);
		if (LevelManager.TrileSet != null && !LevelManager.TrileSet.Triles.ContainsKey(TrileId))
		{
			TrileId = -1;
		}
		RefreshTrile();
	}

	public void RefreshTrile()
	{
		Trile = LevelManager.SafeGetTrile(TrileId);
		if (VisualTrileId.HasValue)
		{
			VisualTrile = LevelManager.SafeGetTrile(VisualTrileId.Value);
		}
		else
		{
			VisualTrile = Trile;
		}
	}

	public CollisionType GetRotatedFace(FaceOrientation face)
	{
		FaceOrientation key = FezMath.OrientationFromPhi(face.ToPhi() - Phi);
		CollisionType collisionType = Trile.Faces[key];
		if (collisionType == CollisionType.TopOnly)
		{
			TrileEmplacement emplacement = Emplacement;
			emplacement.Y++;
			Vector3 mask = face.AsAxis().GetMask();
			if (LevelManager.Triles.TryGetValue(emplacement, out var value) && value.Enabled && !value.IsMovingGroup && (value.Trile.Geometry == null || !value.Trile.Geometry.Empty || value.Trile.Faces[key] != CollisionType.None) && !value.Trile.Immaterial && value.Trile.Faces[key] != CollisionType.Immaterial && !value.Trile.Thin && !value.Trile.ActorSettings.Type.IsPickable() && (value.Trile.Size.Y == 1f || value.ForceTopMaybe) && FezMath.AlmostEqual(value.Center.Dot(mask), Center.Dot(mask)))
			{
				collisionType = CollisionType.None;
			}
		}
		return collisionType;
	}

	public TrileInstance Clone()
	{
		TrileInstance trileInstance = new TrileInstance(Position, TrileId)
		{
			ActorSettings = new InstanceActorSettings(ActorSettings),
			TrileId = TrileId,
			Phi = Phi
		};
		if (Overlaps)
		{
			trileInstance.OverlappedTriles = new List<TrileInstance>();
			foreach (TrileInstance overlappedTrile in OverlappedTriles)
			{
				trileInstance.OverlappedTriles.Add(overlappedTrile.Clone());
			}
		}
		return trileInstance;
	}
}
