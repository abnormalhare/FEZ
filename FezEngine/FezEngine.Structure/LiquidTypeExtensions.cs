namespace FezEngine.Structure;

public static class LiquidTypeExtensions
{
	public static bool IsWater(this LiquidType waterType)
	{
		if (waterType != LiquidType.Blood && waterType != LiquidType.Water && waterType != LiquidType.Purple)
		{
			return waterType == LiquidType.Green;
		}
		return true;
	}
}
