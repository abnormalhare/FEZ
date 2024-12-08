using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class TrileSetReader : ContentTypeReader<TrileSet>
{
	protected override TrileSet Read(ContentReader input, TrileSet existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new TrileSet();
		}
		existingInstance.Name = input.ReadString();
		existingInstance.Triles = input.ReadObject(existingInstance.Triles);
		FutureTexture2D futureAtlas = input.ReadObject<FutureTexture2D>(FutureTexture2DReader.Instance);
		DrawActionScheduler.Schedule(delegate
		{
			existingInstance.TextureAtlas = futureAtlas.Create();
		});
		existingInstance.OnDeserialization();
		return existingInstance;
	}
}
