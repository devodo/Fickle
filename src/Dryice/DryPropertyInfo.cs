﻿using System;
using System.Globalization;
using System.Reflection;

namespace Dryice
{
	public class DryPropertyInfo
		: PropertyInfo
	{
		private string name; 
		private Type declaringType;
		private Type propertyType;
		
		public DryPropertyInfo(Type declaringType, Type propertyType, string name)
		{
			this.name = name;
			this.propertyType = propertyType;
			this.declaringType = declaringType;
		}

		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotImplementedException();
		}

		public override bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotImplementedException();
		}

		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			throw new NotImplementedException();
		}

		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			return new DryMethodInfo(this.declaringType, this.propertyType, "get_" + this.name, new ParameterInfo[0]);
		}

		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			throw new NotImplementedException();
		}

		public override ParameterInfo[] GetIndexParameters()
		{
			throw new NotImplementedException();
		}

		public override string Name
		{
			get
			{
				return name;
			}
		}

		public override Type DeclaringType
		{
			get
			{
				return declaringType;
			}
		}

		public override Type ReflectedType
		{
			get
			{
				return declaringType;
			}
		}

		public override Type PropertyType
		{
			get
			{
				return propertyType;
			}
		}

		public override PropertyAttributes Attributes
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotImplementedException();
		}
	}
}