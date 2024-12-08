using Steamworks;

namespace FezGame.Services;

public class SteamUser
{
	public static readonly SteamUser Default = new SteamUser();

	public string PersonaName => SteamFriends.GetPersonaName();
}
