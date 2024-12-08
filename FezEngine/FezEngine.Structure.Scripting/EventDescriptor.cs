using Common;

namespace FezEngine.Structure.Scripting;

public struct EventDescriptor
{
	public readonly string Name;

	public readonly string Description;

	public readonly DynamicMethodDelegate AddHandler;

	public readonly DynamicMethodDelegate AddEndTriggerHandler;

	public EventDescriptor(string name, string description, DynamicMethodDelegate @delegate, DynamicMethodDelegate endTrigger)
	{
		Name = name;
		Description = description;
		AddHandler = @delegate;
		AddEndTriggerHandler = endTrigger;
	}
}
