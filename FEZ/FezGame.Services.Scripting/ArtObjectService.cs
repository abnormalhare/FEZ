using System;
using Common;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Services.Scripting;

public class ArtObjectService : IArtObjectService, IScriptingBase
{
	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public event Action<int> TreasureOpened = Util.NullAction;

	public void ResetEvents()
	{
		this.TreasureOpened = Util.NullAction;
	}

	public void OnTreasureOpened(int id)
	{
		this.TreasureOpened(id);
	}

	public void SetRotation(int id, float x, float y, float z)
	{
		LevelManager.ArtObjects[id].Rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(y), MathHelper.ToRadians(x), MathHelper.ToRadians(z));
	}

	public void GlitchOut(int id, bool permanent, string spawnedActor)
	{
		if (LevelManager.ArtObjects.ContainsKey(id))
		{
			if (string.IsNullOrEmpty(spawnedActor))
			{
				ServiceHelper.AddComponent(new GlitchyDespawner(ServiceHelper.Game, LevelManager.ArtObjects[id]));
			}
			else
			{
				ServiceHelper.AddComponent(new GlitchyDespawner(ServiceHelper.Game, LevelManager.ArtObjects[id], LevelManager.ArtObjects[id].Position)
				{
					ActorToSpawn = (ActorType)Enum.Parse(typeof(ActorType), spawnedActor, ignoreCase: true)
				});
			}
			if (permanent)
			{
				GameState.SaveData.ThisLevel.InactiveArtObjects.Add(-id - 1);
			}
		}
	}

	public LongRunningAction Move(int id, float dX, float dY, float dZ, float easeInFor, float easeOutAfter, float easeOutFor)
	{
		TrileGroup group = null;
		int? attachedGroup = LevelManager.ArtObjects[id].ActorSettings.AttachedGroup;
		if (attachedGroup.HasValue && LevelManager.Groups.ContainsKey(attachedGroup.Value))
		{
			group = LevelManager.Groups[attachedGroup.Value];
		}
		return new LongRunningAction(delegate(float elapsedSeconds, float totalSeconds)
		{
			if (!LevelManager.ArtObjects.TryGetValue(id, out var value))
			{
				return true;
			}
			if (totalSeconds < easeInFor)
			{
				elapsedSeconds *= Easing.EaseIn(totalSeconds / easeInFor, EasingType.Quadratic);
			}
			if (easeOutFor != 0f && totalSeconds > easeOutAfter)
			{
				elapsedSeconds *= Easing.EaseOut(1f - (totalSeconds - easeOutAfter) / easeOutFor, EasingType.Quadratic);
			}
			Vector3 vector = new Vector3(dX, dY, dZ) * elapsedSeconds;
			if (group != null)
			{
				foreach (TrileInstance trile in group.Triles)
				{
					trile.Position += vector;
					LevelManager.UpdateInstance(trile);
				}
			}
			value.Position += vector;
			return false;
		});
	}

	public LongRunningAction TiltOnVertex(int id, float durationSeconds)
	{
		return new LongRunningAction(delegate(float _, float totalSeconds)
		{
			if (!LevelManager.ArtObjects.TryGetValue(id, out var value))
			{
				return true;
			}
			float value2 = Easing.EaseInOut(totalSeconds / durationSeconds, EasingType.Sine);
			value.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0)) * FezMath.Saturate(value2)) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f * FezMath.Saturate(value2));
			return false;
		});
	}

	public LongRunningAction Rotate(int id, float dX, float dY, float dZ)
	{
		return new LongRunningAction(delegate(float elapsedSeconds, float _)
		{
			if (!LevelManager.ArtObjects.TryGetValue(id, out var value))
			{
				return true;
			}
			value.Rotation = Quaternion.CreateFromYawPitchRoll(dY * ((float)Math.PI * 2f) * elapsedSeconds, dX * ((float)Math.PI * 2f) * elapsedSeconds, dZ * ((float)Math.PI * 2f) * elapsedSeconds) * value.Rotation;
			return false;
		});
	}

	public LongRunningAction RotateIncrementally(int id, float initPitch, float initYaw, float initRoll, float secondsUntilDouble)
	{
		return new LongRunningAction(delegate(float elapsedSeconds, float _)
		{
			if (!LevelManager.ArtObjects.TryGetValue(id, out var value))
			{
				return true;
			}
			initYaw = FezMath.DoubleIter(initYaw, elapsedSeconds, secondsUntilDouble);
			initPitch = FezMath.DoubleIter(initPitch, elapsedSeconds, secondsUntilDouble);
			initRoll = FezMath.DoubleIter(initRoll, elapsedSeconds, secondsUntilDouble);
			value.Rotation = Quaternion.CreateFromYawPitchRoll(initYaw, initPitch, initRoll) * value.Rotation;
			return false;
		});
	}

	public LongRunningAction HoverFloat(int id, float height, float cyclesPerSecond)
	{
		float lastDelta = 0f;
		return new LongRunningAction(delegate(float _, float sinceStarted)
		{
			if (!LevelManager.ArtObjects.TryGetValue(id, out var value))
			{
				return true;
			}
			float num = (float)Math.Sin(sinceStarted * ((float)Math.PI * 2f) * cyclesPerSecond) * height;
			value.Position = new Vector3(value.Position.X, value.Position.Y - lastDelta + num, value.Position.Z);
			lastDelta = num;
			return false;
		});
	}

	public LongRunningAction BeamGomez(int id)
	{
		throw new InvalidOperationException();
	}

	public LongRunningAction Pulse(int id, string textureName)
	{
		BackgroundPlane lightPlane = new BackgroundPlane(LevelMaterializer.StaticPlanesMesh, textureName, animated: false)
		{
			Position = LevelManager.ArtObjects[id].Position - CameraManager.Viewpoint.ForwardVector() * 10f,
			Rotation = CameraManager.Rotation,
			AllowOverbrightness = true,
			LightMap = true,
			AlwaysOnTop = true,
			PixelatedLightmap = true
		};
		LevelManager.AddPlane(lightPlane);
		return new LongRunningAction(delegate(float _, float sinceStarted)
		{
			float value = FezMath.Saturate(sinceStarted / 2f);
			float num = Easing.EaseOut(FezMath.Saturate(value), EasingType.Quadratic);
			lightPlane.Filter = new Color(1f - num, 1f - num, 1f - num);
			num = Easing.EaseOut(FezMath.Saturate(value), EasingType.Quartic);
			lightPlane.Scale = new Vector3(1f + num * 10f);
			if (num == 1f)
			{
				LevelManager.RemovePlane(lightPlane);
			}
			return num == 1f;
		});
	}

	public LongRunningAction Say(int id, string text, bool zuish)
	{
		SpeechBubble.Font = (zuish ? SpeechFont.Zuish : SpeechFont.Pixel);
		SpeechBubble.ChangeText(zuish ? text : GameText.GetString(text));
		PlayerManager.Velocity *= Vector3.UnitY;
		PlayerManager.Action = ActionType.ReadingSign;
		return new LongRunningAction(delegate
		{
			if (!LevelManager.ArtObjects.TryGetValue(id, out var value))
			{
				return true;
			}
			SpeechBubble.Origin = value.Position + CameraManager.Viewpoint.RightVector() * value.ArtObject.Size * 0.75f - Vector3.UnitY;
			return SpeechBubble.Hidden ? true : false;
		});
	}

	public LongRunningAction StartEldersSequence(int id)
	{
		ArtObjectInstance aoInstance = LevelManager.ArtObjects[id];
		ServiceHelper.AddComponent(new EldersHexahedron(ServiceHelper.Game, aoInstance));
		return new LongRunningAction((float _, float __) => false);
	}

	public void MoveNutToEnd(int id)
	{
		LevelManager.ArtObjects[id].ActorSettings.ShouldMoveToEnd = true;
	}

	public void MoveNutToHeight(int id, float height)
	{
		LevelManager.ArtObjects[id].ActorSettings.ShouldMoveToHeight = height;
	}
}
