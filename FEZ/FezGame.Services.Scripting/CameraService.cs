using System;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Services.Scripting;

public class CameraService : ICameraService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	public event Action Rotated = Util.NullAction;

	public void ResetEvents()
	{
		this.Rotated = Util.NullAction;
	}

	public void OnRotate()
	{
		this.Rotated();
	}

	public void SetPixelsPerTrixel(int pixelsPerTrixel)
	{
		if (!EngineState.FarawaySettings.InTransition)
		{
			CameraManager.PixelsPerTrixel = pixelsPerTrixel;
		}
	}

	public void SetCanRotate(bool canRotate)
	{
		PlayerManager.CanRotate = canRotate;
	}

	public void Rotate(int distance)
	{
		CameraManager.ChangeViewpoint(CameraManager.Viewpoint.GetRotatedView(distance));
	}

	public void RotateTo(string viewName)
	{
		Viewpoint viewpoint = (Viewpoint)Enum.Parse(typeof(Viewpoint), viewName, ignoreCase: true);
		if (viewpoint != CameraManager.Viewpoint)
		{
			CameraManager.ChangeViewpoint(viewpoint);
		}
	}

	public LongRunningAction FadeTo(string colorName)
	{
		Color toColor = Util.FromName(colorName);
		ScreenFade component = new ScreenFade(ServiceHelper.Game)
		{
			FromColor = new Color(new Vector4(toColor.ToVector3(), 0f)),
			ToColor = toColor,
			Duration = 2f
		};
		ServiceHelper.AddComponent(component);
		return new LongRunningAction((float elapsed, float since) => component.IsDisposed);
	}

	public LongRunningAction FadeFrom(string colorName)
	{
		Color fromColor = Util.FromName(colorName);
		ScreenFade component = new ScreenFade(ServiceHelper.Game)
		{
			FromColor = fromColor,
			ToColor = new Color(new Vector4(fromColor.ToVector3(), 0f)),
			Duration = 2f
		};
		ServiceHelper.AddComponent(component);
		return new LongRunningAction((float elapsed, float since) => component.IsDisposed);
	}

	public void Flash(string colorName)
	{
		Color fromColor = Util.FromName(colorName);
		ServiceHelper.AddComponent(new ScreenFade(ServiceHelper.Game)
		{
			FromColor = fromColor,
			ToColor = new Color(new Vector4(fromColor.ToVector3(), 0f)),
			Duration = 0.1f
		});
	}

	public void Shake(float distance, float durationSeconds)
	{
		ServiceHelper.AddComponent(new CamShake(ServiceHelper.Game)
		{
			Duration = TimeSpan.FromSeconds(durationSeconds),
			Distance = distance
		});
	}

	public void SetDescending(bool descending)
	{
		LevelManager.Descending = descending;
	}

	public void Unconstrain()
	{
		CameraManager.Constrained = false;
	}
}
