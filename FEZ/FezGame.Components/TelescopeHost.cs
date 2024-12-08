using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class TelescopeHost : DrawableGameComponent
{
	private enum StateType
	{
		Idle,
		In,
		VisibleWait,
		Out
	}

	private const float FadeSeconds = 0.75f;

	private const string Code = "RTLTLTRTRTLT";

	private readonly Dictionary<char, int[]> LetterValues = new Dictionary<char, int[]>(3)
	{
		{
			'R',
			new int[8] { 0, 1, 0, 1, 0, 0, 1, 0 }
		},
		{
			'T',
			new int[8] { 0, 1, 0, 1, 0, 1, 0, 0 }
		},
		{
			'L',
			new int[8] { 0, 1, 0, 0, 1, 1, 0, 0 }
		}
	};

	private readonly Dictionary<Viewpoint, Texture2D> Textures = new Dictionary<Viewpoint, Texture2D>(3, ViewpointComparer.Default);

	private Texture2D Mask;

	private Texture2D Vignette;

	private ArtObjectInstance TelescopeAo;

	private Viewpoint NowViewing;

	private readonly Texture2D[] LeftTextures = new Texture2D[3];

	private readonly int[] Message;

	private float BitTimer;

	private int MessageIndex;

	private TimeSpan Fader;

	private StateType State;

	[ServiceDependency]
	public ITimeManager TimeManager { get; set; }

	[ServiceDependency]
	public IDotManager DotManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IInputManager InputManager { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	public TelescopeHost(Game game)
		: base(game)
	{
		base.DrawOrder = 100;
		Message = "RTLTLTRTRTLT".SelectMany((char x) => LetterValues[x]).ToArray();
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		Textures.Clear();
		LeftTextures[0] = (LeftTextures[1] = (LeftTextures[2] = null));
		base.Enabled = (TelescopeAo = LevelManager.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.Telescope)) != null;
		if (!base.Enabled)
		{
			base.Visible = false;
			return;
		}
		DrawActionScheduler.Schedule(delegate
		{
			Textures.Add(Viewpoint.Front, CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/telescope/TELESCOPE_STARS_A"));
			Textures.Add(Viewpoint.Right, CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/telescope/TELESCOPE_STARS_B"));
			Textures.Add(Viewpoint.Back, CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/telescope/TELESCOPE_STARS_C"));
			LeftTextures[0] = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/telescope/TELESCOPE_STARS_D_0");
			LeftTextures[1] = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/telescope/TELESCOPE_STARS_D_1");
			LeftTextures[2] = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/telescope/TELESCOPE_STARS_D_PAUSE");
			Mask = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/telescope/TELESCOPE_MASK");
			Vignette = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/telescope/TELESCOPE_VIGNETTE");
		});
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || GameState.InMenuCube || GameState.InFpsMode)
		{
			return;
		}
		SkyHost.Instance.RotateLayer(0, TelescopeAo.Rotation);
		SkyHost.Instance.RotateLayer(1, TelescopeAo.Rotation);
		SkyHost.Instance.RotateLayer(2, TelescopeAo.Rotation);
		SkyHost.Instance.RotateLayer(3, TelescopeAo.Rotation);
		if (State == StateType.Idle)
		{
			CheckForUp();
		}
		else if (State == StateType.Out)
		{
			Fader -= gameTime.ElapsedGameTime;
			Fader -= gameTime.ElapsedGameTime;
		}
		else
		{
			Fader += gameTime.ElapsedGameTime;
			CheckForExit();
		}
		if (Fader.TotalSeconds > 0.75 || Fader.TotalSeconds < 0.0)
		{
			switch (State)
			{
			case StateType.In:
				State = StateType.VisibleWait;
				break;
			case StateType.Out:
				State = StateType.Idle;
				base.Visible = false;
				PlayerManager.CanControl = true;
				break;
			}
		}
	}

	private void CheckForUp()
	{
		if (GameState.Loading || GameState.InMap || GameState.Paused || !CameraManager.ActionRunning || !PlayerManager.Grounded || InputManager.GrabThrow != FezButtonState.Pressed)
		{
			return;
		}
		Viewpoint viewpoint = FezMath.OrientationFromDirection(FezMath.AlmostClamp(Vector3.Transform(Vector3.Left, TelescopeAo.Rotation))).AsViewpoint();
		if (CameraManager.Viewpoint == viewpoint)
		{
			Vector3 vector = Vector3.Transform(PlayerManager.Position - TelescopeAo.Position, TelescopeAo.Rotation);
			if (Math.Abs(vector.Z) < 0.5f && Math.Abs(vector.Y) < 0.5f)
			{
				base.Visible = true;
				State = StateType.In;
				BitTimer = 0f;
				MessageIndex = -3;
				Fader = TimeSpan.Zero;
				NowViewing = CameraManager.Viewpoint;
				PlayerManager.CanControl = false;
				PlayerManager.Action = ActionType.ReadTurnAround;
				DotManager.Hidden = true;
				DotManager.PreventPoI = true;
			}
		}
	}

	private void CheckForExit()
	{
		if (InputManager.Back == FezButtonState.Pressed || InputManager.CancelTalk == FezButtonState.Pressed || InputManager.GrabThrow == FezButtonState.Pressed)
		{
			State = StateType.Out;
			Fader = TimeSpan.FromSeconds(0.75);
			DotManager.Hidden = false;
			DotManager.PreventPoI = false;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InFpsMode || GameState.Loading || GameState.InMap)
		{
			return;
		}
		float num = Easing.EaseOut(FezMath.Saturate(Fader.TotalSeconds / 0.75), EasingType.Sine);
		Texture2D texture2D;
		if (NowViewing == Viewpoint.Left)
		{
			BitTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (BitTimer <= 0f)
			{
				MessageIndex++;
				if (MessageIndex > Message.Length + 2)
				{
					MessageIndex = -1;
				}
				BitTimer = ((MessageIndex < 0 || MessageIndex >= Message.Length || Message[MessageIndex] == 1) ? 0.75f : 0.5f);
			}
			texture2D = ((MessageIndex >= 0 && MessageIndex < Message.Length && !(BitTimer < 0.25f)) ? LeftTextures[Message[MessageIndex]] : LeftTextures[2]);
		}
		else
		{
			texture2D = Textures[NowViewing];
		}
		float num2 = base.GraphicsDevice.Viewport.Width;
		float num3 = base.GraphicsDevice.Viewport.Height;
		float num4 = texture2D.Width;
		float num5 = texture2D.Height;
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		float viewScale = base.GraphicsDevice.GetViewScale();
		Matrix textureMatrix = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, -0.5f, -0.5f, 1f, 0f, 0f, 0f, 0f, 0f) * new Matrix(num2 / num4 / 2f / viewScale, 0f, 0f, 0f, 0f, num3 / num5 / 2f / viewScale, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f) * new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0.5f, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f);
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.PrepareStencilWrite(StencilMask.CutsceneWipe);
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
		TargetRenderer.DrawFullscreen(Mask, textureMatrix, new Color(1f, 1f, 1f, num));
		graphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.CutsceneWipe);
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
		TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, num));
		graphicsDevice.GetDssCombiner().StencilFunction = CompareFunction.NotEqual;
		float skyOpacity = GameState.SkyOpacity;
		GameState.SkyOpacity = num;
		graphicsDevice.SetBlendingMode(BlendingMode.Additive);
		SkyHost.Instance.DrawBackground();
		GameState.SkyOpacity = skyOpacity;
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		graphicsDevice.GetDssCombiner().StencilFunction = CompareFunction.Always;
		TargetRenderer.DrawFullscreen(Vignette, textureMatrix, new Color(1f, 1f, 1f, num * (1f - TimeManager.NightContribution)));
		graphicsDevice.GetDssCombiner().StencilFunction = CompareFunction.NotEqual;
		TargetRenderer.DrawFullscreen(texture2D, textureMatrix, new Color(1f, 1f, 1f, num * TimeManager.NightContribution));
	}
}
