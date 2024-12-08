using System.Collections.Generic;

namespace FezEngine.Structure;

internal interface ISpatialStructure<T>
{
	bool Empty { get; }

	IEnumerable<T> Cells { get; }

	void Clear();

	void Free(IEnumerable<T> cells);

	void Free(T cell);

	void Fill(IEnumerable<T> cells);

	void Fill(T cell);

	bool IsFilled(T cell);
}
