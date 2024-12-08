using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Effects.Structures;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Localization;

namespace FezGame.Components;

public class SpeechBubble : DrawableGameComponent, ISpeechBubbleManager
{
	private readonly Color TextColor = Color.White;

	private const int TextBorder = 4;

	private Vector2 scalableMiddleSize;

	private float sinceShown;

	private readonly Mesh textMesh;

	private readonly Mesh canvasMesh;

	private Group scalableMiddle;

	private Group scalableTop;

	private Group scalableBottom;

	private Group neGroup;

	private Group nwGroup;

	private Group seGroup;

	private Group swGroup;

	private Group tailGroup;

	private Group bGroup;

	private Group textGroup;

	private GlyphTextRenderer GTR;

	private SpriteBatch spriteBatch;

	private SpriteFont zuishFont;

	private RenderTarget2D text;

	private string originalString;

	private string textString;

	private float distanceFromCenterAtTextChange;

	private bool changingText;

	private bool show;

	private Vector3 origin;

	private Vector3 lastUsedOrigin;

	private RenderTarget2D bTexture;

	private Vector3 oldCamPos;

	public bool Hidden
	{
		get
		{
			if (!show)
			{
				return !changingText;
			}
			return false;
		}
	}

	public Vector3 Origin
	{
		private get
		{
			return origin;
		}
		set
		{
			origin = value;
			if (!FezMath.AlmostEqual(lastUsedOrigin, origin, 0.0625f) && sinceShown >= 1f && !changingText)
			{
				OnTextChanged(update: true);
			}
		}
	}

	public SpeechFont Font { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IFontManager FontManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public void ChangeText(string toText)
	{
		originalString = toText.ToUpper(CultureInfo.InvariantCulture);
		if (changingText)
		{
			return;
		}
		changingText = true;
		Waiters.Wait(() => sinceShown == 0f, delegate
		{
			if (changingText)
			{
				changingText = false;
				UpdateBTexture();
				OnTextChanged(update: false);
				show = true;
			}
		}).AutoPause = true;
	}

	public void Hide()
	{
		show = false;
		changingText = false;
	}

	public SpeechBubble(Game game)
		: base(game)
	{
		textMesh = new Mesh
		{
			AlwaysOnTop = true,
			SamplerState = SamplerState.PointClamp,
			Blending = BlendingMode.Alphablending
		};
		canvasMesh = new Mesh
		{
			AlwaysOnTop = true,
			SamplerState = SamplerState.PointClamp,
			Blending = BlendingMode.Alphablending
		};
		base.DrawOrder = 150;
		Font = SpeechFont.Pixel;
		show = false;
	}

	public override void Initialize()
	{
		scalableTop = canvasMesh.AddFace(new Vector3(1f, 0.5f, 0f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: false);
		scalableBottom = canvasMesh.CloneGroup(scalableTop);
		scalableMiddle = canvasMesh.AddFace(new Vector3(1f, 1f, 0f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: false);
		neGroup = canvasMesh.AddFace(new Vector3(0.5f, 0.5f, 0f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: false);
		nwGroup = canvasMesh.CloneGroup(neGroup);
		seGroup = canvasMesh.CloneGroup(neGroup);
		swGroup = canvasMesh.CloneGroup(neGroup);
		tailGroup = canvasMesh.AddFace(new Vector3(0.3125f, 0.25f, 0f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: false, doublesided: true);
		textGroup = textMesh.AddFace(new Vector3(1f, 1f, 0f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: false);
		bGroup = canvasMesh.AddFace(new Vector3(1f, 1f, 0f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: false);
		swGroup.Position = Vector3.Zero;
		scalableBottom.Position = new Vector3(0.5f, 0f, 0f);
		scalableMiddle.Position = new Vector3(0f, 0.5f, 0f);
		tailGroup.Position = new Vector3(0.5f, -0.25f, 0f);
		GTR = new GlyphTextRenderer(base.Game);
		LevelManager.LevelChanged += Hide;
		base.Initialize();
	}

	protected override void LoadContent()
	{
		Mesh mesh = textMesh;
		BaseEffect effect = (canvasMesh.Effect = new DefaultEffect.Textured());
		mesh.Effect = effect;
		tailGroup.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/speech_bubble/SpeechBubbleTail");
		neGroup.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/speech_bubble/SpeechBubbleNE");
		nwGroup.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/speech_bubble/SpeechBubbleNW");
		seGroup.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/speech_bubble/SpeechBubbleSE");
		swGroup.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/speech_bubble/SpeechBubbleSW");
		scalableBottom.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/FullBlack");
		Group group = scalableMiddle;
		Texture texture2 = (scalableTop.Texture = scalableBottom.Texture);
		group.Texture = texture2;
		zuishFont = CMProvider.Global.Load<SpriteFont>("Fonts/Zuish");
		zuishFont.LineSpacing++;
		spriteBatch = new SpriteBatch(base.GraphicsDevice);
		GamepadState.OnLayoutChanged = (EventHandler)Delegate.Combine(GamepadState.OnLayoutChanged, new EventHandler(OnLayoutChanged));
	}

	protected override void UnloadContent()
	{
		GamepadState.OnLayoutChanged = (EventHandler)Delegate.Remove(GamepadState.OnLayoutChanged, new EventHandler(OnLayoutChanged));
		if (text != null)
		{
			text.Unhook();
			text.Dispose();
		}
	}

	private void OnTextChanged(bool update)
	{
		float num = 2f;
		string text = textString;
		textString = originalString;
		SpriteFont spriteFont = ((Font == SpeechFont.Pixel) ? FontManager.Big : zuishFont);
		if (Font == SpeechFont.Zuish)
		{
			textString = textString.Replace(" ", "  ");
		}
		float num2 = ((Culture.IsCJK && Font == SpeechFont.Pixel) ? FontManager.SmallFactor : 1f);
		bool flag = base.GraphicsDevice.DisplayMode.Width < 1280 && Font == SpeechFont.Pixel;
		float num3 = 0f;
		if (Font != SpeechFont.Zuish)
		{
			float num4 = (update ? 0.9f : 0.85f);
			float num5 = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
			num3 = (Origin - CameraManager.InterpolatedCenter).Dot(CameraManager.Viewpoint.RightVector());
			float val = (flag ? (Math.Max((0f - num3) * 16f * CameraManager.PixelsPerTrixel + 640f * num4, 50f) * (2f / 3f)) : (Math.Max((0f - num3) * 16f * CameraManager.PixelsPerTrixel + 1280f * num5 / 2f * num4, 50f) / (CameraManager.PixelsPerTrixel / 2f)));
			if (GameState.InMap)
			{
				val = 500f;
			}
			val = Math.Max(val, 70f);
			List<GlyphTextRenderer.FilledInGlyph> glyphLocations;
			string obj = GTR.FillInGlyphs(textString, out glyphLocations);
			if (Culture.IsCJK)
			{
				num2 /= 2f;
			}
			StringBuilder stringBuilder = new StringBuilder(WordWrap.Split(obj, spriteFont, val / num2));
			if (Culture.IsCJK)
			{
				num2 *= 2f;
			}
			bool flag2 = true;
			int num6 = 0;
			for (int i = 0; i < stringBuilder.Length; i++)
			{
				if (flag2 && stringBuilder[i] == '^')
				{
					for (int j = i; j < i + glyphLocations[num6].Length; j++)
					{
						if (stringBuilder[j] == '\r' || stringBuilder[j] == '\n')
						{
							stringBuilder.Remove(j, 1);
							j--;
						}
					}
					stringBuilder.Remove(i, glyphLocations[num6].Length);
					stringBuilder.Insert(i, glyphLocations[num6].OriginalGlyph);
					num6++;
				}
				else
				{
					flag2 = stringBuilder[i] == ' ' || stringBuilder[i] == '\r' || stringBuilder[i] == '\n';
				}
			}
			textString = stringBuilder.ToString();
			if (!update)
			{
				distanceFromCenterAtTextChange = num3;
			}
		}
		if (update && (text == textString || Math.Abs(distanceFromCenterAtTextChange - num3) < 1.5f))
		{
			textString = text;
			return;
		}
		if (Culture.IsCJK && Font == SpeechFont.Pixel)
		{
			if (base.GraphicsDevice.GetViewScale() < 1.5f)
			{
				spriteFont = FontManager.Small;
			}
			else
			{
				spriteFont = FontManager.Big;
				num2 /= 2f;
			}
			num2 *= num;
		}
		bool multilineGlyphs;
		Vector2 vector = GTR.MeasureWithGlyphs(spriteFont, textString, num2, out multilineGlyphs);
		if (!Culture.IsCJK && multilineGlyphs)
		{
			spriteFont.LineSpacing += 8;
			bool num7 = multilineGlyphs;
			vector = GTR.MeasureWithGlyphs(spriteFont, textString, num2, out multilineGlyphs);
			multilineGlyphs = num7;
		}
		float num8 = 1f;
		if (Culture.IsCJK && Font == SpeechFont.Pixel)
		{
			num8 = num;
		}
		scalableMiddleSize = vector + Vector2.One * 4f * 2f * num8 + Vector2.UnitX * 4f * 2f * num8;
		if (Font == SpeechFont.Zuish)
		{
			scalableMiddleSize += Vector2.UnitY * 2f;
		}
		int num9 = (int)scalableMiddleSize.X;
		int num10 = (int)scalableMiddleSize.Y;
		if (Culture.IsCJK && Font == SpeechFont.Pixel)
		{
			num2 *= 2f;
			num9 *= 2;
			num10 *= 2;
		}
		Vector2 vector2 = scalableMiddleSize;
		if (this.text != null)
		{
			this.text.Unhook();
			this.text.Dispose();
		}
		this.text = new RenderTarget2D(base.GraphicsDevice, num9, num10, mipMap: false, base.GraphicsDevice.PresentationParameters.BackBufferFormat, base.GraphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		base.GraphicsDevice.SetRenderTarget(this.text);
		base.GraphicsDevice.PrepareDraw();
		base.GraphicsDevice.Clear(ClearOptions.Target, ColorEx.TransparentWhite, 1f, 0);
		Vector2 vector3 = (Culture.IsCJK ? new Vector2(4f * num) : Vector2.Zero);
		if (Culture.IsCJK)
		{
			spriteBatch.BeginLinear();
		}
		else
		{
			spriteBatch.BeginPoint();
		}
		if (Font == SpeechFont.Pixel)
		{
			GTR.DrawString(spriteBatch, spriteFont, textString, (vector2 / 2f - vector / 2f + vector3).Round(), TextColor, num2);
		}
		else
		{
			spriteBatch.DrawString(spriteFont, textString, vector2 / 2f - vector / 2f, TextColor, 0f, Vector2.Zero, scalableMiddleSize / vector2, SpriteEffects.None, 0f);
		}
		spriteBatch.End();
		base.GraphicsDevice.SetRenderTarget(null);
		if (Font == SpeechFont.Zuish)
		{
			float x = scalableMiddleSize.X;
			scalableMiddleSize.X = scalableMiddleSize.Y;
			scalableMiddleSize.Y = x;
		}
		if (Culture.IsCJK && Font == SpeechFont.Pixel)
		{
			scalableMiddleSize /= num;
		}
		scalableMiddleSize /= 16f;
		scalableMiddleSize -= Vector2.One;
		textMesh.SamplerState = ((Culture.IsCJK && Font == SpeechFont.Pixel) ? SamplerState.AnisotropicClamp : SamplerState.PointClamp);
		textGroup.Texture = this.text;
		oldCamPos = CameraManager.InterpolatedCenter;
		lastUsedOrigin = Origin;
		if (!Culture.IsCJK && multilineGlyphs)
		{
			spriteFont.LineSpacing -= 8;
		}
	}

	private void UpdateBTexture()
	{
		SpriteFont small = FontManager.Small;
		Vector2 vector = small.MeasureString(GTR.FillInGlyphs(" {B} ")) * FezMath.Saturate(FontManager.SmallFactor);
		if (bTexture != null)
		{
			bTexture.Dispose();
		}
		bTexture = new RenderTarget2D(base.GraphicsDevice, (int)vector.X, (int)vector.Y, mipMap: false, base.GraphicsDevice.PresentationParameters.BackBufferFormat, base.GraphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		base.GraphicsDevice.SetRenderTarget(bTexture);
		base.GraphicsDevice.PrepareDraw();
		base.GraphicsDevice.Clear(ClearOptions.Target, ColorEx.TransparentWhite, 1f, 0);
		spriteBatch.BeginPoint();
		GTR.DrawString(spriteBatch, small, " {B} ", new Vector2(0f, 0f), Color.White, FezMath.Saturate(FontManager.SmallFactor));
		spriteBatch.End();
		base.GraphicsDevice.SetRenderTarget(null);
		bGroup.Texture = bTexture;
		float num = (Culture.IsCJK ? 25f : 24f);
		bGroup.Scale = new Vector3(vector.X / num, vector.Y / num, 1f);
		if (bGroup.Material == null)
		{
			bGroup.Material = new Material();
		}
	}

	private void OnLayoutChanged(object sender, EventArgs e)
	{
		if (!string.IsNullOrEmpty(originalString))
		{
			OnTextChanged(update: false);
		}
		UpdateBTexture();
	}

	public override void Update(GameTime gameTime)
	{
		if (show && !changingText && !FezMath.AlmostEqual(CameraManager.InterpolatedCenter, oldCamPos, 0.0625f))
		{
			OnTextChanged(update: true);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		bool flag = show;
		if (show && changingText)
		{
			flag = false;
		}
		if (!flag && sinceShown > 1f)
		{
			sinceShown = 1f;
		}
		sinceShown += (float)gameTime.ElapsedGameTime.TotalSeconds * (float)(flag ? 1 : (-2)) * 5f;
		if (sinceShown < 0f)
		{
			sinceShown = 0f;
		}
		if (sinceShown == 0f && !flag)
		{
			if (!changingText && Font == SpeechFont.Zuish)
			{
				Font = SpeechFont.Pixel;
			}
			return;
		}
		scalableBottom.Scale = new Vector3(scalableMiddleSize.X, 1f, 1f);
		seGroup.Position = new Vector3(scalableMiddleSize.X + 0.5f, 0f, 0f);
		scalableMiddle.Scale = new Vector3(scalableMiddleSize.X + 1f, scalableMiddleSize.Y, 1f);
		nwGroup.Position = new Vector3(0f, scalableMiddleSize.Y + 0.5f, 0f);
		scalableTop.Position = new Vector3(0.5f, nwGroup.Position.Y, 0f);
		scalableTop.Scale = scalableBottom.Scale;
		neGroup.Position = new Vector3(seGroup.Position.X, nwGroup.Position.Y, 0f);
		bool flag2 = GameState.InMap && Font == SpeechFont.Pixel;
		if (!(CameraManager.PixelsPerTrixel == 3f || flag2))
		{
			seGroup.Scale = new Vector3(1f, 1f, 1f);
		}
		float num = 3f * bGroup.Scale.X / 4f;
		float y = 0f;
		if (Culture.IsCJK)
		{
			num /= 2f;
			y = 0.25f;
		}
		bGroup.Position = seGroup.Position + new Vector3(0.5f, -0.5f, 0f) - new Vector3(num, y, 0f);
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num2 = (float)base.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		float num3 = (flag2 ? (0.5f * CameraManager.Radius / 26.666666f / num2 / viewScale) : 0.5f);
		canvasMesh.Scale = new Vector3(num3);
		tailGroup.Scale = new Vector3((Font != SpeechFont.Zuish) ? 1 : (-1), 1f, 1f);
		canvasMesh.Rotation = Quaternion.Normalize(CameraManager.Rotation);
		canvasMesh.Position = Origin + canvasMesh.WorldMatrix.Left * 2f + Vector3.UnitY * 0.65f;
		canvasMesh.Position = (canvasMesh.Position * 16f * CameraManager.PixelsPerTrixel).Round() / 16f / CameraManager.PixelsPerTrixel;
		if (Font == SpeechFont.Zuish)
		{
			textMesh.Scale = new Vector3(scalableMiddleSize.Y + 1f, scalableMiddleSize.X + 1f, 1f) * num3;
		}
		else
		{
			textMesh.Scale = new Vector3(scalableMiddleSize.X + 1f, scalableMiddleSize.Y + 1f, 1f) * num3;
		}
		textMesh.Rotation = canvasMesh.Rotation;
		if (Font == SpeechFont.Zuish)
		{
			textMesh.Rotation *= Quaternion.CreateFromYawPitchRoll(0f, 0f, -(float)Math.PI / 2f);
		}
		textMesh.Position = canvasMesh.Position;
		if (Font == SpeechFont.Zuish)
		{
			textMesh.Position += (scalableMiddleSize.Y + 1f) * Vector3.UnitY / 2f;
		}
		canvasMesh.Material.Opacity = FezMath.Saturate(sinceShown);
		textMesh.Material.Opacity = FezMath.Saturate(sinceShown);
		if (flag)
		{
			bGroup.Material.Opacity = FezMath.Saturate(sinceShown - (0.075f * (float)textString.StripPunctuation().Length + 2f) * 2f);
		}
		else
		{
			bGroup.Material.Opacity = Math.Min(bGroup.Material.Opacity, FezMath.Saturate(sinceShown));
		}
		canvasMesh.Draw();
		textMesh.Draw();
	}

	public void ForceDrawOrder(int drawOrder)
	{
		base.DrawOrder = drawOrder;
		OnDrawOrderChanged(this, EventArgs.Empty);
	}

	public void RevertDrawOrder()
	{
		base.DrawOrder = 150;
		OnDrawOrderChanged(this, EventArgs.Empty);
	}
}
