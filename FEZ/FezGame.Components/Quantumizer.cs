using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class Quantumizer : GameComponent
{
	private readonly List<TrileInstance> BatchedInstances = new List<TrileInstance>();

	private readonly List<TrileInstance> RandomInstances = new List<TrileInstance>();

	private readonly List<TrileInstance> CleanInstances = new List<TrileInstance>();

	private readonly List<Vector4> AllEmplacements = new List<Vector4>();

	private static readonly Random Random = new Random();

	private int[] RandomTrileIds;

	private int FreezeFrames;

	private readonly HashSet<Point> SsPosToRecull = new HashSet<Point>();

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public Quantumizer(Game game)
		: base(game)
	{
		base.UpdateOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		BatchedInstances.Clear();
		CleanInstances.Clear();
		AllEmplacements.Clear();
		AllEmplacements.TrimExcess();
		CleanInstances.TrimExcess();
		BatchedInstances.TrimExcess();
		RandomTrileIds = null;
		if (base.Enabled)
		{
			LevelMaterializer.TrileInstanceBatched -= BatchInstance;
		}
		base.Enabled = false;
		if (!LevelManager.Quantum || LevelManager.TrileSet == null)
		{
			return;
		}
		base.Enabled = true;
		List<int> list = (from x in LevelMaterializer.MaterializedTriles
			where x.Geometry != null && !x.Geometry.Empty && !x.ActorSettings.Type.IsTreasure() && x.ActorSettings.Type != ActorType.SplitUpCube
			select x.Id).ToList();
		RandomTrileIds = new int[250];
		int num = 0;
		for (int i = 0; i < 250; i++)
		{
			int index = Random.Next(0, list.Count);
			int num2 = list[index];
			RandomTrileIds[num++] = num2;
			LevelManager.TrileSet[num2].ForceKeep = true;
			list.RemoveAt(index);
		}
		list = null;
		Trile trile = LevelManager.TrileSet.Triles.Values.FirstOrDefault((Trile x) => x.Name == "__QIPT");
		if (trile == null)
		{
			trile = new Trile(CollisionType.None)
			{
				Name = "__QIPT",
				Immaterial = true,
				SeeThrough = true,
				Thin = true,
				TrileSet = LevelManager.TrileSet,
				MissingTrixels = null,
				Id = IdentifierPool.FirstAvailable(LevelManager.TrileSet.Triles)
			};
			LevelManager.TrileSet.Triles.Add(trile.Id, trile);
			LevelMaterializer.RebuildTrile(trile);
		}
		List<int> list2 = new List<int>();
		bool flag = LevelManager.Size.X > LevelManager.Size.Z;
		float[] array = new float[4]
		{
			0f,
			(float)Math.PI / 2f,
			(float)Math.PI,
			4.712389f
		};
		for (int j = 0; (float)j < LevelManager.Size.Y; j++)
		{
			if (flag)
			{
				list2.Clear();
				list2.AddRange(Enumerable.Range(0, (int)LevelManager.Size.Z));
				for (int k = 0; (float)k < LevelManager.Size.X; k++)
				{
					int z;
					if (list2.Count > 0)
					{
						int index2 = RandomHelper.Random.Next(0, list2.Count);
						z = list2[index2];
						list2.RemoveAt(index2);
					}
					else
					{
						z = RandomHelper.Random.Next(0, (int)LevelManager.Size.Z);
					}
					LevelManager.RestoreTrile(new TrileInstance(new TrileEmplacement(k, j, z), trile.Id)
					{
						Phi = array[Random.Next(0, 4)]
					});
				}
				while (list2.Count > 0)
				{
					int index3 = RandomHelper.Random.Next(0, list2.Count);
					int z2 = list2[index3];
					list2.RemoveAt(index3);
					LevelManager.RestoreTrile(new TrileInstance(new TrileEmplacement(RandomHelper.Random.Next(0, (int)LevelManager.Size.X), j, z2), trile.Id)
					{
						Phi = array[Random.Next(0, 4)]
					});
				}
				continue;
			}
			list2.Clear();
			list2.AddRange(Enumerable.Range(0, (int)LevelManager.Size.X));
			for (int l = 0; (float)l < LevelManager.Size.Z; l++)
			{
				int x2;
				if (list2.Count > 0)
				{
					int index4 = RandomHelper.Random.Next(0, list2.Count);
					x2 = list2[index4];
					list2.RemoveAt(index4);
				}
				else
				{
					x2 = RandomHelper.Random.Next(0, (int)LevelManager.Size.X);
				}
				LevelManager.RestoreTrile(new TrileInstance(new TrileEmplacement(x2, j, l), trile.Id)
				{
					Phi = array[Random.Next(0, 4)]
				});
			}
			while (list2.Count > 0)
			{
				int index5 = RandomHelper.Random.Next(0, list2.Count);
				int x3 = list2[index5];
				list2.RemoveAt(index5);
				LevelManager.RestoreTrile(new TrileInstance(new TrileEmplacement(x3, j, RandomHelper.Random.Next(0, (int)LevelManager.Size.Z)), trile.Id)
				{
					Phi = array[Random.Next(0, 4)]
				});
			}
		}
		foreach (TrileInstance value in LevelManager.Triles.Values)
		{
			value.VisualTrileId = RandomTrileIds[Random.Next(0, RandomTrileIds.Length)];
			value.RefreshTrile();
			value.NeedsRandomCleanup = true;
			value.RandomTracked = false;
		}
		LevelMaterializer.CleanUp();
		LevelMaterializer.TrileInstanceBatched += BatchInstance;
	}

	private void BatchInstance(TrileInstance instance)
	{
		if (!instance.RandomTracked)
		{
			BatchedInstances.Add(instance);
			instance.RandomTracked = true;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || GameState.InFpsMode || GameState.InMenuCube || !CameraManager.Viewpoint.IsOrthographic())
		{
			return;
		}
		Viewpoint viewpoint = CameraManager.Viewpoint;
		Vector3 vector = viewpoint.ScreenSpaceMask();
		Vector3 vector2 = Vector3.One - vector + Vector3.UnitY;
		bool flag = viewpoint.SideMask() == Vector3.Right;
		Vector3 position = PlayerManager.Position;
		bool viewTransitionReached = CameraManager.ViewTransitionReached;
		if (CameraManager.ProjectionTransitionNewlyReached)
		{
			LevelMaterializer.CullInstances();
		}
		RandomInstances.Clear();
		CleanInstances.Clear();
		AllEmplacements.Clear();
		for (int num = BatchedInstances.Count - 1; num >= 0; num--)
		{
			TrileInstance trileInstance = BatchedInstances[num];
			if (trileInstance.InstanceId == -1)
			{
				BatchedInstances.RemoveAt(num);
				trileInstance.RandomTracked = false;
			}
			else
			{
				Vector4 positionPhi = trileInstance.Data.PositionPhi;
				Vector3 vector3 = position - new Vector3(positionPhi.X, positionPhi.Y, positionPhi.Z);
				if ((vector3 * vector).LengthSquared() > 30f && (viewTransitionReached || (vector3 * vector2).LengthSquared() > 30f))
				{
					if (viewTransitionReached)
					{
						AllEmplacements.Add(positionPhi);
						RandomInstances.Add(trileInstance);
					}
				}
				else
				{
					CleanInstances.Add(trileInstance);
				}
			}
		}
		if (BatchedInstances.Count == 0)
		{
			return;
		}
		bool flag2 = false;
		if (FreezeFrames-- < 0)
		{
			if (RandomHelper.Probability(0.019999999552965164))
			{
				FreezeFrames = Random.Next(0, 15);
			}
		}
		else
		{
			flag2 = true;
		}
		if (RandomHelper.Probability(0.8999999761581421) && viewTransitionReached)
		{
			int num2 = Random.Next(0, flag2 ? (RandomInstances.Count / 50) : RandomInstances.Count);
			while (num2-- >= 0 && RandomInstances.Count > 0)
			{
				int count = RandomInstances.Count;
				int index = Random.Next(0, count);
				TrileInstance trileInstance2 = RandomInstances[index];
				RandomInstances.RemoveAt(index);
				if (!trileInstance2.VisualTrileId.HasValue || trileInstance2.TrileId == trileInstance2.VisualTrileId)
				{
					if (!LevelMaterializer.CullInstanceOut(trileInstance2))
					{
						LevelMaterializer.CullInstanceOut(trileInstance2, skipUnregister: true);
					}
					trileInstance2.VisualTrileId = RandomHelper.InList(RandomTrileIds);
					trileInstance2.RefreshTrile();
					LevelMaterializer.CullInstanceIn(trileInstance2, forceAdd: true);
				}
				trileInstance2.NeedsRandomCleanup = true;
				if (trileInstance2.InstanceId != -1)
				{
					int index2 = Random.Next(0, count);
					Vector4 data = AllEmplacements[index2];
					AllEmplacements.RemoveAt(index2);
					LevelMaterializer.GetTrileMaterializer(trileInstance2.VisualTrile).FakeUpdate(trileInstance2.InstanceId, data);
				}
			}
		}
		SsPosToRecull.Clear();
		foreach (TrileInstance cleanInstance in CleanInstances)
		{
			if (cleanInstance.VisualTrileId.HasValue)
			{
				if (!LevelMaterializer.CullInstanceOut(cleanInstance))
				{
					LevelMaterializer.CullInstanceOut(cleanInstance, skipUnregister: true);
				}
				cleanInstance.VisualTrileId = null;
				cleanInstance.RefreshTrile();
				if (viewTransitionReached)
				{
					TrileEmplacement emplacement = cleanInstance.Emplacement;
					SsPosToRecull.Add(new Point(flag ? emplacement.X : emplacement.Z, emplacement.Y));
				}
				else
				{
					LevelMaterializer.CullInstanceIn(cleanInstance, forceAdd: true);
				}
			}
			else if (cleanInstance.NeedsRandomCleanup)
			{
				LevelMaterializer.GetTrileMaterializer(cleanInstance.Trile).UpdateInstance(cleanInstance);
				cleanInstance.NeedsRandomCleanup = false;
			}
		}
		if (SsPosToRecull.Count > 0)
		{
			foreach (Point item in SsPosToRecull)
			{
				LevelManager.RecullAt(item);
			}
		}
		base.Update(gameTime);
	}
}
