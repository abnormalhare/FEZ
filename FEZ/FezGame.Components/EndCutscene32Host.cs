using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Components.EndCutscene32;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class EndCutscene32Host : DrawableGameComponent
{
	public static readonly Color PurpleBlack = new Color(15, 1, 27);

	public readonly List<DrawableGameComponent> Scenes = new List<DrawableGameComponent>();

	private bool firstCycle = true;

	private bool noDestroy;

	public static EndCutscene32Host Instance;

	public DrawableGameComponent ActiveScene => Scenes[0];

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	public EndCutscene32Host(Game game)
		: base(game)
	{
		Instance = this;
	}

	public override void Initialize()
	{
		base.Initialize();
		DrawableGameComponent item;
		ServiceHelper.AddComponent(item = new Pixelizer(base.Game, this));
		Scenes.Add(item);
		ServiceHelper.AddComponent(item = new FezGrid(base.Game, this));
		Scenes.Add(item);
		ServiceHelper.AddComponent(item = new Fractal(base.Game, this));
		Scenes.Add(item);
		ServiceHelper.AddComponent(item = new AxisDna(base.Game, this));
		Scenes.Add(item);
		ServiceHelper.AddComponent(item = new TetraordialOoze(base.Game, this));
		Scenes.Add(item);
		ServiceHelper.AddComponent(item = new VibratingMembrane(base.Game, this));
		Scenes.Add(item);
		ServiceHelper.AddComponent(item = new DrumSolo(base.Game, this));
		Scenes.Add(item);
		bool enabled;
		foreach (DrawableGameComponent scene in Scenes)
		{
			enabled = (scene.Visible = false);
			scene.Enabled = enabled;
		}
		DrawableGameComponent drawableGameComponent = Scenes[0];
		enabled = (Scenes[0].Visible = true);
		drawableGameComponent.Enabled = enabled;
		LevelManager.LevelChanged += TryDestroy;
	}

	private void TryDestroy()
	{
		if (!(LevelManager.Name != "DRUM") || noDestroy)
		{
			return;
		}
		foreach (DrawableGameComponent scene in Scenes)
		{
			ServiceHelper.RemoveComponent(scene);
		}
		Scenes.Clear();
		ServiceHelper.RemoveComponent(this);
	}

	public void Cycle()
	{
		if (firstCycle)
		{
			PlayerManager.Hidden = false;
			GameState.SkipRendering = true;
			GameState.SkyOpacity = 0f;
			IGameStateManager gameState = GameState;
			bool inEndCutscene = (GameState.InCutscene = true);
			gameState.InEndCutscene = inEndCutscene;
			noDestroy = true;
			LevelManager.Reset();
			noDestroy = false;
			SoundManager.PlayNewSong("32bit", 0f);
			SoundManager.PlayNewAmbience();
			SoundManager.KillSounds();
			firstCycle = false;
		}
		ServiceHelper.RemoveComponent(ActiveScene);
		Scenes.RemoveAt(0);
		if (Scenes.Count > 0)
		{
			DrawableGameComponent activeScene = ActiveScene;
			bool inEndCutscene = (ActiveScene.Visible = true);
			activeScene.Enabled = inEndCutscene;
			ActiveScene.Update(new GameTime());
		}
		else
		{
			ServiceHelper.RemoveComponent(this);
		}
	}

	protected override void Dispose(bool disposing)
	{
		GameState.InEndCutscene = false;
		GameState.SkipRendering = false;
		Instance = null;
		LevelManager.LevelChanged -= TryDestroy;
		base.Dispose(disposing);
	}
}
