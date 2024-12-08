using System;
using FezEngine.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class FrameReader : ContentTypeReader<FrameContent>
{
	protected override FrameContent Read(ContentReader input, FrameContent existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new FrameContent();
		}
		existingInstance.Duration = input.ReadObject<TimeSpan>();
		existingInstance.Rectangle = input.ReadObject<Rectangle>();
		return existingInstance;
	}
}
