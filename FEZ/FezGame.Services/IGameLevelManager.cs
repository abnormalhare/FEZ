using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Structure;

namespace FezGame.Services;

public interface IGameLevelManager : ILevelManager
{
	IDictionary<TrileInstance, TrileGroup> PickupGroups { get; }

	string LastLevelName { get; set; }

	int? DestinationVolumeId { get; set; }

	bool DestinationIsFarAway { get; set; }

	bool WentThroughSecretPassage { get; set; }

	bool SongChanged { get; set; }

	void RemoveArtObject(ArtObjectInstance aoInstance);

	void ChangeLevel(string levelName);

	void ChangeSky(Sky sky);

	void Reset();
}
