using System;
using System.Globalization;
using Common;
using ContentSerialization;
using FezEngine.Services.Scripting;

namespace FezEngine.Structure.Scripting;

public class ScriptCondition : ScriptPart, IDeserializationCallback
{
	private object[] processedArguments;

	private object processedValue;

	private float processedNumber;

	private Type valueType;

	private DynamicMethodDelegate GetValue;

	public string Property { get; set; }

	public ComparisonOperator Operator { get; set; }

	public string Value { get; set; }

	public ScriptCondition()
	{
		Operator = ComparisonOperator.None;
	}

	public override string ToString()
	{
		return ((base.Object == null) ? "(none)" : base.Object.ToString()) + "." + (Property ?? "(none)") + " " + Operator.ToSymbol() + " " + Value;
	}

	public void OnDeserialization()
	{
		Process();
	}

	public void Process()
	{
		EntityTypeDescriptor entityTypeDescriptor = EntityTypes.Types[base.Object.Type];
		PropertyDescriptor propertyDescriptor = entityTypeDescriptor.Properties[Property];
		processedValue = Convert.ChangeType(Value, propertyDescriptor.Type, CultureInfo.InvariantCulture);
		GetValue = propertyDescriptor.GetValue;
		valueType = propertyDescriptor.Type;
		if (Operator != 0 && Operator != ComparisonOperator.NotEqual)
		{
			if (valueType == typeof(int))
			{
				processedNumber = (int)processedValue;
			}
			else
			{
				processedNumber = (float)processedValue;
			}
		}
		processedArguments = (entityTypeDescriptor.Static ? new object[0] : new object[1] { base.Object.Identifier });
	}

	public bool Check(IScriptingBase service)
	{
		object obj = GetValue(service, processedArguments);
		switch (Operator)
		{
		case ComparisonOperator.Equal:
			return obj.Equals(processedValue);
		case ComparisonOperator.NotEqual:
			return !obj.Equals(processedValue);
		default:
		{
			float num = ((!(valueType == typeof(int))) ? ((float)obj) : ((float)(int)obj));
			return Operator switch
			{
				ComparisonOperator.LessEqual => num <= processedNumber, 
				ComparisonOperator.Less => num < processedNumber, 
				ComparisonOperator.GreaterEqual => num >= processedNumber, 
				ComparisonOperator.Greater => num > processedNumber, 
				_ => throw new InvalidOperationException(), 
			};
		}
		}
	}

	public ScriptCondition Clone()
	{
		return new ScriptCondition
		{
			Object = ((base.Object == null) ? null : base.Object.Clone()),
			Operator = Operator,
			Property = Property,
			Value = Value
		};
	}
}
