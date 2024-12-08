using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class BitDoorState
{
	private readonly Vector3[] SixtyFourOffsets = new Vector3[64]
	{
		new Vector3(8f, 52f, 0f),
		new Vector3(12f, 52f, 0f),
		new Vector3(8f, 48f, 0f),
		new Vector3(12f, 48f, 0f),
		new Vector3(0f, 44f, 0f),
		new Vector3(4f, 44f, 0f),
		new Vector3(8f, 44f, 0f),
		new Vector3(12f, 44f, 0f),
		new Vector3(16f, 44f, 0f),
		new Vector3(20f, 44f, 0f),
		new Vector3(0f, 40f, 0f),
		new Vector3(4f, 40f, 0f),
		new Vector3(8f, 40f, 0f),
		new Vector3(12f, 40f, 0f),
		new Vector3(16f, 40f, 0f),
		new Vector3(20f, 40f, 0f),
		new Vector3(0f, 36f, 0f),
		new Vector3(4f, 36f, 0f),
		new Vector3(8f, 36f, 0f),
		new Vector3(12f, 36f, 0f),
		new Vector3(16f, 36f, 0f),
		new Vector3(20f, 36f, 0f),
		new Vector3(0f, 32f, 0f),
		new Vector3(4f, 32f, 0f),
		new Vector3(8f, 32f, 0f),
		new Vector3(12f, 32f, 0f),
		new Vector3(16f, 32f, 0f),
		new Vector3(20f, 32f, 0f),
		new Vector3(0f, 28f, 0f),
		new Vector3(4f, 28f, 0f),
		new Vector3(16f, 28f, 0f),
		new Vector3(20f, 28f, 0f),
		new Vector3(0f, 24f, 0f),
		new Vector3(4f, 24f, 0f),
		new Vector3(16f, 24f, 0f),
		new Vector3(20f, 24f, 0f),
		new Vector3(0f, 20f, 0f),
		new Vector3(4f, 20f, 0f),
		new Vector3(8f, 20f, 0f),
		new Vector3(12f, 20f, 0f),
		new Vector3(16f, 20f, 0f),
		new Vector3(20f, 20f, 0f),
		new Vector3(0f, 16f, 0f),
		new Vector3(4f, 16f, 0f),
		new Vector3(8f, 16f, 0f),
		new Vector3(12f, 16f, 0f),
		new Vector3(16f, 16f, 0f),
		new Vector3(20f, 16f, 0f),
		new Vector3(0f, 12f, 0f),
		new Vector3(4f, 12f, 0f),
		new Vector3(8f, 12f, 0f),
		new Vector3(12f, 12f, 0f),
		new Vector3(16f, 12f, 0f),
		new Vector3(20f, 12f, 0f),
		new Vector3(0f, 8f, 0f),
		new Vector3(4f, 8f, 0f),
		new Vector3(8f, 8f, 0f),
		new Vector3(12f, 8f, 0f),
		new Vector3(16f, 8f, 0f),
		new Vector3(20f, 8f, 0f),
		new Vector3(8f, 4f, 0f),
		new Vector3(12f, 4f, 0f),
		new Vector3(8f, 0f, 0f),
		new Vector3(12f, 0f, 0f)
	};

	private readonly Vector3[] ThirtyTwoOffsets = new Vector3[32]
	{
		new Vector3(0f, 2.625f, 0f),
		new Vector3(0.375f, 2.625f, 0f),
		new Vector3(0.75f, 2.625f, 0f),
		new Vector3(1.125f, 2.625f, 0f),
		new Vector3(0f, 2.25f, 0f),
		new Vector3(0.375f, 2.25f, 0f),
		new Vector3(0.75f, 2.25f, 0f),
		new Vector3(1.125f, 2.25f, 0f),
		new Vector3(0f, 1.875f, 0f),
		new Vector3(0.375f, 1.875f, 0f),
		new Vector3(0.75f, 1.875f, 0f),
		new Vector3(1.125f, 1.875f, 0f),
		new Vector3(0f, 1.5f, 0f),
		new Vector3(0.375f, 1.5f, 0f),
		new Vector3(0.75f, 1.5f, 0f),
		new Vector3(1.125f, 1.5f, 0f),
		new Vector3(0f, 1.125f, 0f),
		new Vector3(0.375f, 1.125f, 0f),
		new Vector3(0.75f, 1.125f, 0f),
		new Vector3(1.125f, 1.125f, 0f),
		new Vector3(0f, 0.75f, 0f),
		new Vector3(0.375f, 0.75f, 0f),
		new Vector3(0.75f, 0.75f, 0f),
		new Vector3(1.125f, 0.75f, 0f),
		new Vector3(0f, 0.375f, 0f),
		new Vector3(0.375f, 0.375f, 0f),
		new Vector3(0.75f, 0.375f, 0f),
		new Vector3(1.125f, 0.375f, 0f),
		new Vector3(0f, 0f, 0f),
		new Vector3(0.375f, 0f, 0f),
		new Vector3(0.75f, 0f, 0f),
		new Vector3(1.125f, 0f, 0f)
	};

	private readonly Vector3[] SixteenOffsets = new Vector3[16]
	{
		new Vector3(0.5f, 3f, 0f),
		new Vector3(0f, 2.5f, 0f),
		new Vector3(0.5f, 2.5f, 0f),
		new Vector3(1f, 2.5f, 0f),
		new Vector3(0f, 2f, 0f),
		new Vector3(0.5f, 2f, 0f),
		new Vector3(1f, 2f, 0f),
		new Vector3(0f, 1.5f, 0f),
		new Vector3(1f, 1.5f, 0f),
		new Vector3(0f, 1f, 0f),
		new Vector3(0.5f, 1f, 0f),
		new Vector3(1f, 1f, 0f),
		new Vector3(0f, 0.5f, 0f),
		new Vector3(0.5f, 0.5f, 0f),
		new Vector3(1f, 0.5f, 0f),
		new Vector3(0.5f, 0f, 0f)
	};

	private readonly Vector3[] EightOffsets = new Vector3[8]
	{
		new Vector3(0.5f, 1.5f, 0f),
		new Vector3(0f, 1f, 0f),
		new Vector3(0.5f, 1f, 0f),
		new Vector3(1f, 1f, 0f),
		new Vector3(0f, 0.5f, 0f),
		new Vector3(0.5f, 0.5f, 0f),
		new Vector3(1f, 0.5f, 0f),
		new Vector3(0.5f, 0f, 0f)
	};

	private readonly Vector3[] FourOffsets = new Vector3[4]
	{
		new Vector3(0f, 1.75f, 0f),
		new Vector3(0f, 1.25f, 0f),
		new Vector3(0f, 0.75f, 0f),
		new Vector3(0f, 0.25f, 0f)
	};

	private static readonly TimeSpan DoorShakeTime = TimeSpan.FromSeconds(0.5);

	private static readonly TimeSpan DoorOpenTime = TimeSpan.FromSeconds(3.0);

	private readonly Viewpoint ExpectedViewpoint;

	private readonly SoundEffect RumbleSound;

	private readonly SoundEffect sLightUp;

	private readonly SoundEffect sFadeOut;

	private Texture2D BitTexture;

	private Texture2D AntiBitTexture;

	public readonly ArtObjectInstance AoInstance;

	private readonly List<BackgroundPlane> BitPlanes = new List<BackgroundPlane>();

	private bool close;

	private bool opening;

	private TimeSpan sinceClose;

	private TimeSpan sinceMoving;

	private Vector3 doorOrigin;

	private Vector3 doorDestination;

	private SoundEmitter rumbleEmitter;

	private int lastBits;

	[ServiceDependency]
	public ILevelService LevelService { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public IBitDoorService BitDoorService { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	public BitDoorState(ArtObjectInstance artObject)
	{
		ServiceHelper.InjectServices(this);
		AoInstance = artObject;
		switch (artObject.ArtObject.ActorType)
		{
		case ActorType.EightBitDoor:
		case ActorType.OneBitDoor:
		case ActorType.FourBitDoor:
		case ActorType.TwoBitDoor:
		case ActorType.SixteenBitDoor:
			DrawActionScheduler.Schedule(delegate
			{
				BitTexture = CMProvider.Global.Load<Texture2D>("Other Textures/glow/GLOWBIT");
				AntiBitTexture = CMProvider.Global.Load<Texture2D>("Other Textures/glow/GLOWBIT_anti");
			});
			break;
		case ActorType.ThirtyTwoBitDoor:
			DrawActionScheduler.Schedule(delegate
			{
				BitTexture = CMProvider.Global.Load<Texture2D>("Other Textures/glow/small_glowbit");
				AntiBitTexture = CMProvider.Global.Load<Texture2D>("Other Textures/glow/small_glowbit_anti");
			});
			break;
		default:
		{
			DrawActionScheduler.Schedule(delegate
			{
				BitTexture = CMProvider.Global.Load<Texture2D>("Other Textures/glow/code_machine_glowbit");
				AntiBitTexture = CMProvider.Global.Load<Texture2D>("Other Textures/glow/code_machine_glowbit_anti");
			});
			for (int i = 0; i < 64; i++)
			{
				SixtyFourOffsets[i] /= 16f;
			}
			break;
		}
		}
		RumbleSound = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/Rumble");
		sLightUp = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Zu/DoorBitLightUp");
		sFadeOut = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Zu/DoorBitFadeOut");
		ExpectedViewpoint = FezMath.OrientationFromDirection(Vector3.Transform(Vector3.UnitZ, AoInstance.Rotation).MaxClamp()).AsViewpoint();
		lastBits = -1;
	}

	private void InitBitPlanes()
	{
		if (lastBits == GameState.SaveData.CubeShards + GameState.SaveData.SecretCubes)
		{
			return;
		}
		foreach (BackgroundPlane bitPlane in BitPlanes)
		{
			LevelManager.RemovePlane(bitPlane);
		}
		BitPlanes.Clear();
		if (AoInstance.Rotation == new Quaternion(0f, 0f, 0f, -1f))
		{
			AoInstance.Rotation = Quaternion.Identity;
		}
		int bitCount = AoInstance.ArtObject.ActorType.GetBitCount();
		for (int i = 0; i < bitCount; i++)
		{
			Texture2D texture = ((i < GameState.SaveData.CubeShards) ? BitTexture : AntiBitTexture);
			BackgroundPlane backgroundPlane = new BackgroundPlane(LevelMaterializer.StaticPlanesMesh, texture)
			{
				Rotation = AoInstance.Rotation,
				Opacity = 0f,
				Fullbright = true
			};
			BitPlanes.Add(backgroundPlane);
			LevelManager.AddPlane(backgroundPlane);
		}
		lastBits = GameState.SaveData.CubeShards + GameState.SaveData.SecretCubes;
	}

	public void Update(TimeSpan elapsed)
	{
		if (lastBits == -1)
		{
			InitBitPlanes();
		}
		DetermineIsClose();
		if (!AoInstance.ActorSettings.Inactive)
		{
			if (!opening && !close && sinceClose.TotalSeconds > 0.0)
			{
				sinceClose -= elapsed;
			}
			else if (close && sinceClose.TotalSeconds < 3.0)
			{
				sinceClose += elapsed;
			}
			FadeBits();
			if (GameState.SaveData.CubeShards + GameState.SaveData.SecretCubes >= AoInstance.ArtObject.ActorType.GetBitCount() && sinceClose.TotalSeconds > 0.5)
			{
				OpenDoor(elapsed);
			}
		}
	}

	private void DetermineIsClose()
	{
		close = false;
		if (AoInstance.Visible && !AoInstance.ActorSettings.Inactive && CameraManager.Viewpoint == ExpectedViewpoint && !PlayerManager.Background)
		{
			Vector3 position = AoInstance.Position;
			Vector3 value = Vector3.Transform(new Vector3(0f, 0f, 1f), AoInstance.Rotation);
			Vector3 vector = position + Vector3.Transform(value, AoInstance.Rotation);
			Vector3 vector2 = (vector - PlayerManager.Position).Abs() * CameraManager.Viewpoint.ScreenSpaceMask();
			close = vector2.X + vector2.Z < 2f && vector2.Y < 2f && (vector - PlayerManager.Position).Dot(CameraManager.Viewpoint.ForwardVector()) >= 0f;
		}
	}

	public Vector3 GetOpenOffset()
	{
		switch (AoInstance.ArtObject.ActorType.GetBitCount())
		{
		case 2:
			return new Vector3(0f, 4f, 0f);
		case 1:
		case 4:
		case 8:
		case 16:
			return new Vector3(0f, 4f, 0f) - Vector3.Transform(new Vector3(0f, 0f, 0.125f), AoInstance.Rotation);
		case 32:
			return new Vector3(0f, -4f, 0f) - Vector3.Transform(new Vector3(0f, 0f, 0.1875f), AoInstance.Rotation);
		case 64:
			return new Vector3(0f, -4f, 0f) - Vector3.Transform(new Vector3(0f, 0f, 0.125f), AoInstance.Rotation);
		default:
			throw new InvalidOperationException();
		}
	}

	private void OpenDoor(TimeSpan elapsed)
	{
		if (!opening)
		{
			doorOrigin = AoInstance.Position + GetOpenOffset() * FezMath.XZMask;
			doorDestination = AoInstance.Position + GetOpenOffset();
			opening = true;
			rumbleEmitter = RumbleSound.EmitAt(doorOrigin, loop: true);
			LevelService.ResolvePuzzle();
		}
		sinceMoving += elapsed;
		Vector3 position;
		if (sinceMoving > DoorShakeTime)
		{
			float amount = FezMath.Saturate(Easing.EaseInOut((float)(sinceMoving.Ticks - DoorShakeTime.Ticks) / (float)DoorOpenTime.Ticks, EasingType.Sine));
			position = Vector3.Lerp(doorOrigin, doorDestination, amount);
		}
		else
		{
			position = doorOrigin;
		}
		position += new Vector3(RandomHelper.Centered(0.014999999664723873), RandomHelper.Centered(0.014999999664723873), RandomHelper.Centered(0.014999999664723873)) * CameraManager.Viewpoint.ScreenSpaceMask();
		AoInstance.Position = position;
		if (sinceMoving > DoorOpenTime + DoorShakeTime)
		{
			rumbleEmitter.FadeOutAndDie(0.25f);
			BitDoorService.OnOpen(AoInstance.Id);
			AoInstance.ActorSettings.Inactive = true;
			GameState.SaveData.ThisLevel.InactiveArtObjects.Add(AoInstance.Id);
			GameState.Save();
			opening = false;
		}
	}

	private void FadeBits()
	{
		if (sinceClose.Ticks == 0L)
		{
			return;
		}
		InitBitPlanes();
		Vector3 position = AoInstance.Position;
		Vector3 vector = new Vector3(0f, 0f, 1f);
		for (int i = 0; i < BitPlanes.Count; i++)
		{
			Vector3 value = vector;
			switch (BitPlanes.Count)
			{
			case 2:
				value += new Vector3(0f, 0.25f - (float)i * 0.5f, 0f);
				break;
			case 4:
				value += new Vector3(0f, -1f, 0f) + FourOffsets[i];
				break;
			case 8:
				value += new Vector3(-0.5f, -0.75f, 0f) + EightOffsets[i];
				break;
			case 16:
				value += new Vector3(-0.5f, -1.5f, 0f) + SixteenOffsets[i];
				break;
			case 32:
				value += new Vector3(-0.5625f, -1.3125f, 0f) + ThirtyTwoOffsets[i];
				break;
			case 64:
				value += new Vector3(-0.625f, -1.625f, 0f) + SixtyFourOffsets[i];
				break;
			}
			int num = GameState.SaveData.CubeShards + GameState.SaveData.SecretCubes;
			BitPlanes[i].Position = position + Vector3.Transform(value, AoInstance.Rotation);
			float opacity = BitPlanes[i].Opacity;
			BitPlanes[i].Opacity = (float)(num > i).AsNumeric() * Easing.EaseIn(FezMath.Saturate(sinceClose.TotalSeconds * 2.0 - (double)((float)i / ((float)BitPlanes.Count * 0.666f + 3.996f))), EasingType.Sine);
			if (BitPlanes[i].Opacity > opacity && opacity > 0.1f && BitPlanes[i].Loop)
			{
				sLightUp.EmitAt(position);
				BitPlanes[i].Loop = false;
			}
			else if (BitPlanes[i].Opacity < opacity && !BitPlanes[i].Loop)
			{
				sFadeOut.EmitAt(position);
				BitPlanes[i].Loop = true;
			}
		}
	}
}
