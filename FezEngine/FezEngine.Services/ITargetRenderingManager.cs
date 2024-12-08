using System;
using FezEngine.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Services;

public interface ITargetRenderingManager
{
	bool HasRtInQueue { get; }

	event Action<GameTime> PreDraw;

	void OnPreDraw(GameTime gameTime);

	void OnRtPrepare();

	void ScheduleHook(int drawOrder, RenderTarget2D rt);

	void UnscheduleHook(RenderTarget2D rt);

	void Resolve(RenderTarget2D rt, bool reschedule);

	bool IsHooked(RenderTarget2D rt);

	RenderTargetHandle TakeTarget();

	void ReturnTarget(RenderTargetHandle handle);

	void DrawFullscreen(Color color);

	void DrawFullscreen(Texture texture);

	void DrawFullscreen(Texture texture, Color color);

	void DrawFullscreen(Texture texture, Matrix textureMatrix);

	void DrawFullscreen(Texture texture, Matrix textureMatrix, Color color);

	void DrawFullscreen(BaseEffect effect);

	void DrawFullscreen(BaseEffect effect, Color color);

	void DrawFullscreen(BaseEffect effect, Texture texture);

	void DrawFullscreen(BaseEffect effect, Texture texture, Matrix? textureMatrix);

	void DrawFullscreen(BaseEffect effect, Texture texture, Matrix? textureMatrix, Color color);
}
