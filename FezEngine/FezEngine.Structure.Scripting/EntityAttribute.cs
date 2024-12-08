using System;

namespace FezEngine.Structure.Scripting;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class EntityAttribute : Attribute
{
	public Type Model { get; set; }

	public ActorType[] RestrictTo { get; set; }

	public bool Static { get; set; }
}
