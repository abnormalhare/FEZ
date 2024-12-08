using System;

namespace FezGame.Structure;

public struct MtUpdateContext
{
	public int StartIndex;

	public int EndIndex;

	public TimeSpan Elapsed;
}
public struct MtUpdateContext<T>
{
	public int StartIndex;

	public int EndIndex;

	public TimeSpan Elapsed;

	public T Result;
}
