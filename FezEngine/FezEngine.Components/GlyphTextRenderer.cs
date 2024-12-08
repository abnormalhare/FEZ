using System;
using System.Collections.Generic;
using Common;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Localization;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Components;

public class GlyphTextRenderer
{
	private struct GlyphDescription
	{
		public Texture2D Image;

		public GlyphMetadata Metadata;

		public GlyphDescription(Texture2D image, GlyphMetadata md)
		{
			Image = image;
			Metadata = md;
		}
	}

	private struct GlyphMetadata
	{
		public int SpacesBefore;

		public int SpacesAfter;

		public bool IsTall;
	}

	public struct GlyphLocation
	{
		public string UpToThere;

		public string Glyph;
	}

	public struct FilledInGlyph
	{
		public int Length;

		public string OriginalGlyph;
	}

	public const float SafeArea = 0.15f;

	private readonly Game game;

	private readonly Dictionary<string, GlyphDescription> ButtonImages = new Dictionary<string, GlyphDescription>();

	private static readonly Dictionary<string, MappedAction> ActionMap = new Dictionary<string, MappedAction>
	{
		{
			"BACK",
			MappedAction.OpenMap
		},
		{
			"START",
			MappedAction.Pause
		},
		{
			"A",
			MappedAction.Jump
		},
		{
			"B",
			MappedAction.CancelTalk
		},
		{
			"X",
			MappedAction.GrabThrow
		},
		{
			"Y",
			MappedAction.OpenInventory
		},
		{
			"RB",
			MappedAction.MapZoomIn
		},
		{
			"LB",
			MappedAction.MapZoomOut
		},
		{
			"RT",
			MappedAction.RotateRight
		},
		{
			"LT",
			MappedAction.RotateLeft
		},
		{
			"UP",
			MappedAction.Up
		},
		{
			"DOWN",
			MappedAction.Down
		},
		{
			"LEFT",
			MappedAction.Left
		},
		{
			"RIGHT",
			MappedAction.Right
		},
		{
			"LS",
			MappedAction.FpViewToggle
		},
		{
			"RS",
			MappedAction.ClampLook
		}
	};

	private static readonly Dictionary<Buttons, string> Buttonize = new Dictionary<Buttons, string>
	{
		{
			Buttons.Start,
			"START"
		},
		{
			Buttons.Back,
			"BACK"
		},
		{
			Buttons.LeftStick,
			"LS"
		},
		{
			Buttons.RightStick,
			"RS"
		},
		{
			Buttons.LeftShoulder,
			"LB"
		},
		{
			Buttons.RightShoulder,
			"RB"
		},
		{
			Buttons.A,
			"A"
		},
		{
			Buttons.B,
			"B"
		},
		{
			Buttons.X,
			"X"
		},
		{
			Buttons.Y,
			"Y"
		},
		{
			Buttons.RightTrigger,
			"RT"
		},
		{
			Buttons.LeftTrigger,
			"LT"
		}
	};

	private bool bigGlyphDetected;

	private readonly List<GlyphLocation> GlyphLocations = new List<GlyphLocation>();

	public bool IgnoreKeyboardRemapping { get; set; }

	public Vector2 Margin => new Vector2((float)game.GraphicsDevice.Viewport.Width * 0.15f / 2f, (float)game.GraphicsDevice.Viewport.Height * 0.15f / 2f).Round();

	public Vector2 SafeViewportSize => new Vector2((float)game.GraphicsDevice.Viewport.Width * 0.85f, (float)game.GraphicsDevice.Viewport.Height * 0.85f).Round();

	public GlyphTextRenderer(Game game)
	{
		this.game = game;
		ContentManager global = ServiceHelper.Get<IContentManagerProvider>().Global;
		string text = "Other Textures/glyphs/";
		GlyphMetadata md = new GlyphMetadata
		{
			SpacesBefore = 1,
			SpacesAfter = 1,
			IsTall = false
		};
		GlyphMetadata md2 = new GlyphMetadata
		{
			SpacesBefore = 1,
			SpacesAfter = 1,
			IsTall = true
		};
		GlyphMetadata md3 = new GlyphMetadata
		{
			SpacesBefore = 2,
			SpacesAfter = 2,
			IsTall = true
		};
		GlyphMetadata md4 = new GlyphMetadata
		{
			SpacesBefore = 2,
			SpacesAfter = 2,
			IsTall = false
		};
		GlyphMetadata md5 = new GlyphMetadata
		{
			SpacesBefore = 2,
			SpacesAfter = 1,
			IsTall = false
		};
		GlyphMetadata md6 = new GlyphMetadata
		{
			SpacesBefore = 3,
			SpacesAfter = 2,
			IsTall = false
		};
		GlyphMetadata md7 = new GlyphMetadata
		{
			SpacesBefore = 3,
			SpacesAfter = 3,
			IsTall = false
		};
		GlyphMetadata md8 = new GlyphMetadata
		{
			SpacesBefore = 4,
			SpacesAfter = 4,
			IsTall = false
		};
		GlyphMetadata md9 = new GlyphMetadata
		{
			SpacesBefore = 5,
			SpacesAfter = 5,
			IsTall = false
		};
		GlyphMetadata md10 = new GlyphMetadata
		{
			SpacesBefore = 6,
			SpacesAfter = 6,
			IsTall = false
		};
		GlyphMetadata md11 = new GlyphMetadata
		{
			SpacesBefore = 7,
			SpacesAfter = 7,
			IsTall = false
		};
		GlyphMetadata md12 = new GlyphMetadata
		{
			SpacesBefore = 8,
			SpacesAfter = 8,
			IsTall = false
		};
		GlyphMetadata md13 = new GlyphMetadata
		{
			SpacesBefore = 9,
			SpacesAfter = 9,
			IsTall = false
		};
		ButtonImages.Add("BACK", new GlyphDescription(global.Load<Texture2D>(text + "BackButton"), md5));
		ButtonImages.Add("START", new GlyphDescription(global.Load<Texture2D>(text + "StartButton"), md5));
		ButtonImages.Add("A", new GlyphDescription(global.Load<Texture2D>(text + "AButton"), md));
		ButtonImages.Add("B", new GlyphDescription(global.Load<Texture2D>(text + "BButton"), md));
		ButtonImages.Add("X", new GlyphDescription(global.Load<Texture2D>(text + "XButton"), md));
		ButtonImages.Add("Y", new GlyphDescription(global.Load<Texture2D>(text + "YButton"), md));
		ButtonImages.Add("RB", new GlyphDescription(global.Load<Texture2D>(text + "RightBumper"), md6));
		ButtonImages.Add("LB", new GlyphDescription(global.Load<Texture2D>(text + "LeftBumper"), md6));
		ButtonImages.Add("RT", new GlyphDescription(global.Load<Texture2D>(text + "RightTrigger"), md2));
		ButtonImages.Add("LT", new GlyphDescription(global.Load<Texture2D>(text + "LeftTrigger"), md2));
		ButtonImages.Add("LS", new GlyphDescription(global.Load<Texture2D>(text + "LeftStick"), md3));
		ButtonImages.Add("RS", new GlyphDescription(global.Load<Texture2D>(text + "RightStick"), md3));
		ButtonImages.Add("UP", new GlyphDescription(global.Load<Texture2D>(text + "DPadUp"), md3));
		ButtonImages.Add("DOWN", new GlyphDescription(global.Load<Texture2D>(text + "DPadDown"), md3));
		ButtonImages.Add("LEFT", new GlyphDescription(global.Load<Texture2D>(text + "DPadLeft"), md3));
		ButtonImages.Add("RIGHT", new GlyphDescription(global.Load<Texture2D>(text + "DPadRight"), md3));
		ButtonImages.Add("LA", new GlyphDescription(global.Load<Texture2D>(text + "LeftArrow"), md));
		ButtonImages.Add("RA", new GlyphDescription(global.Load<Texture2D>(text + "RightArrow"), md));
		ButtonImages.Add("BACK_SONY", new GlyphDescription(global.Load<Texture2D>(text + "BackButton_SONY"), md5));
		ButtonImages.Add("START_SONY", new GlyphDescription(global.Load<Texture2D>(text + "StartButton_SONY"), md5));
		ButtonImages.Add("A_SONY", new GlyphDescription(global.Load<Texture2D>(text + "AButton_SONY"), md));
		ButtonImages.Add("B_SONY", new GlyphDescription(global.Load<Texture2D>(text + "BButton_SONY"), md));
		ButtonImages.Add("X_SONY", new GlyphDescription(global.Load<Texture2D>(text + "XButton_SONY"), md));
		ButtonImages.Add("Y_SONY", new GlyphDescription(global.Load<Texture2D>(text + "YButton_SONY"), md));
		ButtonImages.Add("LS_SONY", new GlyphDescription(global.Load<Texture2D>(text + "LeftStick_SONY"), md3));
		ButtonImages.Add("RS_SONY", new GlyphDescription(global.Load<Texture2D>(text + "RightStick_SONY"), md3));
		ButtonImages.Add("UP_SONY", new GlyphDescription(global.Load<Texture2D>(text + "DPadUp_SONY"), md3));
		ButtonImages.Add("DOWN_SONY", new GlyphDescription(global.Load<Texture2D>(text + "DPadDown_SONY"), md3));
		ButtonImages.Add("LEFT_SONY", new GlyphDescription(global.Load<Texture2D>(text + "DPadLeft_SONY"), md3));
		ButtonImages.Add("RIGHT_SONY", new GlyphDescription(global.Load<Texture2D>(text + "DPadRight_SONY"), md3));
		ButtonImages.Add("RB_PS3", new GlyphDescription(global.Load<Texture2D>(text + "RightBumper_PS3"), md6));
		ButtonImages.Add("LB_PS3", new GlyphDescription(global.Load<Texture2D>(text + "LeftBumper_PS3"), md6));
		ButtonImages.Add("RT_PS3", new GlyphDescription(global.Load<Texture2D>(text + "RightTrigger_PS3"), md2));
		ButtonImages.Add("LT_PS3", new GlyphDescription(global.Load<Texture2D>(text + "LeftTrigger_PS3"), md2));
		ButtonImages.Add("RB_PS4", new GlyphDescription(global.Load<Texture2D>(text + "RightBumper_PS4"), md6));
		ButtonImages.Add("LB_PS4", new GlyphDescription(global.Load<Texture2D>(text + "LeftBumper_PS4"), md6));
		ButtonImages.Add("RT_PS4", new GlyphDescription(global.Load<Texture2D>(text + "RightTrigger_PS4"), md2));
		ButtonImages.Add("LT_PS4", new GlyphDescription(global.Load<Texture2D>(text + "LeftTrigger_PS4"), md2));
		text = "Other Textures/keyboard_glyphs/";
		ButtonImages.Add("P_D0", new GlyphDescription(global.Load<Texture2D>(text + "P_0"), md));
		ButtonImages.Add("P_D1", new GlyphDescription(global.Load<Texture2D>(text + "P_1"), md));
		ButtonImages.Add("P_D2", new GlyphDescription(global.Load<Texture2D>(text + "P_2"), md));
		ButtonImages.Add("P_D3", new GlyphDescription(global.Load<Texture2D>(text + "P_3"), md));
		ButtonImages.Add("P_D4", new GlyphDescription(global.Load<Texture2D>(text + "P_4"), md));
		ButtonImages.Add("P_D5", new GlyphDescription(global.Load<Texture2D>(text + "P_5"), md));
		ButtonImages.Add("P_D6", new GlyphDescription(global.Load<Texture2D>(text + "P_6"), md));
		ButtonImages.Add("P_D7", new GlyphDescription(global.Load<Texture2D>(text + "P_7"), md));
		ButtonImages.Add("P_D8", new GlyphDescription(global.Load<Texture2D>(text + "P_8"), md));
		ButtonImages.Add("P_D9", new GlyphDescription(global.Load<Texture2D>(text + "P_9"), md));
		ButtonImages.Add("P_Q", new GlyphDescription(global.Load<Texture2D>(text + "P_Q"), md));
		ButtonImages.Add("P_W", new GlyphDescription(global.Load<Texture2D>(text + "P_W"), md));
		ButtonImages.Add("P_E", new GlyphDescription(global.Load<Texture2D>(text + "P_E"), md));
		ButtonImages.Add("P_R", new GlyphDescription(global.Load<Texture2D>(text + "P_R"), md));
		ButtonImages.Add("P_T", new GlyphDescription(global.Load<Texture2D>(text + "P_T"), md));
		ButtonImages.Add("P_Y", new GlyphDescription(global.Load<Texture2D>(text + "P_Y"), md));
		ButtonImages.Add("P_U", new GlyphDescription(global.Load<Texture2D>(text + "P_U"), md));
		ButtonImages.Add("P_I", new GlyphDescription(global.Load<Texture2D>(text + "P_I"), md));
		ButtonImages.Add("P_O", new GlyphDescription(global.Load<Texture2D>(text + "P_O"), md));
		ButtonImages.Add("P_P", new GlyphDescription(global.Load<Texture2D>(text + "P_P"), md));
		ButtonImages.Add("P_A", new GlyphDescription(global.Load<Texture2D>(text + "P_A"), md));
		ButtonImages.Add("P_S", new GlyphDescription(global.Load<Texture2D>(text + "P_S"), md));
		ButtonImages.Add("P_D", new GlyphDescription(global.Load<Texture2D>(text + "P_D"), md));
		ButtonImages.Add("P_F", new GlyphDescription(global.Load<Texture2D>(text + "P_F"), md));
		ButtonImages.Add("P_G", new GlyphDescription(global.Load<Texture2D>(text + "P_G"), md));
		ButtonImages.Add("P_H", new GlyphDescription(global.Load<Texture2D>(text + "P_H"), md));
		ButtonImages.Add("P_J", new GlyphDescription(global.Load<Texture2D>(text + "P_J"), md));
		ButtonImages.Add("P_K", new GlyphDescription(global.Load<Texture2D>(text + "P_K"), md));
		ButtonImages.Add("P_L", new GlyphDescription(global.Load<Texture2D>(text + "P_L"), md));
		ButtonImages.Add("P_Z", new GlyphDescription(global.Load<Texture2D>(text + "P_Z"), md));
		ButtonImages.Add("P_X", new GlyphDescription(global.Load<Texture2D>(text + "P_X"), md));
		ButtonImages.Add("P_C", new GlyphDescription(global.Load<Texture2D>(text + "P_C"), md));
		ButtonImages.Add("P_V", new GlyphDescription(global.Load<Texture2D>(text + "P_V"), md));
		ButtonImages.Add("P_B", new GlyphDescription(global.Load<Texture2D>(text + "P_B"), md));
		ButtonImages.Add("P_N", new GlyphDescription(global.Load<Texture2D>(text + "P_N"), md));
		ButtonImages.Add("P_M", new GlyphDescription(global.Load<Texture2D>(text + "P_M"), md));
		ButtonImages.Add("P_NumLock", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_LOCK"), md11));
		ButtonImages.Add("P_NumPad0", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_0"), md8));
		ButtonImages.Add("P_NumPad1", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_1"), md8));
		ButtonImages.Add("P_NumPad2", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_2"), md8));
		ButtonImages.Add("P_NumPad3", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_3"), md8));
		ButtonImages.Add("P_NumPad4", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_4"), md8));
		ButtonImages.Add("P_NumPad5", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_5"), md8));
		ButtonImages.Add("P_NumPad6", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_6"), md8));
		ButtonImages.Add("P_NumPad7", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_7"), md8));
		ButtonImages.Add("P_NumPad8", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_8"), md8));
		ButtonImages.Add("P_NumPad9", new GlyphDescription(global.Load<Texture2D>(text + "P_NUM_9"), md8));
		ButtonImages.Add("P_Up", new GlyphDescription(global.Load<Texture2D>(text + "P_ARROW_UP"), md));
		ButtonImages.Add("P_Down", new GlyphDescription(global.Load<Texture2D>(text + "P_ARROW_DOWN"), md));
		ButtonImages.Add("P_Left", new GlyphDescription(global.Load<Texture2D>(text + "P_ARROW_LEFT"), md));
		ButtonImages.Add("P_Right", new GlyphDescription(global.Load<Texture2D>(text + "P_ARROW_RIGHT"), md));
		ButtonImages.Add("P_Home", new GlyphDescription(global.Load<Texture2D>(text + "P_HOME"), md8));
		ButtonImages.Add("P_End", new GlyphDescription(global.Load<Texture2D>(text + "P_END"), md7));
		ButtonImages.Add("P_PageUp", new GlyphDescription(global.Load<Texture2D>(text + "P_PAGE_UP"), md7));
		ButtonImages.Add("P_PageDown", new GlyphDescription(global.Load<Texture2D>(text + "P_PAGE_DOWN"), md9));
		ButtonImages.Add("P_Insert", new GlyphDescription(global.Load<Texture2D>(text + "P_INSERT"), md9));
		ButtonImages.Add("P_Scroll", new GlyphDescription(global.Load<Texture2D>(text + "P_SCROLL"), md9));
		ButtonImages.Add("P_RightAlt", new GlyphDescription(global.Load<Texture2D>(text + "P_R_ALT"), md8));
		ButtonImages.Add("P_RightControl", new GlyphDescription(global.Load<Texture2D>(text + "P_R_CONTROL"), md12));
		ButtonImages.Add("P_RightWindows", new GlyphDescription(global.Load<Texture2D>(text + "P_R_WINDOWS"), md12));
		ButtonImages.Add("P_RightShift", new GlyphDescription(global.Load<Texture2D>(text + "P_R_SHIFT"), md10));
		ButtonImages.Add("P_LeftAlt", new GlyphDescription(global.Load<Texture2D>(text + "P_L_ALT"), md8));
		ButtonImages.Add("P_LeftControl", new GlyphDescription(global.Load<Texture2D>(text + "P_L_CONTROL"), md12));
		ButtonImages.Add("P_LeftWindows", new GlyphDescription(global.Load<Texture2D>(text + "P_L_WINDOWS"), md12));
		ButtonImages.Add("P_LeftShift", new GlyphDescription(global.Load<Texture2D>(text + "P_L_SHIFT"), md10));
		ButtonImages.Add("P_Escape", new GlyphDescription(global.Load<Texture2D>(text + "P_ESCAPE"), md10));
		ButtonImages.Add("P_F1", new GlyphDescription(global.Load<Texture2D>(text + "P_F1"), md4));
		ButtonImages.Add("P_F2", new GlyphDescription(global.Load<Texture2D>(text + "P_F2"), md4));
		ButtonImages.Add("P_F3", new GlyphDescription(global.Load<Texture2D>(text + "P_F3"), md4));
		ButtonImages.Add("P_F4", new GlyphDescription(global.Load<Texture2D>(text + "P_F4"), md4));
		ButtonImages.Add("P_F5", new GlyphDescription(global.Load<Texture2D>(text + "P_F5"), md4));
		ButtonImages.Add("P_F6", new GlyphDescription(global.Load<Texture2D>(text + "P_F6"), md4));
		ButtonImages.Add("P_F7", new GlyphDescription(global.Load<Texture2D>(text + "P_F7"), md4));
		ButtonImages.Add("P_F8", new GlyphDescription(global.Load<Texture2D>(text + "P_F8"), md4));
		ButtonImages.Add("P_F9", new GlyphDescription(global.Load<Texture2D>(text + "P_F9"), md4));
		ButtonImages.Add("P_F10", new GlyphDescription(global.Load<Texture2D>(text + "P_F10"), md7));
		ButtonImages.Add("P_F11", new GlyphDescription(global.Load<Texture2D>(text + "P_F11"), md7));
		ButtonImages.Add("P_F12", new GlyphDescription(global.Load<Texture2D>(text + "P_F12"), md7));
		ButtonImages.Add("P_OemPeriod", new GlyphDescription(global.Load<Texture2D>(text + "P_PERIOD"), md));
		ButtonImages.Add("P_OemComma", new GlyphDescription(global.Load<Texture2D>(text + "P_COMMA"), md));
		ButtonImages.Add("P_OemSemicolon", new GlyphDescription(global.Load<Texture2D>(text + "P_SEMICOLON"), md));
		ButtonImages.Add("P_OemQuotes", new GlyphDescription(global.Load<Texture2D>(text + "P_QUOTES"), md));
		ButtonImages.Add("P_OemTilde", new GlyphDescription(global.Load<Texture2D>(text + "P_TILDE"), md));
		ButtonImages.Add("P_OemOpenBrackets", new GlyphDescription(global.Load<Texture2D>(text + "P_R_BRACKET"), md));
		ButtonImages.Add("P_OemCloseBrackets", new GlyphDescription(global.Load<Texture2D>(text + "P_L_BRACKET"), md));
		ButtonImages.Add("P_OemPipe", new GlyphDescription(global.Load<Texture2D>(text + "P_PIPE"), md));
		ButtonImages.Add("P_OemQuestion", new GlyphDescription(global.Load<Texture2D>(text + "P_QUESTION"), md));
		ButtonImages.Add("P_Divide", new GlyphDescription(global.Load<Texture2D>(text + "P_DIVIDE"), md));
		ButtonImages.Add("P_Add", new GlyphDescription(global.Load<Texture2D>(text + "P_PLUS"), md));
		ButtonImages.Add("P_OemMinus", new GlyphDescription(global.Load<Texture2D>(text + "P_MINUS"), md));
		ButtonImages.Add("P_Multiply", new GlyphDescription(global.Load<Texture2D>(text + "P_MULTIPLY"), md));
		ButtonImages.Add("P_OemPlus", new GlyphDescription(global.Load<Texture2D>(text + "P_EQUAL"), md));
		ButtonImages.Add("P_Space", new GlyphDescription(global.Load<Texture2D>(text + "P_SPACE"), md9));
		ButtonImages.Add("P_Tab", new GlyphDescription(global.Load<Texture2D>(text + "P_TAB"), md7));
		ButtonImages.Add("P_Back", new GlyphDescription(global.Load<Texture2D>(text + "P_BACKSPACE"), md13));
		ButtonImages.Add("P_Delete", new GlyphDescription(global.Load<Texture2D>(text + "P_DELETE"), md10));
		ButtonImages.Add("P_CapsLock", new GlyphDescription(global.Load<Texture2D>(text + "P_CAPS"), md13));
		ButtonImages.Add("P_Enter", new GlyphDescription(global.Load<Texture2D>(text + "P_ENTER"), md9));
		ButtonImages.Add("P_WASD", new GlyphDescription(global.Load<Texture2D>(text + "P_WASD"), md9));
		ButtonImages.Add("P_IJKL", new GlyphDescription(global.Load<Texture2D>(text + "P_IJKL"), md9));
		ButtonImages.Add("P_Arrows", new GlyphDescription(global.Load<Texture2D>(text + "P_ARROWS"), md9));
		ButtonImages.Add("P_ZQSD", new GlyphDescription(global.Load<Texture2D>(text + "P_ZQSD"), md9));
		ButtonImages.Add("P_ESDF", new GlyphDescription(global.Load<Texture2D>(text + "P_ESDF"), md9));
	}

	public void DrawString(SpriteBatch batch, SpriteFont font, string text, Vector2 position)
	{
		DrawString(batch, font, text, position, Color.White);
	}

	public void DrawString(SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color color)
	{
		DrawString(batch, font, text, position, color, 1f);
	}

	public void DrawString(SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color color, float scale)
	{
		text = FindButtonGlyphs(text);
		position = position.Round();
		batch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		if (Culture.IsCJK)
		{
			batch.End();
			batch.BeginPoint();
		}
		float y = font.MeasureString("X").Y;
		foreach (GlyphLocation glyphLocation in GlyphLocations)
		{
			if (!ButtonImages.TryGetValue(glyphLocation.Glyph, out var value))
			{
				continue;
			}
			Texture2D image = value.Image;
			float y2 = (font.MeasureString(glyphLocation.UpToThere) * scale).Y;
			string text2 = glyphLocation.UpToThere.Substring(glyphLocation.UpToThere.LastIndexOf('\n') + 1);
			Vector2 vector = font.MeasureString(text2) * scale;
			vector.Y = y2 - vector.Y * 0.5f;
			Vector2 vector2 = position + vector;
			float num = y / (float)image.Height * scale;
			if (image.Height <= 32)
			{
				float num2 = num;
				num = (float)Math.Ceiling(num);
				if (num2 == num && game.GraphicsDevice.GetViewScale() >= 2f)
				{
					num += 1f;
				}
			}
			if (Culture.IsCJK && Culture.Language != Language.Chinese)
			{
				num *= 1.2f;
			}
			if (Culture.IsCJK)
			{
				vector2.Y -= (float)image.Height * num * 0.05f;
			}
			Rectangle destinationRectangle = new Rectangle(FezMath.Round(vector2.X - (float)image.Width * num / 2f), FezMath.Round(vector2.Y - (float)image.Height * num / 2f), FezMath.Round((float)image.Width * num), FezMath.Round((float)image.Height * num));
			batch.Draw(image, destinationRectangle, new Color(255, 255, 255, color.A));
		}
		if (Culture.IsCJK)
		{
			batch.End();
			batch.BeginLinear();
		}
	}

	public Vector2 MeasureWithGlyphs(SpriteFont font, string text, float scale)
	{
		return font.MeasureString(FindButtonGlyphs(text)) * scale;
	}

	public Vector2 MeasureWithGlyphs(SpriteFont font, string text, float scale, out bool multilineGlyphs)
	{
		Vector2 result = font.MeasureString(FindButtonGlyphs(text)) * scale;
		multilineGlyphs = text.IndexOf('\n') != -1 && bigGlyphDetected;
		return result;
	}

	private string TryReplaceGlyph(string glyph)
	{
		MappedAction value;
		if (glyph == "RS")
		{
			switch (SettingsManager.Settings.KeyboardMapping[MappedAction.LookUp])
			{
			case Keys.W:
				glyph = "P_WASD";
				break;
			case Keys.Z:
				glyph = "P_ZQSD";
				break;
			case Keys.E:
				glyph = "P_ESDF";
				break;
			case Keys.I:
				glyph = "P_IJKL";
				break;
			case Keys.Up:
				glyph = "P_Arrows";
				break;
			}
		}
		else if (glyph == "LS")
		{
			if (ServiceHelper.Get<ILevelManager>().Name == "ELDERS")
			{
				glyph = "P_" + SettingsManager.Settings.KeyboardMapping[MappedAction.FpViewToggle];
			}
			else
			{
				switch (SettingsManager.Settings.KeyboardMapping[MappedAction.Left])
				{
				case Keys.A:
					glyph = "P_WASD";
					break;
				case Keys.Q:
					glyph = "P_ZQSD";
					break;
				case Keys.S:
					glyph = "P_ESDF";
					break;
				case Keys.L:
					glyph = "P_IJKL";
					break;
				case Keys.Left:
					glyph = "P_Arrows";
					break;
				}
			}
		}
		else if (ActionMap.TryGetValue(glyph, out value))
		{
			glyph = "P_" + (IgnoreKeyboardRemapping ? SettingsManager.Settings.UiKeyboardMapping[value] : SettingsManager.Settings.KeyboardMapping[value]);
		}
		return glyph;
	}

	private string TryReplaceGlyphSony(string glyph)
	{
		switch (glyph)
		{
		case "BACK":
		case "START":
		case "A":
		case "B":
		case "X":
		case "Y":
		case "LS":
		case "RS":
		case "UP":
		case "DOWN":
		case "LEFT":
		case "RIGHT":
			return glyph + "_SONY";
		case "RB":
		case "LB":
		case "RT":
		case "LT":
			if (GamepadState.Layout == GamepadState.GamepadLayout.PlayStation4)
			{
				return glyph + "_PS4";
			}
			return glyph + "_PS3";
		default:
			return glyph;
		}
	}

	private string FindButtonGlyphs(string text)
	{
		GlyphLocations.Clear();
		bigGlyphDetected = false;
		int num;
		while ((num = text.IndexOf("{")) != -1 && text.IndexOf("}", num) != -1)
		{
			int num2 = text.IndexOf('}', num);
			string text2 = text.Substring(num + 1, num2 - num - 1);
			string text3 = text.Substring(0, num);
			if (GamepadState.AnyConnected)
			{
				if (ActionMap.TryGetValue(text2, out var value))
				{
					if (SettingsManager.Settings.ControllerMapping.TryGetValue(value, out var value2))
					{
						text2 = Buttonize[value2];
					}
					if (GamepadState.Layout != 0)
					{
						text2 = TryReplaceGlyphSony(text2);
					}
				}
			}
			else
			{
				text2 = TryReplaceGlyph(text2);
			}
			string text4 = "";
			if (ButtonImages.TryGetValue(text2, out var value3))
			{
				bigGlyphDetected = value3.Metadata.IsTall;
				text4 = new string(' ', value3.Metadata.SpacesAfter);
				text3 += new string(' ', value3.Metadata.SpacesBefore);
			}
			GlyphLocations.Add(new GlyphLocation
			{
				UpToThere = text3,
				Glyph = text2
			});
			text = text3 + text4 + text.Substring(num2 + 1);
		}
		return text;
	}

	public Texture2D GetReplacedGlyphTexture(string glyph)
	{
		glyph = glyph.Substring(1, glyph.Length - 2);
		string key = glyph;
		if (GamepadState.AnyConnected)
		{
			if (ActionMap.TryGetValue(glyph, out var value))
			{
				if (SettingsManager.Settings.ControllerMapping.TryGetValue(value, out var value2))
				{
					glyph = Buttonize[value2];
				}
				if (GamepadState.Layout != 0)
				{
					glyph = TryReplaceGlyphSony(glyph);
				}
			}
		}
		else
		{
			glyph = TryReplaceGlyph(glyph);
		}
		if (!ButtonImages.TryGetValue(glyph, out var value3))
		{
			Logger.LogOnce("GlyphTextRenderer", LogSeverity.Warning, "Could not find glyph in button images : " + glyph);
			return ButtonImages[key].Image;
		}
		return value3.Image;
	}

	public string FillInGlyphs(string text, out List<FilledInGlyph> glyphLocations)
	{
		glyphLocations = new List<FilledInGlyph>();
		bigGlyphDetected = false;
		int num;
		while ((num = text.IndexOf("{")) != -1)
		{
			int num2 = text.IndexOf('}', num);
			string text2 = text.Substring(num + 1, num2 - num - 1);
			string text3 = text.Substring(0, num);
			string text4 = text2;
			if (GamepadState.AnyConnected)
			{
				if (ActionMap.TryGetValue(text2, out var value))
				{
					if (SettingsManager.Settings.ControllerMapping.TryGetValue(value, out var value2))
					{
						text2 = Buttonize[value2];
					}
					if (GamepadState.Layout != 0)
					{
						text2 = TryReplaceGlyphSony(text2);
					}
				}
			}
			else
			{
				text2 = TryReplaceGlyph(text2);
			}
			string text5 = "";
			int length = 0;
			if (ButtonImages.TryGetValue(text2, out var value3))
			{
				bigGlyphDetected = value3.Metadata.IsTall;
				text5 = new string('^', value3.Metadata.SpacesAfter);
				text3 += new string('^', value3.Metadata.SpacesBefore);
				length = value3.Metadata.SpacesAfter + value3.Metadata.SpacesBefore;
			}
			glyphLocations.Add(new FilledInGlyph
			{
				Length = length,
				OriginalGlyph = "{" + text4 + "}"
			});
			text = text3 + text5 + text.Substring(num2 + 1);
		}
		return text;
	}

	public string FillInGlyphs(string text)
	{
		bigGlyphDetected = false;
		int num;
		while ((num = text.IndexOf("{")) != -1)
		{
			int num2 = text.IndexOf('}', num);
			string text2 = text.Substring(num + 1, num2 - num - 1);
			string text3 = text.Substring(0, num);
			if (GamepadState.AnyConnected)
			{
				if (ActionMap.TryGetValue(text2, out var value))
				{
					if (SettingsManager.Settings.ControllerMapping.TryGetValue(value, out var value2))
					{
						text2 = Buttonize[value2];
					}
					if (GamepadState.Layout != 0)
					{
						text2 = TryReplaceGlyphSony(text2);
					}
				}
			}
			else
			{
				text2 = TryReplaceGlyph(text2);
			}
			string text4 = "";
			if (ButtonImages.TryGetValue(text2, out var value3))
			{
				bigGlyphDetected = value3.Metadata.IsTall;
				text4 = new string('^', value3.Metadata.SpacesAfter);
				text3 += new string('^', value3.Metadata.SpacesBefore);
			}
			text = text3 + text4 + text.Substring(num2 + 1);
		}
		return text;
	}

	public void DrawShadowedText(SpriteBatch batch, SpriteFont font, string text, Vector2 position)
	{
		DrawShadowedText(batch, font, text, position, Color.White);
	}

	public void DrawShadowedText(SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color color)
	{
		DrawShadowedText(batch, font, text, position, color, 1f);
	}

	public void DrawShadowedText(SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color color, float scale)
	{
		DrawShadowedText(batch, font, text, position, color, scale, Color.Black, 1f / 3f, 1f);
	}

	public void DrawShadowedText(SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color color, float scale, Color shadowColor, float shadowOpacity, float shadowOffset)
	{
		DrawString(batch, font, text, position + shadowOffset * Vector2.One, new Color(shadowColor.R, shadowColor.G, shadowColor.B, (byte)(shadowOpacity * ((float)(int)color.A / 255f) * 255f)), scale);
		DrawString(batch, font, text, position, color, scale);
	}

	public void DrawBloomedText(SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color color, Color bloomColor, float bloomOpacity)
	{
		bloomColor = new Color(bloomColor.R, bloomColor.G, bloomColor.B, (byte)(bloomOpacity * 255f * (float)(int)color.A / 255f));
		DrawString(batch, font, text, position + Vector2.One, bloomColor);
		DrawString(batch, font, text, position - Vector2.One, bloomColor);
		DrawString(batch, font, text, position + new Vector2(-1f, 1f), bloomColor);
		DrawString(batch, font, text, position + new Vector2(1f, -1f), bloomColor);
		DrawString(batch, font, text, position, color);
	}

	public void DrawStringLF(SpriteBatch batch, SpriteFont font, string text, Color color, Vector2 offset, float scale)
	{
		string[] array = text.Split('\r');
		float num = 0f;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text2 = array2[i].Trim();
			DrawString(batch, font, text2, new Vector2(0f, num) + offset, color, scale);
			num += font.MeasureString(text2).Y * scale * 1f / 3f + 15f;
		}
	}

	public void DrawStringLFLeftAlign(SpriteBatch batch, SpriteFont font, string text, Color color, Vector2 offset, float scale)
	{
		string[] array = text.Split('\r');
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			Vector2 vector = MeasureWithGlyphs(font, text2.Trim(), scale);
			num2 = Math.Max(vector.X, num2);
			num3 += vector.Y;
			num4 = vector.Y;
		}
		num3 += num4;
		offset -= new Vector2(num2, num3);
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text3 = array2[i].Trim();
			Vector2 vector2 = MeasureWithGlyphs(font, text3, scale);
			DrawString(batch, font, text3, new Vector2(num2 - vector2.X, num) + offset, color, scale);
			num += vector2.Y;
		}
	}

	public void DrawCenteredString(SpriteBatch batch, SpriteFont font, string text, Color color, Vector2 offset, float scale)
	{
		DrawCenteredString(batch, font, text, color, offset, scale, shadow: true);
	}

	public void DrawCenteredString(SpriteBatch batch, SpriteFont font, string text, Color color, Vector2 offset, float scale, bool shadow)
	{
		float num = (float)game.GraphicsDevice.Viewport.Width * 0.8f;
		text = WordWrap.Split(text, font, (num - offset.X / 2f) / scale);
		float y = MeasureWithGlyphs(font, text, scale).Y;
		int num2 = 0;
		int num3 = 0;
		while (num2 < text.Length && num2 != -1)
		{
			num2 = text.IndexOf(Environment.NewLine, num2);
			if (num2 != -1)
			{
				num2 += 2;
			}
			num3++;
		}
		int num4 = game.GraphicsDevice.Viewport.Width / 2;
		num2 = 0;
		Vector2 vector = offset + num4 * Vector2.UnitX;
		while (num2 < text.Length && num2 != -1)
		{
			int num5 = text.IndexOf("\r\n", num2);
			string text2 = text.Substring(num2, (num5 == -1) ? (text.Length - num2) : (num5 - num2));
			float x = MeasureWithGlyphs(font, text2, scale).X;
			if (shadow)
			{
				DrawString(batch, font, text2, vector - new Vector2(x / 2f, 0f) + new Vector2(1f, 1f), new Color(0, 0, 0, (byte)(color.A / 2)), scale);
			}
			DrawString(batch, font, text2, vector - new Vector2(x / 2f, 0f), color, scale);
			vector.Y += y / (float)num3;
			num2 = num5;
			if (num2 != -1)
			{
				num2 += 2;
			}
		}
	}
}
