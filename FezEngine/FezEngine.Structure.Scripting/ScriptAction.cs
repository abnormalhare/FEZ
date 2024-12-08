using System;
using System.Globalization;
using System.Linq;
using Common;
using ContentSerialization;
using ContentSerialization.Attributes;

namespace FezEngine.Structure.Scripting;

public class ScriptAction : ScriptPart, IDeserializationCallback
{
	public string Operation { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Killswitch { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Blocking { get; set; }

	[Serialization(Optional = true)]
	public string[] Arguments { get; set; }

	public object[] ProcessedArguments { get; private set; }

	public DynamicMethodDelegate Invoke { get; private set; }

	public override string ToString()
	{
		if (Arguments == null)
		{
			return ((base.Object == null) ? "(none)" : base.Object.ToString()) + "." + (Operation ?? "(none)") + "()";
		}
		return ((base.Object == null) ? "(none)" : base.Object.ToString()) + "." + (Operation ?? "(none)") + "(" + Util.DeepToString(Arguments, omitBrackets: true) + ")";
	}

	public void OnDeserialization()
	{
		Process();
	}

	public void Process()
	{
		EntityTypeDescriptor entityTypeDescriptor = EntityTypes.Types[base.Object.Type];
		OperationDescriptor operationDescriptor = entityTypeDescriptor.Operations[Operation];
		int num = ((!entityTypeDescriptor.Static) ? 1 : 0);
		int num2 = num + operationDescriptor.Parameters.Length;
		ProcessedArguments = new object[num2];
		if (!entityTypeDescriptor.Static)
		{
			ProcessedArguments[0] = base.Object.Identifier.Value;
		}
		for (int i = 0; i < operationDescriptor.Parameters.Length; i++)
		{
			ParameterDescriptor parameterDescriptor = operationDescriptor.Parameters[i];
			if (Arguments.Length <= i)
			{
				if (parameterDescriptor.Type == typeof(string))
				{
					ProcessedArguments[num + i] = string.Empty;
				}
				else
				{
					ProcessedArguments[num + i] = Activator.CreateInstance(parameterDescriptor.Type);
				}
			}
			else
			{
				ProcessedArguments[num + i] = Convert.ChangeType(Arguments[i], parameterDescriptor.Type, CultureInfo.InvariantCulture);
			}
		}
		Invoke = operationDescriptor.Call;
	}

	public ScriptAction Clone()
	{
		return new ScriptAction
		{
			Operation = Operation,
			Killswitch = Killswitch,
			Blocking = Blocking,
			Arguments = ((Arguments == null) ? null : Arguments.ToArray()),
			Object = ((base.Object == null) ? null : base.Object.Clone())
		};
	}
}
