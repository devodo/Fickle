﻿using System;
using System.Linq.Expressions;

namespace Dryice.Expressions
{
	public class ReferencedTypeExpression
		: Expression
	{
		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)ServiceExpressionType.ReferencedType;
			}
		}

		public Type ReferencedType { get; private set; }

		public ReferencedTypeExpression(Type referencedType)
		{
			this.ReferencedType = referencedType;
		}
	}
}