using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class TrackedSongReader : ContentTypeReader<TrackedSong>
{
	protected override TrackedSong Read(ContentReader input, TrackedSong existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new TrackedSong();
		}
		existingInstance.Loops = input.ReadObject(existingInstance.Loops);
		existingInstance.Name = input.ReadString();
		existingInstance.Tempo = input.ReadInt32();
		existingInstance.TimeSignature = input.ReadInt32();
		existingInstance.Notes = input.ReadObject<ShardNotes[]>();
		existingInstance.AssembleChord = input.ReadObject<AssembleChords>();
		existingInstance.RandomOrdering = input.ReadBoolean();
		existingInstance.CustomOrdering = input.ReadObject<int[]>();
		return existingInstance;
	}
}
