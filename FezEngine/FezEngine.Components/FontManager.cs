using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public class FontManager : GameComponent, IFontManager
{
	private Language lastLanguage;

	public SpriteFont Big { get; private set; }

	public SpriteFont Small { get; private set; }

	public float SmallFactor { get; private set; }

	public float BigFactor { get; private set; }

	public float TopSpacing { get; private set; }

	public float SideSpacing { get; private set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public FontManager(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		ReloadFont(force: true);
	}

	private void ReloadFont(bool force = false)
	{
		if (force || lastLanguage.IsCjk() || Culture.Language.IsCjk())
		{
			switch (Culture.Language)
			{
			case Language.Japanese:
			{
				Small = CMProvider.Global.Load<SpriteFont>("Fonts/Japanese Small");
				Big = CMProvider.Global.Load<SpriteFont>("Fonts/Japanese Big");
				SmallFactor = 0.5f;
				BigFactor = 0.34125f;
				float sideSpacing = (TopSpacing = 10f);
				SideSpacing = sideSpacing;
				break;
			}
			case Language.Korean:
			{
				Small = CMProvider.Global.Load<SpriteFont>("Fonts/Korean Small");
				Big = CMProvider.Global.Load<SpriteFont>("Fonts/Korean Big");
				SmallFactor = 0.5f;
				BigFactor = 0.34125f;
				float sideSpacing = (TopSpacing = 10f);
				SideSpacing = sideSpacing;
				break;
			}
			case Language.Chinese:
			{
				Small = CMProvider.Global.Load<SpriteFont>("Fonts/Chinese Small");
				Big = CMProvider.Global.Load<SpriteFont>("Fonts/Chinese Big");
				SmallFactor = 0.5f;
				BigFactor = 0.34125f;
				float sideSpacing = (TopSpacing = 10f);
				SideSpacing = sideSpacing;
				break;
			}
			default:
				Small = CMProvider.Global.Load<SpriteFont>("Fonts/Latin Small");
				Big = CMProvider.Global.Load<SpriteFont>("Fonts/Latin Big");
				SmallFactor = 1.5f;
				BigFactor = 2f;
				Small.LineSpacing = 18;
				Big.LineSpacing = 18;
				SideSpacing = 8f;
				TopSpacing = 0f;
				break;
			}
		}
		Small.DefaultCharacter = ' ';
		Big.DefaultCharacter = ' ';
		lastLanguage = Culture.Language;
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (lastLanguage != Culture.Language)
		{
			ReloadFont();
		}
	}
}
