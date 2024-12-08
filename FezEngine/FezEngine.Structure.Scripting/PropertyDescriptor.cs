using System;
using Common;

namespace FezEngine.Structure.Scripting;

public struct PropertyDescriptor
{
	public readonly string Name;

	public readonly string Description;

	public readonly Type Type;

	public readonly DynamicMethodDelegate GetValue;

	public PropertyDescriptor(string name, string description, Type type, DynamicMethodDelegate @delegate)
	{
		Name = name;
		Description = description;
		Type = type;
		GetValue = @delegate;
	}
}
