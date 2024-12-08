using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components.EndCutscene64;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class EndCutscene64Host : DrawableGameComponent
{
	public static readonly Color PurpleBlack = new Color(15, 1, 27);

	public readonly List<DrawableGameComponent> Scenes = new List<DrawableGameComponent>();

	private bool firstCycle = true;

	private bool noDestroy;

	public static EndCutscene64Host Instance;

	public SoundEmitter eNoise;

	public DrawableGameComponent ActiveScene => Scenes[0];

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	public EndCutscene64Host(Game game)
		: base(game)
	{
		Instance = this;
	}

	public override void Initialize()
	{
		base.Initialize();
		DrawableGameComponent item;
		ServiceHelper.AddComponent(item = new ZoomOut(base.Game, this));
		Scenes.Add(item);
		ServiceHelper.AddComponent(item = new MulticoloredSpace(base.Game, this));
		Scenes.Add(item);
		ServiceHelper.AddComponent(item = new DotsAplenty(base.Game, this));
		Scenes.Add(item);
		ServiceHelper.AddComponent(item = new WhiteNoise(base.Game, this));
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
		if (noDestroy)
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

	private void FirstCycle()
	{
		PlayerManager.Hidden = true;
		GameState.SkipRendering = true;
		IGameStateManager gameState = GameState;
		bool inEndCutscene = (GameState.InCutscene = true);
		gameState.InEndCutscene = inEndCutscene;
		GameState.SkyOpacity = 0f;
		noDestroy = true;
		LevelManager.Reset();
		noDestroy = false;
		SoundManager.PlayNewSong();
		SoundManager.PlayNewAmbience();
		SoundManager.KillSounds();
	}

	public void Cycle()
	{
		if (firstCycle)
		{
			FirstCycle();
			firstCycle = false;
		}
		ServiceHelper.RemoveComponent(ActiveScene);
		Scenes.RemoveAt(0);
		if (Scenes.Count > 0)
		{
			DrawableGameComponent activeScene = ActiveScene;
			bool enabled = (ActiveScene.Visible = true);
			activeScene.Enabled = enabled;
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
