namespace FezEngine.Structure;

public static class NpcActionExtensions
{
	public static bool AllowsRandomChange(this NpcAction action)
	{
		switch (action)
		{
		case NpcAction.Idle:
		case NpcAction.Idle3:
		case NpcAction.Walk:
			return true;
		default:
			return false;
		}
	}

	public static bool Loops(this NpcAction action)
	{
		switch (action)
		{
		case NpcAction.Idle2:
		case NpcAction.Turn:
		case NpcAction.Burrow:
		case NpcAction.Hide:
		case NpcAction.ComeOut:
		case NpcAction.TakeOff:
		case NpcAction.Land:
			return false;
		default:
			return true;
		}
	}

	public static bool IsSpecialIdle(this NpcAction action)
	{
		if (action == NpcAction.Idle2 || action == NpcAction.Idle3)
		{
			return true;
		}
		return false;
	}
}
