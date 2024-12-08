using System.Collections.Generic;

namespace FezEngine.Structure.Input;

public class CodeInputComparer : IEqualityComparer<CodeInput>
{
	public static readonly CodeInputComparer Default = new CodeInputComparer();

	public bool Equals(CodeInput x, CodeInput y)
	{
		return x == y;
	}

	public int GetHashCode(CodeInput obj)
	{
		return (int)obj;
	}
}
