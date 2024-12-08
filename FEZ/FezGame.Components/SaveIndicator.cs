using FezEngine;
using FezEngine.Effects;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class SaveIndicator : DrawableGameComponent
{
	private const float FadeInTime = 0.1f;

	private const float FadeOutTime = 0.1f;

	private const float LongShowTime = 0.5f;

	private const float ShortShowTime = 0.5f;

	private Mesh mesh;

	private float sinceLastSaveStarted = 4f;

	private float planeOpacity;

	private bool wasSaving;

	private float sinceLoadingVisible;

	private float currentShowTime;

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public SaveIndicator(Game game)
		: base(game)
	{
		base.DrawOrder = 2101;
	}

	protected override void LoadContent()
	{
		mesh = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			AlwaysOnTop = true,
			DepthWrites = false
		};
		mesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, Color.Red, centeredOnOrigin: true);
		DrawActionScheduler.Schedule(delegate
		{
			mesh.Effect = new DefaultEffect.VertexColored
			{
				ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up)
			};
		});
	}

	public override void Update(GameTime gameTime)
	{
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		sinceLoadingVisible = (GameState.LoadingVisible ? FezMath.Saturate(sinceLoadingVisible + num * 2f) : FezMath.Saturate(sinceLoadingVisible - num * 3f));
		if (GameState.Saving || GameState.IsAchievementSave)
		{
			if (!wasSaving)
			{
				wasSaving = true;
				sinceLastSaveStarted = 0f;
				currentShowTime = (GameState.IsAchievementSave ? 0.5f : 0.5f);
				GameState.IsAchievementSave = false;
			}
		}
		else if (wasSaving)
		{
			wasSaving = false;
		}
		if (sinceLastSaveStarted < currentShowTime + 0.2f)
		{
			sinceLastSaveStarted += num;
		}
		planeOpacity = FezMath.Saturate(sinceLastSaveStarted / 0.1f) * FezMath.Saturate((currentShowTime - sinceLastSaveStarted + 0.2f) / 0.1f);
	}

	public override void Draw(GameTime gameTime)
	{
		if (planeOpacity != 0f && !Fez.LongScreenshot)
		{
			float aspectRatio = base.GraphicsDevice.Viewport.AspectRatio;
			mesh.Position = new Vector3(5.5f * aspectRatio, -7f + 1.4f * aspectRatio, 0f);
			mesh.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(14f * aspectRatio, 14f, 0.1f, 100f);
			mesh.Material.Opacity = planeOpacity;
			mesh.FirstGroup.Position = new Vector3(0f, 1.75f * Easing.EaseIn(sinceLoadingVisible, EasingType.Quadratic) * (float)((!GameState.DotLoading) ? 1 : 0), 0f);
			mesh.FirstGroup.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (0f - (float)gameTime.ElapsedGameTime.TotalSeconds) * 3f) * mesh.FirstGroup.Rotation;
			mesh.Draw();
		}
	}
}
