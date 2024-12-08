using System.Collections.Generic;
using System.Linq;
using Common;

namespace FezEngine.Structure.Scripting;

public struct OperationDescriptor
{
	public readonly string Name;

	public readonly string Description;

	public readonly ParameterDescriptor[] Parameters;

	public readonly DynamicMethodDelegate Call;

	public OperationDescriptor(string name, string description, DynamicMethodDelegate @delegate, IEnumerable<ParameterDescriptor> parameters)
	{
		Name = name;
		Description = description;
		Parameters = parameters.ToArray();
		Call = @delegate;
	}
}
