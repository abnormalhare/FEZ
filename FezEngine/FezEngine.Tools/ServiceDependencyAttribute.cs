using System;

namespace FezEngine.Tools;

public class ServiceDependencyAttribute : Attribute
{
	public bool Optional { get; set; }
}
