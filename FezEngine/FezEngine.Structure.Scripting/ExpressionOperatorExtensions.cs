using System;

namespace FezEngine.Structure.Scripting;

public static class ExpressionOperatorExtensions
{
	public static string ToSymbol(this ComparisonOperator op)
	{
		return op switch
		{
			ComparisonOperator.None => "", 
			ComparisonOperator.Equal => "=", 
			ComparisonOperator.GreaterEqual => ">=", 
			ComparisonOperator.Greater => ">", 
			ComparisonOperator.LessEqual => "<=", 
			ComparisonOperator.Less => "<", 
			ComparisonOperator.NotEqual => "!=", 
			_ => throw new InvalidOperationException(), 
		};
	}
}
