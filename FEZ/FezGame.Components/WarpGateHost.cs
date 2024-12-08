using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class WarpGateHost : DrawableGameComponent
{
	private readonly Dictionary<WarpDestinations, WarpPanel> panels = new Dictionary<WarpDestinations, WarpPanel>(WarpDestinationsComparer.Default);

	private ArtObjectInstance warpGateAo;

	private string CurrentLevelName;

	private Vector3 InterpolatedCenter;

	public static WarpGateHost Instance { get; private set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public WarpGateHost(Game game)
		: base(game)
	{
		base.DrawOrder = 10;
		Instance = this;
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		bool visible = (base.Enabled = false);
		base.Visible = visible;
	}

	private void TryInitialize()
	{
		warpGateAo = LevelManager.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.WarpGate);
		bool visible = (base.Enabled = warpGateAo != null);
		base.Visible = visible;
		if (!base.Enabled)
		{
			foreach (KeyValuePair<WarpDestinations, WarpPanel> panel in panels)
			{
				panel.Value.PanelMask.Dispose();
				panel.Value.Layers.Dispose();
			}
			panels.Clear();
			return;
		}
		if (panels.Count == 0)
		{
			WarpPanel naturePanel = new WarpPanel
			{
				Face = FaceOrientation.Front,
				Destination = "NATURE_HUB",
				PanelMask = new Mesh
				{
					DepthWrites = false
				},
				Layers = new Mesh
				{
					AlwaysOnTop = true,
					DepthWrites = false,
					SamplerState = SamplerState.PointClamp
				}
			};
			panels.Add(WarpDestinations.First, naturePanel);
			DrawActionScheduler.Schedule(delegate
			{
				Mesh panelMask = naturePanel.PanelMask;
				panelMask.Effect = new DefaultEffect.Textured();
				panelMask.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/fullwhite");
				panelMask.AddFace(new Vector3(3.875f), Vector3.Backward * 1.5625f, FaceOrientation.Front, centeredOnOrigin: true);
				Mesh layers = naturePanel.Layers;
				layers.Effect = new DefaultEffect.Textured();
				Texture2D texture = CMProvider.Global.Load<Texture2D>("Other Textures/warp/nature/background");
				layers.AddFace(new Vector3(10f, 4f, 10f), Vector3.Forward * 5f, FaceOrientation.Front, centeredOnOrigin: true).Texture = texture;
				layers.AddFace(new Vector3(10f, 4f, 10f), Vector3.Right * 5f, FaceOrientation.Left, centeredOnOrigin: true).Texture = texture;
				layers.AddFace(new Vector3(10f, 4f, 10f), Vector3.Left * 5f, FaceOrientation.Right, centeredOnOrigin: true).Texture = texture;
				Group group = layers.AddFace(new Vector3(32f, 32f, 1f), new Vector3(0f, 2f, -8f), FaceOrientation.Front, centeredOnOrigin: true);
				group.Texture = CMProvider.Global.Load<Texture2D>("Skies/WATERFRONT/WATERFRONT_C");
				group.Material = new Material
				{
					Opacity = 0.3f,
					Diffuse = Vector3.Lerp(Vector3.One, new Vector3(0.1215686f, 0.96f, 1f), 0.7f)
				};
				Group group2 = layers.AddFace(new Vector3(32f, 32f, 1f), new Vector3(0f, 2f, -8f), FaceOrientation.Front, centeredOnOrigin: true);
				group2.Texture = CMProvider.Global.Load<Texture2D>("Skies/WATERFRONT/WATERFRONT_B");
				group2.Material = new Material
				{
					Opacity = 0.5f,
					Diffuse = Vector3.Lerp(Vector3.One, new Vector3(0.1215686f, 0.96f, 1f), 0.5f)
				};
				Group group3 = layers.AddFace(new Vector3(32f, 32f, 1f), new Vector3(0f, 2f, -8f), FaceOrientation.Front, centeredOnOrigin: true);
				group3.Texture = CMProvider.Global.Load<Texture2D>("Skies/WATERFRONT/WATERFRONT_A");
				group3.Material = new Material
				{
					Opacity = 1f,
					Diffuse = Vector3.Lerp(Vector3.One, new Vector3(0.1215686f, 0.96f, 1f), 0.4f)
				};
			});
			WarpPanel graveyardPanel = new WarpPanel
			{
				Face = FaceOrientation.Right,
				Destination = "GRAVEYARD_GATE",
				PanelMask = new Mesh
				{
					DepthWrites = false
				},
				Layers = new Mesh
				{
					AlwaysOnTop = true,
					DepthWrites = false,
					SamplerState = SamplerState.PointClamp
				}
			};
			panels.Add(WarpDestinations.Graveyard, graveyardPanel);
			DrawActionScheduler.Schedule(delegate
			{
				Mesh panelMask2 = graveyardPanel.PanelMask;
				panelMask2.Effect = new DefaultEffect.Textured();
				panelMask2.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/fullwhite");
				panelMask2.AddFace(new Vector3(3.875f), Vector3.Right * 1.5625f, FaceOrientation.Right, centeredOnOrigin: true);
				Mesh layers2 = graveyardPanel.Layers;
				layers2.Effect = new DefaultEffect.Textured();
				Texture2D texture2 = CMProvider.Global.Load<Texture2D>("Other Textures/warp/graveyard/back");
				Group group4 = layers2.AddFace(Vector3.One * 16f, Vector3.Zero, FaceOrientation.Right, centeredOnOrigin: true);
				group4.Texture = texture2;
				group4.SamplerState = SamplerState.PointWrap;
				layers2.AddFace(new Vector3(1f, 16f, 32f), new Vector3(-8f, 4f, 0f), FaceOrientation.Right, centeredOnOrigin: true).Texture = CMProvider.Global.Load<Texture2D>("Skies/GRAVE/GRAVE_CLOUD_C");
				layers2.AddFace(new Vector3(1f, 16f, 32f), new Vector3(-8f, 4f, 0f), FaceOrientation.Right, centeredOnOrigin: true).Texture = CMProvider.Global.Load<Texture2D>("Skies/GRAVE/GRAVE_CLOUD_B");
				layers2.AddFace(new Vector3(1f, 16f, 32f), new Vector3(-8f, 4f, 0f), FaceOrientation.Right, centeredOnOrigin: true).Texture = CMProvider.Global.Load<Texture2D>("Skies/GRAVE/GRAVE_CLOUD_A");
				Group group5 = layers2.AddFace(Vector3.One * 16f, Vector3.Zero, FaceOrientation.Right, centeredOnOrigin: true);
				group5.SamplerState = SamplerState.PointWrap;
				group5.Blending = BlendingMode.Additive;
				group5.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/warp/graveyard/rainoverlay");
			});
			WarpPanel industrialPanel = new WarpPanel
			{
				Face = FaceOrientation.Left,
				Destination = "INDUSTRIAL_HUB",
				PanelMask = new Mesh
				{
					DepthWrites = false
				},
				Layers = new Mesh
				{
					AlwaysOnTop = true,
					DepthWrites = false,
					SamplerState = SamplerState.PointClamp
				}
			};
			panels.Add(WarpDestinations.Mechanical, industrialPanel);
			DrawActionScheduler.Schedule(delegate
			{
				Mesh panelMask3 = industrialPanel.PanelMask;
				panelMask3.Effect = new DefaultEffect.Textured();
				panelMask3.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/fullwhite");
				panelMask3.AddFace(new Vector3(3.875f), Vector3.Left * 1.5625f, FaceOrientation.Left, centeredOnOrigin: true);
				Mesh layers3 = industrialPanel.Layers;
				layers3.Effect = new DefaultEffect.Textured();
				Texture2D texture3 = CMProvider.Global.Load<Texture2D>("Other Textures/warp/industrial/background");
				layers3.AddFace(new Vector3(10f, 4f, 10f), Vector3.Right * 5f, FaceOrientation.Left, centeredOnOrigin: true).Texture = texture3;
				layers3.AddFace(new Vector3(10f, 4f, 10f), Vector3.Backward * 5f, FaceOrientation.Back, centeredOnOrigin: true).Texture = texture3;
				layers3.AddFace(new Vector3(10f, 4f, 10f), Vector3.Forward * 5f, FaceOrientation.Front, centeredOnOrigin: true).Texture = texture3;
				Group group6 = layers3.AddFace(new Vector3(1f, 8f, 8f), new Vector3(8f, 0f, 0f), FaceOrientation.Left, centeredOnOrigin: true);
				group6.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/warp/industrial/INDUST_CLOUD_B");
				group6.Material = new Material
				{
					Opacity = 0.5f
				};
				Group group7 = layers3.AddFace(new Vector3(1f, 8f, 8f), new Vector3(8f, 0f, 0f), FaceOrientation.Left, centeredOnOrigin: true);
				group7.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/warp/industrial/INDUST_CLOUD_F");
				group7.Material = new Material
				{
					Opacity = 0.325f
				};
			});
			WarpPanel sewerPanel = new WarpPanel
			{
				Face = FaceOrientation.Back,
				Destination = "SEWER_HUB",
				PanelMask = new Mesh
				{
					DepthWrites = false
				},
				Layers = new Mesh
				{
					AlwaysOnTop = true,
					DepthWrites = false,
					SamplerState = SamplerState.PointClamp
				}
			};
			panels.Add(WarpDestinations.Sewers, sewerPanel);
			DrawActionScheduler.Schedule(delegate
			{
				Mesh panelMask4 = sewerPanel.PanelMask;
				panelMask4.Effect = new DefaultEffect.Textured();
				panelMask4.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/fullwhite");
				panelMask4.AddFace(new Vector3(3.875f), Vector3.Forward * 1.5625f, FaceOrientation.Back, centeredOnOrigin: true);
				Mesh layers4 = sewerPanel.Layers;
				layers4.Effect = new DefaultEffect.Textured();
				Texture2D texture4 = CMProvider.Global.Load<Texture2D>("Skies/SEWER/BRICK_BACKGROUND");
				Group group8 = layers4.AddFace(Vector3.One * 16f, Vector3.Backward * 8f, FaceOrientation.Back, centeredOnOrigin: true);
				group8.Texture = texture4;
				group8.SamplerState = SamplerState.PointWrap;
				Group group9 = layers4.AddFace(Vector3.One * 16f, Vector3.Right * 8f, FaceOrientation.Left, centeredOnOrigin: true);
				group9.Texture = texture4;
				group9.SamplerState = SamplerState.PointWrap;
				Group group10 = layers4.AddFace(Vector3.One * 16f, Vector3.Left * 8f, FaceOrientation.Right, centeredOnOrigin: true);
				group10.Texture = texture4;
				group10.SamplerState = SamplerState.PointWrap;
				Group group11 = layers4.AddFace(new Vector3(128f, 8f, 1f), new Vector3(0f, 4f, -8f), FaceOrientation.Back, centeredOnOrigin: true);
				group11.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/warp/sewer/sewage");
				group11.SamplerState = SamplerState.PointWrap;
			});
			WarpPanel zuPanel = new WarpPanel
			{
				Face = FaceOrientation.Front,
				Destination = "ZU_CITY_RUINS",
				PanelMask = new Mesh
				{
					DepthWrites = false
				},
				Layers = new Mesh
				{
					AlwaysOnTop = true,
					DepthWrites = false,
					SamplerState = SamplerState.PointClamp
				}
			};
			panels.Add(WarpDestinations.Zu, zuPanel);
			DrawActionScheduler.Schedule(delegate
			{
				Mesh panelMask5 = zuPanel.PanelMask;
				panelMask5.Effect = new DefaultEffect.Textured();
				panelMask5.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/fullwhite");
				panelMask5.AddFace(new Vector3(3.875f), Vector3.Backward * 1.5625f, FaceOrientation.Front, centeredOnOrigin: true);
				Mesh layers5 = zuPanel.Layers;
				layers5.Effect = new DefaultEffect.Textured();
				Texture2D texture5 = CMProvider.Global.Load<Texture2D>("Other Textures/warp/zu/back");
				Group group12 = layers5.AddFace(new Vector3(16f, 32f, 16f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
				group12.Texture = texture5;
				group12.SamplerState = SamplerState.PointWrap;
				Group group13 = layers5.AddFace(new Vector3(32f, 32f, 1f), new Vector3(0f, 0f, -8f), FaceOrientation.Front, centeredOnOrigin: true);
				group13.Texture = CMProvider.Global.Load<Texture2D>("Skies/ABOVE/ABOVE_C");
				group13.Material = new Material
				{
					Opacity = 0.4f,
					Diffuse = Vector3.Lerp(new Vector3(35f / 51f, 1f, 0.97647f), new Vector3(11f / 85f, 0.4f, 1f), 0.7f)
				};
				Group group14 = layers5.AddFace(new Vector3(32f, 32f, 1f), new Vector3(0f, 0f, -8f), FaceOrientation.Front, centeredOnOrigin: true);
				group14.Texture = CMProvider.Global.Load<Texture2D>("Skies/ABOVE/ABOVE_B");
				group14.Material = new Material
				{
					Opacity = 0.6f,
					Diffuse = Vector3.Lerp(new Vector3(35f / 51f, 1f, 0.97647f), new Vector3(11f / 85f, 0.4f, 1f), 0.6f)
				};
				Group group15 = layers5.AddFace(new Vector3(32f, 32f, 1f), new Vector3(0f, 0f, -8f), FaceOrientation.Front, centeredOnOrigin: true);
				group15.Texture = CMProvider.Global.Load<Texture2D>("Skies/ABOVE/ABOVE_A");
				group15.Material = new Material
				{
					Opacity = 1f,
					Diffuse = Vector3.Lerp(new Vector3(35f / 51f, 1f, 0.97647f), new Vector3(11f / 85f, 0.4f, 1f), 0.5f)
				};
			});
		}
		if (Fez.LongScreenshot)
		{
			GameState.SaveData.UnlockedWarpDestinations.Add("SEWER_HUB");
			GameState.SaveData.UnlockedWarpDestinations.Add("GRAVEYARD_GATE");
			GameState.SaveData.UnlockedWarpDestinations.Add("INDUSTRIAL_HUB");
			GameState.SaveData.UnlockedWarpDestinations.Add("ZU_CITY_RUINS");
		}
		string text = LevelManager.Name.Replace('\\', '/');
		CurrentLevelName = text.Substring(text.LastIndexOf('/') + 1);
		Volume volume;
		if (!GameState.SaveData.UnlockedWarpDestinations.Contains(CurrentLevelName))
		{
			GameState.SaveData.UnlockedWarpDestinations.Add(CurrentLevelName);
		}
		else if (GameState.SaveData.UnlockedWarpDestinations.Count > 1 && (volume = LevelManager.Volumes.Values.FirstOrDefault((Volume x) => x.ActorSettings != null && x.ActorSettings.IsPointOfInterest && Vector3.DistanceSquared(x.BoundingBox.GetCenter(), warpGateAo.Position) < 4f)) != null)
		{
			volume.ActorSettings.DotDialogue.Clear();
			volume.ActorSettings.DotDialogue.AddRange(new DotDialogueLine[3]
			{
				new DotDialogueLine
				{
					ResourceText = "DOT_WARP_A",
					Grouped = true
				},
				new DotDialogueLine
				{
					ResourceText = "DOT_WARP_B",
					Grouped = true
				},
				new DotDialogueLine
				{
					ResourceText = "DOT_WARP_UP",
					Grouped = true
				}
			});
			if (GameState.SaveData.OneTimeTutorials.TryGetValue("DOT_WARP_A", out var value) && value)
			{
				volume.ActorSettings.PreventHey = true;
			}
		}
		Vector3 zero = Vector3.Zero;
		if (warpGateAo.ArtObject.Name == "GATE_INDUSTRIALAO")
		{
			zero -= Vector3.UnitY;
		}
		foreach (WarpPanel value2 in panels.Values)
		{
			value2.PanelMask.Position = warpGateAo.Position + zero;
			value2.Layers.Position = warpGateAo.Position + zero;
			value2.Enabled = value2.Destination != CurrentLevelName && GameState.SaveData.UnlockedWarpDestinations.Contains(value2.Destination);
			if (value2.Destination == "ZU_CITY_RUINS")
			{
				switch (CurrentLevelName)
				{
				case "NATURE_HUB":
					value2.Face = FaceOrientation.Front;
					value2.PanelMask.Rotation = Quaternion.Identity;
					value2.Layers.Rotation = Quaternion.Identity;
					break;
				case "GRAVEYARD_GATE":
					value2.Face = FaceOrientation.Right;
					value2.PanelMask.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 2f);
					value2.Layers.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 2f);
					break;
				case "INDUSTRIAL_HUB":
					value2.Face = FaceOrientation.Left;
					value2.PanelMask.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, 4.712389f);
					value2.Layers.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, 4.712389f);
					break;
				case "SEWER_HUB":
					value2.Face = FaceOrientation.Back;
					value2.PanelMask.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI);
					value2.Layers.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI);
					break;
				}
			}
		}
	}

	protected override void LoadContent()
	{
		LightingPostProcess.DrawOnTopLights += DrawLights;
	}

	public override void Update(GameTime gameTime)
	{
		if (!GameState.Loading && !warpGateAo.ActorSettings.Inactive && !GameState.InMap && !GameState.Paused)
		{
			UpdateParallax(gameTime.ElapsedGameTime);
			UpdateDoors();
		}
	}

	private void UpdateDoors()
	{
		if (!CameraManager.Viewpoint.IsOrthographic() || InputManager.Up != FezButtonState.Pressed || !PlayerManager.Grounded || PlayerManager.Action == ActionType.GateWarp || PlayerManager.Action == ActionType.WakingUp || PlayerManager.Action == ActionType.LesserWarp || !SpeechBubble.Hidden)
		{
			return;
		}
		Vector3 vector = PlayerManager.Position * CameraManager.Viewpoint.ScreenSpaceMask();
		Vector3 vector2 = warpGateAo.Position * CameraManager.Viewpoint.ScreenSpaceMask();
		if (warpGateAo.ArtObject.Size.Y == 8f)
		{
			vector2 -= new Vector3(0f, 1f, 0f);
		}
		Vector3 a = vector2 - vector;
		if (Math.Abs(a.Dot(CameraManager.Viewpoint.SideMask())) > 1.5f || Math.Abs(a.Y) > 2f)
		{
			return;
		}
		foreach (WarpPanel value in panels.Values)
		{
			if (value.Enabled && value.Face == CameraManager.Viewpoint.VisibleOrientation())
			{
				PlayerManager.Action = ActionType.GateWarp;
				PlayerManager.WarpPanel = value;
				PlayerManager.OriginWarpViewpoint = panels.Values.First((WarpPanel x) => x.Destination == CurrentLevelName).Face.AsViewpoint();
				break;
			}
		}
	}

	private void UpdateParallax(TimeSpan elapsed)
	{
		if (!CameraManager.Viewpoint.IsOrthographic())
		{
			return;
		}
		float amount = MathHelper.Clamp((float)elapsed.TotalSeconds * CameraManager.InterpolationSpeed, 0f, 1f);
		InterpolatedCenter = Vector3.Lerp(InterpolatedCenter, CameraManager.Center, amount);
		Vector3 forward = CameraManager.View.Forward;
		forward.Z *= -1f;
		Vector3 right = CameraManager.View.Right;
		Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
		Vector3 vector2 = (InterpolatedCenter - warpGateAo.Position) / 2.5f;
		Vector3 a = (CameraManager.InterpolatedCenter - warpGateAo.Position) * vector;
		foreach (WarpDestinations key in panels.Keys)
		{
			WarpPanel warpPanel = panels[key];
			if (warpPanel.Enabled && !(warpPanel.Face.AsVector().Dot(forward) <= 0f))
			{
				warpPanel.Timer += elapsed;
				Vector3 vector3 = vector2 * vector;
				switch (key)
				{
				case WarpDestinations.Mechanical:
					warpPanel.Layers.Groups[3].Position = vector3 + new Vector3(0f, 2f, 0f) + (float)(warpPanel.Timer.TotalSeconds % 16.0 - 8.0) * right;
					warpPanel.Layers.Groups[4].Position = vector3 - new Vector3(0f, 2f, 0f) + (float)((warpPanel.Timer.TotalSeconds + 8.0) % 16.0 - 8.0) * right;
					break;
				case WarpDestinations.First:
					warpPanel.Layers.Groups[3].Position = vector3 - Vector3.UnitY * 1.5f + (float)(warpPanel.Timer.TotalSeconds * 0.25 % 16.0 - 8.0) * right;
					warpPanel.Layers.Groups[4].Position = vector3 - Vector3.UnitY * 1.5f + (float)(warpPanel.Timer.TotalSeconds * 0.5 % 16.0 - 8.0) * right;
					warpPanel.Layers.Groups[5].Position = vector3 - Vector3.UnitY * 1.5f + (float)(warpPanel.Timer.TotalSeconds % 16.0 - 8.0) * right;
					break;
				case WarpDestinations.Zu:
				{
					float m3 = (0f - a.Dot(right)) / 16f;
					float m4 = a.Y / 32f;
					warpPanel.Layers.Groups[0].TextureMatrix.Set(new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, m3, m4, 1f, 0f, 0f, 0f, 0f, 1f));
					warpPanel.Layers.Groups[1].Position = vector3 - Vector3.UnitY * 1.5f + (float)(warpPanel.Timer.TotalSeconds * 0.25 % 16.0 - 8.0) * right;
					warpPanel.Layers.Groups[2].Position = vector3 - Vector3.UnitY * 1.5f + (float)(warpPanel.Timer.TotalSeconds * 0.5 % 16.0 - 8.0) * right;
					warpPanel.Layers.Groups[3].Position = vector3 - Vector3.UnitY * 1.5f + (float)(warpPanel.Timer.TotalSeconds % 16.0 - 8.0) * right;
					break;
				}
				case WarpDestinations.Graveyard:
				{
					float num = (a.X + a.Z) / 16f;
					float num2 = a.Y / 16f;
					warpPanel.Layers.Groups[0].TextureMatrix.Set(new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, num, num2, 1f, 0f, 0f, 0f, 0f, 1f));
					warpPanel.Layers.Groups[4].TextureMatrix.Set(new Matrix(2f, 0f, 0f, 0f, 0f, 2f, 0f, 0f, num / 2f, num2 / 2f - (float)warpPanel.Timer.TotalSeconds * 5f, 1f, 0f, 0f, 0f, 0f, 1f));
					warpPanel.Layers.Groups[1].Position = vector3 - Vector3.UnitY * 1.5f + (float)(warpPanel.Timer.TotalSeconds * 0.5 % 16.0 - 8.0) * right;
					warpPanel.Layers.Groups[2].Position = vector3 - Vector3.UnitY * 1.5f + (float)(warpPanel.Timer.TotalSeconds % 16.0 - 8.0) * right;
					warpPanel.Layers.Groups[3].Position = vector3 - Vector3.UnitY * 1.5f + (float)(warpPanel.Timer.TotalSeconds * 2.0 % 16.0 - 8.0) * right;
					break;
				}
				case WarpDestinations.Sewers:
				{
					float m = (a.X + a.Z) / 8f;
					float m2 = a.Y / 8f;
					Matrix value = new Matrix(4f, 0f, 0f, 0f, 0f, 4f, 0f, 0f, m, m2, 1f, 0f, 0f, 0f, 0f, 1f);
					warpPanel.Layers.Groups[0].TextureMatrix.Set(value);
					warpPanel.Layers.Groups[1].TextureMatrix.Set(value);
					warpPanel.Layers.Groups[2].TextureMatrix.Set(value);
					warpPanel.Layers.Groups[3].Position = vector3 + new Vector3(0f, -8f, 0f);
					break;
				}
				}
			}
		}
	}

	private void DrawLights()
	{
		if (!base.Visible || GameState.Loading || warpGateAo.ActorSettings.Inactive || Fez.LongScreenshot)
		{
			return;
		}
		foreach (WarpDestinations key in panels.Keys)
		{
			WarpPanel warpPanel = panels[key];
			if (warpPanel.Enabled)
			{
				(warpPanel.PanelMask.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
				warpPanel.PanelMask.Draw();
				(warpPanel.PanelMask.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
			}
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.Loading && !warpGateAo.ActorSettings.Inactive && !GameState.StereoMode)
		{
			DoDraw();
		}
	}

	public void DoDraw()
	{
		foreach (WarpDestinations key in panels.Keys)
		{
			WarpPanel warpPanel = panels[key];
			if (warpPanel.Enabled)
			{
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
				base.GraphicsDevice.PrepareStencilWrite(StencilMask.WarpGate);
				warpPanel.PanelMask.Draw();
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.WarpGate);
				warpPanel.Layers.Draw();
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
				base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
				warpPanel.PanelMask.Draw();
			}
		}
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
	}
}
