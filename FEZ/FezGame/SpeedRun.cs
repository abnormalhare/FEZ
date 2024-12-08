using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame;

public static class SpeedRun
{
	private static int CubeCount;

	private static string RunData;

	private static Stopwatch Timer;

	private static SpriteBatch Batch;

	private static Texture2D Font;

	private static bool Began;

	private static bool TimeCalled;

	private static readonly char[] chars = new char[12]
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'C', 'A'
	};

	private static Rectangle charSrc = new Rectangle(0, 0, 5, 5);

	private static readonly Rectangle colonSrc = new Rectangle(60, 0, 3, 5);

	public static void Begin(Texture2D font)
	{
		CubeCount = 0;
		RunData = string.Empty;
		Timer = Stopwatch.StartNew();
		Font = font;
		Batch = new SpriteBatch(Font.GraphicsDevice);
		TimeCalled = false;
		Began = true;
	}

	public static void Dispose()
	{
		Began = false;
		if (Batch != null)
		{
			Batch.Dispose();
			Batch = null;
		}
	}

	public static void AddCube(bool anti)
	{
		if (Began)
		{
			TimeSpan elapsed = Timer.Elapsed;
			RunData += string.Format("\n{5}{0:D2}: {1}:{2:D2}:{3:D2}:{4:D3}", ++CubeCount, elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds, anti ? 'A' : 'C');
		}
	}

	public static void CallTime(string saveFolder)
	{
		if (Began)
		{
			TimeCalled = true;
			Timer.Stop();
			TimeSpan elapsed = Timer.Elapsed;
			DateTime now = DateTime.Now;
			File.WriteAllText(Path.Combine(saveFolder, $"RUN_{now.Day:D2}{now.Month:D2}{now.Year:D4}_{now.Hour:D2}{now.Minute:D2}{now.Second:D2}.txt"), string.Format("{0}:{1:D2}:{2:D2}:{3:D3}:{5:D4}{4}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds, RunData.Replace("\n", Environment.NewLine), elapsed.Ticks % 10000));
		}
	}

	public static void PauseForLoading()
	{
		if (Began)
		{
			Timer.Stop();
		}
	}

	public static void ResumeAfterLoading()
	{
		if (Began && !TimeCalled)
		{
			Timer.Start();
		}
	}

	public static void Draw(float scale)
	{
		if (Began)
		{
			TimeSpan elapsed = Timer.Elapsed;
			Viewport viewport = Batch.GraphicsDevice.Viewport;
			Vector2 vector = new Vector2(viewport.Width - (int)(250f * ((float)viewport.Width / 1280f)), 100 * (int)((float)viewport.Height / 720f));
			string text = string.Format(Timer.IsRunning ? "{0}:{1:D2}:{2:D2}:{3:D3}{4}" : "{0}:{1:D2}:{2:D2}:{3:D3}:{5:D4}{4}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds, (CubeCount > 32) ? RunData.Substring(17 * (CubeCount - 32)) : RunData, elapsed.Ticks % 10000);
			Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
			DrawText(text, Color.Black, vector + new Vector2(scale, scale), scale);
			DrawText(text, Color.White, vector, scale);
			Batch.End();
		}
	}

	private static void DrawText(string text, Color color, Vector2 pos, float scale)
	{
		float x = pos.X;
		foreach (char c in text)
		{
			Rectangle? sourceRectangle;
			int num;
			switch (c)
			{
			case '\n':
				pos.X = x;
				pos.Y += 15f * scale;
				continue;
			case ' ':
				pos.X += 9f * scale;
				continue;
			case ':':
				sourceRectangle = colonSrc;
				pos.X -= 3f * scale;
				num = 8;
				break;
			default:
				charSrc.X = 5 * Array.IndexOf(chars, c);
				sourceRectangle = charSrc;
				num = 15;
				break;
			}
			Batch.Draw(Font, pos, sourceRectangle, color, 0f, Vector2.Zero, 2f * scale, SpriteEffects.None, 0f);
			pos.X += (float)num * scale;
		}
	}
}
