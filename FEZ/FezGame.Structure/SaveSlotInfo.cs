using System;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Structure;

internal class SaveSlotInfo
{
	public TimeSpan PlayTime;

	public float Percentage;

	public int Index;

	public bool Empty;

	public Texture2D PreviewTexture;

	public SaveData SaveData;
}
