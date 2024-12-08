using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class NameOfGodPuzzleHost : GameComponent
{
	private struct ZuishSlot
	{
		public readonly string TrileName;

		public readonly FaceOrientation Face;

		public ZuishSlot(string trileName, FaceOrientation face)
		{
			TrileName = trileName;
			Face = face;
		}
	}

	private static readonly int[] SpiralTraversal = new int[8] { -1, 1, -3, 3, -5, 5, -7, 7 };

	private static readonly Vector3 PuzzleCenter = new Vector3(13.5f, 57.5f, 14.5f);

	private static readonly ZuishSlot[] Slots = new ZuishSlot[8]
	{
		new ZuishSlot("ZUISH_BLOCKS_0", FaceOrientation.Back),
		new ZuishSlot("ZUISH_BLOCKS_4", FaceOrientation.Front),
		new ZuishSlot("ZUISH_BLOCKS_1", FaceOrientation.Right),
		new ZuishSlot("ZUISH_BLOCKS_0", FaceOrientation.Front),
		new ZuishSlot("ZUISH_BLOCKS_1", FaceOrientation.Right),
		new ZuishSlot("ZUISH_BLOCKS_5", FaceOrientation.Back),
		new ZuishSlot("ZUISH_BLOCKS_2", FaceOrientation.Back),
		new ZuishSlot("ZUISH_BLOCKS_1", FaceOrientation.Back)
	};

	private int RightPositions;

	private List<TrileInstance> Blocks;

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

	public NameOfGodPuzzleHost(Game game)
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
		if (LevelManager.Name == "ZU_ZUISH")
		{
			if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(0))
			{
				TrileInstance[] array = LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type == ActorType.PickUp).ToArray();
				foreach (TrileInstance trileInstance in array)
				{
					trileInstance.PhysicsState = null;
					LevelManager.ClearTrile(trileInstance);
				}
				if (!GameState.SaveData.ThisLevel.DestroyedTriles.Contains(new TrileEmplacement(PuzzleCenter + Vector3.UnitY * 2f + Vector3.UnitZ - FezMath.HalfVector)))
				{
					Trile trile = LevelManager.ActorTriles(ActorType.PieceOfHeart).FirstOrDefault();
					if (trile != null)
					{
						Vector3 position = PuzzleCenter + Vector3.UnitY * 2f - FezMath.HalfVector + Vector3.UnitZ;
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
			Blocks.AddRange(LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type == ActorType.PickUp));
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || !CameraManager.Viewpoint.IsOrthographic() || GameState.Loading)
		{
			return;
		}
		Vector3 b2 = CameraManager.Viewpoint.SideMask();
		Vector3 depthMask = CameraManager.Viewpoint.DepthMask();
		Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
		if (RightPositions == 8)
		{
			bool flag = false;
			foreach (TrileInstance block in Blocks)
			{
				if (!flag)
				{
					ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, block, PuzzleCenter + Vector3.UnitY * 2f + Vector3.UnitZ)
					{
						FlashOnSpawn = true,
						ActorToSpawn = ActorType.PieceOfHeart
					});
					flag = true;
				}
				else
				{
					ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, block));
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
		Vector3 b3 = CameraManager.Viewpoint.RightVector();
		foreach (TrileInstance block2 in Blocks)
		{
			float num = block2.Center.Dot(b3);
			block2.LastTreasureSin = 0f;
			foreach (TrileInstance block3 in Blocks)
			{
				if (block3 != block2 && num > block3.Center.Dot(b3))
				{
					block2.LastTreasureSin++;
				}
			}
		}
		foreach (TrileInstance instance in Blocks)
		{
			if (!instance.Enabled || !(instance.Position.Y >= 57f) || !(instance.Position.Y < 57.75f) || PlayerManager.HeldInstance == instance || PlayerManager.PushedInstance == instance || !instance.PhysicsState.Grounded || instance.PhysicsState.Ground.First == PlayerManager.PushedInstance)
			{
				continue;
			}
			if (!instance.PhysicsState.Background)
			{
				Vector3 value = ((instance.Center - PuzzleCenter) / 1f).Round();
				value = Vector3.Max(value, new Vector3(-8f, 0f, -8f));
				value = Vector3.Min(value, new Vector3(8f, 1f, 8f));
				Vector3 vector2 = PuzzleCenter + value * 1f;
				Vector3 vector3 = (vector2 - instance.Center) * FezMath.XZMask;
				float num2 = Math.Max(vector3.Length(), 0.1f);
				instance.PhysicsState.Velocity += 0.25f * (vector3 / num2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
				if (num2 <= 0.1f && value.Y == 0f && instance != PlayerManager.Ground.First && !Blocks.Any((TrileInstance x) => x.PhysicsState.Ground.First == instance) && Blocks.Any((TrileInstance b) => b != instance && FezMath.AlmostEqual(b.Position.Y, instance.Position.Y) && Math.Abs((b.Center - instance.Center).Dot(depthMask)) < 1f))
				{
					instance.Enabled = false;
					for (int i = 0; i < SpiralTraversal.Length; i++)
					{
						int num3 = SpiralTraversal[i];
						NearestTriles nearestTriles = LevelManager.NearestTrile(PuzzleCenter + (float)num3 * 1f * depthMask, QueryOptions.None, CameraManager.Viewpoint.GetRotatedView(1));
						if (nearestTriles.Deep == null)
						{
							nearestTriles.Deep = LevelManager.NearestTrile(PuzzleCenter + (float)num3 * 1f * depthMask - depthMask * 0.5f, QueryOptions.None, CameraManager.Viewpoint.GetRotatedView(1)).Deep;
						}
						if (nearestTriles.Deep == null)
						{
							nearestTriles.Deep = LevelManager.NearestTrile(PuzzleCenter + (float)num3 * 1f * depthMask + depthMask * 0.5f, QueryOptions.None, CameraManager.Viewpoint.GetRotatedView(1)).Deep;
						}
						if (nearestTriles.Deep == null)
						{
							Vector3 vector5 = (instance.PhysicsState.Center = vector * instance.PhysicsState.Center + PuzzleCenter * depthMask + depthMask * num3 * 1f);
							vector2 = vector5;
							break;
						}
					}
					instance.Enabled = true;
				}
				if (Math.Abs(vector3.X) <= 1f / 64f && Math.Abs(vector3.Y) <= 1f / 64f)
				{
					instance.PhysicsState.Velocity = Vector3.Zero;
					instance.PhysicsState.Center = vector2;
					instance.PhysicsState.UpdateInstance();
					LevelManager.UpdateInstance(instance);
				}
				if ((instance.PhysicsState.Ground.NearLow == null || (instance.PhysicsState.Ground.NearLow.PhysicsState != null && Math.Abs((instance.PhysicsState.Ground.NearLow.Center - instance.Center).Dot(b2)) > 0.875f)) && (instance.PhysicsState.Ground.FarHigh == null || (instance.PhysicsState.Ground.FarHigh.PhysicsState != null && Math.Abs((instance.PhysicsState.Ground.FarHigh.Center - instance.Center).Dot(b2)) > 0.875f)))
				{
					instance.PhysicsState.Ground = default(MultipleHits<TrileInstance>);
					instance.PhysicsState.Center += Vector3.Down * 0.1f;
				}
			}
			if (instance.PhysicsState.Grounded && instance.PhysicsState.Velocity == Vector3.Zero)
			{
				string cubemapPath = instance.Trile.CubemapPath;
				FaceOrientation faceOrientation = FezMath.OrientationFromPhi(CameraManager.Viewpoint.ToPhi() + instance.Phi);
				if (faceOrientation == FaceOrientation.Right || faceOrientation == FaceOrientation.Left)
				{
					faceOrientation = faceOrientation.GetOpposite();
				}
				int num4 = (int)MathHelper.Clamp(instance.LastTreasureSin, 0f, 7f);
				if (Slots[num4].Face == faceOrientation && Slots[num4].TrileName == cubemapPath)
				{
					RightPositions++;
				}
			}
		}
	}
}
