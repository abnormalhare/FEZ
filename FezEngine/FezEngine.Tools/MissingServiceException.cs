using System;

namespace FezEngine.Tools;

public class MissingServiceException : Exception
{
	private const string messageFormat = "The service dependency for {0} in {1} could not be resolved.";

	public MissingServiceException(Type requiringType, Type requiredType)
		: base($"The service dependency for {requiredType} in {requiringType} could not be resolved.")
	{
	}
}
