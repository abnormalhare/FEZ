using System;

namespace FezEngine.Structure.Scripting;

public struct ParameterDescriptor
{
	public readonly string Name;

	public readonly Type Type;

	public ParameterDescriptor(string name, Type type)
	{
		Name = name;
		Type = type;
	}
}
