using System.Collections.Generic;

namespace FezEngine.Structure.Input;

public static class PatternTester
{
	public static bool Test(IList<CodeInput> input, CodeInput[] pattern)
	{
		int count = input.Count;
		bool result = false;
		for (int i = 0; i < pattern.Length && i < count && input[count - i - 1] == pattern[pattern.Length - i - 1]; i++)
		{
			if (i == pattern.Length - 1)
			{
				result = true;
				input.Clear();
				break;
			}
		}
		return result;
	}

	public static bool Test(IList<VibrationMotor> input, VibrationMotor[] pattern)
	{
		int count = input.Count;
		bool result = false;
		int num = 0;
		for (int i = 0; i + num < pattern.Length && i < count; i++)
		{
			while (pattern[pattern.Length - i - 1 - num] == VibrationMotor.None)
			{
				num++;
				if (i + num >= pattern.Length)
				{
					break;
				}
			}
			if (input[count - i - 1] != pattern[pattern.Length - i - 1 - num])
			{
				break;
			}
			if (i == pattern.Length - 1 - num)
			{
				result = true;
				input.Clear();
				break;
			}
		}
		return result;
	}
}
