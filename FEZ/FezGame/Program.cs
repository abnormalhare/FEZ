using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Common;
using FezEngine.Tools;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using SDL2;

namespace FezGame;

internal static class Program
{
	private static Fez fez;

	[STAThread]
	private static void Main(string[] args)
	{
		Logger.Clear();
		FNALoggerEXT.LogInfo = delegate(string msg)
		{
			Logger.Log("FNA", LogSeverity.Information, msg);
		};
		FNALoggerEXT.LogWarn = delegate(string msg)
		{
			Logger.Log("FNA", LogSeverity.Warning, msg);
		};
		FNALoggerEXT.LogError = delegate(string msg)
		{
			Logger.Log("FNA", LogSeverity.Error, msg);
		};
		SettingsManager.InitializeSettings();
		PersistentThreadPool.SingleThreaded = SettingsManager.Settings.Singlethreaded;
		Fez.DisableSteamworksInit = SettingsManager.Settings.DisableSteamworks;
		Queue<string> queue = new Queue<string>();
		foreach (string item in args)
		{
			queue.Enqueue(item);
		}
		while (queue.Count > 0)
		{
			switch (queue.Dequeue().ToLower(CultureInfo.InvariantCulture))
			{
			case "-c":
			case "--clear-save-file":
			{
				string text2 = Path.Combine(Util.LocalSaveFolder, "SaveSlot");
				if (File.Exists(text2 + "0"))
				{
					File.Delete(text2 + "0");
				}
				break;
			}
			case "-ng":
			case "--no-gamepad":
				SettingsManager.Settings.DisableController = true;
				break;
			case "-r":
			case "--region":
				SettingsManager.Settings.Language = (Language)Enum.Parse(typeof(Language), queue.Dequeue());
				break;
			case "--trace":
				TraceFlags.TraceContentLoad = true;
				break;
			case "-pd":
			case "--public-demo":
			{
				Fez.PublicDemo = true;
				string text = Path.Combine(Util.LocalSaveFolder, "SaveSlot");
				for (int j = -1; j < 3; j++)
				{
					if (File.Exists(text + j))
					{
						File.Delete(text + j);
					}
				}
				break;
			}
			case "-nm":
			case "--no-music":
				Fez.NoMusic = true;
				break;
			case "-st":
			case "--singlethreaded":
				SettingsManager.Settings.Singlethreaded = true;
				PersistentThreadPool.SingleThreaded = true;
				break;
			case "--msaa-option":
				SettingsManager.Settings.MultiSampleOption = true;
				break;
			case "--attempt-highdpi":
				SettingsManager.Settings.HighDPI = true;
				break;
			case "--gotta-gomez-fast":
				Fez.SpeedRunMode = true;
				PersistentThreadPool.SingleThreaded = true;
				break;
			}
		}
		if (SettingsManager.Settings.HighDPI)
		{
			Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
		}
		Logger.Try(MainInternal);
		if (fez != null)
		{
			fez.Dispose();
		}
		fez = null;
		Logger.Log("FEZ", "Exiting.");
		if (SDL.SDL_GetPlatform().Equals("Windows"))
		{
			try
			{
				ThreadExecutionState.TearDown();
			}
			catch (Exception ex)
			{
				Logger.Log("ThreadExecutionState", ex.ToString());
			}
		}
	}

	private static void MainInternal()
	{
		Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
		fez = new Fez();
		if (!fez.IsDisposed)
		{
			fez.Run();
		}
	}
}
