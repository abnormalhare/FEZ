using System;
using System.Collections.Generic;
using Common;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public class PlaneParticleSystem : DrawableGameComponent
{
	private class Particle
	{
		private readonly IDefaultCameraManager Camera;

		public Vector3 Position;

		private PlaneParticleSystem System;

		private PlaneParticleSystemSettings Settings;

		private Vector4 SpawnColor;

		private Vector4 LifeColor;

		private Vector4 DieColor;

		private float AgeSeconds;

		private Vector3 Velocity;

		private Vector3 GravityEffect;

		private Vector3 SizeBirth;

		private Vector3 SizeDeath;

		private Vector3 Scale;

		private float Phi;

		public int InstanceIndex { get; set; }

		public Vector3 TotalVelocity => Velocity + GravityEffect;

		public bool DeathProvoked { get; private set; }

		public bool Dead => AgeSeconds > Settings.ParticleLifetime;

		public Particle()
		{
			Camera = ServiceHelper.Get<IDefaultCameraManager>();
		}

		public void Initialize(PlaneParticleSystem system)
		{
			System = system;
			Settings = system.Settings;
			LifeColor = Settings.ColorLife.Evaluate().ToVector4();
			SpawnColor = ((Settings.ColorBirth == null) ? new Vector4(LifeColor.XYZ(), 0f) : Settings.ColorBirth.Evaluate().ToVector4());
			DieColor = ((Settings.ColorDeath == null) ? new Vector4(LifeColor.XYZ(), 0f) : Settings.ColorDeath.Evaluate().ToVector4());
			Scale = (SizeBirth = Settings.SizeBirth.Evaluate());
			SizeDeath = ((Settings.SizeDeath.Base == new Vector3(-1f)) ? SizeBirth : Settings.SizeDeath.Evaluate());
			Position = new Vector3(RandomHelper.Between(Settings.SpawnVolume.Min.X, Settings.SpawnVolume.Max.X), RandomHelper.Between(Settings.SpawnVolume.Min.Y, Settings.SpawnVolume.Max.Y), RandomHelper.Between(Settings.SpawnVolume.Min.Z, Settings.SpawnVolume.Max.Z));
			if (Settings.EnergySource.HasValue)
			{
				float num = Settings.Velocity.Evaluate().Length();
				Velocity = Vector3.Normalize(Position - Settings.EnergySource.Value) * num;
			}
			else
			{
				Velocity = Settings.Velocity.Evaluate();
			}
			GravityEffect = Vector3.Zero;
			AgeSeconds = 0f;
			DeathProvoked = false;
			Phi = (Settings.Orientation.HasValue ? Settings.Orientation.Value.ToPhi() : Camera.Viewpoint.ToPhi());
			System.Geometry.Instances[InstanceIndex] = new Matrix(Position.X, Position.Y, Position.Z, Phi, SizeBirth.X, SizeBirth.Y, SizeBirth.Z, 0f, SpawnColor.X, SpawnColor.Y, SpawnColor.Z, SpawnColor.W, 0f, 0f, 0f, 0f);
			System.Geometry.InstancesDirty = true;
		}

		public void Update(float elapsedSeconds)
		{
			AgeSeconds += elapsedSeconds;
			float num = AgeSeconds / Settings.ParticleLifetime;
			if (elapsedSeconds != 0f)
			{
				GravityEffect += Settings.Gravity;
			}
			if (Settings.Acceleration != 0f)
			{
				Velocity.X = FezMath.DoubleIter(Velocity.X, elapsedSeconds, 1f / Settings.Acceleration);
				Velocity.Y = FezMath.DoubleIter(Velocity.Y, elapsedSeconds, 1f / Settings.Acceleration);
				Velocity.Z = FezMath.DoubleIter(Velocity.Z, elapsedSeconds, 1f / Settings.Acceleration);
			}
			Position += (Velocity + GravityEffect) * elapsedSeconds;
			Vector4 value;
			if (num < Settings.FadeInDuration)
			{
				float amount = num / Settings.FadeInDuration;
				value = Vector4.Lerp(SpawnColor, LifeColor, amount);
			}
			else if (num > 1f - Settings.FadeOutDuration)
			{
				float amount2 = (num - (1f - Settings.FadeOutDuration)) / Settings.FadeOutDuration;
				value = Vector4.Lerp(LifeColor, DieColor, amount2);
			}
			else
			{
				value = LifeColor;
			}
			if (System.FadingOut)
			{
				value = Vector4.Lerp(value, DieColor, System.FadeOutAge);
			}
			if (Settings.Billboarding)
			{
				Phi = System.BillboardingPhi;
			}
			if (SizeBirth != SizeDeath)
			{
				Scale = Vector3.Lerp(SizeBirth, SizeDeath, num);
			}
			Vector3 vector = Position;
			if (Settings.ClampToTrixels)
			{
				vector = (vector * 16f).Round() / 16f + Scale / 2f;
			}
			System.Geometry.Instances[InstanceIndex] = new Matrix(vector.X, vector.Y, vector.Z, Phi, Scale.X, Scale.Y, Scale.Z, 0f, value.X, value.Y, value.Z, value.W, 0f, 0f, 0f, 0f);
			System.Geometry.InstancesDirty = true;
		}

		public void CommitToMatrix()
		{
			Vector3 vector = Position;
			if (Settings.ClampToTrixels)
			{
				vector = (vector * 16f).Round() / 16f + Scale / 2f;
			}
			System.Geometry.Instances[InstanceIndex] = new Matrix(vector.X, vector.Y, vector.Z, Phi, SizeBirth.X, SizeBirth.Y, SizeBirth.Z, 0f, SpawnColor.X, SpawnColor.Y, SpawnColor.Z, SpawnColor.W, 0f, 0f, 0f, 0f);
			System.Geometry.InstancesDirty = true;
		}

		public void Hide()
		{
			System.Geometry.Instances[InstanceIndex] = default(Matrix);
			System.Geometry.InstancesDirty = true;
		}

		public void ProvokeDeath()
		{
			AgeSeconds = Settings.ParticleLifetime + float.Epsilon;
			DeathProvoked = true;
		}
	}

	private const int InstancesPerBatch = 60;

	private readonly Pool<Particle> particles;

	private readonly List<Particle> activeParticles = new List<Particle>();

	private Mesh mesh;

	private PlaneParticleEffect effect;

	private PlaneParticleSystemSettings settings;

	private TimeSpan age;

	private TimeSpan sinceSpawned;

	private TimeSpan untilNextSpawn;

	private BoundingFrustum cachedFrustum;

	private ShaderInstancedIndexedPrimitives<VertexPositionTextureInstance, Matrix> Geometry;

	public bool InScreen;

	private bool FadingOut;

	private float FadeOutDuration;

	private float SinceFadingOut;

	private float FadeOutAge;

	public PlaneParticleSystemSettings Settings
	{
		get
		{
			return settings;
		}
		set
		{
			settings = value.Clone();
		}
	}

	public bool Dead { get; private set; }

	public int ActiveParticles => activeParticles.Count;

	public int DrawnParticles => Geometry.InstanceCount;

	public int MaximumCount
	{
		get
		{
			return particles.Size;
		}
		set
		{
			if (particles.Size != value)
			{
				Clear();
				particles.Size = value;
				while (particles.Available > particles.Size)
				{
					particles.Take();
				}
				if (mesh != null)
				{
					SetupGeometry();
				}
			}
		}
	}

	public bool Initialized { get; private set; }

	public bool HalfUpdate { get; set; }

	private float BillboardingPhi { get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	public event Action<Vector3> CollisionCallback = Util.NullAction;

	public PlaneParticleSystem()
		: base(ServiceHelper.Game)
	{
		particles = new Pool<Particle>();
	}

	public PlaneParticleSystem(Game game, int maximumCount, PlaneParticleSystemSettings settings)
		: base(game)
	{
		Settings = settings;
		particles = new Pool<Particle>(maximumCount);
		PrepareNextSpawn();
	}

	private void RefreshEffects()
	{
		mesh.Effect = (effect = new PlaneParticleEffect());
	}

	public void Revive()
	{
		Dead = false;
		bool enabled = (base.Visible = true);
		base.Enabled = enabled;
		FadingOut = false;
		age = (sinceSpawned = (untilNextSpawn = TimeSpan.Zero));
		PrepareNextSpawn();
		if (effect == null)
		{
			DrawActionScheduler.Schedule(delegate
			{
				effect.Additive = settings.BlendingMode == BlendingMode.Additive;
				effect.Fullbright = settings.FullBright;
			});
		}
		else
		{
			effect.Additive = settings.BlendingMode == BlendingMode.Additive;
			effect.Fullbright = settings.FullBright;
		}
	}

	public void SetViewProjectionSticky(bool enabled)
	{
		effect.ForcedViewProjection = (enabled ? new Matrix?(CameraManager.View * CameraManager.Projection) : null);
		cachedFrustum = (enabled ? CameraManager.Frustum : null);
	}

	public void MoveActiveParticles(Vector3 offset)
	{
		foreach (Particle activeParticle in activeParticles)
		{
			activeParticle.Position += offset;
			activeParticle.CommitToMatrix();
		}
	}

	public void FadeOutAndDie(float forSeconds)
	{
		if (!FadingOut)
		{
			FadingOut = true;
			FadeOutDuration = forSeconds;
			SinceFadingOut = 0f;
		}
	}

	private void SetupGeometry()
	{
		mesh.ClearGroups();
		mesh.AddGroup().Geometry = (Geometry = new ShaderInstancedIndexedPrimitives<VertexPositionTextureInstance, Matrix>(PrimitiveType.TriangleList, 60));
		Geometry.Vertices = new VertexPositionTextureInstance[4]
		{
			new VertexPositionTextureInstance(new Vector3(-0.5f, -0.5f, 0f), new Vector2(0f, 1f)),
			new VertexPositionTextureInstance(new Vector3(-0.5f, 0.5f, 0f), new Vector2(0f, 0f)),
			new VertexPositionTextureInstance(new Vector3(0.5f, 0.5f, 0f), new Vector2(1f, 0f)),
			new VertexPositionTextureInstance(new Vector3(0.5f, -0.5f, 0f), new Vector2(1f, 1f))
		};
		Geometry.Indices = new int[6] { 0, 1, 2, 0, 2, 3 };
		Geometry.Instances = new Matrix[MaximumCount];
		Geometry.MaximizeBuffers(MaximumCount);
		int num = particles.Size;
		List<Particle> list = new List<Particle>();
		while (particles.Available > 0)
		{
			Particle particle = particles.Take();
			num = (particle.InstanceIndex = num - 1);
			list.Add(particle);
		}
		foreach (Particle item in list)
		{
			particles.Return(item);
		}
		if (settings.Texture != null)
		{
			mesh.Texture.Set(settings.Texture);
		}
	}

	public void RefreshTexture()
	{
		mesh.Texture.Set(settings.Texture);
	}

	public void Clear()
	{
		foreach (Particle activeParticle in activeParticles)
		{
			activeParticle.Hide();
			particles.Return(activeParticle);
		}
		activeParticles.Clear();
	}

	public override void Initialize()
	{
		base.Initialize();
		mesh = new Mesh
		{
			SkipStates = true
		};
		BaseEffect.InstancingModeChanged += RefreshEffects;
		DrawActionScheduler.Schedule(delegate
		{
			mesh.Effect = (effect = new PlaneParticleEffect());
		});
		SetupGeometry();
		Initialized = true;
		LightingPostProcess.DrawGeometryLights += DrawLights;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (LightingPostProcess != null)
		{
			LightingPostProcess.DrawGeometryLights -= DrawLights;
		}
		if (Geometry != null)
		{
			Geometry.Dispose();
			Geometry = null;
		}
		if (mesh != null)
		{
			mesh.Dispose();
			mesh = null;
		}
		effect = null;
		Initialized = false;
		BaseEffect.InstancingModeChanged -= RefreshEffects;
	}

	public void Update(TimeSpan elapsed)
	{
		if (!base.Enabled)
		{
			return;
		}
		BoundingFrustum frustum = CameraManager.Frustum;
		if (effect != null)
		{
			if (!effect.ForcedViewProjection.HasValue)
			{
				Vector3 position = CameraManager.Position;
				Vector3 center = CameraManager.Center;
				BillboardingPhi = (float)Math.Atan2(position.X - center.X, position.Z - center.Z);
			}
			else
			{
				frustum = cachedFrustum;
			}
		}
		if (settings.SystemLifetime != 0f)
		{
			age += elapsed;
			if (age.TotalSeconds > (double)settings.SystemLifetime)
			{
				base.Enabled = false;
				base.Visible = false;
				Dead = true;
				foreach (Particle activeParticle in activeParticles)
				{
					particles.Return(activeParticle);
				}
				activeParticles.Clear();
				return;
			}
		}
		if (FadingOut)
		{
			SinceFadingOut += (float)elapsed.TotalSeconds;
			FadeOutAge = FezMath.Saturate(SinceFadingOut / FadeOutDuration);
			if (FadeOutAge >= 1f)
			{
				base.Enabled = false;
				base.Visible = false;
				Dead = true;
				foreach (Particle activeParticle2 in activeParticles)
				{
					particles.Return(activeParticle2);
				}
				activeParticles.Clear();
				return;
			}
		}
		if (!InScreen)
		{
			return;
		}
		sinceSpawned -= elapsed;
		bool flag = untilNextSpawn.Ticks > 0;
		untilNextSpawn -= elapsed;
		while (sinceSpawned.Ticks <= 0 || (flag && untilNextSpawn.Ticks <= 0))
		{
			if (flag && untilNextSpawn.Ticks <= 0)
			{
				for (int i = 0; i < settings.SpawnBatchSize; i++)
				{
					if (particles.Available > 0)
					{
						Particle particle = particles.Take();
						particle.Initialize(this);
						activeParticles.Add(particle);
					}
				}
				flag = false;
			}
			if (sinceSpawned.Ticks <= 0)
			{
				PrepareNextSpawn();
			}
		}
		int num = -1;
		float elapsedSeconds = (float)elapsed.TotalSeconds;
		int num2 = ((!HalfUpdate) ? activeParticles.Count : (activeParticles.Count / 2));
		for (int j = 0; j < num2; j++)
		{
			Particle particle2 = activeParticles[j];
			particle2.Update(elapsedSeconds);
			if (Settings.UseCallback && !particle2.DeathProvoked)
			{
				Vector3 position2 = particle2.Position;
				if (Geometry.Instances[particle2.InstanceIndex].M12 < LevelManager.WaterHeight)
				{
					this.CollisionCallback(position2);
					particle2.ProvokeDeath();
					continue;
				}
				TrileEmplacement id = new TrileEmplacement((int)position2.X, (int)position2.Y, (int)position2.Z);
				if (LevelManager.IsInRange(ref id))
				{
					TrileInstance trileInstance = LevelManager.TrileInstanceAt(ref id);
					if (trileInstance != null && trileInstance.Enabled && !trileInstance.Trile.Immaterial && frustum.Contains(trileInstance.Center) != 0)
					{
						Vector3 transformedSize = trileInstance.TransformedSize;
						Vector3 center2 = trileInstance.Center;
						if (new BoundingBox(center2 - transformedSize / 2f, center2 + transformedSize / 2f).Contains(position2) != 0)
						{
							this.CollisionCallback(position2 * FezMath.XZMask + trileInstance.Center * Vector3.UnitY + trileInstance.Trile.Size.Y / 2f * Vector3.UnitY);
							particle2.ProvokeDeath();
						}
					}
				}
			}
			if (particle2.Dead)
			{
				particle2.Hide();
				activeParticles.RemoveAt(j);
				j--;
				num2--;
				particles.Return(particle2);
			}
			else
			{
				num = Math.Max(num, particle2.InstanceIndex);
			}
		}
		Geometry.InstanceCount = num;
	}

	private void PrepareNextSpawn()
	{
		float num = 1f / settings.SpawningSpeed;
		TimeSpan timeSpan = TimeSpan.FromSeconds(settings.RandomizeSpawnTime ? RandomHelper.Between(0.0, num) : num);
		if (untilNextSpawn < sinceSpawned)
		{
			untilNextSpawn = sinceSpawned;
		}
		untilNextSpawn += timeSpan;
		sinceSpawned += TimeSpan.FromSeconds(num);
	}

	private void DrawLights(GameTime gameTime)
	{
		if (base.Visible && !Settings.NoLightDraw && InScreen)
		{
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			graphicsDevice.GetRasterCombiner().CullMode = CullMode.CullCounterClockwiseFace;
			graphicsDevice.GetDssCombiner().DepthBufferFunction = CompareFunction.LessEqual;
			bool depthBufferWriteEnable = graphicsDevice.GetDssCombiner().DepthBufferWriteEnable;
			StencilOperation stencilPass = graphicsDevice.GetDssCombiner().StencilPass;
			if (settings.BlendingMode == BlendingMode.Additive)
			{
				graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = false;
				graphicsDevice.GetDssCombiner().StencilPass = StencilOperation.Keep;
				graphicsDevice.GetBlendCombiner().BlendingMode = BlendingMode.Additive;
			}
			else
			{
				graphicsDevice.GetBlendCombiner().BlendingMode = BlendingMode.Alphablending;
			}
			effect.Pass = LightingEffectPass.Pre;
			mesh.Draw();
			effect.Pass = LightingEffectPass.Main;
			if (settings.BlendingMode == BlendingMode.Additive)
			{
				graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = depthBufferWriteEnable;
				graphicsDevice.GetDssCombiner().StencilPass = stencilPass;
				graphicsDevice.GetBlendCombiner().BlendingMode = BlendingMode.Alphablending;
			}
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (base.DrawOrder == 0 || EngineState.Loading || EngineState.StereoMode || EngineState.InMap)
		{
			return;
		}
		InScreen = CameraManager.Frustum.Contains(Settings.SpawnVolume) != ContainmentType.Disjoint;
		if (InScreen)
		{
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			if (Settings.StencilMask.HasValue)
			{
				graphicsDevice.PrepareStencilWrite(Settings.StencilMask);
			}
			else
			{
				graphicsDevice.GetDssCombiner().StencilFunction = CompareFunction.Always;
				graphicsDevice.GetDssCombiner().StencilPass = StencilOperation.Keep;
			}
			graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = false;
			graphicsDevice.GetRasterCombiner().CullMode = CullMode.CullCounterClockwiseFace;
			graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
			Draw();
			if (Settings.StencilMask.HasValue)
			{
				graphicsDevice.GetDssCombiner().StencilFunction = CompareFunction.Always;
				graphicsDevice.GetDssCombiner().StencilPass = StencilOperation.Keep;
			}
		}
	}

	public void Draw()
	{
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		if (settings.Doublesided)
		{
			graphicsDevice.GetRasterCombiner().CullMode = CullMode.None;
		}
		if (Settings.BlendingMode != BlendingMode.Alphablending)
		{
			graphicsDevice.SetBlendingMode(Settings.BlendingMode);
		}
		mesh.Draw();
		if (Settings.BlendingMode != BlendingMode.Alphablending)
		{
			graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		}
		if (Settings.Doublesided)
		{
			graphicsDevice.GetRasterCombiner().CullMode = CullMode.CullCounterClockwiseFace;
		}
	}
}
