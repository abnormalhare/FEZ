using Microsoft.Xna.Framework;

namespace FezEngine.Structure.Input;

public static class ControllerIndexExtensions
{
	private static readonly PlayerIndex[] None = new PlayerIndex[0];

	private static readonly PlayerIndex[] One = new PlayerIndex[1];

	private static readonly PlayerIndex[] Two = new PlayerIndex[1] { PlayerIndex.Two };

	private static readonly PlayerIndex[] Three = new PlayerIndex[1] { PlayerIndex.Three };

	private static readonly PlayerIndex[] Four = new PlayerIndex[1] { PlayerIndex.Four };

	private static readonly PlayerIndex[] Any = new PlayerIndex[4]
	{
		PlayerIndex.One,
		PlayerIndex.Two,
		PlayerIndex.Three,
		PlayerIndex.Four
	};

	public static PlayerIndex GetPlayer(this ControllerIndex index)
	{
		return index switch
		{
			ControllerIndex.One => PlayerIndex.One, 
			ControllerIndex.Two => PlayerIndex.Two, 
			ControllerIndex.Three => PlayerIndex.Three, 
			ControllerIndex.Four => PlayerIndex.Four, 
			_ => PlayerIndex.One, 
		};
	}

	public static PlayerIndex[] GetPlayers(this ControllerIndex index)
	{
		return index switch
		{
			ControllerIndex.None => None, 
			ControllerIndex.One => One, 
			ControllerIndex.Two => Two, 
			ControllerIndex.Three => Three, 
			ControllerIndex.Four => Four, 
			ControllerIndex.Any => Any, 
			_ => None, 
		};
	}

	public static ControllerIndex ToControllerIndex(this PlayerIndex index)
	{
		return index switch
		{
			PlayerIndex.One => ControllerIndex.One, 
			PlayerIndex.Two => ControllerIndex.Two, 
			PlayerIndex.Three => ControllerIndex.Three, 
			PlayerIndex.Four => ControllerIndex.Four, 
			_ => ControllerIndex.None, 
		};
	}
}
