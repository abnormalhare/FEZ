using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public abstract class LevelManager : GameComponent, ILevelManager
{
	private enum QueryResult
	{
		Nothing,
		Thin,
		Full
	}

	private class FastPointComparer : IEqualityComparer<Point>
	{
		public static readonly FastPointComparer Default = new FastPointComparer();

		public bool Equals(Point x, Point y)
		{
			if (x.X == y.X)
			{
				return x.Y == y.Y;
			}
			return false;
		}

		public int GetHashCode(Point obj)
		{
			return obj.X | (obj.Y << 16);
		}
	}

	private const bool AccurateCollision = false;

	private readonly Trile fallbackTrile;

	protected Level levelData;

	private Color actualAmbient;

	private Color actualDiffuse;

	private Dictionary<Point, Limit> screenSpaceLimits = new Dictionary<Point, Limit>(FastPointComparer.Default);

	private static readonly object Mutex = new object();

	private Worker<Dictionary<Point, Limit>> screenInvalidationWorker;

	public TrileFace StartingPosition
	{
		get
		{
			return levelData.StartingPosition;
		}
		set
		{
			levelData.StartingPosition = value;
		}
	}

	public string Name
	{
		get
		{
			return levelData.Name;
		}
		set
		{
			levelData.Name = value;
		}
	}

	public string FullPath { get; set; }

	public bool SkipPostProcess
	{
		get
		{
			return levelData.SkipPostProcess;
		}
		set
		{
			levelData.SkipPostProcess = value;
		}
	}

	public virtual float BaseAmbient
	{
		get
		{
			return levelData.BaseAmbient;
		}
		set
		{
			levelData.BaseAmbient = value;
		}
	}

	public virtual float BaseDiffuse
	{
		get
		{
			return levelData.BaseDiffuse;
		}
		set
		{
			levelData.BaseDiffuse = value;
		}
	}

	public Color ActualAmbient
	{
		get
		{
			return actualAmbient;
		}
		set
		{
			bool num = actualAmbient != value;
			actualAmbient = value;
			if (num)
			{
				OnLightingChanged();
			}
		}
	}

	public Color ActualDiffuse
	{
		get
		{
			return actualDiffuse;
		}
		set
		{
			bool num = actualDiffuse != value;
			actualDiffuse = value;
			if (num)
			{
				OnLightingChanged();
			}
		}
	}

	public bool Flat
	{
		get
		{
			return levelData.Flat;
		}
		set
		{
			levelData.Flat = value;
		}
	}

	public Vector3 Size
	{
		get
		{
			return levelData.Size;
		}
		set
		{
			levelData.Size = value;
		}
	}

	public string SequenceSamplesPath
	{
		get
		{
			return levelData.SequenceSamplesPath;
		}
		set
		{
			levelData.SequenceSamplesPath = value;
		}
	}

	public bool HaloFiltering
	{
		get
		{
			return levelData.HaloFiltering;
		}
		set
		{
			levelData.HaloFiltering = value;
		}
	}

	public string GomezHaloName
	{
		get
		{
			return levelData.GomezHaloName;
		}
		set
		{
			levelData.GomezHaloName = value;
		}
	}

	public bool BlinkingAlpha
	{
		get
		{
			return levelData.BlinkingAlpha;
		}
		set
		{
			levelData.BlinkingAlpha = value;
		}
	}

	public bool Loops
	{
		get
		{
			return levelData.Loops;
		}
		set
		{
			levelData.Loops = value;
		}
	}

	public float WaterHeight
	{
		get
		{
			return levelData.WaterHeight;
		}
		set
		{
			levelData.WaterHeight = value;
		}
	}

	public float OriginalWaterHeight { get; set; }

	public float WaterSpeed { get; set; }

	public LiquidType WaterType
	{
		get
		{
			return levelData.WaterType;
		}
		set
		{
			levelData.WaterType = value;
		}
	}

	public bool Descending
	{
		get
		{
			return levelData.Descending;
		}
		set
		{
			levelData.Descending = value;
		}
	}

	public bool Rainy
	{
		get
		{
			return levelData.Rainy;
		}
		set
		{
			levelData.Rainy = value;
		}
	}

	public string SongName
	{
		get
		{
			return levelData.SongName;
		}
		set
		{
			levelData.SongName = value;
		}
	}

	public bool LowPass
	{
		get
		{
			return levelData.LowPass;
		}
		set
		{
			levelData.LowPass = value;
		}
	}

	public LevelNodeType NodeType
	{
		get
		{
			return levelData.NodeType;
		}
		set
		{
			levelData.NodeType = value;
		}
	}

	public int FAPFadeOutStart
	{
		get
		{
			return levelData.FAPFadeOutStart;
		}
		set
		{
			levelData.FAPFadeOutStart = value;
		}
	}

	public int FAPFadeOutLength
	{
		get
		{
			return levelData.FAPFadeOutLength;
		}
		set
		{
			levelData.FAPFadeOutLength = value;
		}
	}

	public bool Quantum
	{
		get
		{
			return levelData.Quantum;
		}
		set
		{
			levelData.Quantum = value;
		}
	}

	public IDictionary<TrileEmplacement, TrileInstance> Triles => levelData.Triles;

	public Sky Sky => levelData.Sky;

	public TrileSet TrileSet => levelData.TrileSet;

	public TrackedSong Song => levelData.Song;

	public IDictionary<int, Volume> Volumes => levelData.Volumes;

	public IDictionary<int, ArtObjectInstance> ArtObjects => levelData.ArtObjects;

	public IDictionary<int, BackgroundPlane> BackgroundPlanes => levelData.BackgroundPlanes;

	public IDictionary<int, TrileGroup> Groups => levelData.Groups;

	public IDictionary<int, NpcInstance> NonPlayerCharacters => levelData.NonPlayerCharacters;

	public IDictionary<int, Script> Scripts => levelData.Scripts;

	public IDictionary<int, MovementPath> Paths => levelData.Paths;

	public IList<string> MutedLoops => levelData.MutedLoops;

	public IList<AmbienceTrack> AmbienceTracks => levelData.AmbienceTracks;

	public Dictionary<Point, Limit> ScreenSpaceLimits => screenSpaceLimits;

	public bool SkipInvalidation { get; set; }

	public bool IsInvalidatingScreen => screenInvalidationWorker != null;

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { protected get; set; }

	[ServiceDependency]
	public IFogManager FogManager { protected get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { protected get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { protected get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { protected get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { protected get; set; }

	public event Action LevelChanged = Util.NullAction;

	public event Action LevelChanging = Util.NullAction;

	public event Action LightingChanged = Util.NullAction;

	public event Action SkyChanged = Util.NullAction;

	public event Action ScreenInvalidated;

	public event Action<TrileInstance> TrileRestored = Util.NullAction;

	protected LevelManager(Game game)
		: base(game)
	{
		levelData = new Level
		{
			SkyName = "Blue"
		};
		fallbackTrile = new Trile(CollisionType.TopOnly)
		{
			Id = -1
		};
		base.UpdateOrder = -2;
	}

	public override void Initialize()
	{
		CameraManager.ViewpointChanged += InvalidateScreen;
		InvalidateScreen();
	}

	public abstract void Load(string levelName);

	public abstract void Rebuild();

	public void ClearArtSatellites()
	{
		foreach (ArtObjectInstance levelArtObject in LevelMaterializer.LevelArtObjects)
		{
			levelArtObject.Dispose(final: true);
		}
		foreach (BackgroundPlane value in BackgroundPlanes.Values)
		{
			value.Dispose();
		}
	}

	public Trile SafeGetTrile(int trileId)
	{
		if (trileId == -1)
		{
			return fallbackTrile;
		}
		if (TrileSet != null)
		{
			return TrileSet[trileId];
		}
		return fallbackTrile;
	}

	public TrileInstance TrileInstanceAt(ref TrileEmplacement id)
	{
		if (!Triles.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public bool TrileExists(TrileEmplacement emplacement)
	{
		return levelData.Triles.ContainsKey(emplacement);
	}

	protected void AddInstance(TrileEmplacement emplacement, TrileInstance instance)
	{
		levelData.Triles.Add(emplacement, instance);
		instance.Removed = false;
	}

	public virtual void RecordMoveToEnd(int groupId)
	{
	}

	public virtual bool IsPathRecorded(int groupId)
	{
		return false;
	}

	public bool IsCornerTrile(ref TrileEmplacement id, ref FaceOrientation face1, ref FaceOrientation face2)
	{
		TrileEmplacement traversal = id.GetTraversal(ref face1);
		if (!levelData.Triles.TryGetValue(traversal, out var value) || value.Trile.SeeThrough || value.ForceSeeThrough)
		{
			return true;
		}
		traversal = id.GetTraversal(ref face2);
		if (!levelData.Triles.TryGetValue(traversal, out value) || value.Trile.SeeThrough || value.ForceSeeThrough)
		{
			return true;
		}
		traversal = traversal.GetTraversal(ref face1);
		if (!levelData.Triles.TryGetValue(traversal, out value) || value.Trile.SeeThrough || value.ForceSeeThrough)
		{
			return true;
		}
		return false;
	}

	public bool IsBorderTrileFace(ref TrileEmplacement id, ref FaceOrientation face)
	{
		TrileEmplacement traversal = id.GetTraversal(ref face);
		if (levelData.Triles.TryGetValue(traversal, out var value) && !value.Trile.SeeThrough)
		{
			return value.ForceSeeThrough;
		}
		return true;
	}

	public bool IsBorderTrile(ref TrileEmplacement id)
	{
		bool flag = false;
		for (int i = 0; i < 6; i++)
		{
			if (flag)
			{
				break;
			}
			FaceOrientation face = (FaceOrientation)i;
			flag |= IsBorderTrileFace(ref id, ref face);
		}
		return flag;
	}

	public bool IsInRange(ref TrileEmplacement id)
	{
		Vector3 size = levelData.Size;
		if (id.X >= 0 && (float)id.X < size.X && id.Y >= 0 && (float)id.Y < size.Y && id.Z >= 0)
		{
			return (float)id.Z < size.Z;
		}
		return false;
	}

	public bool IsInRange(Vector3 position)
	{
		Vector3 size = levelData.Size;
		if (position.X >= 0f && position.X < size.X && position.Y >= 0f && position.Y < size.Y && position.Z >= 0f)
		{
			return position.Z < size.Z;
		}
		return false;
	}

	public bool VolumeExists(int id)
	{
		return levelData.Volumes.ContainsKey(id);
	}

	public void SwapTrile(TrileInstance instance, Trile newTrile)
	{
		LevelMaterializer.CullInstanceOut(instance);
		LevelMaterializer.RemoveInstance(instance);
		instance.TrileId = newTrile.Id;
		instance.RefreshTrile();
		LevelMaterializer.AddInstance(instance);
		LevelMaterializer.CullInstanceIn(instance);
	}

	public void RestoreTrile(TrileInstance instance)
	{
		if (!TrileExists(instance.Emplacement))
		{
			LevelMaterializer.AddInstance(instance);
			AddInstance(instance.Emplacement, instance);
			InvalidateScreenSpaceTile(instance.Emplacement);
			this.TrileRestored(instance);
		}
	}

	public bool ClearTrile(TrileInstance instance)
	{
		return ClearTrile(instance, skipRecull: false);
	}

	public bool ClearTrile(TrileInstance instance, bool skipRecull)
	{
		LevelMaterializer.RemoveInstance(instance);
		bool flag;
		if (Triles.TryGetValue(instance.Emplacement, out var value) && instance != value && value.OverlappedTriles != null)
		{
			flag = value.OverlappedTriles.Remove(instance);
		}
		else
		{
			flag = Triles.Remove(instance.Emplacement);
			if (flag && instance.Overlaps)
			{
				RestoreTrile(instance.PopOverlap());
			}
		}
		if (!flag)
		{
			foreach (TrileInstance value2 in Triles.Values)
			{
				if (value2.Overlaps && value2.OverlappedTriles.Contains(instance))
				{
					flag = value2.OverlappedTriles.Remove(instance);
					if (flag)
					{
						break;
					}
				}
			}
		}
		if (!flag)
		{
			foreach (KeyValuePair<TrileEmplacement, TrileInstance> trile in Triles)
			{
				if (trile.Value == instance)
				{
					flag = Triles.Remove(trile.Key);
					if (flag)
					{
						break;
					}
				}
			}
		}
		bool flag2 = false;
		foreach (TrileGroup value3 in Groups.Values)
		{
			flag2 |= value3.Triles.Remove(instance);
		}
		if (flag2)
		{
			int[] array = Groups.Keys.ToArray();
			foreach (int key in array)
			{
				if (Groups[key].Triles.Count == 0)
				{
					Groups.Remove(key);
				}
			}
		}
		if (!skipRecull)
		{
			LevelMaterializer.CullInstanceOut(instance, skipUnregister: true);
			RecullAt(instance);
		}
		instance.Removed = true;
		return flag;
	}

	public bool ClearTrile(TrileEmplacement emplacement)
	{
		if (Triles.TryGetValue(emplacement, out var value))
		{
			LevelMaterializer.RemoveInstance(value);
			bool flag = Triles.Remove(emplacement);
			value.Removed = true;
			LevelMaterializer.CullInstanceOut(value, skipUnregister: true);
			bool flag2 = false;
			foreach (TrileGroup value2 in Groups.Values)
			{
				flag2 |= value2.Triles.Remove(value);
			}
			if (flag2)
			{
				int[] array = Groups.Keys.ToArray();
				foreach (int key in array)
				{
					if (Groups[key].Triles.Count == 0)
					{
						Groups.Remove(key);
					}
				}
			}
			if (flag && value.Overlaps)
			{
				RestoreTrile(value.PopOverlap());
			}
			return true;
		}
		return false;
	}

	public void RecullAt(TrileInstance instance)
	{
		RecullAt(instance.Emplacement);
	}

	public void RecullAt(TrileEmplacement emplacement)
	{
		Viewpoint viewpoint = CameraManager.Viewpoint;
		if (viewpoint.IsOrthographic())
		{
			int x = ((viewpoint.SideMask() == Vector3.Right) ? emplacement.X : emplacement.Z);
			int y = emplacement.Y;
			RecullAt(new Point(x, y), skipCommit: false);
		}
	}

	public void RecullAt(Point ssPos, bool skipCommit)
	{
		WaitForScreenInvalidation();
		InvalidateScreenSpaceTile(ssPos);
		LevelMaterializer.FreeScreenSpace(ssPos.X, ssPos.Y);
		LevelMaterializer.FillScreenSpace(ssPos.X, ssPos.Y);
		LevelMaterializer.CommitBatchesIfNeeded();
	}

	protected void OnLevelChanged()
	{
		this.LevelChanged();
	}

	protected void OnLevelChanging()
	{
		this.LevelChanging();
		OnLightingChanged();
	}

	protected void OnSkyChanged()
	{
		this.SkyChanged();
	}

	protected virtual void OnLightingChanged()
	{
		this.LightingChanged();
	}

	public void AddPlane(BackgroundPlane plane)
	{
		lock (BackgroundPlanes)
		{
			int key = (plane.Id = IdentifierPool.FirstAvailable(BackgroundPlanes));
			BackgroundPlanes.Add(key, plane);
		}
	}

	public void RemovePlane(BackgroundPlane plane)
	{
		lock (BackgroundPlanes)
		{
			BackgroundPlanes.Remove(plane.Id);
			plane.Dispose();
		}
	}

	public TrileInstance ActualInstanceAt(Vector3 position)
	{
		Vector3 vector = CameraManager.Viewpoint.ForwardVector();
		bool flag = vector.Z != 0f;
		bool flag2 = flag;
		int forwardSign = (flag ? ((int)vector.Z) : ((int)vector.X));
		Vector3 screenSpacePosition = new Vector3(flag2 ? position.X : position.Z, position.Y, flag ? position.Z : position.X);
		TrileEmplacement emplacement = new TrileEmplacement((int)Math.Floor(position.X), (int)Math.Floor(position.Y), (int)Math.Floor(position.Z));
		float num = FezMath.Frac(screenSpacePosition.Z);
		QueryResult queryResult;
		TrileInstance trileInstance = OffsetInstanceAt(emplacement, screenSpacePosition, flag, forwardSign, useSelector: false, keepNearest: false, QueryOptions.None, out queryResult);
		if (trileInstance == null)
		{
			if (!(num < 0.5f))
			{
				return OffsetInstanceAt(emplacement.GetOffset((!flag) ? 1 : 0, 0, flag ? 1 : 0), screenSpacePosition, flag, forwardSign, useSelector: false, keepNearest: false, QueryOptions.None, out queryResult);
			}
			trileInstance = OffsetInstanceAt(emplacement.GetOffset((!flag) ? (-1) : 0, 0, flag ? (-1) : 0), screenSpacePosition, flag, forwardSign, useSelector: false, keepNearest: false, QueryOptions.None, out queryResult);
		}
		return trileInstance;
	}

	public NearestTriles NearestTrile(Vector3 position)
	{
		return NearestTrile(position, QueryOptions.None);
	}

	public NearestTriles NearestTrile(Vector3 position, QueryOptions options)
	{
		return NearestTrile(position, options, null);
	}

	public NearestTriles NearestTrile(Vector3 position, QueryOptions options, Viewpoint? vp)
	{
		NearestTriles result = default(NearestTriles);
		bool hasValue = vp.HasValue;
		Viewpoint viewpoint = (hasValue ? vp.Value : CameraManager.Viewpoint);
		if (!hasValue)
		{
			WaitForScreenInvalidation();
		}
		bool flag = viewpoint == Viewpoint.Front || viewpoint == Viewpoint.Back;
		bool flag2 = (options & QueryOptions.Background) == QueryOptions.Background;
		bool flag3 = (options & QueryOptions.Simple) == QueryOptions.Simple;
		TrileEmplacement emplacement = new TrileEmplacement((int)position.X, (int)position.Y, (int)position.Z);
		Vector3 vector = viewpoint.ForwardVector();
		int num = (flag ? ((int)vector.Z) : ((int)vector.X));
		Vector3 screenSpacePosition = new Vector3(flag ? position.X : position.Z, position.Y, -1f);
		Point key = ((!flag) ? new Point(emplacement.Z, emplacement.Y) : new Point(emplacement.X, emplacement.Y));
		Limit value = default(Limit);
		int num3;
		if (hasValue)
		{
			num = (flag ? ((int)vector.Z) : ((int)vector.X));
			if (flag2)
			{
				num *= -1;
			}
			float num2 = ((flag ? Size.Z : Size.X) - 1f) / 2f;
			if (flag)
			{
				emplacement.Z = (int)(num2 - (float)num * num2);
			}
			else
			{
				emplacement.X = (int)(num2 - (float)num * num2);
			}
			num3 = (int)(num2 + (float)num * num2);
		}
		else if (flag3)
		{
			if (!screenSpaceLimits.TryGetValue(key, out value))
			{
				return result;
			}
			int num4 = (flag2 ? value.End : value.Start);
			if (flag)
			{
				emplacement.Z = num4;
			}
			else
			{
				emplacement.X = num4;
			}
			num3 = (flag2 ? value.Start : value.End);
			if (flag2)
			{
				num *= -1;
			}
		}
		else
		{
			Limit value2;
			bool flag4 = screenSpaceLimits.TryGetValue(key, out value2);
			int num5 = ((FezMath.Frac(screenSpacePosition.X) > 0.5f) ? 1 : (-1));
			int num6 = ((FezMath.Frac(screenSpacePosition.Y) > 0.5f) ? 1 : (-1));
			key.X += num5;
			Limit value3;
			bool flag5 = screenSpaceLimits.TryGetValue(key, out value3);
			key.X -= num5;
			key.Y += num6;
			Limit value4;
			bool flag6 = screenSpaceLimits.TryGetValue(key, out value4);
			if (!flag4 && !flag6 && !flag5)
			{
				return result;
			}
			if (flag4)
			{
				value = value2;
				if (!flag5 && !flag6)
				{
					flag3 = true;
				}
			}
			else
			{
				value.Start = ((num == 1) ? int.MaxValue : int.MinValue);
				value.End = ((num == 1) ? int.MinValue : int.MaxValue);
				value.NoOffset = true;
			}
			if (flag5)
			{
				value.Start = ((num == 1) ? Math.Min(value.Start, value3.Start) : Math.Max(value.Start, value3.Start));
				value.End = ((num == 1) ? Math.Max(value.End, value3.End) : Math.Min(value.End, value3.End));
			}
			if (flag6)
			{
				value.Start = ((num == 1) ? Math.Min(value.Start, value4.Start) : Math.Max(value.Start, value4.Start));
				value.End = ((num == 1) ? Math.Max(value.End, value4.End) : Math.Min(value.End, value4.End));
			}
			int num7 = (flag2 ? value.End : value.Start);
			if (flag)
			{
				emplacement.Z = num7;
			}
			else
			{
				emplacement.X = num7;
			}
			num3 = (flag2 ? value.Start : value.End);
			if (flag2)
			{
				num *= -1;
			}
		}
		num3 += num;
		bool flag7 = (flag ? (emplacement.Z != num3) : (emplacement.X != num3));
		QueryResult nearestQueryResult;
		if (flag)
		{
			while (flag7)
			{
				TrileInstance trileInstance = OffsetInstanceAt(ref emplacement, ref screenSpacePosition, depthIsZ: true, num, useSelector: true, keepNearest: false, flag3, options, out nearestQueryResult);
				if (trileInstance != null)
				{
					if (nearestQueryResult == QueryResult.Full)
					{
						result.Deep = trileInstance;
						break;
					}
					if (result.Surface == null)
					{
						result.Surface = trileInstance;
					}
				}
				emplacement.Z += num;
				flag7 = emplacement.Z != num3;
			}
		}
		else
		{
			while (flag7)
			{
				TrileInstance trileInstance2 = OffsetInstanceAt(ref emplacement, ref screenSpacePosition, depthIsZ: false, num, useSelector: true, keepNearest: false, flag3, options, out nearestQueryResult);
				if (trileInstance2 != null)
				{
					if (nearestQueryResult == QueryResult.Full)
					{
						result.Deep = trileInstance2;
						break;
					}
					if (result.Surface == null)
					{
						result.Surface = trileInstance2;
					}
				}
				emplacement.X += num;
				flag7 = emplacement.X != num3;
			}
		}
		return result;
	}

	private TrileInstance OffsetInstanceAt(TrileEmplacement emplacement, Vector3 screenSpacePosition, bool depthIsZ, int forwardSign, bool useSelector, bool keepNearest, QueryOptions context, out QueryResult queryResult)
	{
		return OffsetInstanceAt(ref emplacement, ref screenSpacePosition, depthIsZ, forwardSign, useSelector, keepNearest, simpleTest: false, context, out queryResult);
	}

	private TrileInstance OffsetInstanceAt(ref TrileEmplacement emplacement, ref Vector3 screenSpacePosition, bool depthIsZ, int forwardSign, bool useSelector, bool keepNearest, bool simpleTest, QueryOptions context, out QueryResult nearestQueryResult)
	{
		QueryResult queryResult = QueryResult.Nothing;
		if (Triles.TryGetValue(emplacement, out var value))
		{
			value = OffsetInstanceOrOverlapsContain(value, screenSpacePosition, depthIsZ, forwardSign, useSelector, context, out queryResult);
		}
		if (simpleTest || (value != null && !keepNearest))
		{
			nearestQueryResult = queryResult;
			return value;
		}
		TrileInstance trileInstance = value;
		nearestQueryResult = queryResult;
		int num = ((FezMath.Frac(screenSpacePosition.X) > 0.5f) ? 1 : (-1));
		TrileEmplacement id = (depthIsZ ? emplacement.GetOffset(num, 0, 0) : emplacement.GetOffset(0, 0, num));
		value = TrileInstanceAt(ref id);
		if (value != null)
		{
			value = OffsetInstanceOrOverlapsContain(value, screenSpacePosition, depthIsZ, forwardSign, useSelector, context, out queryResult);
			if (value != null)
			{
				nearestQueryResult = queryResult;
				if (!keepNearest)
				{
					return value;
				}
				trileInstance = KeepNearestInstance(trileInstance, value, depthIsZ, forwardSign);
			}
		}
		int offsetY = ((FezMath.Frac(screenSpacePosition.Y) > 0.5f) ? 1 : (-1));
		TrileEmplacement id2 = emplacement.GetOffset(0, offsetY, 0);
		value = TrileInstanceAt(ref id2);
		if (value != null)
		{
			value = OffsetInstanceOrOverlapsContain(value, screenSpacePosition, depthIsZ, forwardSign, useSelector, context, out queryResult);
			if (value != null)
			{
				nearestQueryResult = queryResult;
				if (!keepNearest)
				{
					return value;
				}
				trileInstance = KeepNearestInstance(trileInstance, value, depthIsZ, forwardSign);
			}
		}
		id2 = id.GetOffset(0, offsetY, 0);
		value = TrileInstanceAt(ref id2);
		if (value != null)
		{
			value = OffsetInstanceOrOverlapsContain(value, screenSpacePosition, depthIsZ, forwardSign, useSelector, context, out queryResult);
			if (value != null)
			{
				nearestQueryResult = queryResult;
				if (!keepNearest)
				{
					return value;
				}
				trileInstance = KeepNearestInstance(trileInstance, value, depthIsZ, forwardSign);
			}
		}
		return trileInstance;
	}

	private TrileInstance OffsetInstanceOrOverlapsContain(TrileInstance instance, Vector3 screenSpacePosition, bool depthIsZ, int forwardSign, bool useSelector, QueryOptions context, out QueryResult queryResult)
	{
		TrileInstance trileInstance = null;
		queryResult = QueryResult.Full;
		QueryResult queryResult2 = QueryResult.Nothing;
		if (OffsetInstanceContains(screenSpacePosition, instance, depthIsZ) && (!useSelector || InstanceMaterialForQuery(instance, context, out queryResult)))
		{
			trileInstance = instance;
			queryResult2 = queryResult;
		}
		if (instance.Overlaps)
		{
			foreach (TrileInstance overlappedTrile in instance.OverlappedTriles)
			{
				if (OffsetInstanceContains(screenSpacePosition, overlappedTrile, depthIsZ) && (!useSelector || InstanceMaterialForQuery(overlappedTrile, context, out queryResult)))
				{
					trileInstance = KeepNearestInstance(trileInstance, overlappedTrile, depthIsZ, forwardSign);
					queryResult2 = queryResult;
				}
			}
		}
		queryResult = queryResult2;
		return trileInstance;
	}

	private static bool OffsetInstanceContains(Vector3 screenSpacePosition, TrileInstance instance, bool depthIsZ)
	{
		Vector3 center = instance.Center;
		Vector3 transformedSize = instance.TransformedSize;
		Vector3 vector = new Vector3(depthIsZ ? center.X : center.Z, center.Y, depthIsZ ? center.Z : center.X);
		Vector3 vector2 = new Vector3(depthIsZ ? (transformedSize.X / 2f) : (transformedSize.Z / 2f), transformedSize.Y / 2f, depthIsZ ? (transformedSize.Z / 2f) : (transformedSize.X / 2f));
		if (screenSpacePosition.X > vector.X - vector2.X && screenSpacePosition.X < vector.X + vector2.X && screenSpacePosition.Y >= vector.Y - vector2.Y && screenSpacePosition.Y < vector.Y + vector2.Y)
		{
			if (screenSpacePosition.Z != -1f)
			{
				if (screenSpacePosition.Z > vector.Z - vector2.Z)
				{
					return screenSpacePosition.Z < vector.Z + vector2.Z;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private bool InstanceMaterialForQuery(TrileInstance instance, QueryOptions options, out QueryResult queryResult)
	{
		Trile trile = instance.Trile;
		CollisionType rotatedFace = instance.GetRotatedFace(((options & QueryOptions.Background) == QueryOptions.Background) ? CameraManager.VisibleOrientation.GetOpposite() : CameraManager.VisibleOrientation);
		if (!trile.Immaterial && (instance.PhysicsState == null || !instance.PhysicsState.UpdatingPhysics) && rotatedFace != CollisionType.Immaterial)
		{
			queryResult = (trile.Thin ? QueryResult.Thin : QueryResult.Full);
		}
		else
		{
			queryResult = QueryResult.Nothing;
		}
		return queryResult != QueryResult.Nothing;
	}

	private static TrileInstance KeepNearestInstance(TrileInstance nearest, TrileInstance contender, bool depthIsZ, int forwardSign)
	{
		Vector3 vector = (depthIsZ ? Vector3.UnitZ : Vector3.UnitX) * -forwardSign;
		float num = ((nearest == null) ? float.MinValue : (nearest.Center + nearest.TransformedSize * vector / 2f).Dot(vector));
		float num2 = (contender.Center + contender.TransformedSize * vector / 2f).Dot(vector);
		if (!(num > num2))
		{
			return contender;
		}
		return nearest;
	}

	public IEnumerable<Trile> ActorTriles(ActorType type)
	{
		if (TrileSet != null)
		{
			return TrileSet.Triles.Values.Where((Trile x) => x.ActorSettings.Type == type);
		}
		return Enumerable.Repeat<Trile>(null, 1);
	}

	public IEnumerable<string> LinkedLevels()
	{
		return levelData.Scripts.Values.Select((Script script) => from action in script.Actions
			where action.Object.Type == "Level"
			select action.Arguments.FirstOrDefault()).SelectMany((IEnumerable<string> x) => x);
	}

	public void UpdateInstance(TrileInstance instance)
	{
		if (instance.LastUpdatePosition.Round() != instance.Position.Round())
		{
			TrileEmplacement trileEmplacement = new TrileEmplacement(instance.LastUpdatePosition);
			TrileInstance value2;
			if (Triles.TryGetValue(trileEmplacement, out var value))
			{
				if (value == instance)
				{
					Triles.Remove(trileEmplacement);
					if (instance.Overlaps)
					{
						value2 = instance.PopOverlap();
						AddInstance(trileEmplacement, value2);
					}
				}
				else if (value.Overlaps && value.OverlappedTriles.Contains(instance))
				{
					value.OverlappedTriles.Remove(instance);
				}
			}
			if (Triles.TryGetValue(instance.Emplacement, out value2))
			{
				instance.PushOverlap(value2);
				Triles.Remove(instance.Emplacement);
			}
			LevelMaterializer.UpdateInstance(instance);
			Triles.Add(instance.Emplacement, instance);
			instance.Update();
			LevelMaterializer.UpdateRow(trileEmplacement, instance);
			if (!IsInvalidatingScreen)
			{
				InvalidateScreenSpaceTile(trileEmplacement);
				InvalidateScreenSpaceTile(instance.Emplacement);
			}
		}
		if (instance.InstanceId != -1)
		{
			LevelMaterializer.GetTrileMaterializer(instance.VisualTrile).UpdateInstance(instance);
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (screenInvalidationWorker == null && this.ScreenInvalidated != null && !EngineState.Loading)
		{
			if (SkipInvalidation)
			{
				this.ScreenInvalidated = null;
				return;
			}
			this.ScreenInvalidated();
			this.ScreenInvalidated = null;
		}
	}

	public void WaitForScreenInvalidation()
	{
		while (screenInvalidationWorker != null)
		{
			Thread.Sleep(0);
		}
	}

	public void AbortInvalidation()
	{
		if (screenInvalidationWorker != null)
		{
			screenInvalidationWorker.Abort();
		}
	}

	private void InvalidateScreenSpaceTile(TrileEmplacement emplacement)
	{
		Vector3 b = CameraManager.Viewpoint.SideMask();
		Point ssPos = new Point((int)emplacement.AsVector.Dot(b), emplacement.Y);
		InvalidateScreenSpaceTile(ssPos);
	}

	private void InvalidateScreenSpaceTile(Point ssPos)
	{
		WaitForScreenInvalidation();
		screenSpaceLimits.Remove(ssPos);
		FillScreenSpaceTile(ssPos, screenSpaceLimits);
	}

	private void InvalidateScreen()
	{
		if (SkipInvalidation)
		{
			return;
		}
		lock (Mutex)
		{
			if (screenInvalidationWorker != null)
			{
				screenInvalidationWorker.Abort();
			}
		}
		WaitForScreenInvalidation();
		Dictionary<Point, Limit> newLimits = new Dictionary<Point, Limit>(screenSpaceLimits.Count, FastPointComparer.Default);
		screenInvalidationWorker = ThreadPool.Take<Dictionary<Point, Limit>>(DoInvalidateScreen);
		screenInvalidationWorker.Priority = ThreadPriority.Normal;
		screenInvalidationWorker.Finished += delegate
		{
			lock (Mutex)
			{
				if (screenInvalidationWorker != null)
				{
					if (!screenInvalidationWorker.Aborted)
					{
						screenSpaceLimits = newLimits;
					}
					ThreadPool.Return(screenInvalidationWorker);
					screenInvalidationWorker = null;
				}
			}
		};
		screenInvalidationWorker.Start(newLimits);
	}

	private void DoInvalidateScreen(Dictionary<Point, Limit> newLimits)
	{
		float num = Size.Dot(CameraManager.Viewpoint.SideMask());
		for (int i = 0; (float)i < num; i++)
		{
			for (int j = 0; (float)j < Size.Y; j++)
			{
				if (screenInvalidationWorker.Aborted)
				{
					return;
				}
				FillScreenSpaceTile(new Point(i, j), newLimits);
			}
		}
	}

	private void FillScreenSpaceTile(Point p, IDictionary<Point, Limit> newLimits)
	{
		Vector3 vector = CameraManager.Viewpoint.ForwardVector();
		bool flag = vector.Z != 0f;
		bool flag2 = flag;
		int num = (flag ? ((int)vector.Z) : ((int)vector.X));
		float num2 = ((flag ? Size.Z : Size.X) - 1f) / 2f;
		int num3 = (int)(num2 + (float)num * num2) + num;
		Limit limit = default(Limit);
		limit.Start = (int)(num2 - (float)num * num2);
		limit.End = num3;
		limit.NoOffset = true;
		Limit value = limit;
		TrileEmplacement key = new TrileEmplacement(flag2 ? p.X : value.Start, p.Y, flag ? value.Start : p.X);
		bool flag3 = true;
		bool flag4 = false;
		while (flag3)
		{
			if (Triles.TryGetValue(key, out var value2))
			{
				value.NoOffset &= value2.Position == value2.Emplacement.AsVector;
				int num4 = (flag ? key.Z : key.X);
				if (!flag4)
				{
					value.Start = num4;
					flag4 = true;
				}
				value.End = num4;
			}
			if (flag)
			{
				key.Z += num;
				flag3 = key.Z != num3;
			}
			else
			{
				key.X += num;
				flag3 = key.X != num3;
			}
		}
		if (value.End != num3)
		{
			if (newLimits.ContainsKey(p))
			{
				newLimits.Remove(p);
			}
			newLimits.Add(p, value);
		}
	}

	public void PrepareFullCull()
	{
		LevelMaterializer.PrepareFullCull();
	}

	public abstract bool WasPathSupposedToBeRecorded(int id);
}
