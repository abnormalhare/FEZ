using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class TetrisPuzzleHost : GameComponent
{
	private static readonly int[] SpiralTraversal = new int[5] { 0, 1, -1, 2, -2 };

	private static readonly Vector3 PuzzleCenter = new Vector3(14.5f, 19.5f, 13.5f);

	private static readonly Vector3 TwoHigh = new Vector3(-1f, 0f, 0f);

	private static readonly Vector3 Interchangeable1_1 = new Vector3(0f, 0f, -1f);

	private static readonly Vector3 Interchangeable1_2 = new Vector3(1f, 0f, 1f);

	private static readonly Vector3 Interchangeable2_1 = new Vector3(0f, 0f, 1f);

	private static readonly Vector3 Interchangeable2_2 = new Vector3(1f, 0f, -1f);

	private int RightPositions;

	private List<TrileInstance> Blocks;

	private float SinceSolved;

	private float SinceStarted;

	[ServiceDependency]
	public ILevelService LevelService { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	public TetrisPuzzleHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		Blocks = null;
		RightPositions = 0;
		if (LevelManager.Name == "ZU_TETRIS")
		{
			if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(0))
			{
				TrileInstance[] array = LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type == ActorType.SinkPickup).ToArray();
				foreach (TrileInstance trileInstance in array)
				{
					trileInstance.PhysicsState = null;
					LevelManager.ClearTrile(trileInstance);
				}
				if (!GameState.SaveData.ThisLevel.DestroyedTriles.Contains(new TrileEmplacement(PuzzleCenter + Vector3.UnitY - FezMath.HalfVector)))
				{
					Trile trile = LevelManager.ActorTriles(ActorType.SecretCube).FirstOrDefault();
					if (trile != null)
					{
						Vector3 position = PuzzleCenter + Vector3.UnitY - FezMath.HalfVector;
						LevelManager.ClearTrile(new TrileEmplacement(position));
						IGameLevelManager levelManager = LevelManager;
						TrileInstance obj = new TrileInstance(position, trile.Id)
						{
							OriginalEmplacement = new TrileEmplacement(position)
						};
						TrileInstance trileInstance2 = obj;
						levelManager.RestoreTrile(obj);
						if (trileInstance2.InstanceId == -1)
						{
							LevelMaterializer.CullInstanceIn(trileInstance2);
						}
					}
				}
				base.Enabled = false;
			}
			else
			{
				base.Enabled = true;
			}
		}
		else
		{
			base.Enabled = false;
		}
		if (base.Enabled)
		{
			Blocks = new List<TrileInstance>();
			Blocks.AddRange(LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type == ActorType.SinkPickup));
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || !CameraManager.Viewpoint.IsOrthographic() || GameState.Loading)
		{
			return;
		}
		Vector3 depthMask = CameraManager.Viewpoint.DepthMask();
		Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
		if (RightPositions == 4)
		{
			SinceSolved += (float)gameTime.ElapsedGameTime.TotalSeconds;
			float num = FezMath.Saturate(SinceSolved);
			float amount = Easing.EaseIn(num, EasingType.Cubic);
			foreach (TrileInstance block in Blocks)
			{
				Vector3 v = block.Center - PuzzleCenter;
				if (v.Length() > 0.5f)
				{
					Vector3 vector2 = FezMath.AlmostClamp(v, 0.1f).Sign();
					Vector3 value = (PuzzleCenter + vector2 * 1.75f) * FezMath.XZMask + Vector3.UnitY * block.Center;
					Vector3 value2 = (PuzzleCenter + vector2) * FezMath.XZMask + Vector3.UnitY * block.Center;
					block.PhysicsState.Center = Vector3.Lerp(value, value2, amount);
					block.PhysicsState.UpdateInstance();
					LevelManager.UpdateInstance(block);
				}
			}
			if (num != 1f)
			{
				return;
			}
			bool flag = false;
			foreach (TrileInstance block2 in Blocks)
			{
				if (!flag)
				{
					ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, block2, PuzzleCenter + Vector3.UnitY));
					flag = true;
				}
				else
				{
					ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, block2));
				}
			}
			GameState.SaveData.ThisLevel.InactiveArtObjects.Add(0);
			foreach (Volume item in LevelManager.Volumes.Values.Where((Volume x) => x.ActorSettings != null && x.ActorSettings.IsPointOfInterest && x.Enabled))
			{
				item.Enabled = false;
				GameState.SaveData.ThisLevel.InactiveVolumes.Add(item.Id);
			}
			LevelService.ResolvePuzzle();
			base.Enabled = false;
			return;
		}
		RightPositions = 0;
		int num2 = 0;
		int num3 = 0;
		Blocks.Sort((TrileInstance a, TrileInstance b) => b.LastTreasureSin.CompareTo(a.LastTreasureSin));
		foreach (TrileInstance instance in Blocks)
		{
			if (!instance.PhysicsState.Grounded)
			{
				instance.LastTreasureSin = SinceStarted;
			}
			if (!(instance.Position.Y >= 19f) || !(instance.Position.Y <= 20.5f) || PlayerManager.HeldInstance == instance || PlayerManager.PushedInstance == instance || !instance.PhysicsState.Grounded || instance.PhysicsState.Ground.First == PlayerManager.PushedInstance)
			{
				continue;
			}
			Vector3 value3 = ((instance.Center - PuzzleCenter) / 1.75f).Round();
			value3 = Vector3.Max(value3, new Vector3(-3f, 0f, -3f));
			value3 = Vector3.Min(value3, new Vector3(3f, 1f, 3f));
			Vector3 vector3 = (PuzzleCenter + value3 * 1.75f - instance.Center) * FezMath.XZMask;
			float num4 = Math.Max(vector3.Length(), 0.1f);
			instance.PhysicsState.Velocity += 0.25f * (vector3 / num4) * (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (num4 <= 0.1f && value3.Y == 0f && instance != PlayerManager.Ground.First && !Blocks.Any((TrileInstance x) => x.PhysicsState.Ground.First == instance) && Blocks.Any((TrileInstance b) => b != instance && FezMath.AlmostEqual(b.Position.Y, instance.Position.Y) && Math.Abs((b.Center - instance.Center).Dot(depthMask)) < 1.75f))
			{
				int num5 = Math.Sign((instance.Center - PuzzleCenter).Dot(depthMask));
				if (num5 == 0)
				{
					num5 = 1;
				}
				for (int i = -2; i <= 2; i++)
				{
					int num6 = SpiralTraversal[i + 2] * num5;
					Vector3 tetativePosition = vector * instance.PhysicsState.Center + PuzzleCenter * depthMask + depthMask * num6 * 1.75f;
					if (Blocks.All((TrileInstance b) => b == instance || !FezMath.AlmostEqual(b.Position.Y, instance.Position.Y) || Math.Abs((b.Center - tetativePosition).Dot(depthMask)) >= 1.5749999f))
					{
						instance.PhysicsState.Center = tetativePosition;
						break;
					}
				}
			}
			if (RightPositions >= 4 || !(num4 <= 0.1f) || !instance.PhysicsState.Grounded)
			{
				continue;
			}
			if (instance.Position.Y == 20f)
			{
				if (value3.X == TwoHigh.X && value3.Z == TwoHigh.Z)
				{
					RightPositions += 2;
				}
				continue;
			}
			if ((value3.X == Interchangeable1_1.X && value3.Z == Interchangeable1_1.Z) || (value3.X == Interchangeable1_2.X && value3.Z == Interchangeable1_2.Z))
			{
				num2++;
			}
			if ((value3.X == Interchangeable2_1.X && value3.Z == Interchangeable2_1.Z) || (value3.X == Interchangeable2_2.X && value3.Z == Interchangeable2_2.Z))
			{
				num3++;
			}
		}
		if (RightPositions < 4 && (num2 == 2 || num3 == 2))
		{
			RightPositions += 2;
		}
	}
}
