﻿using System;
using System.Collections.Generic;
using System.Linq;
using Platform;

namespace Fickle
{
	public class TypeSystem
	{
		private static readonly HashSet<Type> primitiveTypes = new HashSet<Type>();
		private static readonly Dictionary<string, Type> primitiveTypeByName = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

		static void AddPrimitiveType(Type type, string name)
		{
			primitiveTypes.Add(type);
			primitiveTypeByName[name] = type;

			if (type.IsValueType && type != typeof(void))
			{
				type = typeof(Nullable<>).MakeGenericType(type);

				primitiveTypes.Add(type);
				primitiveTypeByName[name + "?"] = type;
			}
		}

		static TypeSystem()
		{
			AddPrimitiveType(typeof(void), "Void");
			AddPrimitiveType(typeof(bool), "Bool");
			AddPrimitiveType(typeof(bool), "Boolean");
			AddPrimitiveType(typeof(byte), "Byte");
			AddPrimitiveType(typeof(char), "Char");
			AddPrimitiveType(typeof(short), "Short");
			AddPrimitiveType(typeof(ushort), "UShort");
			AddPrimitiveType(typeof(int), "Int");
			AddPrimitiveType(typeof(int), "Integer");
			AddPrimitiveType(typeof(uint), "UInt");
			AddPrimitiveType(typeof(long), "Long");
			AddPrimitiveType(typeof(ulong), "ULong");
			AddPrimitiveType(typeof(float), "Float");
			AddPrimitiveType(typeof(double), "Double");
			AddPrimitiveType(typeof(string), "String");
			AddPrimitiveType(typeof(DateTime), "DateTime");
			AddPrimitiveType(typeof(TimeSpan), "TimeSpan");
			AddPrimitiveType(typeof(Guid), "UUID");
			AddPrimitiveType(typeof(decimal), "Decimal");
		}

		public static string GetPrimitiveName(Type type, bool naked = false)
		{
			if (type.GetUnwrappedNullableType().IsEnum)
			{
				if (type.GetUnderlyingType() == null || naked)
				{
					return type.GetUnwrappedNullableType().Name;
				}
				else
				{
					return type.GetUnwrappedNullableType().Name + "?";
				}
			}

			if (naked)
			{
				return primitiveTypeByName.FirstOrDefault(c => c.Value == type.GetUnwrappedNullableType()).Key;
			}
			else
			{
				return primitiveTypeByName.FirstOrDefault(c => c.Value == type).Key;
			}
		}

		public static bool IsPrimitiveType(Type type)
		{
			return primitiveTypes.Contains(type)
			       || type.GetUnwrappedNullableType().IsEnum
			       || ((type is FickleType) && ((FickleType)type).IsPrimitive);
		}

		public static bool IsNotPrimitiveType(Type type)
		{
			return !IsPrimitiveType(type);
		}

		public static Type GetPrimitiveType(string name)
		{
			Type type;

			primitiveTypeByName.TryGetValue(name, out type);

			return type;
		}
	}
}
