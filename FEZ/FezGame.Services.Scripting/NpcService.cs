using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Services.Scripting;

internal class NpcService : INpcService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public LongRunningAction Say(int id, string line, string customSound, string customAnimation)
	{
		SpeechLine speechLine = new SpeechLine
		{
			Text = line
		};
		NpcInstance npc = LevelManager.NonPlayerCharacters[id];
		if (!string.IsNullOrEmpty(customSound))
		{
			if (speechLine.OverrideContent == null)
			{
				speechLine.OverrideContent = new NpcActionContent();
			}
			speechLine.OverrideContent.Sound = LoadSound(customSound);
		}
		if (!string.IsNullOrEmpty(customAnimation))
		{
			if (speechLine.OverrideContent == null)
			{
				speechLine.OverrideContent = new NpcActionContent();
			}
			speechLine.OverrideContent.Animation = LoadAnimation(npc, customAnimation);
		}
		npc.CustomSpeechLine = speechLine;
		return new LongRunningAction((float _, float __) => npc.CustomSpeechLine == null);
	}

	public void CarryGeezerLetter(int id)
	{
		ServiceHelper.AddComponent(new GeezerLetterSender(ServiceHelper.Game, id));
	}

	private AnimatedTexture LoadAnimation(NpcInstance npc, string name)
	{
		string assetName = "Character Animations/" + npc.Name + "/" + name;
		return CMProvider.CurrentLevel.Load<AnimatedTexture>(assetName);
	}

	private SoundEffect LoadSound(string name)
	{
		string assetName = "Sounds/Npc/" + name;
		return CMProvider.CurrentLevel.Load<SoundEffect>(assetName);
	}

	public void ResetEvents()
	{
	}
}
