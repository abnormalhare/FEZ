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

internal class UnfoldPuzzleHost : GameComponent
{
	private static readonly Vector3 PuzzleCenter = new Vector3(9f, 57.5f, 11f);

	private static readonly Vector3[] Slots1 = new Vector3[6]
	{
		new Vector3(-2f, 1f, 0f),
		new Vector3(-1f, 1f, 0f),
		new Vector3(0f, 1f, 0f),
		new Vector3(0f, 0f, 0f),
		new Vector3(1f, 0f, 0f),
		new Vector3(2f, 0f, 0f)
	};

	private static readonly Vector3[] Slots2 = new Vector3[6]
	{
		new Vector3(-3f, 1f, 0f),
		new Vector3(-2f, 1f, 0f),
		new Vector3(-1f, 1f, 0f),
		new Vector3(-1f, 0f, 0f),
		new Vector3(0f, 0f, 0f),
		new Vector3(1f, 0f, 0f)
	};

	private static readonly Vector3[] Slots3 = new Vector3[6]
	{
		new Vector3(-3f, 1f, 0f),
		new Vector3(-2f, 1f, 0f),
		new Vector3(-1f, 1f, 0f),
		new Vector3(-5f, 0f, 0f),
		new Vector3(-4f, 0f, 0f),
		new Vector3(-3f, 0f, 0f)
	};

	private static readonly Vector3[] Slots4 = new Vector3[6]
	{
		new Vector3(-4f, 1f, 0f),
		new Vector3(-3f, 1f, 0f),
		new Vector3(-2f, 1f, 0f),
		new Vector3(-6f, 0f, 0f),
		new Vector3(-5f, 0f, 0f),
		new Vector3(-4f, 0f, 0f)
	};

	private int RightPositions1;

	private int RightPositions2;

	private int RightPositions3;

	private int RightPositions4;

	private List<TrileInstance> Blocks;

	[ServiceDependency]
	public IGroupService GroupService { get; set; }

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

	public UnfoldPuzzleHost(Game game)
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
		RightPositions1 = (RightPositions2 = (RightPositions3 = (RightPositions4 = 0)));
		if (LevelManager.Name == "ZU_UNFOLD")
		{
			if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(0))
			{
				TrileInstance[] array = LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type == ActorType.SinkPickup).ToArray();
				foreach (TrileInstance trileInstance in array)
				{
					trileInstance.PhysicsState = null;
					LevelManager.ClearTrile(trileInstance);
				}
				if (!GameState.SaveData.ThisLevel.DestroyedTriles.Contains(new TrileEmplacement(PuzzleCenter + Vector3.UnitY * 2f + Vector3.UnitZ - FezMath.HalfVector)))
				{
					Trile trile = LevelManager.ActorTriles(ActorType.SecretCube).FirstOrDefault();
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
			Blocks.AddRange(LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type == ActorType.SinkPickup));
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || !CameraManager.Viewpoint.IsOrthographic() || GameState.Loading)
		{
			return;
		}
		Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
		CameraManager.Viewpoint.SideMask();
		if (RightPositions1 == 6 || RightPositions2 == 6 || RightPositions3 == 6 || RightPositions4 == 6)
		{
			bool flag = false;
			foreach (TrileInstance block in Blocks)
			{
				if (!flag)
				{
					ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, block, PuzzleCenter + Vector3.UnitY * 2f + Vector3.UnitZ)
					{
						FlashOnSpawn = true
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
		}
		else
		{
			if (CameraManager.Viewpoint.ForwardVector() != -Vector3.UnitZ)
			{
				return;
			}
			RightPositions1 = (RightPositions2 = (RightPositions3 = (RightPositions4 = 0)));
			foreach (TrileInstance block2 in Blocks)
			{
				if (!block2.PhysicsState.Background && block2.Enabled && block2.Position.Y >= 57f && (block2.Position.Z > 9f || block2.Center.X >= 11f || (block2.Center.X <= 6f && block2.Center.Y > 57f) || (block2.Center.X <= 5f && block2.Center.Y == 56.5f)))
				{
					if (PlayerManager.PushedInstance == block2 && block2.PhysicsState.WallCollision.First.Collided && Math.Abs((block2.PhysicsState.WallCollision.First.Destination.Center - block2.Center).Dot(Vector3.UnitY)) > 0.9375f)
					{
						block2.PhysicsState.WallCollision = default(MultipleHits<CollisionResult>);
						block2.PhysicsState.Center += PlayerManager.LookingDirection.Sign() * CameraManager.Viewpoint.RightVector() * 0.01f;
					}
					if (PlayerManager.HeldInstance != block2 && PlayerManager.PushedInstance != block2 && block2.PhysicsState.Grounded && block2.PhysicsState.Ground.First != PlayerManager.PushedInstance)
					{
						Vector3 value = ((block2.Center - PuzzleCenter) / 1f).Round();
						value = Vector3.Max(value, new Vector3(-7f, 0f, 0f));
						value = Vector3.Min(value, new Vector3(7f, 1f, 0f));
						Vector3 vector2 = PuzzleCenter + value * 1f;
						if (value.Y == 0f)
						{
							vector2 += Vector3.UnitZ;
						}
						Vector3 vector3 = (vector2 - block2.Center) * vector;
						float num = Math.Abs(vector3.X);
						float num2 = Math.Sign(vector3.X);
						block2.PhysicsState.Velocity += 0.25f * ((num < 1f) ? (num2 * (float)Math.Sqrt(num)) : num2) * Vector3.UnitX * (float)gameTime.ElapsedGameTime.TotalSeconds;
						if (Math.Abs(vector3.X) <= 3f / 128f && Math.Abs(vector3.Y) <= 3f / 128f)
						{
							block2.PhysicsState.Velocity *= Vector3.UnitY;
							block2.PhysicsState.Center = block2.PhysicsState.Center * (Vector3.One - vector) + vector2 * vector;
							block2.PhysicsState.UpdateInstance();
							LevelManager.UpdateInstance(block2);
						}
					}
				}
				if (!(block2.Position.Y >= 57f) || (!(block2.Position.Z > 9f) && !(block2.Center.X >= 11f) && (!(block2.Center.X <= 6f) || !(block2.Center.Y > 57f)) && (!(block2.Center.X <= 5f) || block2.Center.Y != 56.5f)))
				{
					continue;
				}
				Vector3 vector4 = (block2.Center * 16f).Round() / 16f;
				if (!block2.PhysicsState.Grounded || !(block2.PhysicsState.Velocity.XZ() == Vector2.Zero))
				{
					continue;
				}
				for (int i = 0; i < Slots1.Length; i++)
				{
					if ((PuzzleCenter + Slots1[i]) * vector == vector4 * vector)
					{
						RightPositions1++;
						break;
					}
				}
				for (int j = 0; j < Slots2.Length; j++)
				{
					if ((PuzzleCenter + Slots2[j]) * vector == vector4 * vector)
					{
						RightPositions2++;
						break;
					}
				}
				for (int k = 0; k < Slots3.Length; k++)
				{
					if ((PuzzleCenter + Slots3[k]) * vector == vector4 * vector)
					{
						RightPositions3++;
						break;
					}
				}
				for (int l = 0; l < Slots4.Length; l++)
				{
					if ((PuzzleCenter + Slots4[l]) * vector == vector4 * vector)
					{
						RightPositions4++;
						break;
					}
				}
			}
		}
	}
}
