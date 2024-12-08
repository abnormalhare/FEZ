using System;

namespace FezEngine.Tools;

public class MissingSetterException : Exception
{
	private const string messageFormat = "The service dependency for {0} in {1} could not be injected because a setter could not be found.";

	public MissingSetterException(Type requiringType, Type requiredType)
		: base($"The service dependency for {requiredType} in {requiringType} could not be injected because a setter could not be found.")
	{
	}
}
