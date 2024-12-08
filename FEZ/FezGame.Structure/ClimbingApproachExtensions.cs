using FezEngine;

namespace FezGame.Structure;

public static class ClimbingApproachExtensions
{
	public static HorizontalDirection AsDirection(this ClimbingApproach approach)
	{
		if (approach == ClimbingApproach.Left)
		{
			return HorizontalDirection.Left;
		}
		_ = 1;
		return HorizontalDirection.Right;
	}
}
