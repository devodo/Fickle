﻿//
// Copyright (c) 2013-2014 Thong Nguyen (tumtumtum@gmail.com)
//


using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Dryice.Expressions;
using Dryice.Model;
using Platform.VirtualFileSystem;

namespace Dryice
{
	public class ServiceExpressionBuilder
	{
		public ServiceModel ServiceModel { get; private set; }
		public CodeGenerationOptions Options { get; private set; }

		public ServiceExpressionBuilder(ServiceModel serviceModel, CodeGenerationOptions options)
		{
			this.ServiceModel = serviceModel;
			this.Options = options;
		}

		public virtual Type GetTypeFromName(string name)
		{
			return this.ServiceModel.GetTypeFromName(name);
		}

		public virtual Expression Build(ServiceProperty property)
		{
			return new PropertyDefinitionExpression(property.Name, this.GetTypeFromName(property.TypeName));
		}

		public virtual Expression Build(ServiceClass serviceClass)
		{
			Type baseType = null;
			var propertyDefinitions = serviceClass.Properties.Select(Build).ToList();

			if (!string.IsNullOrEmpty(this.Options.BaseTypeTypeName))
			{
				baseType = new DryType(this.Options.BaseTypeTypeName);
			}

			if (baseType == null && !string.IsNullOrEmpty(serviceClass.BaseTypeName))
			{
				baseType = new DryType(serviceClass.BaseTypeName);
			}

			if (baseType == null)
			{
				baseType = typeof(object);
			}

			return new TypeDefinitionExpression(this.GetTypeFromName(serviceClass.Name), baseType, null, propertyDefinitions.ToGroupedExpression());
		}

		public virtual Expression Build(ServiceParameter parameter, int index)
		{
			return new ParameterDefinitionExpression(parameter.Name, this.GetTypeFromName(parameter.TypeName), index);
		}

		public virtual Expression Build(ServiceMethod method)
		{
			var i = 0;
			var parameterExpressions = new ReadOnlyCollection<Expression>(method.Parameters.Select(c => Build(c, i++)).ToList());

			return new ServiceMethodDefinitionExpression(method.Name, parameterExpressions, this.ServiceModel.GetServiceType(method.Returns), null, true, null, method);
		}

		public virtual Expression Build(ServiceGateway serviceGateway)
		{
			Type baseType = null;
			
			if (!string.IsNullOrEmpty(this.Options.BaseTypeTypeName))
			{
				baseType = new DryType(this.Options.BaseTypeTypeName);
			}

			if (baseType == null && !string.IsNullOrEmpty(serviceGateway.BaseTypeName))
			{
				baseType = new DryType(serviceGateway.BaseTypeName);
			}

			if (baseType == null)
			{
				baseType = typeof(object);
			}

			var methodDefinitions = serviceGateway.Methods.Select(Build).ToList();

			return new TypeDefinitionExpression(new DryType(serviceGateway.Name), baseType, null, methodDefinitions.ToGroupedExpression(GroupedExpressionsExpressionStyle.Wide), false, null);
		}
	}
}