using System;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.EndCutscene32;

internal class TetraordialOoze : DrawableGameComponent
{
	private enum State
	{
		Zoom
	}

	private const float NoiseZoomDuration = 14f;

	private const int TetraCount = 2000;

	private readonly EndCutscene32Host Host;

	public Mesh NoiseMesh;

	private Mesh TetraMesh;

	private float Time;

	private State ActiveState;

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public TetraordialOoze(Game game, EndCutscene32Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		Reset();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (TetraMesh != null)
		{
			TetraMesh.Dispose();
		}
		TetraMesh = null;
	}

	private void Reset()
	{
		CameraManager.Center = Vector3.Zero;
		CameraManager.Direction = Vector3.UnitZ;
		CameraManager.Radius = 10f;
		CameraManager.SnapInterpolation();
		Random random = RandomHelper.Random;
		TetraMesh = new Mesh();
		DrawActionScheduler.Schedule(delegate
		{
			TetraMesh.Effect = new DefaultEffect.VertexColored();
		});
		for (int i = 0; i < 2000; i++)
		{
			float num = RandomHelper.Unit();
			Group group = random.Next(0, 5) switch
			{
				0 => AddL(TetraMesh), 
				1 => AddO(TetraMesh), 
				2 => AddI(TetraMesh), 
				3 => AddS(TetraMesh), 
				_ => AddT(TetraMesh), 
			};
			if (i == 0)
			{
				group.Position = Vector3.Zero;
				num = 0f;
			}
			else
			{
				float num2 = 5.714286f;
				Vector3 vector = new Vector3((float)random.NextDouble() * (float)((random.Next(0, 2) == 1) ? 1 : (-1)), (float)random.NextDouble() * (float)((random.Next(0, 2) == 1) ? 1 : (-1)), -1f);
				while (vector.LengthSquared() <= 1.002f)
				{
					vector = new Vector3((float)random.NextDouble() * (float)((random.Next(0, 2) == 1) ? 1 : (-1)), (float)random.NextDouble() * (float)((random.Next(0, 2) == 1) ? 1 : (-1)), -1f);
				}
				float num3 = vector.Length();
				Vector3 vector2 = vector / num3;
				group.Position = vector2 * (float)Math.Pow(num3, 2.0) * new Vector3(num2 / 2f, num2 / 2f / CameraManager.AspectRatio, 1f);
			}
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, RandomHelper.Between(0.0, 6.2831854820251465));
			group.Scale = new Vector3(MathHelper.Lerp(0.0005f, 0.0075f, 1f - num));
			group.Material = new Material
			{
				Diffuse = new Vector3(1f - num),
				Opacity = 0f
			};
		}
	}

	private static Group AddL(Mesh m)
	{
		Vector3 vector = new Vector3(4f, 1f, 0f);
		Group group = m.AddGroup();
		group.Geometry = new IndexedUserPrimitives<VertexPositionColor>(new VertexPositionColor[6]
		{
			new VertexPositionColor(new Vector3(0f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(4f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(4f, 1f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(1f, 1f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(1f, 2f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(0f, 2f, 0f) - vector / 2f, Color.White)
		}, new int[7] { 0, 1, 2, 3, 4, 5, 0 }, PrimitiveType.LineStrip);
		return group;
	}

	private static Group AddO(Mesh m)
	{
		Vector3 vector = new Vector3(2f, 2f, 0f);
		Group group = m.AddGroup();
		group.Geometry = new IndexedUserPrimitives<VertexPositionColor>(new VertexPositionColor[4]
		{
			new VertexPositionColor(new Vector3(0f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(2f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(2f, 2f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(0f, 2f, 0f) - vector / 2f, Color.White)
		}, new int[5] { 0, 1, 2, 3, 0 }, PrimitiveType.LineStrip);
		return group;
	}

	private static Group AddI(Mesh m)
	{
		Vector3 vector = new Vector3(4f, 1f, 0f);
		Group group = m.AddGroup();
		group.Geometry = new IndexedUserPrimitives<VertexPositionColor>(new VertexPositionColor[4]
		{
			new VertexPositionColor(new Vector3(0f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(4f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(4f, 1f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(0f, 1f, 0f) - vector / 2f, Color.White)
		}, new int[5] { 0, 1, 2, 3, 0 }, PrimitiveType.LineStrip);
		return group;
	}

	private static Group AddS(Mesh m)
	{
		Vector3 vector = new Vector3(3f, 2f, 0f);
		Group group = m.AddGroup();
		group.Geometry = new IndexedUserPrimitives<VertexPositionColor>(new VertexPositionColor[8]
		{
			new VertexPositionColor(new Vector3(0f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(2f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(2f, 1f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(3f, 1f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(3f, 2f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(1f, 2f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(1f, 1f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(0f, 1f, 0f) - vector / 2f, Color.White)
		}, new int[9] { 0, 1, 2, 3, 4, 5, 6, 7, 0 }, PrimitiveType.LineStrip);
		return group;
	}

	private static Group AddT(Mesh m)
	{
		Vector3 vector = new Vector3(3f, 2f, 0f);
		Group group = m.AddGroup();
		group.Geometry = new IndexedUserPrimitives<VertexPositionColor>(new VertexPositionColor[8]
		{
			new VertexPositionColor(new Vector3(0f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(3f, 0f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(3f, 1f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(2f, 1f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(2f, 2f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(1f, 2f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(1f, 1f, 0f) - vector / 2f, Color.White),
			new VertexPositionColor(new Vector3(0f, 1f, 0f) - vector / 2f, Color.White)
		}, new int[9] { 0, 1, 2, 3, 4, 5, 6, 7, 0 }, PrimitiveType.LineStrip);
		return group;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.Loading)
		{
			return;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		Time += num;
		if (ActiveState != 0)
		{
			return;
		}
		if (Time == 0f)
		{
			CameraManager.Center = Vector3.Zero;
			CameraManager.Direction = Vector3.UnitZ;
			CameraManager.Radius = 10f;
			CameraManager.SnapInterpolation();
		}
		if (Time == 0f)
		{
			NoiseMesh.Scale = new Vector3(1.87495f);
			TetraMesh.Scale = Vector3.One;
		}
		for (int i = 0; i < TetraMesh.Groups.Count / 10; i++)
		{
			SwapTetraminos();
		}
		if (TetraMesh.Groups.Count < 10 && RandomHelper.Probability(0.10000000149011612))
		{
			SwapTetraminos();
		}
		float num2 = FezMath.Saturate(Time / 14f);
		if (num2 != 1f)
		{
			NoiseMesh.Scale *= MathHelper.Lerp(1.0025f, 1.01625f, num2);
			GameState.SkyRender = true;
			CameraManager.Radius /= MathHelper.Lerp(1.0025f, 1.01625f, num2);
			CameraManager.SnapInterpolation();
			GameState.SkyRender = false;
		}
		float opacity = MathHelper.Lerp(0f, 1f, Easing.EaseIn(FezMath.Saturate(num2 * 4f), EasingType.Linear));
		foreach (Group group in TetraMesh.Groups)
		{
			group.Material.Opacity = opacity;
		}
		NoiseMesh.Material.Opacity = 1f - FezMath.Saturate(num2 * 1.5f);
		if (num2 == 1f)
		{
			ChangeState();
		}
	}

	private void SwapTetraminos()
	{
		int num = RandomHelper.Random.Next(0, TetraMesh.Groups.Count);
		int num2 = RandomHelper.Random.Next(0, TetraMesh.Groups.Count);
		Group group = TetraMesh.Groups[num];
		Group group2 = TetraMesh.Groups[num2];
		if (CameraManager.Frustum.Contains(Vector3.Transform(group.Position, group.WorldMatrix)) == ContainmentType.Disjoint)
		{
			TetraMesh.RemoveGroupAt(num);
			return;
		}
		if (CameraManager.Frustum.Contains(Vector3.Transform(group2.Position, group2.WorldMatrix)) == ContainmentType.Disjoint)
		{
			TetraMesh.RemoveGroupAt(num2);
			return;
		}
		Vector3 position = group.Position;
		Material material = group.Material;
		Vector3 scale = group.Scale;
		group.Position = group2.Position;
		group.Scale = group2.Scale;
		group.Material = group2.Material;
		group2.Position = position;
		group2.Scale = scale;
		group2.Material = material;
	}

	private void ChangeState()
	{
		if (ActiveState == State.Zoom)
		{
			Host.Cycle();
			return;
		}
		Time = 0f;
		ActiveState++;
		Update(new GameTime());
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		base.GraphicsDevice.Clear(EndCutscene32Host.PurpleBlack);
		if (ActiveState == State.Zoom)
		{
			if (NoiseMesh.Material.Opacity > 0f)
			{
				NoiseMesh.Draw();
			}
			if (!FezMath.AlmostEqual(TetraMesh.FirstGroup.Material.Opacity, 0f))
			{
				TetraMesh.Draw();
			}
		}
	}
}
