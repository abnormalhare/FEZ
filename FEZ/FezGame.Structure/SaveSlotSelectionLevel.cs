using System;
using System.Globalization;
using System.IO;
using Common;
using EasyStorage;
using FezEngine.Components;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Structure;

internal class SaveSlotSelectionLevel : MenuLevel
{
	private readonly SaveSlotInfo[] Slots = new SaveSlotInfo[3];

	public Func<bool> RecoverMainMenu;

	public Action RunStart;

	private Texture2D GottaGomezFast;

	private IGameStateManager GameState;

	public override void Initialize()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		base.Initialize();
		AButtonString = "ChooseWithGlyph";
		base.BButtonString = "ExitWithGlyph";
		GameState = ServiceHelper.Get<IGameStateManager>();
		base.Title = "SaveSlotTitle";
		for (int i = 0; i < 3; i++)
		{
			SaveSlotInfo saveSlotInfo = (Slots[i] = new SaveSlotInfo
			{
				Index = i
			});
			PCSaveDevice val = new PCSaveDevice("FEZ");
			string text = "SaveSlot" + i;
			if (!val.FileExists(text))
			{
				saveSlotInfo.Empty = true;
				continue;
			}
			SaveData saveData = null;
			if (!val.Load(text, (LoadAction)delegate(BinaryReader stream)
			{
				saveData = SaveFileOperations.Read(new CrcReader(stream));
			}) || saveData == null)
			{
				saveSlotInfo.Empty = true;
				continue;
			}
			saveSlotInfo.Percentage = ((float)(saveData.CubeShards + saveData.SecretCubes + saveData.PiecesOfHeart) + (float)saveData.CollectedParts / 8f) / 32f;
			saveSlotInfo.PlayTime = new TimeSpan(saveData.PlayTime);
			string text2 = saveData.Level;
			if (text2.Contains("GOMEZ_HOUSE"))
			{
				text2 = "GOMEZ_HOUSE";
			}
			if (text2.Contains("VILLAGEVILLE") || text2 == "ELDERS")
			{
				text2 = "VILLAGEVILLE_3D";
			}
			if (text2 == "PYRAMID" || text2 == "HEX_REBUILD")
			{
				text2 = "STARGATE";
			}
			try
			{
				saveSlotInfo.PreviewTexture = base.CMProvider.Global.Load<Texture2D>("Other Textures/map_screens/" + text2);
			}
			catch
			{
				Logger.Log("Content", "Room " + text2 + " does not have a map image!");
			}
			IsDynamic = true;
		}
		if (RunStart != null && Fez.SpeedRunMode)
		{
			IsDynamic = true;
			AddItem(null).Selectable = false;
			GottaGomezFast = base.CMProvider.Global.Load<Texture2D>("Other Textures/GottaGomezFast");
		}
		if (IsDynamic)
		{
			for (int j = 0; j < 5; j++)
			{
				AddItem(null).Selectable = false;
			}
			base.SelectedIndex = Items.Count;
			if (base.SelectedIndex == 6)
			{
				AddItem(null, delegate
				{
					BeginSpeedRun();
				}).SuffixText = () => "SPEEDRUN";
			}
			OnPostDraw = (Action<SpriteBatch, SpriteFont, GlyphTextRenderer, float>)Delegate.Combine(OnPostDraw, new Action<SpriteBatch, SpriteFont, GlyphTextRenderer, float>(IconPostDraw));
		}
		SaveSlotInfo[] slots = Slots;
		foreach (SaveSlotInfo slot in slots)
		{
			if (slot.Empty)
			{
				AddItem(null, delegate
				{
					ChooseSaveSlot(slot);
				}).SuffixText = () => StaticText.GetString("NewSlot");
				continue;
			}
			AddItem("SaveSlotPrefix", delegate
			{
				ChooseSaveSlot(slot);
			}).SuffixText = () => string.Format(CultureInfo.InvariantCulture, " {0} ({1:P1} - {2:dd\\.hh\\:mm})", new object[3]
			{
				slot.Index + 1,
				slot.Percentage,
				slot.PlayTime
			});
		}
	}

	private void ChooseSaveSlot(SaveSlotInfo slot)
	{
		GameState.SaveSlot = slot.Index;
		GameState.LoadSaveFile(delegate
		{
			GameState.Save();
			GameState.SaveImmediately();
			if (RecoverMainMenu != null && RecoverMainMenu())
			{
				RecoverMainMenu = null;
			}
		});
	}

	private void BeginSpeedRun()
	{
		GameState.SaveSlot = 4;
		GameState.LoadSaveFile(delegate
		{
			GameState.Save();
			GameState.SaveImmediately();
			if (RecoverMainMenu != null && RecoverMainMenu())
			{
				RecoverMainMenu = null;
			}
		});
		RunStart();
		SpeedRun.Begin(base.CMProvider.Global.Load<Texture2D>("Other Textures/SpeedRun"));
	}

	private void IconPostDraw(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha)
	{
		Texture2D texture2D = null;
		int num = 4;
		int num2 = base.SelectedIndex - 5;
		if (Fez.SpeedRunMode)
		{
			num += 2;
			num2 -= 2;
		}
		if (base.SelectedIndex > num && !Slots[num2].Empty)
		{
			texture2D = Slots[num2].PreviewTexture;
		}
		else if (base.SelectedIndex == num)
		{
			texture2D = GottaGomezFast;
		}
		if (texture2D != null)
		{
			float viewScale = batch.GraphicsDevice.GetViewScale();
			int num3 = (int)(256f * viewScale);
			batch.Draw(texture2D, new Rectangle(batch.GraphicsDevice.Viewport.Width / 2, batch.GraphicsDevice.Viewport.Height / 2 - (int)(96f * viewScale), num3, num3), null, Color.White, 0f, new Vector2(texture2D.Width / 2, texture2D.Height / 2), SpriteEffects.None, 0f);
		}
	}
}
