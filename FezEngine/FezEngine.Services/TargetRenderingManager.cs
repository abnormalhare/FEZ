using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine.Effects;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Services;

public class TargetRenderingManager : GameComponent, ITargetRenderingManager
{
	private class RtHook
	{
		public int DrawOrder;

		public RenderTarget2D Target;
	}

	private readonly Mesh fullscreenPlane;

	private BasicPostEffect basicPostEffect;

	private readonly List<RenderTargetHandle> fullscreenRTs = new List<RenderTargetHandle>();

	private GraphicsDevice graphicsDevice;

	private Texture2D fullWhite;

	private readonly List<RtHook> renderTargetsToHook = new List<RtHook>();

	private int currentlyHookedRtIndex = -1;

	public bool HasRtInQueue => renderTargetsToHook.Count > 0;

	[ServiceDependency]
	public IGraphicsDeviceService GraphicsDeviceService { protected get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { protected get; set; }

	private event Action nextFrameHooks;

	public event Action<GameTime> PreDraw = Util.NullAction;

	public TargetRenderingManager(Game game)
		: base(game)
	{
		fullscreenPlane = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = true
		};
		fullscreenPlane.AddFace(Vector3.One * 2f, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
	}

	public void ScheduleHook(int drawOrder, RenderTarget2D rt)
	{
		if (renderTargetsToHook.Any((RtHook x) => x.Target == rt))
		{
			if (!renderTargetsToHook.Any((RtHook x) => x.Target == rt && x.DrawOrder == drawOrder))
			{
				throw new InvalidOperationException("Tried to hook already-hooked RT, but with different draw order");
			}
		}
		else if (currentlyHookedRtIndex != -1)
		{
			nextFrameHooks += delegate
			{
				renderTargetsToHook.Add(new RtHook
				{
					DrawOrder = drawOrder,
					Target = rt
				});
				renderTargetsToHook.Sort((RtHook a, RtHook b) => a.DrawOrder.CompareTo(b.DrawOrder));
			};
		}
		else
		{
			renderTargetsToHook.Add(new RtHook
			{
				DrawOrder = drawOrder,
				Target = rt
			});
			renderTargetsToHook.Sort((RtHook a, RtHook b) => a.DrawOrder.CompareTo(b.DrawOrder));
		}
	}

	public void UnscheduleHook(RenderTarget2D rt)
	{
		renderTargetsToHook.RemoveAll((RtHook x) => x.Target == rt);
	}

	public void Resolve(RenderTarget2D rt, bool reschedule)
	{
		if (currentlyHookedRtIndex == -1)
		{
			throw new InvalidOperationException("No render target hooked right now!");
		}
		if (renderTargetsToHook[currentlyHookedRtIndex].Target != rt)
		{
			throw new InvalidOperationException("Not the right render target hooked, can't resolve!");
		}
		if (!reschedule)
		{
			UnscheduleHook(rt);
		}
		else
		{
			currentlyHookedRtIndex++;
		}
		if (currentlyHookedRtIndex == renderTargetsToHook.Count)
		{
			graphicsDevice.SetRenderTarget(null);
			currentlyHookedRtIndex = -1;
		}
		else
		{
			graphicsDevice.SetRenderTarget(renderTargetsToHook[currentlyHookedRtIndex].Target);
		}
	}

	public bool IsHooked(RenderTarget2D rt)
	{
		if (currentlyHookedRtIndex == -1)
		{
			return false;
		}
		return renderTargetsToHook[currentlyHookedRtIndex].Target == rt;
	}

	public void OnPreDraw(GameTime gameTime)
	{
		this.PreDraw(gameTime);
	}

	public void OnRtPrepare()
	{
		if (this.nextFrameHooks != null)
		{
			this.nextFrameHooks();
			this.nextFrameHooks = null;
		}
		if (renderTargetsToHook.Count != 0)
		{
			RtHook rtHook = renderTargetsToHook[currentlyHookedRtIndex = 0];
			graphicsDevice.SetRenderTarget(rtHook.Target);
		}
	}

	public override void Initialize()
	{
		graphicsDevice = GraphicsDeviceService.GraphicsDevice;
		graphicsDevice.DeviceReset += delegate
		{
			RecreateTargets();
		};
		basicPostEffect = new BasicPostEffect();
		fullWhite = CMProvider.Global.Load<Texture2D>("Other Textures/FullWhite");
	}

	private void RecreateTargets()
	{
		List<Action<RenderTarget2D>> list = new List<Action<RenderTarget2D>>();
		foreach (RenderTargetHandle fullscreenRT in fullscreenRTs)
		{
			list.Clear();
			foreach (RtHook item in renderTargetsToHook)
			{
				if (fullscreenRT.Target == item.Target)
				{
					RtHook _ = item;
					list.Add(delegate(RenderTarget2D t)
					{
						_.Target = t;
					});
				}
			}
			fullscreenRT.Target.Dispose();
			fullscreenRT.Target = CreateFullscreenTarget();
			foreach (Action<RenderTarget2D> item2 in list)
			{
				item2(fullscreenRT.Target);
			}
		}
	}

	public RenderTargetHandle TakeTarget()
	{
		RenderTargetHandle handle = null;
		foreach (RenderTargetHandle fullscreenRT in fullscreenRTs)
		{
			if (!fullscreenRT.Locked)
			{
				handle = fullscreenRT;
				break;
			}
		}
		if (handle == null)
		{
			fullscreenRTs.Add(handle = new RenderTargetHandle());
			DrawActionScheduler.Schedule(delegate
			{
				handle.Target = CreateFullscreenTarget();
			});
		}
		handle.Locked = true;
		return handle;
	}

	private RenderTarget2D CreateFullscreenTarget()
	{
		base.Game.GraphicsDevice.SetupViewport();
		int width = graphicsDevice.Viewport.Width;
		int height = graphicsDevice.Viewport.Height;
		return new RenderTarget2D(graphicsDevice, width, height, mipMap: false, graphicsDevice.PresentationParameters.BackBufferFormat, graphicsDevice.PresentationParameters.DepthStencilFormat, graphicsDevice.PresentationParameters.MultiSampleCount, RenderTargetUsage.PlatformContents);
	}

	public void ReturnTarget(RenderTargetHandle handle)
	{
		if (handle != null)
		{
			if (IsHooked(handle.Target))
			{
				Resolve(handle.Target, reschedule: false);
			}
			else
			{
				UnscheduleHook(handle.Target);
			}
			handle.Locked = false;
		}
	}

	public void DrawFullscreen(Color color)
	{
		DrawFullscreen(basicPostEffect, fullWhite, null, color);
	}

	public void DrawFullscreen(Texture texture)
	{
		DrawFullscreen(basicPostEffect, texture, null, Color.White);
	}

	public void DrawFullscreen(Texture texture, Color color)
	{
		DrawFullscreen(basicPostEffect, texture, null, color);
	}

	public void DrawFullscreen(Texture texture, Matrix textureMatrix)
	{
		DrawFullscreen(basicPostEffect, texture, textureMatrix, Color.White);
	}

	public void DrawFullscreen(Texture texture, Matrix textureMatrix, Color color)
	{
		DrawFullscreen(basicPostEffect, texture, textureMatrix, color);
	}

	public void DrawFullscreen(BaseEffect effect)
	{
		DrawFullscreen(effect, null, null, Color.White);
	}

	public void DrawFullscreen(BaseEffect effect, Color color)
	{
		DrawFullscreen(effect, fullWhite, null, color);
	}

	public void DrawFullscreen(BaseEffect effect, Texture texture)
	{
		DrawFullscreen(effect, texture, null, Color.White);
	}

	public void DrawFullscreen(BaseEffect effect, Texture texture, Matrix? textureMatrix)
	{
		DrawFullscreen(effect, texture, textureMatrix, Color.White);
	}

	public void DrawFullscreen(BaseEffect effect, Texture texture, Matrix? textureMatrix, Color color)
	{
		bool ignoreCache = effect.IgnoreCache;
		effect.IgnoreCache = true;
		if (texture != null)
		{
			fullscreenPlane.Texture.Set(texture);
		}
		if (textureMatrix.HasValue)
		{
			fullscreenPlane.TextureMatrix.Set(textureMatrix.Value);
		}
		if (color != Color.White)
		{
			fullscreenPlane.Material.Diffuse = color.ToVector3();
			fullscreenPlane.Material.Opacity = (float)(int)color.A / 255f;
		}
		fullscreenPlane.Effect = effect;
		fullscreenPlane.Draw();
		if (color != Color.White)
		{
			fullscreenPlane.Material.Diffuse = Vector3.One;
			fullscreenPlane.Material.Opacity = 1f;
		}
		if (texture != null)
		{
			fullscreenPlane.Texture.Set(null);
		}
		if (textureMatrix.HasValue)
		{
			fullscreenPlane.TextureMatrix.Set(Matrix.Identity);
		}
		effect.IgnoreCache = ignoreCache;
	}
}
