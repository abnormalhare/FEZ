using System;
using System.Collections.Generic;
using System.Globalization;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Structure;

internal class CreditsMenuLevel : MenuLevel
{
	private List<CreditsEntry> scroller;

	private float sinceStarted;

	private float destinationTime;

	private RenderTarget2D maskRT;

	private GraphicsDevice GraphicsDevice;

	private bool started;

	private float wasFactor;

	private SpriteBatch SpriteBatch;

	private GlyphTextRenderer gtr;

	private bool ready;

	public IFontManager FontManager { private get; set; }

	public override void Initialize()
	{
		base.Initialize();
		GraphicsDevice = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice;
		GraphicsDevice.DeviceReset += LoadCredits;
		SpriteBatch = new SpriteBatch(GraphicsDevice);
		gtr = new GlyphTextRenderer(ServiceHelper.Game);
		OnClose = delegate
		{
			if (started)
			{
				ServiceHelper.Get<ISoundManager>().PlayNewSong(null, 0.5f);
				ServiceHelper.Get<ISoundManager>().UnshelfSong();
				ServiceHelper.Get<ISoundManager>().MusicVolumeFactor = wasFactor;
			}
			if (maskRT != null)
			{
				maskRT.Dispose();
				maskRT = null;
			}
			started = false;
			sinceStarted = (destinationTime = 0f);
		};
		scroller = new List<CreditsEntry>
		{
			new CreditsEntry
			{
				Size = new Vector2(512f, 512f)
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("PolytronProduction"),
				IsTitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("Design"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("ArtLevelDesignCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("Programming"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("ProgrammingCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("Producer"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("ProducerCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("Music"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("MusicCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("SoundEffects"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("SoundEffectsCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("Animation"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("AnimationCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("MacLinuxDeveloper"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("MacLinuxDeveloperCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("MacLinuxQaTeam"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("MacLinuxQaTeamCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("PcQaTeam"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("PcQaTeamCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("SupportedBy"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("SupportedByCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("PhilSpecialThanks"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("PhilSpecialThanksCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("RenoSpecialThanks"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("RenoSpecialThanksCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("XblaSpecialThanks"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("XblaSpecialThanksCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("EndMusicCredits"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("ThirdParty"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("ThirdPartyCredits")
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("LicenseInfo"),
				IsSubtitle = true
			},
			new CreditsEntry
			{
				Text = CreditsText.GetString("PolytronFooter"),
				IsTitle = true
			}
		};
		scroller[30].Text += "\r\nSteamworks.NET";
		LoadCredits();
		foreach (CreditsEntry item in scroller)
		{
			if (item.Text != null)
			{
				item.Text = item.Text.ToUpper(CultureInfo.InvariantCulture);
				if (item.IsTitle)
				{
					item.Size = FontManager.Big.MeasureString(item.Text) * FontManager.BigFactor;
				}
				else
				{
					item.Size = FontManager.Small.MeasureString(item.Text) * FontManager.SmallFactor;
				}
			}
		}
	}

	private void LoadCredits(object _, EventArgs __)
	{
		LoadCredits();
	}

	private void LoadCredits()
	{
		if (maskRT != null)
		{
			maskRT.Dispose();
		}
		maskRT = null;
		ready = false;
		ContentManager global = base.CMProvider.Global;
		float viewScale = GraphicsDevice.GetViewScale();
		bool flag = viewScale >= 1.5f;
		bool flag2 = viewScale >= 2f;
		scroller[0].Size = new Vector2(512f);
		scroller[0].Image = global.Load<Texture2D>("Other Textures/credits_logo" + (flag2 ? "_1440" : (flag ? "_1080" : "")));
		ready = true;
	}

	public override void Dispose()
	{
		base.Dispose();
		GraphicsDevice.DeviceReset -= LoadCredits;
		if (maskRT != null)
		{
			maskRT.Dispose();
		}
		maskRT = null;
	}

	public override void Reset()
	{
		destinationTime = 0f;
		sinceStarted = -0.5f;
		if (!started)
		{
			ServiceHelper.Get<ISoundManager>().PlayNewSong("Gomez", 0f, interrupt: false);
			wasFactor = ServiceHelper.Get<ISoundManager>().MusicVolumeFactor;
			ServiceHelper.Get<ISoundManager>().MusicVolumeFactor = 1f;
		}
		started = true;
	}

	public override void Update(TimeSpan elapsed)
	{
		if (!ServiceHelper.Game.IsActive || !ready)
		{
			return;
		}
		sinceStarted += (float)elapsed.TotalSeconds;
		float viewScale = GraphicsDevice.GetViewScale();
		GraphicsDevice.SetRenderTarget(maskRT);
		GraphicsDevice.Clear(ClearOptions.Target, ColorEx.TransparentWhite, 1f, 0);
		SpriteBatch.BeginPoint();
		float num = 27.5f * viewScale;
		SpriteFont small = FontManager.Small;
		float num2 = 0f;
		destinationTime = MathHelper.Lerp(destinationTime, Math.Max(sinceStarted, 0f), 0.01f);
		bool flag = true;
		foreach (CreditsEntry item in scroller)
		{
			float num3 = num2 - num * destinationTime;
			if (num3 + item.Size.Y * viewScale > -50f * viewScale && num3 < 600f * viewScale)
			{
				flag = false;
				if (item.Image != null)
				{
					float num4 = FezMath.Saturate(sinceStarted * 2f);
					SpriteBatch.Draw(item.Image, new Vector2(512f * viewScale - (float)FezMath.Round(256f * viewScale), FezMath.Round(num3)), new Color(num4, num4, num4, num4));
					GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
					num2 += 512f * viewScale - item.Size.Y * viewScale;
				}
				else
				{
					if (item.IsTitle)
					{
						num3 += 60f * viewScale;
						num2 += 60f * viewScale;
					}
					if (item.IsSubtitle)
					{
						num3 += 30f * viewScale;
						num2 += 30f * viewScale;
					}
					Color color = (item.IsSubtitle ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.White);
					float num5 = (item.IsTitle ? FontManager.BigFactor : FontManager.SmallFactor);
					SpriteFont font = (item.IsTitle ? FontManager.Big : small);
					gtr.DrawCenteredString(SpriteBatch, font, item.Text, color, new Vector2(0f, FezMath.Round(num3)), num5 * viewScale, shadow: true);
					if (item.IsTitle)
					{
						num2 += 15f * viewScale;
					}
				}
			}
			else
			{
				if (item.IsTitle)
				{
					num2 += 75f * viewScale;
				}
				if (item.IsSubtitle)
				{
					num2 += 30f * viewScale;
				}
			}
			num2 += item.Size.Y * viewScale;
		}
		if (flag)
		{
			base.ForceCancel = true;
		}
		SpriteBatch.End();
		GraphicsDevice.SetRenderTarget(null);
	}

	public override void PostDraw(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha)
	{
		int width = batch.GraphicsDevice.Viewport.Width;
		int height = batch.GraphicsDevice.Viewport.Height;
		float viewScale = batch.GraphicsDevice.GetViewScale();
		if (maskRT == null)
		{
			maskRT = new RenderTarget2D(GraphicsDevice, (int)(1024f * viewScale), (int)(512f * viewScale), mipMap: false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PlatformContents);
			Update(TimeSpan.Zero);
		}
		batch.Draw(maskRT, new Vector2((float)width / 2f - 512f * viewScale, (float)height / 2f - 256f * viewScale), Color.White);
	}
}
