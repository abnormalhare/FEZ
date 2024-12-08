using Common;
using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class StaticPreloader : GameComponent
{
	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	public StaticPreloader(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		SharedContentManager.Preload();
		Logger.Log("StaticPreloader", "SharedContentManager preloaded.");
		SoundManager.InitializeLibrary();
		Logger.Log("StaticPreloader", "Music library initialized.");
		SoundManager.ReloadVolumeLevels();
		Logger.Log("StaticPreloader", "Volume levels loaded.");
		PlayerManager.FillAnimations();
		Logger.Log("StaticPreloader", "Animations filled.");
		TextScroll.PreInitialize();
		Logger.Log("StaticPreloader", "Text scroll pre-initialized.");
		WorldMap.PreInitialize();
		Logger.Log("StaticPreloader", "World map pre-initialized.");
		PauseMenu.PreInitialize();
		Logger.Log("StaticPreloader", "Pause menu pre-initialized.");
	}
}
