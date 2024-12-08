using System;
using System.Linq;
using Common;
using FezEngine.Effects;
using FezEngine.Readers;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public class FarawayPlaceHost : DrawableGameComponent
{
	private class PlaceFader : DrawableGameComponent
	{
		private CombineEffect CombineEffect;

		private RenderTargetHandle rth;

		public Mesh PlacesMesh { private get; set; }

		public Mesh FarawayWaterMesh { private get; set; }

		[ServiceDependency]
		public ITargetRenderingManager TargetRenderer { get; set; }

		[ServiceDependency]
		public IEngineStateManager GameState { get; set; }

		public PlaceFader(Game game)
			: base(game)
		{
			base.DrawOrder = 1001;
		}

		protected override void LoadContent()
		{
			DrawActionScheduler.Schedule(delegate
			{
				CombineEffect = new CombineEffect
				{
					RedGamma = 1f
				};
			});
		}

		public override void Update(GameTime gameTime)
		{
			if (GameState.StereoMode != (base.DrawOrder == 1002))
			{
				base.DrawOrder = (GameState.StereoMode ? 1002 : 1001);
				OnDrawOrderChanged(this, EventArgs.Empty);
			}
			if (base.Visible)
			{
				if (GameState.StereoMode && rth == null)
				{
					rth = TargetRenderer.TakeTarget();
					TargetRenderer.ScheduleHook(base.DrawOrder, rth.Target);
				}
				else if (rth != null && !GameState.StereoMode)
				{
					TargetRenderer.ReturnTarget(rth);
					rth = null;
				}
			}
			if (!base.Visible && rth != null)
			{
				TargetRenderer.ReturnTarget(rth);
				rth = null;
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (rth != null)
			{
				TargetRenderer.ReturnTarget(rth);
				rth = null;
			}
		}

		public override void Draw(GameTime gameTime)
		{
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			graphicsDevice.PrepareStencilRead(CompareFunction.NotEqual, StencilMask.Water);
			lock (FarawayPlaceMutex)
			{
				if (PlacesMesh != null)
				{
					PlacesMesh.Draw();
				}
			}
			graphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			lock (FarawayWaterMutex)
			{
				if (FarawayWaterMesh != null)
				{
					FarawayWaterMesh.Draw();
				}
			}
			if (GameState.StereoMode && rth != null)
			{
				TargetRenderer.Resolve(rth.Target, reschedule: true);
				base.GraphicsDevice.Clear(Color.Black);
				base.GraphicsDevice.SetupViewport();
				CombineEffect combineEffect = CombineEffect;
				Texture2D rightTexture = (CombineEffect.LeftTexture = rth.Target);
				combineEffect.RightTexture = rightTexture;
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
				TargetRenderer.DrawFullscreen(CombineEffect);
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			}
		}
	}

	private const float Visibility = 0.125f;

	private Mesh ThisLevelMesh;

	private Mesh LastLevelMesh;

	private Mesh NextLevelMesh;

	private Mesh FarawayWaterMesh;

	private Mesh LastWaterMesh;

	private float OriginalFakeRadius;

	private float DestinationFakeRadius;

	private float FakeRadius;

	private bool IsFake;

	private PlaceFader Fader;

	private Vector3 waterRightVector;

	private Texture2D HorizontalGradientTex;

	private static readonly object FarawayWaterMutex = new object();

	private static readonly object FarawayPlaceMutex = new object();

	private bool hasntSnapped;

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IFogManager FogManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	public FarawayPlaceHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		Material material = new Material();
		LastLevelMesh = new Mesh
		{
			DepthWrites = false,
			Material = material,
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.PointClamp
		};
		ThisLevelMesh = new Mesh
		{
			DepthWrites = false,
			Material = material,
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.PointClamp
		};
		NextLevelMesh = new Mesh
		{
			DepthWrites = false,
			Material = material,
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.PointClamp
		};
		DrawActionScheduler.Schedule(delegate
		{
			LastLevelMesh.Effect = new FarawayEffect();
			ThisLevelMesh.Effect = new FarawayEffect();
			NextLevelMesh.Effect = new FarawayEffect();
		});
		LevelManager.LevelChanged += TryInitialize;
		ServiceHelper.AddComponent(Fader = new PlaceFader(base.Game));
		Fader.Visible = false;
	}

	private void TryInitialize()
	{
		ThisLevelMesh.ClearGroups();
		NextLevelMesh.ClearGroups();
		lock (FarawayWaterMutex)
		{
			FarawayWaterMesh = null;
		}
		(NextLevelMesh.Effect as FarawayEffect).CleanUp();
		foreach (Script item in LevelManager.Scripts.Values.Where((Script x) => x.Actions.Any((ScriptAction y) => y.Operation == "ChangeToFarAwayLevel")))
		{
			int? identifier = item.Triggers.FirstOrDefault((ScriptTrigger x) => x.Object.Type == "Volume" && x.Event == "Enter").Object.Identifier;
			if (!LevelManager.Volumes.TryGetValue(identifier.Value, out var volume))
			{
				continue;
			}
			FaceOrientation faceOrientation = volume.Orientations.FirstOrDefault();
			ScriptAction scriptAction = item.Actions.FirstOrDefault((ScriptAction x) => x.Operation == "ChangeToFarAwayLevel");
			string destLevelName = scriptAction.Arguments[0];
			int pixPerTrix = 0;
			string dlSong;
			FaceOrientation destFace;
			Vector2 destOffset;
			bool destinationHasWater;
			float destWl;
			float waterLevel;
			float destLevelHeight;
			using (MemoryContentManager memoryContentManager = new MemoryContentManager(base.Game.Services, base.Game.Content.RootDirectory))
			{
				string text = destLevelName;
				if (!MemoryContentManager.AssetExists("Levels\\" + destLevelName.Replace('/', '\\')))
				{
					text = LevelManager.FullPath.Substring(0, LevelManager.FullPath.LastIndexOf("/") + 1) + destLevelName.Substring(destLevelName.LastIndexOf("/") + 1);
				}
				LevelReader.MinimalRead = true;
				Level level;
				try
				{
					level = memoryContentManager.Load<Level>("Levels/" + text);
				}
				catch (Exception)
				{
					Logger.Log("FarawayPlaceHost", LogSeverity.Warning, "Couldn't load faraway place destination level : " + destLevelName);
					goto end_IL_017a;
				}
				LevelReader.MinimalRead = false;
				dlSong = level.SongName;
				int num;
				try
				{
					num = int.Parse(scriptAction.Arguments[1]);
				}
				catch (Exception)
				{
					num = -1;
				}
				Volume volume2 = ((num == -1 || !level.Volumes.ContainsKey(num)) ? level.Volumes[level.Scripts.Values.First((Script s) => s.Actions.Any((ScriptAction a) => a.Object.Type == "Level" && a.Operation.Contains("Level") && a.Arguments[0] == LevelManager.Name)).Triggers.First((ScriptTrigger t) => t.Object.Type == "Volume" && t.Event == "Enter").Object.Identifier.Value] : level.Volumes[num]);
				destFace = volume2.Orientations.FirstOrDefault();
				Vector3 vector = (level.Size / 2f - (volume2.From + volume2.To) / 2f) * (destFace.AsViewpoint().RightVector() + Vector3.Up);
				destOffset = new Vector2(vector.X + vector.Z, vector.Y);
				destinationHasWater = level.WaterType != LiquidType.None;
				destWl = level.WaterHeight - (volume2.From + volume2.To).Y / 2f + EngineState.WaterLevelOffset;
				float num2 = LevelManager.WaterHeight - (volume.From + volume.To).Y / 2f;
				waterLevel = num2 - destWl / 4f;
				destLevelHeight = level.Size.Y;
				Script script = level.Scripts.Values.FirstOrDefault((Script s) => s.Triggers.Any((ScriptTrigger t) => t.Event == "Start" && t.Object.Type == "Level") && s.Actions.Any((ScriptAction a) => a.Object.Type == "Camera" && a.Operation == "SetPixelsPerTrixel"));
				if (script != null)
				{
					ScriptAction scriptAction2 = script.Actions.First((ScriptAction a) => a.Object.Type == "Camera" && a.Operation == "SetPixelsPerTrixel");
					try
					{
						pixPerTrix = int.Parse(scriptAction2.Arguments[0]);
					}
					catch (Exception)
					{
					}
				}
				destWl = level.WaterHeight;
				goto IL_04b8;
				end_IL_017a:;
			}
			continue;
			IL_04b8:
			DrawActionScheduler.Schedule(delegate
			{
				string text2 = string.Concat("Other Textures/faraway_thumbs/", destLevelName, " (", destFace.AsViewpoint(), ")");
				Texture2D texture = CMProvider.CurrentLevel.Load<Texture2D>(text2);
				texture.Name = text2;
				if (!ThisLevelMesh.Groups.Any((Group x) => x.Texture == texture))
				{
					if (pixPerTrix == 0)
					{
						pixPerTrix = (int)CameraManager.PixelsPerTrixel;
					}
					Group group = ThisLevelMesh.AddFace(new Vector3(texture.Width, texture.Height, texture.Width) / 16f / 2f, Vector3.Zero, faceOrientation, centeredOnOrigin: true);
					Group group2 = NextLevelMesh.AddFace(new Vector3(texture.Width, texture.Height, texture.Width) / 16f / 2f, Vector3.Zero, faceOrientation, centeredOnOrigin: true);
					FarawayPlaceData farawayPlaceData = default(FarawayPlaceData);
					farawayPlaceData.OriginalCenter = (volume.From + volume.To) / 2f;
					farawayPlaceData.Viewpoint = faceOrientation.AsViewpoint();
					farawayPlaceData.Volume = volume;
					farawayPlaceData.DestinationOffset = destOffset.X * faceOrientation.AsViewpoint().RightVector() + Vector3.Up * destOffset.Y;
					farawayPlaceData.WaterLevelOffset = waterLevel;
					farawayPlaceData.DestinationLevelName = destLevelName;
					farawayPlaceData.DestinationWaterLevel = destWl;
					farawayPlaceData.DestinationLevelSize = destLevelHeight;
					FarawayPlaceData farawayPlaceData2 = farawayPlaceData;
					if (LevelManager.WaterType == LiquidType.None && destinationHasWater)
					{
						if (HorizontalGradientTex == null || HorizontalGradientTex.IsDisposed)
						{
							HorizontalGradientTex = CMProvider.Global.Load<Texture2D>("Other Textures/WaterHorizGradient");
						}
						DefaultEffect.Textured effect = new DefaultEffect.Textured
						{
							AlphaIsEmissive = false
						};
						lock (FarawayWaterMutex)
						{
							FarawayPlaceHost farawayPlaceHost = this;
							Mesh obj = new Mesh
							{
								Effect = effect
							};
							Mesh farawayWaterMesh = obj;
							farawayPlaceData2.WaterBodyMesh = obj;
							farawayPlaceHost.FarawayWaterMesh = farawayWaterMesh;
							FarawayWaterMesh.AddFace(Vector3.One, new Vector3(-0.5f, -1f, -0.5f) + faceOrientation.AsVector().Abs() * 0.5f, faceOrientation, centeredOnOrigin: false).Material = new Material();
							FarawayWaterMesh.AddFace(Vector3.One, new Vector3(-0.5f, -1f, -0.5f) + faceOrientation.AsVector().Abs() * 0.5f, faceOrientation, centeredOnOrigin: false).Material = new Material();
						}
					}
					object customData = (group.CustomData = farawayPlaceData2);
					group2.CustomData = customData;
					Vector3 position = (group.Position = farawayPlaceData2.OriginalCenter);
					group2.Position = position;
					Texture texture3 = (group.Texture = texture);
					group2.Texture = texture3;
					group2.Material = new Material
					{
						Opacity = 0.125f
					};
					group.Material = new Material
					{
						Opacity = 0.125f
					};
					if (volume.ActorSettings == null)
					{
						volume.ActorSettings = new VolumeActorSettings();
					}
					volume.ActorSettings.DestinationSong = dlSong;
					switch (pixPerTrix)
					{
					case 1:
						volume.ActorSettings.DestinationRadius = 80f;
						break;
					case 2:
						volume.ActorSettings.DestinationRadius = 40f;
						break;
					case 3:
						volume.ActorSettings.DestinationRadius = 26.666666f;
						break;
					case 4:
						volume.ActorSettings.DestinationRadius = 20f;
						break;
					case 5:
						volume.ActorSettings.DestinationRadius = 16f;
						break;
					}
					volume.ActorSettings.DestinationPixelsPerTrixel = pixPerTrix;
					volume.ActorSettings.DestinationOffset = destOffset;
				}
			});
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (EngineState.Paused || EngineState.InMap || !CameraManager.Viewpoint.IsOrthographic() || CameraManager.ProjectionTransition)
		{
			return;
		}
		if (EngineState.FarawaySettings.InTransition && EngineState.FarawaySettings.OriginFadeOutStep == 1f && !IsFake)
		{
			for (int num = NextLevelMesh.Groups.Count - 1; num >= 0; num--)
			{
				try
				{
					if (num < NextLevelMesh.Groups.Count)
					{
						Group group = NextLevelMesh.Groups[num];
						FarawayPlaceData farawayPlaceData = (FarawayPlaceData)group.CustomData;
						string levelName = LevelManager.Name.Substring(0, LevelManager.Name.LastIndexOf("/") + 1) + farawayPlaceData.DestinationLevelName;
						if (CameraManager.Viewpoint != farawayPlaceData.Viewpoint)
						{
							NextLevelMesh.RemoveGroupAt(num);
							(NextLevelMesh.Effect as FarawayEffect).CleanUp();
						}
						else
						{
							CMProvider.GetForLevel(levelName).Load<Texture2D>(group.Texture.Name);
						}
					}
				}
				catch (Exception)
				{
				}
			}
			hasntSnapped = true;
			Mesh nextLevelMesh = NextLevelMesh;
			NextLevelMesh = LastLevelMesh;
			LastLevelMesh = nextLevelMesh;
			LastWaterMesh = FarawayWaterMesh;
			ThisLevelMesh.ClearGroups();
			float originalFakeRadius = (float)base.GraphicsDevice.Viewport.Width / (CameraManager.PixelsPerTrixel * 16f);
			OriginalFakeRadius = originalFakeRadius;
			DestinationFakeRadius = EngineState.FarawaySettings.DestinationRadius / 4f;
			EngineState.FarawaySettings.InterpolatedFakeRadius = CameraManager.Radius;
			LastLevelMesh.Effect.ForcedViewMatrix = CameraManager.View;
			(LastLevelMesh.Effect as FarawayEffect).ActualOpacity = 1f;
			if (LastWaterMesh != null && LastWaterMesh.Groups.Count > 0)
			{
				LastWaterMesh.Effect.ForcedViewMatrix = CameraManager.View;
				try
				{
					LastWaterMesh.Groups[0].Material.Opacity = (LastWaterMesh.Groups[1].Material.Opacity = 1f);
				}
				catch (Exception)
				{
				}
				lock (FarawayWaterMutex)
				{
					FarawayWaterMesh = null;
				}
			}
			IsFake = true;
			Fader.PlacesMesh = LastLevelMesh;
			Fader.FarawayWaterMesh = LastWaterMesh;
			LastLevelMesh.AlwaysOnTop = true;
			if (LastWaterMesh != null)
			{
				LastWaterMesh.AlwaysOnTop = true;
			}
			EngineState.FarawaySettings.LoadingAllowed = true;
		}
		if (!EngineState.FarawaySettings.InTransition && IsFake)
		{
			IsFake = false;
			Fader.Visible = false;
			lock (FarawayPlaceMutex)
			{
				Fader.PlacesMesh = null;
			}
			lock (FarawayWaterMutex)
			{
				Fader.FarawayWaterMesh = null;
			}
		}
		if (EngineState.FarawaySettings.InTransition)
		{
			float transitionStep = EngineState.FarawaySettings.TransitionStep;
			Fader.Visible = true;
			float viewScale = base.GraphicsDevice.GetViewScale();
			if (IsFake)
			{
				FakeRadius = MathHelper.Lerp(OriginalFakeRadius, DestinationFakeRadius, transitionStep);
				float amount = MathHelper.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds * CameraManager.InterpolationSpeed, 0f, 1f);
				EngineState.FarawaySettings.InterpolatedFakeRadius = MathHelper.Lerp(EngineState.FarawaySettings.InterpolatedFakeRadius, FakeRadius, amount);
				LastLevelMesh.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(EngineState.FarawaySettings.InterpolatedFakeRadius / viewScale, EngineState.FarawaySettings.InterpolatedFakeRadius / CameraManager.AspectRatio / viewScale, CameraManager.NearPlane, CameraManager.FarPlane);
				if (LastWaterMesh != null)
				{
					LastWaterMesh.Effect.ForcedProjectionMatrix = LastLevelMesh.Effect.ForcedProjectionMatrix;
				}
				EngineState.SkipRendering = true;
				CameraManager.Radius = FakeRadius * 4f;
				(ThisLevelMesh.Effect as FarawayEffect).ActualOpacity = (transitionStep - 0.5f) * 2f;
				(LastLevelMesh.Effect as FarawayEffect).ActualOpacity = 1f - EngineState.FarawaySettings.DestinationCrossfadeStep;
				try
				{
					if (FarawayWaterMesh != null)
					{
						lock (FarawayWaterMutex)
						{
							if (transitionStep > 0.5f)
							{
								FarawayWaterMesh.Groups[0].Material.Opacity = (transitionStep - 0.5f) * 2f;
								FarawayWaterMesh.Groups[1].Material.Opacity = (transitionStep - 0.5f) * 2f;
							}
							else
							{
								FarawayWaterMesh.Groups[0].Material.Opacity = (FarawayWaterMesh.Groups[1].Material.Opacity = 0f);
							}
						}
					}
					else if (LastWaterMesh != null)
					{
						lock (FarawayWaterMutex)
						{
							LastWaterMesh.Groups[0].Material.Opacity = 1f - EngineState.FarawaySettings.DestinationCrossfadeStep;
							LastWaterMesh.Groups[1].Material.Opacity = 1f - EngineState.FarawaySettings.DestinationCrossfadeStep;
							LastWaterMesh.Groups[0].Material.Diffuse = Vector3.Lerp(FogManager.Color.ToVector3(), EngineState.WaterBodyColor * LevelManager.ActualDiffuse.ToVector3(), Easing.EaseIn(transitionStep, EasingType.Sine) * 0.875f + 0.125f);
							LastWaterMesh.Groups[1].Material.Diffuse = Vector3.Lerp(FogManager.Color.ToVector3(), EngineState.WaterFoamColor * LevelManager.ActualDiffuse.ToVector3(), Easing.EaseIn(transitionStep, EasingType.Sine) * 0.875f + 0.125f);
						}
					}
				}
				catch (Exception)
				{
				}
				if (EngineState.FarawaySettings.DestinationCrossfadeStep == 0f && !hasntSnapped)
				{
					hasntSnapped = false;
					CameraManager.SnapInterpolation();
				}
				EngineState.SkipRendering = false;
				foreach (Group group2 in LastLevelMesh.Groups)
				{
					group2.Material.Opacity = Easing.EaseIn(transitionStep, EasingType.Sine) * 0.875f + 0.125f;
				}
			}
			else
			{
				foreach (Group group3 in ThisLevelMesh.Groups)
				{
					group3.Material.Opacity = Easing.EaseIn(transitionStep, EasingType.Sine) * 0.875f + 0.125f;
				}
			}
		}
		if (!EngineState.Loading)
		{
			LastLevelMesh.Material.Diffuse = FogManager.Color.ToVector3();
		}
	}

	private static double GetCustomOffset(double pixelsPerTrixel)
	{
		return 0.0 + 12.0 * Math.Pow(pixelsPerTrixel, -1.0) + -2.0;
	}

	private void PositionFarawayPlaces()
	{
		if (!CameraManager.Viewpoint.IsOrthographic() || CameraManager.ProjectionTransition)
		{
			return;
		}
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		for (int i = 0; i < ThisLevelMesh.Groups.Count; i++)
		{
			Group group = ThisLevelMesh.Groups[i];
			Group group2 = NextLevelMesh.Groups[i];
			FarawayPlaceData farawayPlaceData = (FarawayPlaceData)ThisLevelMesh.Groups[i].CustomData;
			Vector2 vector = ((farawayPlaceData.Volume.ActorSettings == null) ? Vector2.Zero : farawayPlaceData.Volume.ActorSettings.FarawayPlaneOffset);
			bool num2 = farawayPlaceData.Volume.ActorSettings != null && farawayPlaceData.Volume.ActorSettings.WaterLocked;
			float num3 = CameraManager.PixelsPerTrixel;
			if (EngineState.FarawaySettings.InTransition && FezMath.AlmostEqual(EngineState.FarawaySettings.DestinationCrossfadeStep, 1f))
			{
				num3 = MathHelper.Lerp(CameraManager.PixelsPerTrixel, EngineState.FarawaySettings.DestinationPixelsPerTrixel, (EngineState.FarawaySettings.TransitionStep - 0.875f) / 0.125f);
			}
			float num4 = (float)(-4 * ((!LevelManager.Descending) ? 1 : (-1))) / num3 - 15f / 32f + 1f;
			Vector3 vector2 = CameraManager.InterpolatedCenter - farawayPlaceData.OriginalCenter + num4 * Vector3.UnitY;
			float num5 = (float)GetCustomOffset(num3) * (float)((!LevelManager.Descending) ? 1 : (-1)) + 15f / 32f;
			float num6 = 0f;
			if (num2 && farawayPlaceData.WaterLevelOffset.HasValue)
			{
				vector2 *= FezMath.XZMask;
				vector *= Vector2.UnitX;
				num6 = farawayPlaceData.WaterLevelOffset.Value - num5 / 4f - 0.5f + 0.125f;
				farawayPlaceData.Volume.ActorSettings.WaterOffset = num6;
			}
			Vector3 position = (group2.Position = (farawayPlaceData.OriginalCenter + (farawayPlaceData.DestinationOffset + num5 * Vector3.UnitY) / 4f) * farawayPlaceData.Viewpoint.ScreenSpaceMask() + farawayPlaceData.Viewpoint.DepthMask() * CameraManager.InterpolatedCenter + farawayPlaceData.Viewpoint.ForwardVector() * 30f * num + farawayPlaceData.Viewpoint.RightVector() * vector.X + Vector3.Up * vector.Y + vector2 * farawayPlaceData.Viewpoint.ScreenSpaceMask() / 2f + num6 * Vector3.UnitY);
			group.Position = position;
			if (farawayPlaceData.WaterBodyMesh != null && farawayPlaceData.WaterBodyMesh.Groups.Count > 0)
			{
				waterRightVector = farawayPlaceData.Viewpoint.RightVector();
				farawayPlaceData.WaterBodyMesh.Position = group.Position * (farawayPlaceData.Viewpoint.DepthMask() + Vector3.UnitY) + CameraManager.InterpolatedCenter * farawayPlaceData.Viewpoint.SideMask() + (farawayPlaceData.DestinationWaterLevel - farawayPlaceData.DestinationLevelSize / 2f - 0.5f + EngineState.WaterLevelOffset) * Vector3.UnitY / 4f;
				farawayPlaceData.WaterBodyMesh.Groups[0].Scale = new Vector3(CameraManager.Radius);
				farawayPlaceData.WaterBodyMesh.Groups[0].Material.Diffuse = Vector3.Lerp(EngineState.WaterBodyColor * LevelManager.ActualDiffuse.ToVector3(), FogManager.Color.ToVector3(), 0.875f);
				farawayPlaceData.WaterBodyMesh.Groups[1].Scale = new Vector3(CameraManager.Radius, 0.0625f, CameraManager.Radius);
				farawayPlaceData.WaterBodyMesh.Groups[1].Material.Diffuse = Vector3.Lerp(EngineState.WaterFoamColor * LevelManager.ActualDiffuse.ToVector3(), FogManager.Color.ToVector3(), 0.875f);
			}
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (EngineState.Loading || EngineState.InMap)
		{
			return;
		}
		PositionFarawayPlaces();
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.SkyLayer3);
		ThisLevelMesh.Draw();
		lock (FarawayWaterMutex)
		{
			if (FarawayWaterMesh != null)
			{
				FarawayWaterMesh.Draw();
				Vector3 position = FarawayWaterMesh.Position;
				Vector3 scale = FarawayWaterMesh.Scale;
				FarawayWaterMesh.Blending = BlendingMode.Alphablending;
				FarawayWaterMesh.SamplerState = SamplerState.LinearClamp;
				FarawayWaterMesh.Texture = HorizontalGradientTex;
				FarawayWaterMesh.Position -= Math.Abs(FarawayWaterMesh.Groups[0].Scale.X) * waterRightVector;
				FarawayWaterMesh.Draw();
				FarawayWaterMesh.Scale = waterRightVector.Abs() * -1f + Vector3.One - waterRightVector.Abs();
				FarawayWaterMesh.Culling = CullMode.CullClockwiseFace;
				FarawayWaterMesh.Position += Math.Abs(FarawayWaterMesh.Groups[0].Scale.X) * waterRightVector * 2f;
				FarawayWaterMesh.Draw();
				FarawayWaterMesh.Culling = CullMode.CullCounterClockwiseFace;
				FarawayWaterMesh.Position = position;
				FarawayWaterMesh.Scale = scale;
				FarawayWaterMesh.Texture = null;
			}
		}
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
	}
}
