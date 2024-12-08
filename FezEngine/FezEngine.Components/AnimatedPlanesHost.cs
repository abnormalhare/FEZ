using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

public class AnimatedPlanesHost : GameComponent
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	public AnimatedPlanesHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		LevelManager.LevelChanged += delegate
		{
			foreach (BackgroundPlane value in LevelManager.BackgroundPlanes.Values)
			{
				value.OriginalPosition = value.Position;
			}
		};
	}

	public override void Update(GameTime gameTime)
	{
		if (LevelMaterializer.LevelPlanes.Count == 0 || EngineState.Paused || EngineState.InMap || EngineState.Loading)
		{
			return;
		}
		bool flag = CameraManager.Viewpoint.IsOrthographic() && CameraManager.ActionRunning;
		bool inEditor = EngineState.InEditor;
		foreach (BackgroundPlane levelPlane in LevelMaterializer.LevelPlanes)
		{
			if (!levelPlane.Visible && levelPlane.ActorType != ActorType.Bomb)
			{
				continue;
			}
			if (flag && levelPlane.Animated)
			{
				int frame = levelPlane.Timing.Frame;
				levelPlane.Timing.Update(gameTime.ElapsedGameTime);
				if (!levelPlane.Loop && frame > levelPlane.Timing.Frame)
				{
					LevelManager.RemovePlane(levelPlane);
				}
				else
				{
					levelPlane.MarkDirty();
				}
			}
			if (levelPlane.Billboard)
			{
				levelPlane.Rotation = CameraManager.Rotation * levelPlane.OriginalRotation;
			}
			if (!inEditor && levelPlane.ParallaxFactor != 0f && flag)
			{
				Viewpoint view = levelPlane.Orientation.AsViewpoint();
				if (!levelPlane.OriginalPosition.HasValue)
				{
					levelPlane.OriginalPosition = levelPlane.Position;
				}
				float num = (float)(-4 * ((!LevelManager.Descending) ? 1 : (-1))) / CameraManager.PixelsPerTrixel - 15f / 32f + 1f;
				Vector3 vector = CameraManager.InterpolatedCenter - levelPlane.OriginalPosition.Value + num * Vector3.UnitY;
				levelPlane.Position = levelPlane.OriginalPosition.Value + vector * view.ScreenSpaceMask() * levelPlane.ParallaxFactor;
			}
			else if (!inEditor && levelPlane.ParallaxFactor != 0f && levelPlane.OriginalPosition.HasValue && levelPlane.Position != levelPlane.OriginalPosition.Value)
			{
				levelPlane.Position = levelPlane.OriginalPosition.Value;
			}
		}
	}
}
