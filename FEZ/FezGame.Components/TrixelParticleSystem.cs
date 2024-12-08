using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class TrixelParticleSystem : DrawableGameComponent
{
	private class Particle : ISimplePhysicsEntity, IPhysicsEntity
	{
		private readonly int InstanceIndex;

		private readonly TrixelParticleSystem System;

		public bool Enabled;

		public bool Static;

		public float Incandescence;

		public Vector3 Color;

		public Vector4 TextureMatrix;

		public float Delay { get; set; }

		public bool NoVelocityClamping => false;

		public bool IgnoreCollision => false;

		public MultipleHits<TrileInstance> Ground { get; set; }

		public Vector3 Center { get; set; }

		public Vector3 Velocity { get; set; }

		public Vector3 GroundMovement { get; set; }

		public float Elasticity { get; set; }

		public bool Background { get; set; }

		public PointCollision[] CornerCollision { get; private set; }

		public MultipleHits<CollisionResult> WallCollision { get; set; }

		public Vector3 Size { get; set; }

		public bool Grounded
		{
			get
			{
				if (Ground.NearLow == null)
				{
					return Ground.FarHigh != null;
				}
				return true;
			}
		}

		public bool Sliding
		{
			get
			{
				if (FezMath.AlmostEqual(Velocity.X, 0f))
				{
					return !FezMath.AlmostEqual(Velocity.Z, 0f);
				}
				return true;
			}
		}

		public bool StaticGrounds
		{
			get
			{
				if (IsGroundStatic(Ground.NearLow))
				{
					return IsGroundStatic(Ground.FarHigh);
				}
				return false;
			}
		}

		public Particle(TrixelParticleSystem system, int instanceIndex)
		{
			CornerCollision = new PointCollision[1];
			Enabled = true;
			InstanceIndex = instanceIndex;
			System = system;
		}

		public void Update()
		{
			System.Geometry.Instances[InstanceIndex] = new Matrix(Center.X, Center.Y, Center.Z, 0f, Size.X, Size.Y, Size.Z, 0f, Color.X * (1f + Incandescence), Color.Y * (1f + Incandescence), Color.Z * (1f + Incandescence), System.Opacity, TextureMatrix.X, TextureMatrix.Y, TextureMatrix.Z, TextureMatrix.W);
			System.Geometry.InstancesDirty = true;
		}

		public void Hide()
		{
			System.Geometry.Instances[InstanceIndex] = default(Matrix);
			System.Geometry.InstancesDirty = true;
		}

		private static bool IsGroundStatic(TrileInstance ground)
		{
			if (ground != null && ground.PhysicsState != null)
			{
				if (ground.PhysicsState.Velocity == Vector3.Zero)
				{
					return ground.PhysicsState.GroundMovement == Vector3.Zero;
				}
				return false;
			}
			return true;
		}
	}

	public class Settings
	{
		private TrileInstance explodingInstance;

		public Vector3 BaseVelocity { get; set; }

		public int MinimumSize { get; set; }

		public int MaximumSize { get; set; }

		public float Energy { get; set; }

		public float GravityModifier { get; set; }

		public Vector3? EnergySource { get; set; }

		public int ParticleCount { get; set; }

		public bool Darken { get; set; }

		public bool Incandesce { get; set; }

		public bool Crumble { get; set; }

		public TrileInstance ExplodingInstance
		{
			get
			{
				return explodingInstance;
			}
			set
			{
				if (!EnergySource.HasValue)
				{
					EnergySource = value.Position;
				}
				explodingInstance = value;
			}
		}

		public Settings()
		{
			MinimumSize = 1;
			MaximumSize = 8;
			Energy = 1f;
			GravityModifier = 1f;
			ParticleCount = 40;
		}
	}

	private readonly IPhysicsManager PhysicsManager;

	private readonly IGameCameraManager CameraManager;

	private readonly IGameStateManager GameState;

	private readonly ILightingPostProcess LightingPostProcess;

	private readonly ICollisionManager CollisionManager;

	private const int InstancesPerBatch = 60;

	private const int FadeOutStartSeconds = 4;

	private const int LifetimeSeconds = 6;

	private const float VelocityFactor = 0.2f;

	private const float EnergyDecay = 1.5f;

	private readonly List<Particle> particles = new List<Particle>();

	private readonly Settings settings;

	private static readonly object StaticLock = new object();

	private static volatile TrixelParticleEffect effect;

	private Mesh mesh;

	private TimeSpan age;

	private float Opacity = 1f;

	private ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix> Geometry;

	public Vector3 Offset { get; set; }

	public int ActiveParticles => particles.Count((Particle x) => x.Enabled && !x.Static);

	public bool Dead { get; private set; }

	public TrixelParticleSystem(Game game, Settings settings)
		: base(game)
	{
		this.settings = settings;
		base.DrawOrder = 10;
		PhysicsManager = ServiceHelper.Get<IPhysicsManager>();
		CameraManager = ServiceHelper.Get<IGameCameraManager>();
		GameState = ServiceHelper.Get<IGameStateManager>();
		LightingPostProcess = ServiceHelper.Get<ILightingPostProcess>();
		CollisionManager = ServiceHelper.Get<ICollisionManager>();
	}

	public override void Initialize()
	{
		base.Initialize();
		SetupGeometry();
		SetupInstances();
		LightingPostProcess.DrawGeometryLights += PreDraw;
	}

	private void RefreshEffects()
	{
		mesh.Effect = (effect = new TrixelParticleEffect());
	}

	private void SetupGeometry()
	{
		lock (StaticLock)
		{
			if (effect == null)
			{
				effect = new TrixelParticleEffect();
			}
		}
		BaseEffect.InstancingModeChanged += RefreshEffects;
		mesh = new Mesh
		{
			Effect = effect,
			SkipStates = true
		};
		mesh.AddGroup().Geometry = (Geometry = new ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>(PrimitiveType.TriangleList, 60));
		Vector3 vector = new Vector3(0f);
		Geometry.Vertices = new VertexPositionNormalTextureInstance[24]
		{
			new VertexPositionNormalTextureInstance(new Vector3(-1f, -1f, -1f) * 0.5f + vector, -Vector3.UnitZ)
			{
				TextureCoordinate = new Vector2(0.125f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, 1f, -1f) * 0.5f + vector, -Vector3.UnitZ)
			{
				TextureCoordinate = new Vector2(0.125f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, 1f, -1f) * 0.5f + vector, -Vector3.UnitZ)
			{
				TextureCoordinate = new Vector2(0f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, -1f, -1f) * 0.5f + vector, -Vector3.UnitZ)
			{
				TextureCoordinate = new Vector2(0f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, -1f, -1f) * 0.5f + vector, Vector3.UnitX)
			{
				TextureCoordinate = new Vector2(0.125f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, 1f, -1f) * 0.5f + vector, Vector3.UnitX)
			{
				TextureCoordinate = new Vector2(0.125f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, 1f, 1f) * 0.5f + vector, Vector3.UnitX)
			{
				TextureCoordinate = new Vector2(0f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, -1f, 1f) * 0.5f + vector, Vector3.UnitX)
			{
				TextureCoordinate = new Vector2(0f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, -1f, 1f) * 0.5f + vector, Vector3.UnitZ)
			{
				TextureCoordinate = new Vector2(0.125f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, 1f, 1f) * 0.5f + vector, Vector3.UnitZ)
			{
				TextureCoordinate = new Vector2(0.125f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, 1f, 1f) * 0.5f + vector, Vector3.UnitZ)
			{
				TextureCoordinate = new Vector2(0f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, -1f, 1f) * 0.5f + vector, Vector3.UnitZ)
			{
				TextureCoordinate = new Vector2(0f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, -1f, 1f) * 0.5f + vector, -Vector3.UnitX)
			{
				TextureCoordinate = new Vector2(0.125f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, 1f, 1f) * 0.5f + vector, -Vector3.UnitX)
			{
				TextureCoordinate = new Vector2(0.125f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, 1f, -1f) * 0.5f + vector, -Vector3.UnitX)
			{
				TextureCoordinate = new Vector2(0f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, -1f, -1f) * 0.5f + vector, -Vector3.UnitX)
			{
				TextureCoordinate = new Vector2(0f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, -1f, -1f) * 0.5f + vector, -Vector3.UnitY)
			{
				TextureCoordinate = new Vector2(0.125f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, -1f, 1f) * 0.5f + vector, -Vector3.UnitY)
			{
				TextureCoordinate = new Vector2(0.125f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, -1f, 1f) * 0.5f + vector, -Vector3.UnitY)
			{
				TextureCoordinate = new Vector2(0f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, -1f, -1f) * 0.5f + vector, -Vector3.UnitY)
			{
				TextureCoordinate = new Vector2(0f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, 1f, -1f) * 0.5f + vector, Vector3.UnitY)
			{
				TextureCoordinate = new Vector2(0.125f, 1f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(-1f, 1f, 1f) * 0.5f + vector, Vector3.UnitY)
			{
				TextureCoordinate = new Vector2(0.125f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, 1f, 1f) * 0.5f + vector, Vector3.UnitY)
			{
				TextureCoordinate = new Vector2(0f, 0f)
			},
			new VertexPositionNormalTextureInstance(new Vector3(1f, 1f, -1f) * 0.5f + vector, Vector3.UnitY)
			{
				TextureCoordinate = new Vector2(0f, 1f)
			}
		};
		Geometry.Indices = new int[36]
		{
			0, 2, 1, 0, 3, 2, 4, 6, 5, 4,
			7, 6, 8, 10, 9, 8, 11, 10, 12, 14,
			13, 12, 15, 14, 16, 17, 18, 16, 18, 19,
			20, 22, 21, 20, 23, 22
		};
		Geometry.Instances = new Matrix[settings.ParticleCount];
		Geometry.MaximizeBuffers(settings.ParticleCount);
		Geometry.InstanceCount = settings.ParticleCount;
		mesh.Texture.Set(settings.ExplodingInstance.Trile.TrileSet.TextureAtlas);
	}

	private void SetupInstances()
	{
		float elasticity = ((settings.ExplodingInstance.Trile.ActorSettings.Type == ActorType.Vase) ? 0.05f : 0.15f);
		int maxValue = 16 - settings.MinimumSize;
		Vector3 vector = CameraManager.Viewpoint.SideMask();
		Vector3 vector2 = CameraManager.Viewpoint.DepthMask();
		bool flag = vector.X != 0f;
		bool flag2 = CameraManager.Viewpoint == Viewpoint.Front || CameraManager.Viewpoint == Viewpoint.Left;
		Random random = RandomHelper.Random;
		Vector3 value = settings.EnergySource.Value;
		Vector3 vector3 = new Vector3(value.Dot(vector), value.Y, 0f);
		Vector3 vector4 = settings.ExplodingInstance.Center.Dot(vector2) * vector2;
		Vector3 vector5 = value - value.Dot(vector2) * vector2;
		Vector2 vector6 = new Vector2(settings.ExplodingInstance.Trile.TrileSet.TextureAtlas.Width, settings.ExplodingInstance.Trile.TrileSet.TextureAtlas.Height);
		Vector2 vector7 = new Vector2(128f / vector6.X, 16f / vector6.Y);
		Vector2 atlasOffset = settings.ExplodingInstance.Trile.AtlasOffset;
		List<SpaceDivider.DividedCell> list = null;
		if (settings.Crumble)
		{
			list = SpaceDivider.Split(settings.ParticleCount);
		}
		for (int i = 0; i < settings.ParticleCount; i++)
		{
			Particle particle = new Particle(this, i)
			{
				Elasticity = elasticity
			};
			Vector3 vector9;
			Vector3 vector8;
			if (settings.Crumble)
			{
				SpaceDivider.DividedCell dividedCell = list[i];
				vector8 = ((dividedCell.Left - 8) * vector + (dividedCell.Bottom - 8) * Vector3.UnitY + (dividedCell.Left - 8) * vector2) / 16f;
				vector9 = (dividedCell.Width * (vector + vector2) + dividedCell.Height * Vector3.UnitY) / 16f;
			}
			else
			{
				vector8 = new Vector3(random.Next(0, maxValue), random.Next(0, maxValue), random.Next(0, maxValue));
				do
				{
					vector9 = new Vector3(random.Next(settings.MinimumSize, Math.Min(17 - (int)vector8.X, settings.MaximumSize)), random.Next(settings.MinimumSize, Math.Min(17 - (int)vector8.Y, settings.MaximumSize)), random.Next(settings.MinimumSize, Math.Min(17 - (int)vector8.Z, settings.MaximumSize)));
				}
				while (Math.Abs(vector9.X - vector9.Y) > (vector9.X + vector9.Y) / 2f || Math.Abs(vector9.Z - vector9.Y) > (vector9.Z + vector9.Y) / 2f);
				vector8 = (vector8 - new Vector3(8f)) / 16f;
				vector9 /= 16f;
			}
			particle.Size = vector9;
			float num = (flag ? vector9.X : vector9.Z);
			particle.TextureMatrix = new Vector4(num * vector7.X, vector9.Y * vector7.Y, ((float)(flag2 ? 1 : (-1)) * (flag ? vector8.X : vector8.Z) + (flag2 ? 0f : (0f - num)) + 0.5f + 0.0625f) / 8f * vector7.X + atlasOffset.X, (0f - (vector8.Y + vector9.Y) + 0.5f + 0.0625f) * vector7.Y + atlasOffset.Y);
			float num2 = (settings.Darken ? RandomHelper.Between(0.3, 1.0) : 1f);
			particle.Color = new Vector3(num2, num2, num2);
			Vector3 vector10 = settings.ExplodingInstance.Center + vector8 + vector9 / 2f;
			Vector3 vector11 = new Vector3(vector10.Dot(vector), vector10.Y, 0f);
			Vector3 vector12 = vector10 - vector4 - vector5;
			if (vector12 != Vector3.Zero)
			{
				vector12.Normalize();
			}
			if (settings.Crumble)
			{
				vector12 = Vector3.Normalize(new Vector3(RandomHelper.Centered(1.0), RandomHelper.Centered(1.0), RandomHelper.Centered(1.0)));
			}
			float num3 = Math.Min(1f, 1.5f - Vector3.Dot(vector9, Vector3.One));
			float num4 = (float)Math.Pow(1f / (1f + (vector11 - vector3).Length()), 1.5);
			particle.Center = vector10;
			particle.Velocity = vector12 * settings.Energy * num3 * 0.2f * num4 + settings.BaseVelocity;
			if (settings.Incandesce)
			{
				particle.Incandescence = 2f;
			}
			particle.Update();
			particles.Add(particle);
			if (settings.Crumble)
			{
				particle.Delay = FezMath.Saturate(Easing.EaseOut(vector8.Y + 0.5f, EasingType.Cubic) + RandomHelper.Centered(0.10000000149011612));
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (LightingPostProcess != null)
		{
			LightingPostProcess.DrawGeometryLights -= PreDraw;
		}
		if (Geometry != null)
		{
			Geometry.Dispose();
			Geometry = null;
		}
		BaseEffect.InstancingModeChanged -= RefreshEffects;
	}

	public void UnGround()
	{
		foreach (Particle particle in particles)
		{
			if (particle.Enabled)
			{
				particle.Ground = default(MultipleHits<TrileInstance>);
				particle.Static = false;
			}
		}
	}

	public void AddImpulse(Vector3 energySource, float energy)
	{
		Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
		Vector3 vector2 = energySource * vector;
		foreach (Particle particle in particles)
		{
			if (particle.Enabled)
			{
				Vector3 vector3 = particle.Center * vector - vector2;
				float num = (float)Math.Pow(1f / (1f + vector3.Length()), 1.5);
				if (num > 0.1f)
				{
					Vector3 vector4 = ((vector3 == Vector3.Zero) ? Vector3.Zero : Vector3.Normalize(vector3));
					particle.Velocity += vector4 * energy * 0.2f * num;
					particle.Static = false;
				}
			}
		}
	}

	public void Update(TimeSpan elapsed)
	{
		age += elapsed;
		if (age.TotalSeconds > 6.0)
		{
			Dead = true;
			return;
		}
		Vector3 vector = 0.47250003f * CollisionManager.GravityFactor * settings.GravityModifier * (float)elapsed.TotalSeconds * Vector3.Down;
		float num = CameraManager.Radius / 2f;
		float y = CameraManager.Center.Y;
		float num2 = (float)age.TotalSeconds;
		bool flag = num2 > 4f;
		if (flag)
		{
			float num3 = (num2 - 4f) / 2f;
			Opacity = FezMath.Saturate(1f - num3);
		}
		foreach (Particle particle in particles)
		{
			if (settings.Crumble && age.TotalSeconds < (double)particle.Delay)
			{
				particle.Center += Offset;
				particle.Update();
			}
			else
			{
				if (!particle.Enabled)
				{
					continue;
				}
				bool flag2 = false;
				if (!particle.Static)
				{
					if (y - particle.Center.Y > num)
					{
						particle.Hide();
						particle.Enabled = false;
						continue;
					}
					particle.Velocity += vector;
					particle.Incandescence *= 0.95f;
					flag2 = PhysicsManager.Update(particle, simple: true, keepInFront: false);
					particle.Static = !flag2 && particle.Grounded && particle.StaticGrounds;
				}
				if (flag2 || flag)
				{
					particle.Update();
				}
			}
		}
	}

	private void PreDraw(GameTime gameTime)
	{
		if (!GameState.Loading && base.Visible)
		{
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.Level);
			effect.Pass = LightingEffectPass.Pre;
			mesh.Draw();
			effect.Pass = LightingEffectPass.Main;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.Loading && !GameState.StereoMode)
		{
			DoDraw();
		}
	}

	public void DoDraw()
	{
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		if (Opacity == 1f)
		{
			graphicsDevice.PrepareStencilWrite(StencilMask.Level);
		}
		mesh.Draw();
		if (Opacity == 1f)
		{
			graphicsDevice.PrepareStencilWrite(StencilMask.None);
		}
	}
}
