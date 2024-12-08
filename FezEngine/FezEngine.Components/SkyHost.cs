using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Components;

public class SkyHost : DrawableGameComponent
{
	private class CloudState
	{
		public float Phi;

		public float LocalHeightOffset;

		public float GlobalHeightOffset;

		public Group Group;

		public float VisibilityFactor;

		public bool ActualVisibility;

		public float GetHeight(float spreadFactor)
		{
			return LocalHeightOffset * spreadFactor + GlobalHeightOffset;
		}
	}

	private class BgLayerState
	{
		public int Layer;

		public int Side;

		public float WindOffset;

		public Vector2 OriginalTC;

		public float OriginalOpacity;
	}

	private const int Clouds = 64;

	private const int BaseDistance = 32;

	private const int ParallaxDistance = 48;

	private const int HeightSpread = 96;

	private const float MovementSpeed = 0.025f;

	private const float PerspectiveScaling = 4f;

	private readonly List<Mesh> cloudMeshes = new List<Mesh>();

	private readonly Dictionary<Layer, List<CloudState>> cloudStates = new Dictionary<Layer, List<CloudState>>(LayerComparer.Default);

	private readonly Mesh stars;

	private Matrix backgroundMatrix = Matrix.Identity;

	private Texture2D skyBackground;

	private AnimatedTexture shootingStar;

	public Mesh BgLayers;

	private SoundEffect sShootingStar;

	public static SkyHost Instance;

	private Color[] fogColors;

	private Color[] cloudColors;

	private static readonly float[] starSideOffsets = new float[4];

	private float flickerIn;

	private float flickerCount;

	private string lastSkyName;

	public bool flickering;

	private Vector3 lastCamPos;

	private TimeSpan sinceReached;

	private IWaiter waiter;

	private int sideToSwap;

	private float lastCamSide;

	private float sideOffset;

	private float startStep;

	private float startStep2;

	private RenderTargetHandle RtHandle;

	private float RadiusAtFirstDraw;

	private Color CurrentFogColor
	{
		get
		{
			float num = TimeManager.DayFraction * (float)fogColors.Length;
			if (num == (float)fogColors.Length)
			{
				num = 0f;
			}
			Color value = fogColors[Math.Max((int)Math.Floor(num), 0)];
			Color value2 = fogColors[Math.Min((int)Math.Ceiling(num), fogColors.Length - 1)];
			float amount = FezMath.Frac(num);
			TimeManager.CurrentFogColor = Color.Lerp(value, value2, amount);
			TimeManager.CurrentAmbientFactor = Math.Max(TimeManager.CurrentFogColor.ToVector3().Dot(new Vector3(1f / 3f)), 0.1f);
			for (int i = 0; i < 4; i++)
			{
				GamePad.SetLightBarEXT((PlayerIndex)i, TimeManager.CurrentFogColor);
			}
			return TimeManager.CurrentFogColor;
		}
	}

	private Color CurrentCloudTint
	{
		get
		{
			float num = TimeManager.DayFraction * (float)cloudColors.Length;
			if (num == (float)cloudColors.Length)
			{
				num = 0f;
			}
			Color value = cloudColors[Math.Max((int)Math.Floor(num), 0)];
			Color value2 = cloudColors[Math.Min((int)Math.Ceiling(num), cloudColors.Length - 1)];
			float amount = FezMath.Frac(num);
			return Color.Lerp(value, value2, amount);
		}
	}

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	[ServiceDependency]
	public IFogManager FogManager { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	public SkyHost(Game game)
		: base(game)
	{
		base.DrawOrder = 0;
		base.UpdateOrder = 11;
		Instance = this;
		stars = new Mesh
		{
			Culling = CullMode.CullClockwiseFace,
			AlwaysOnTop = true,
			DepthWrites = false,
			SamplerState = SamplerState.PointWrap
		};
		ServiceHelper.AddComponent(new CloudShadowsHost(game, this));
	}

	public override void Initialize()
	{
		base.Initialize();
		int num = 0;
		foreach (FaceOrientation value in Util.GetValues<FaceOrientation>())
		{
			if (value.IsSide())
			{
				stars.AddFace(Vector3.One, value.AsVector() / 2f, value, centeredOnOrigin: true);
				starSideOffsets[num] = (float)num++ / 4f;
			}
		}
		CameraManager.ViewChanged += OnViewChanged;
		CameraManager.ViewpointChanged += OnViewpointChanged;
		TimeManager.Tick += UpdateTimeOfDay;
	}

	protected override void LoadContent()
	{
		DrawActionScheduler.Schedule(delegate
		{
			stars.Effect = new StarsEffect();
		});
		shootingStar = CMProvider.Global.Load<AnimatedTexture>("Background Planes/shootingstar");
		sShootingStar = CMProvider.Global.Load<SoundEffect>("Sounds/Nature/ShootingStar");
		LevelManager.SkyChanged += InitializeSky;
	}

	private void InitializeSky()
	{
		if (LevelManager.Sky == null)
		{
			return;
		}
		ContentManager cm = ((LevelManager.Name == null) ? CMProvider.Global : CMProvider.GetForLevel(LevelManager.Name));
		string skyPath = "Skies/" + LevelManager.Sky.Name + "/";
		if (LevelManager.Sky.Name == lastSkyName && !EngineState.InEditor)
		{
			foreach (string cloud in LevelManager.Sky.Clouds)
			{
				cm.Load<Texture2D>(skyPath + cloud);
			}
			foreach (SkyLayer layer in LevelManager.Sky.Layers)
			{
				cm.Load<Texture2D>(skyPath + layer.Name);
			}
			skyBackground = cm.Load<Texture2D>(skyPath + LevelManager.Sky.Background);
			if (LevelManager.Sky.Stars != null)
			{
				cm.Load<Texture2D>(skyPath + LevelManager.Sky.Stars);
			}
			return;
		}
		lastSkyName = LevelManager.Sky.Name;
		if (LevelManager.Sky.Stars != null)
		{
			DrawActionScheduler.Schedule(delegate
			{
				stars.Texture = cm.Load<Texture2D>(skyPath + LevelManager.Sky.Stars);
			});
		}
		else
		{
			stars.Texture.Set(null);
		}
		DrawActionScheduler.Schedule(delegate
		{
			skyBackground = cm.Load<Texture2D>(skyPath + LevelManager.Sky.Background);
			fogColors = new Color[skyBackground.Width];
			skyBackground.GetData(0, new Rectangle(0, skyBackground.Height / 2, skyBackground.Width, 1), fogColors, 0, skyBackground.Width);
		});
		if (LevelManager.Sky.CloudTint != null)
		{
			DrawActionScheduler.Schedule(delegate
			{
				Texture2D texture2D = cm.Load<Texture2D>(skyPath + LevelManager.Sky.CloudTint);
				cloudColors = new Color[texture2D.Width];
				texture2D.GetData(0, new Rectangle(0, texture2D.Height / 2, texture2D.Width, 1), cloudColors, 0, texture2D.Width);
			});
		}
		else
		{
			cloudColors = new Color[1] { Color.White };
		}
		cloudStates.Clear();
		foreach (Mesh cloudMesh in cloudMeshes)
		{
			cloudMesh.Dispose();
		}
		cloudMeshes.Clear();
		if (BgLayers != null)
		{
			BgLayers.ClearGroups();
		}
		else
		{
			BgLayers = new Mesh
			{
				AlwaysOnTop = true,
				DepthWrites = false
			};
			DrawActionScheduler.Schedule(delegate
			{
				BgLayers.Effect = new DefaultEffect.Textured();
			});
		}
		int num = 0;
		foreach (SkyLayer layer2 in LevelManager.Sky.Layers)
		{
			int num2 = 0;
			foreach (FaceOrientation value in Util.GetValues<FaceOrientation>())
			{
				if (value.IsSide())
				{
					Group group = BgLayers.AddFace(Vector3.One, -value.AsVector() / 2f, value, centeredOnOrigin: true);
					group.AlwaysOnTop = layer2.InFront;
					group.Material = new Material
					{
						Opacity = layer2.Opacity
					};
					group.CustomData = new BgLayerState
					{
						Layer = num,
						Side = num2++,
						OriginalOpacity = layer2.Opacity
					};
				}
			}
			num++;
		}
		DrawActionScheduler.Schedule(delegate
		{
			int num3 = 0;
			foreach (SkyLayer layer3 in LevelManager.Sky.Layers)
			{
				Texture2D texture2D2 = cm.Load<Texture2D>(skyPath + layer3.Name);
				Texture2D texture2D3 = null;
				if (layer3.Name == "OBS_SKY_A")
				{
					texture2D3 = cm.Load<Texture2D>(skyPath + "OBS_SKY_C");
				}
				foreach (FaceOrientation value2 in Util.GetValues<FaceOrientation>())
				{
					if (value2.IsSide())
					{
						BgLayers.Groups[num3++].Texture = ((texture2D3 != null && value2 != 0) ? texture2D3 : texture2D2);
					}
				}
			}
		});
		foreach (Layer value3 in Util.GetValues<Layer>())
		{
			cloudStates.Add(value3, new List<CloudState>());
		}
		foreach (string cloud2 in LevelManager.Sky.Clouds)
		{
			_ = cloud2;
			Mesh item = new Mesh
			{
				AlwaysOnTop = true,
				DepthWrites = false,
				Culling = CullMode.None,
				SamplerState = SamplerState.PointClamp
			};
			cloudMeshes.Add(item);
		}
		DrawActionScheduler.Schedule(delegate
		{
			int num4 = 0;
			foreach (string cloud3 in LevelManager.Sky.Clouds)
			{
				cloudMeshes[num4].Effect = new CloudsEffect();
				cloudMeshes[num4].Texture = cm.Load<Texture2D>(skyPath + cloud3);
				num4++;
			}
		});
		float num5 = 64f * LevelManager.Sky.Density;
		int num6 = (int)Math.Sqrt(num5);
		float num7 = num5 / (float)num6;
		float num8 = RandomHelper.Between(0.0, 6.2831854820251465);
		float num9 = RandomHelper.Between(0.0, 192.0);
		if (cloudMeshes.Count > 0)
		{
			for (int i = 0; i < num6; i++)
			{
				for (int j = 0; (float)j < num7; j++)
				{
					Group group2 = RandomHelper.InList(cloudMeshes).AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
					float num10 = RandomHelper.Between(0.0, 1f / (float)num6 * ((float)Math.PI * 2f));
					float num11 = RandomHelper.Between(0.0, 1f / num7 * 192f);
					Layer key = RandomHelper.EnumField<Layer>();
					cloudStates[key].Add(new CloudState
					{
						Group = group2,
						Phi = ((float)i / (float)num6 * ((float)Math.PI * 2f) + num8 + num10) % ((float)Math.PI * 2f),
						LocalHeightOffset = ((float)j / num7 * 96f * 2f + num9 + num11) % 192f - 96f
					});
					group2.Material = new Material();
				}
			}
		}
		flickerIn = RandomHelper.Between(2.0, 10.0);
		DrawActionScheduler.Schedule(delegate
		{
			ResizeLayers();
			ResizeStars();
			OnViewpointChanged();
		});
		OnViewChanged();
	}

	public override void Update(GameTime gameTime)
	{
		if (!EngineState.Paused && !EngineState.InMap && (!EngineState.Loading || EngineState.FarawaySettings.InTransition) && LevelManager.Sky != null && LevelManager.Name != null)
		{
			ForceUpdate(gameTime);
		}
	}

	private void ForceUpdate(GameTime gameTime)
	{
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds * (TimeManager.TimeFactor / 360f);
		Vector3 obj = ((LevelManager.Size == Vector3.Zero) ? new Vector3(16f) : LevelManager.Size);
		_ = obj / 2f;
		Vector3 forward = CameraManager.View.Forward;
		if (forward.Z != 0f)
		{
			forward.Z *= -1f;
		}
		_ = TimeManager.CurrentAmbientFactor;
		_ = Math.Abs(obj.Dot(CameraManager.Viewpoint.RightVector())) / 32f;
		if (CameraManager.ActionRunning)
		{
			sinceReached += gameTime.ElapsedGameTime;
		}
		if (CameraManager.Viewpoint.IsOrthographic() && LevelManager.Sky.HorizontalScrolling)
		{
			foreach (Group group in BgLayers.Groups)
			{
				float num2 = num * LevelManager.Sky.WindSpeed * 0.025f;
				(group.CustomData as BgLayerState).WindOffset += num2;
			}
		}
		ShootStars();
		ResizeStars();
		DoFlicker(num);
	}

	private void ShootStars()
	{
		if (LevelManager.Sky != null && CameraManager.Viewpoint.IsOrthographic() && !LevelManager.Rainy && !(TimeManager.TimeFactor <= 0f) && LevelManager.Sky.Stars != null && TimeManager.NightContribution == 1f && RandomHelper.Probability(5E-05) && !(LevelManager.Name == "TELESCOPE") && !(LevelManager.Sky.Name == "TREE"))
		{
			Vector3 position = CameraManager.Center + LevelManager.Size / 2f * CameraManager.Viewpoint.ForwardVector() + new Vector3(RandomHelper.Centered(CameraManager.Radius / 2f - (float)shootingStar.FrameWidth / 32f)) * CameraManager.Viewpoint.SideMask() + RandomHelper.Between((0f - CameraManager.Radius) / CameraManager.AspectRatio / 6f, CameraManager.Radius / CameraManager.AspectRatio / 2f - (float)shootingStar.FrameHeight / 32f) * Vector3.UnitY;
			BackgroundPlane plane = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, shootingStar)
			{
				Position = position,
				Rotation = CameraManager.Rotation,
				Doublesided = true,
				Loop = false,
				Fullbright = true,
				Opacity = TimeManager.NightContribution,
				Timing = 
				{
					Step = 0f
				}
			};
			sShootingStar.EmitAt(position);
			LevelManager.AddPlane(plane);
		}
	}

	private void OnViewpointChanged()
	{
		foreach (Layer key in cloudStates.Keys)
		{
			foreach (Group item in cloudStates[key].Select((CloudState x) => x.Group))
			{
				if (item.Mesh.Texture.Value != null)
				{
					item.Scale = new Vector3((float)item.Mesh.TextureMap.Width / 16f, (float)item.Mesh.TextureMap.Height / 16f, 1f) * (CameraManager.Viewpoint.IsOrthographic() ? Vector3.One : new Vector3(4f + key.DistanceFactor() * 2f));
				}
				if (!CameraManager.Viewpoint.IsOrthographic())
				{
					item.Enabled = true;
				}
			}
		}
		if (CameraManager.Viewpoint != Viewpoint.Perspective)
		{
			return;
		}
		float num = ((LevelManager.Sky.Name == "OBS_SKY") ? 1f : 1.5f);
		foreach (Group group in BgLayers.Groups)
		{
			Matrix value = group.TextureMatrix.Value ?? Matrix.Identity;
			value.M31 += (0f - value.M11) / 2f + value.M11 / (2f * num);
			value.M32 += value.M22 / 2f - value.M22 / (2f * num);
			value.M11 /= num;
			value.M22 /= num;
			group.TextureMatrix.Set(value);
		}
	}

	public void RotateLayer(int layerId, Quaternion rotation)
	{
		BgLayers.Groups[layerId].Rotation = rotation;
	}

	private void DoFlicker(float elapsedTime)
	{
		if (LevelManager.Sky == null || LevelManager.Sky.Name != "INDUS_CITY")
		{
			return;
		}
		flickerIn -= elapsedTime;
		if (!(flickerIn <= 0f))
		{
			return;
		}
		if (flickerCount == -1f)
		{
			flickerCount = RandomHelper.Random.Next(2, 6);
			flickering = false;
		}
		flickerIn = RandomHelper.Between(0.05000000074505806, 0.25);
		for (int i = 0; i < 16; i++)
		{
			if (flickering)
			{
				BgLayers.Groups[i].Material.Opacity = (BgLayers.Groups[i].CustomData as BgLayerState).OriginalOpacity;
			}
			else
			{
				BgLayers.Groups[i].Material.Opacity = 0f;
			}
		}
		if (!flickering)
		{
			SoundManager.MuteAmbience("Ambience ^ rain", 0f);
		}
		else
		{
			SoundManager.UnmuteAmbience("Ambience ^ rain", 0f);
		}
		flickering = !flickering;
		flickerCount -= 1f;
		if (flickerCount == 0f)
		{
			SoundManager.UnmuteAmbience("Ambience ^ rain", 0f);
			flickerCount = -1f;
			flickering = false;
			flickerIn = RandomHelper.Between(2.0, 4.0);
			for (int j = 0; j < 16; j++)
			{
				BgLayers.Groups[j].Material.Opacity = (BgLayers.Groups[j].CustomData as BgLayerState).OriginalOpacity;
			}
		}
	}

	private void OnViewChanged()
	{
		if (!EngineState.LoopRender && !EngineState.SkyRender)
		{
			UpdateLayerAndCloudParallax();
		}
	}

	private void UpdateLayerAndCloudParallax()
	{
		if (EngineState.Paused || EngineState.InMap || (EngineState.Loading && !EngineState.FarawaySettings.InTransition) || LevelManager.Sky == null)
		{
			return;
		}
		Vector3 position = CameraManager.Position - CameraManager.ViewOffset;
		if (BgLayers != null && BgLayers.Groups.Count != 0)
		{
			BgLayers.Position = position;
		}
		position = CameraManager.InterpolatedCenter - CameraManager.ViewOffset;
		if (!CameraManager.ActionRunning)
		{
			sinceReached = TimeSpan.Zero;
		}
		if (CameraManager.Viewpoint.IsOrthographic())
		{
			Vector3 vector = CameraManager.Viewpoint.RightVector();
			float num = vector.Dot(position) - vector.Dot(lastCamPos);
			Quaternion rotation = CameraManager.Rotation;
			float num2 = Math.Abs((LevelManager.Size + Vector3.One * 32f).Dot(vector));
			bool flag = CameraManager.ActionRunning && sinceReached.TotalSeconds > 1.0;
			foreach (Layer key in cloudStates.Keys)
			{
				float num3 = MathHelper.Lerp(1f, key.ParallaxFactor(), LevelManager.Sky.CloudsParallax);
				foreach (CloudState item in cloudStates[key])
				{
					while (item.GetHeight(0.2f) - position.Y > 19.2f)
					{
						item.GlobalHeightOffset -= 38.4f;
					}
					while (item.GetHeight(0.2f) - position.Y < -19.2f)
					{
						item.GlobalHeightOffset += 38.4f;
					}
					if (flag)
					{
						item.GlobalHeightOffset += num3 * (position.Y - lastCamPos.Y);
						item.Phi -= num3 * 2.25f * num / num2;
					}
					if (item.Group.Enabled)
					{
						item.Group.Rotation = rotation;
					}
				}
			}
		}
		if (CameraManager.ActionRunning)
		{
			lastCamPos = position;
		}
	}

	private void MoveAndRotateClouds(GameTime gameTime)
	{
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds * (TimeManager.TimeFactor / 360f);
		Vector3 obj = ((LevelManager.Size == Vector3.Zero) ? new Vector3(16f) : LevelManager.Size);
		Vector3 vector = obj / 2f;
		Vector3 forward = CameraManager.View.Forward;
		if (forward.Z != 0f)
		{
			forward.Z *= -1f;
		}
		float currentAmbientFactor = TimeManager.CurrentAmbientFactor;
		float num2 = Math.Abs(obj.Dot(CameraManager.Viewpoint.RightVector())) / 32f;
		if (CameraManager.ActionRunning)
		{
			sinceReached += gameTime.ElapsedGameTime;
		}
		bool flag = CameraManager.Viewpoint.IsOrthographic();
		float num3 = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		foreach (Layer key in cloudStates.Keys)
		{
			float num4 = num * LevelManager.Sky.WindSpeed * 0.025f * key.SpeedFactor() / num2;
			Vector3 vector2 = ((!flag) ? (vector + Vector3.One * (32f + 48f * key.DistanceFactor())) : (vector + Vector3.One * 32f / 2.5f * num3));
			foreach (CloudState item in cloudStates[key])
			{
				if (flag)
				{
					item.Phi -= num4;
				}
				else
				{
					item.GlobalHeightOffset = CameraManager.Center.Y;
				}
				Vector3 vector3 = new Vector3(y: item.GetHeight((!flag) ? 1f : (CameraManager.ProjectionTransition ? MathHelper.Lerp(1f, 0.2f, CameraManager.ViewTransitionStep) : 0.2f)), x: (float)Math.Sin(item.Phi) * vector2.X + vector.X, z: (float)Math.Cos(item.Phi) * vector2.Z + vector.Z);
				Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(item.Phi, (float)Math.PI, (float)Math.PI);
				if (CameraManager.ProjectionTransition)
				{
					if (flag)
					{
						int width = item.Group.Mesh.TextureMap.Width;
						int height2 = item.Group.Mesh.TextureMap.Height;
						item.Group.Scale = new Vector3(width, height2, width) / 16f * (2f - CameraManager.ViewTransitionStep);
						item.Group.Rotation = Quaternion.Slerp(quaternion, CameraManager.Rotation, CameraManager.ViewTransitionStep);
						item.ActualVisibility = (vector3 - vector).Dot(forward) <= 0f;
					}
					else
					{
						item.Group.Rotation = Quaternion.Slerp(CameraManager.Rotation, quaternion, CameraManager.ViewTransitionStep);
						item.ActualVisibility = true;
					}
				}
				else if (flag)
				{
					bool flag2 = (vector3 - vector).Dot(forward) <= 0f;
					if (!item.ActualVisibility && flag2)
					{
						item.Group.Rotation = CameraManager.Rotation;
						item.VisibilityFactor = 0f;
					}
					item.ActualVisibility = flag2;
				}
				else
				{
					item.Group.Rotation = quaternion;
				}
				if (!flag || item.Group.Enabled)
				{
					item.Group.Position = vector3;
				}
				item.VisibilityFactor = FezMath.Saturate(item.VisibilityFactor + (float)gameTime.ElapsedGameTime.TotalSeconds * 5f * (float)(item.ActualVisibility ? 1 : (-1)));
				item.Group.Material.Opacity = key.Opacity() * currentAmbientFactor * Easing.EaseIn(item.VisibilityFactor, EasingType.Quadratic);
				item.Group.Enabled = (double)item.Group.Material.Opacity > 1.0 / 510.0;
			}
		}
	}

	private void UpdateTimeOfDay()
	{
		if (LevelManager.Sky == null)
		{
			return;
		}
		backgroundMatrix.M11 = 0.0001f;
		backgroundMatrix.M31 = TimeManager.DayFraction;
		if (fogColors != null)
		{
			FogManager.Color = CurrentFogColor;
			foreach (Group group in BgLayers.Groups)
			{
				group.Material.Diffuse = Vector3.Lerp(CurrentCloudTint.ToVector3(), FogManager.Color.ToVector3(), LevelManager.Sky.Layers[((BgLayerState)group.CustomData).Layer].FogTint);
			}
		}
		stars.Material.Opacity = ((LevelManager.Rainy || LevelManager.Sky.Name == "PYRAMID_SKY" || LevelManager.Sky.Name == "ABOVE") ? 1f : ((LevelManager.Sky.Name == "OBS_SKY") ? MathHelper.Lerp(TimeManager.NightContribution, 1f, 0.25f) : TimeManager.NightContribution));
	}

	private void ResizeStars()
	{
		if (LevelManager.Sky == null || stars.TextureMap == null || (!LevelManager.Rainy && !(LevelManager.Sky.Name == "ABOVE") && !(LevelManager.Sky.Name == "PYRAMID_SKY") && !(LevelManager.Sky.Name == "OBS_SKY") && TimeManager.NightContribution == 0f))
		{
			return;
		}
		float num = 0f;
		if (EngineState.FarawaySettings.InTransition)
		{
			float viewScale = base.GraphicsDevice.GetViewScale();
			if (!stars.Effect.ForcedProjectionMatrix.HasValue)
			{
				sideToSwap = (int)CameraManager.Viewpoint.VisibleOrientation().GetOpposite();
				if (sideToSwap > 1)
				{
					sideToSwap--;
				}
				if (sideToSwap == 4)
				{
					sideToSwap--;
				}
			}
			float num2 = (float)base.GraphicsDevice.Viewport.Width / (CameraManager.PixelsPerTrixel * 16f);
			if (EngineState.FarawaySettings.OriginFadeOutStep == 1f)
			{
				float amount = Easing.EaseInOut((EngineState.FarawaySettings.TransitionStep - startStep) / (1f - startStep), EasingType.Sine);
				num = (num2 = MathHelper.Lerp(num2, EngineState.FarawaySettings.DestinationRadius, amount));
			}
			else
			{
				num = num2;
			}
			stars.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(num2 / viewScale, num2 / CameraManager.AspectRatio / viewScale, CameraManager.NearPlane, CameraManager.FarPlane);
			int num3 = (int)CameraManager.Viewpoint.VisibleOrientation().GetOpposite();
			if (num3 > 1)
			{
				num3--;
			}
			if (num3 == 4)
			{
				num3--;
			}
			if (num3 != sideToSwap)
			{
				float num4 = starSideOffsets[num3];
				starSideOffsets[num3] = starSideOffsets[sideToSwap];
				starSideOffsets[sideToSwap] = num4;
				sideToSwap = num3;
			}
			stars.Scale = new Vector3(1f, 5f, 1f) * num * 2f;
		}
		else
		{
			if (waiter == null && stars.Effect.ForcedProjectionMatrix.HasValue)
			{
				waiter = Waiters.Wait(1.0, delegate
				{
					stars.Effect.ForcedProjectionMatrix = null;
					waiter = null;
				});
			}
			float num5 = (CameraManager.Viewpoint.IsOrthographic() ? (1f - CameraManager.ViewTransitionStep) : CameraManager.ViewTransitionStep);
			stars.Scale = new Vector3(1f, 5f, 1f) * (CameraManager.Radius * 2f + Easing.EaseOut(CameraManager.ProjectionTransition ? num5 : 1f, EasingType.Quintic) * 40f);
		}
		int num6 = 0;
		foreach (Group group in stars.Groups)
		{
			float num7 = stars.Scale.X / ((float)stars.TextureMap.Width / 16f);
			float num8 = stars.Scale.Y / ((float)stars.TextureMap.Height / 16f);
			float num9 = starSideOffsets[num6++];
			group.TextureMatrix.Set(new Matrix(num7, 0f, 0f, 0f, 0f, num8, 0f, 0f, num9 - num7 / 2f, num9 - num8 / 2f, 1f, 0f, 0f, 0f, 0f, 1f));
		}
	}

	private void ResizeLayers()
	{
		if (BgLayers == null || BgLayers.Groups.Count == 0 || EngineState.SkyRender || LevelManager.Sky == null)
		{
			return;
		}
		float num = 0f;
		float viewScale = base.GraphicsDevice.GetViewScale();
		if (EngineState.FarawaySettings.InTransition)
		{
			float num2 = (float)base.GraphicsDevice.Viewport.Width / (CameraManager.PixelsPerTrixel * 16f);
			if (EngineState.FarawaySettings.OriginFadeOutStep == 1f)
			{
				float num3 = startStep;
				if (num3 == 0f)
				{
					num3 = 0.1275f;
				}
				float amount = Easing.EaseInOut((EngineState.FarawaySettings.TransitionStep - num3) / (1f - num3), EasingType.Sine);
				num = (num2 = MathHelper.Lerp(num2, EngineState.FarawaySettings.DestinationRadius, amount));
			}
			BgLayers.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(num2 / viewScale, num2 / CameraManager.AspectRatio / viewScale, CameraManager.NearPlane, CameraManager.FarPlane);
		}
		else if (BgLayers.Effect.ForcedProjectionMatrix.HasValue)
		{
			BgLayers.Effect.ForcedProjectionMatrix = null;
		}
		Vector3 vector = new Vector3(CameraManager.InterpolatedCenter.X, CameraManager.Position.Y, CameraManager.InterpolatedCenter.Z);
		if (EngineState.FarawaySettings.InTransition)
		{
			Vector3 vector2 = (BgLayers.Position = CameraManager.Position);
			vector = vector2;
		}
		float num6;
		float num7;
		if (EngineState.FarawaySettings.InTransition && EngineState.FarawaySettings.OriginFadeOutStep == 1f)
		{
			float num4 = CameraManager.PixelsPerTrixel;
			if (EngineState.FarawaySettings.InTransition && FezMath.AlmostEqual(EngineState.FarawaySettings.DestinationCrossfadeStep, 1f))
			{
				num4 = MathHelper.Lerp(CameraManager.PixelsPerTrixel, EngineState.FarawaySettings.DestinationPixelsPerTrixel, (EngineState.FarawaySettings.TransitionStep - 0.875f) / 0.125f);
			}
			float num5 = (float)(-4 * ((!LevelManager.Descending) ? 1 : (-1))) / num4 - 15f / 32f + 1f;
			num6 = 0f - EngineState.FarawaySettings.DestinationOffset.X;
			num7 = 0f - EngineState.FarawaySettings.DestinationOffset.Y + num5;
			if (!EngineState.Loading)
			{
				if (startStep2 == 0f)
				{
					startStep2 = EngineState.FarawaySettings.TransitionStep;
				}
				num6 = MathHelper.Lerp(num6, (vector - LevelManager.Size / 2f).Dot(CameraManager.InverseView.Right), Easing.EaseInOut((EngineState.FarawaySettings.TransitionStep - startStep2) / (1f - startStep2), EasingType.Sine));
				num7 = MathHelper.Lerp(num7, vector.Y - LevelManager.Size.Y / 2f - CameraManager.ViewOffset.Y, Easing.EaseInOut((EngineState.FarawaySettings.TransitionStep - startStep2) / (1f - startStep2), EasingType.Sine));
			}
		}
		else
		{
			num6 = (vector - LevelManager.Size / 2f).Dot(CameraManager.InverseView.Right);
			num7 = vector.Y - LevelManager.Size.Y / 2f - CameraManager.ViewOffset.Y;
		}
		if (LevelManager.Sky.NoPerFaceLayerXOffset)
		{
			sideOffset = num6;
		}
		else if (CameraManager.ActionRunning && CameraManager.Viewpoint.IsOrthographic())
		{
			if (sinceReached.TotalSeconds > 0.45)
			{
				sideOffset -= lastCamSide - num6;
			}
			lastCamSide = num6;
		}
		float num8 = (CameraManager.Viewpoint.IsOrthographic() ? (1f - CameraManager.ViewTransitionStep) : CameraManager.ViewTransitionStep);
		if (CameraManager.Viewpoint.IsOrthographic() && !CameraManager.ProjectionTransition)
		{
			if (EngineState.FarawaySettings.InTransition && EngineState.FarawaySettings.OriginFadeOutStep == 1f)
			{
				BgLayers.Scale = new Vector3(1f, 5f, 1f) * num * 2f;
			}
			else
			{
				BgLayers.Scale = new Vector3(1f, 5f, 1f) * CameraManager.Radius * 2f / viewScale;
			}
		}
		else
		{
			BgLayers.Scale = new Vector3(1f, 5f, 1f) * (CameraManager.Radius * 2f + Easing.EaseOut(CameraManager.ProjectionTransition ? num8 : 1f, EasingType.Quintic) * 40f);
		}
		Vector2 vector4 = default(Vector2);
		foreach (Group group in BgLayers.Groups)
		{
			group.Enabled = false;
			BgLayerState bgLayerState = (BgLayerState)group.CustomData;
			float num9 = (float)bgLayerState.Layer / (float)((LevelManager.Sky.Layers.Count == 1) ? 1 : (LevelManager.Sky.Layers.Count - 1));
			int num10 = 1;
			float num11 = BgLayers.Scale.X / ((float)group.TextureMap.Width / 16f) / (float)num10;
			float num12 = BgLayers.Scale.Y / ((float)group.TextureMap.Height / 16f) / (float)num10;
			if (CameraManager.ProjectionTransition)
			{
				group.Scale = Vector3.One + FezMath.XZMask * num9 * 0.125f * num8;
			}
			Vector2 vector3 = new Vector2(sideOffset / ((float)group.TextureMap.Width / 16f), num7 / ((float)group.TextureMap.Height / 16f));
			if (EngineState.FarawaySettings.InTransition && EngineState.FarawaySettings.OriginFadeOutStep != 1f)
			{
				bgLayerState.OriginalTC = vector3;
				startStep = 0f;
				startStep2 = 0f;
			}
			else if (EngineState.FarawaySettings.InTransition && EngineState.FarawaySettings.OriginFadeOutStep == 1f)
			{
				if (vector3 != bgLayerState.OriginalTC && startStep == 0f)
				{
					startStep = EngineState.FarawaySettings.TransitionStep;
				}
				if (startStep != 0f)
				{
					vector3 = Vector2.Lerp(bgLayerState.OriginalTC, vector3, Easing.EaseInOut((EngineState.FarawaySettings.TransitionStep - startStep) / (1f - startStep), EasingType.Sine));
				}
			}
			vector4.X = (LevelManager.Sky.NoPerFaceLayerXOffset ? 0f : ((float)bgLayerState.Side / 4f)) + LevelManager.Sky.LayerBaseXOffset + vector3.X * LevelManager.Sky.HorizontalDistance + vector3.X * LevelManager.Sky.InterLayerHorizontalDistance * num9 + (0f - bgLayerState.WindOffset) * LevelManager.Sky.WindDistance + (0f - bgLayerState.WindOffset) * LevelManager.Sky.WindParallax * num9;
			if (!LevelManager.Sky.VerticalTiling)
			{
				num9 -= 0.5f;
			}
			vector4.Y = LevelManager.Sky.LayerBaseHeight + num9 * LevelManager.Sky.LayerBaseSpacing + (0f - vector3.Y) * LevelManager.Sky.VerticalDistance + (0f - num9) * LevelManager.Sky.InterLayerVerticalDistance * vector3.Y;
			group.TextureMatrix.Set(new Matrix(0f - num11, 0f, 0f, 0f, 0f, num12, 0f, 0f, 0f - vector4.X + num11 / 2f, vector4.Y - num12 / 2f, 1f, 0f, 0f, 0f, 0f, 1f));
		}
	}

	public void DrawBackground()
	{
		base.GraphicsDevice.SamplerStates[0] = SamplerStates.LinearUWrapVClamp;
		TargetRenderer.DrawFullscreen(skyBackground, backgroundMatrix, new Color(EngineState.SkyOpacity, EngineState.SkyOpacity, EngineState.SkyOpacity, 1f));
	}

	public override void Draw(GameTime gameTime)
	{
		if ((!EngineState.Loading || EngineState.FarawaySettings.InTransition) && BgLayers != null && EngineState.SkyOpacity != 0f && LevelManager.Name != null && LevelManager.Sky != null && !EngineState.InMap)
		{
			ForceDraw(gameTime);
		}
	}

	public void ForceDraw(GameTime gameTime)
	{
		if (stars.Material.Opacity > 0f)
		{
			stars.Position = CameraManager.Position - CameraManager.ViewOffset;
		}
		UpdateLayerAndCloudParallax();
		MoveAndRotateClouds(gameTime);
		ResizeLayers();
		RenderTarget2D renderTarget = null;
		bool flag = false;
		if (EngineState.FarawaySettings.OriginFadeOutStep == 1f && RtHandle == null)
		{
			RtHandle = TargetRenderer.TakeTarget();
			renderTarget = base.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
			base.GraphicsDevice.SetRenderTarget(RtHandle.Target);
			base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
			flag = true;
			RadiusAtFirstDraw = CameraManager.Radius;
		}
		else if (RtHandle != null && !EngineState.FarawaySettings.InTransition)
		{
			TargetRenderer.ReturnTarget(RtHandle);
			RtHandle = null;
		}
		EngineState.SkyRender = true;
		Vector3 viewOffset = CameraManager.ViewOffset;
		CameraManager.ViewOffset -= viewOffset;
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.PrepareStencilWrite(StencilMask.Sky);
		graphicsDevice.SetBlendingMode(BlendingMode.Maximum);
		bool flag2 = LevelManager.Name == "INDUSTRIAL_CITY";
		if (EngineState.FarawaySettings.OriginFadeOutStep < 1f || flag || EngineState.FarawaySettings.DestinationCrossfadeStep > 0f)
		{
			bool flag3 = EngineState.FarawaySettings.DestinationCrossfadeStep > 0f;
			if (!flag2 || flickering)
			{
				foreach (Mesh cloudMesh in cloudMeshes)
				{
					if (flag3)
					{
						cloudMesh.Material.Opacity = EngineState.FarawaySettings.DestinationCrossfadeStep;
					}
					float opacity = cloudMesh.Material.Opacity;
					if (EngineState.SkyOpacity != 1f)
					{
						cloudMesh.Material.Opacity = opacity * EngineState.SkyOpacity;
					}
					cloudMesh.Draw();
					cloudMesh.Material.Opacity = opacity;
				}
			}
		}
		if (RtHandle != null)
		{
			if (flag)
			{
				base.GraphicsDevice.SetRenderTarget(renderTarget);
				base.GraphicsDevice.Clear(Color.Black);
			}
			float num = ((EngineState.FarawaySettings.InterpolatedFakeRadius == 0f) ? RadiusAtFirstDraw : EngineState.FarawaySettings.InterpolatedFakeRadius) / RadiusAtFirstDraw;
			Matrix matrix = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, -0.5f, -0.5f, 1f, 0f, 0f, 0f, 0f, 1f);
			Matrix matrix2 = new Matrix(num, 0f, 0f, 0f, 0f, num, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);
			Matrix matrix3 = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0.5f, 0.5f, 1f, 0f, 0f, 0f, 0f, 1f);
			float num2 = num * (1f - EngineState.FarawaySettings.DestinationCrossfadeStep);
			TargetRenderer.DrawFullscreen(RtHandle.Target, matrix * matrix2 * matrix3, new Color(num2, num2, num2));
		}
		if (!flag2 || flickering)
		{
			graphicsDevice.SetBlendingMode(BlendingMode.Screen);
			DrawBackground();
		}
		if (stars.TextureMap != null && stars.Material.Opacity > 0f)
		{
			graphicsDevice.SetBlendingMode(BlendingMode.StarsOverClouds);
			float opacity2 = stars.Material.Opacity;
			stars.Material.Opacity = opacity2 * EngineState.SkyOpacity;
			stars.Draw();
			stars.Material.Opacity = opacity2;
		}
		base.GraphicsDevice.SamplerStates[0] = (LevelManager.Sky.VerticalTiling ? SamplerState.PointWrap : SamplerStates.PointUWrapVClamp);
		graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		if ((LevelManager.Rainy && !flag2) || LevelManager.Sky.Name == "WATERFRONT")
		{
			int count = BgLayers.Groups.Count;
			int num3 = count / 3;
			for (int i = 0; i < 3; i++)
			{
				for (int j = i * num3; j < count && j < (i + 1) * num3; j++)
				{
					BgLayers.Groups[j].Enabled = !(BgLayers.Groups[j].AlwaysOnTop ?? false);
				}
				graphicsDevice.PrepareStencilWrite((StencilMask)(5 + i));
				BgLayers.Draw();
				for (int k = i * num3; k < count && k < (i + 1) * num3; k++)
				{
					BgLayers.Groups[k].Enabled = false;
				}
			}
		}
		else
		{
			if (LevelManager.Name != null && (LevelManager.BlinkingAlpha || (LevelManager.WaterType == LiquidType.Sewer && EngineState.StereoMode)))
			{
				graphicsDevice.PrepareStencilWrite(StencilMask.SkyLayer1);
				graphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
				LevelMaterializer.StaticPlanesMesh.AlwaysOnTop = true;
				LevelMaterializer.StaticPlanesMesh.DepthWrites = false;
				foreach (BackgroundPlane levelPlane in LevelMaterializer.LevelPlanes)
				{
					levelPlane.Group.Enabled = levelPlane.Id < 0;
				}
				LevelMaterializer.StaticPlanesMesh.Draw();
				LevelMaterializer.StaticPlanesMesh.AlwaysOnTop = false;
				LevelMaterializer.StaticPlanesMesh.DepthWrites = true;
				foreach (BackgroundPlane levelPlane2 in LevelMaterializer.LevelPlanes)
				{
					levelPlane2.Group.Enabled = true;
				}
				graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
				graphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.SkyLayer1);
				graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
				base.GraphicsDevice.SamplerStates[0] = (LevelManager.Sky.VerticalTiling ? SamplerState.PointWrap : SamplerStates.PointUWrapVClamp);
			}
			foreach (Group group in BgLayers.Groups)
			{
				group.Enabled = !(group.AlwaysOnTop ?? false);
			}
			BgLayers.Draw();
			graphicsDevice.PrepareStencilWrite(StencilMask.None);
		}
		CameraManager.ViewOffset += viewOffset;
		EngineState.SkyRender = false;
	}
}
