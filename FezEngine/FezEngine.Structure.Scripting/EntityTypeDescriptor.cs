using System;
using System.Collections.Generic;

namespace FezEngine.Structure.Scripting;

public struct EntityTypeDescriptor
{
	public readonly string Name;

	public readonly bool Static;

	public readonly Type Model;

	public readonly ActorType[] RestrictTo;

	public readonly Type Interface;

	public readonly IDictionary<string, OperationDescriptor> Operations;

	public readonly IDictionary<string, PropertyDescriptor> Properties;

	public readonly IDictionary<string, EventDescriptor> Events;

	public EntityTypeDescriptor(string name, bool isStatic, Type modelType, ActorType[] restrictTo, Type interfaceType, IEnumerable<OperationDescriptor> operations, IEnumerable<PropertyDescriptor> properties, IEnumerable<EventDescriptor> events)
	{
		Name = name;
		Static = isStatic;
		Model = modelType;
		RestrictTo = restrictTo;
		Interface = interfaceType;
		Operations = new Dictionary<string, OperationDescriptor>();
		foreach (OperationDescriptor operation in operations)
		{
			Operations.Add(operation.Name, operation);
		}
		Properties = new Dictionary<string, PropertyDescriptor>();
		foreach (PropertyDescriptor property in properties)
		{
			Properties.Add(property.Name, property);
		}
		Events = new Dictionary<string, EventDescriptor>();
		foreach (EventDescriptor @event in events)
		{
			Events.Add(@event.Name, @event);
		}
	}
}
