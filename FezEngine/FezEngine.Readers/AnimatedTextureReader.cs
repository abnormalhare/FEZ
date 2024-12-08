using System.Collections.Generic;
using System.Linq;
using FezEngine.Content;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Readers;

public class AnimatedTextureReader : ContentTypeReader<AnimatedTexture>
{
	protected override AnimatedTexture Read(ContentReader input, AnimatedTexture existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new AnimatedTexture();
		}
		GraphicsDevice graphicsDevice = ((IGraphicsDeviceService)input.ContentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;
		int width = input.ReadInt32();
		int height = input.ReadInt32();
		existingInstance.FrameWidth = input.ReadInt32();
		existingInstance.FrameHeight = input.ReadInt32();
		byte[] packedImageBytes = input.ReadBytes(input.ReadInt32());
		List<FrameContent> list = input.ReadObject<List<FrameContent>>();
		DrawActionScheduler.Schedule(delegate
		{
			existingInstance.Texture = new Texture2D(graphicsDevice, width, height, mipMap: false, SurfaceFormat.Color);
			existingInstance.Texture.SetData(packedImageBytes);
		});
		existingInstance.Offsets = list.Select((FrameContent x) => x.Rectangle).ToArray();
		existingInstance.Timing = new AnimationTiming(0, list.Count - 1, list.Select((FrameContent x) => (float)x.Duration.TotalSeconds).ToArray());
		existingInstance.PotOffset = new Vector2(FezMath.NextPowerOfTwo(existingInstance.FrameWidth) - existingInstance.FrameWidth, FezMath.NextPowerOfTwo(existingInstance.FrameHeight) - existingInstance.FrameHeight);
		return existingInstance;
	}
}
