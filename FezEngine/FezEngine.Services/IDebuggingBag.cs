using System.Collections.Generic;

namespace FezEngine.Services;

public interface IDebuggingBag
{
	IEnumerable<string> Keys { get; }

	object this[string index] { get; }

	void Add(string name, object item);

	void Empty();

	float GetAge(string name);
}
