﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fickle.Model;

namespace Fickle.Generators
{
	public abstract class ServiceModelResponseAmender
	{
		protected readonly ServiceModel serviceModel;
		protected readonly CodeGenerationOptions options;
		
		protected abstract ServiceClass CreateValueResponseServiceClass(Type type);

		protected ServiceModelResponseAmender(ServiceModel serviceModel, CodeGenerationOptions options)
		{
			this.serviceModel = serviceModel;
			this.options = options;
		}

		private ServiceClass AmmendOrCreateResponseStatusClass()
		{
			ServiceClass retval;

			try
			{
				retval = this.serviceModel.GetServiceClass(options.ResponseStatusTypeName);
			}
			catch
			{
				retval = null;	
			}

			var properties = new List<ServiceProperty>
			{
				new ServiceProperty
				{
					Name = "Message",
					TypeName = "string"
				},
				new ServiceProperty
				{
					Name = "ErrorCode",
					TypeName = "string"
				},
				new ServiceProperty
				{
					Name = "HttpStatus",
					TypeName = "int"
				}
			};

			if (retval == null)
			{	
				retval = new ServiceClass(this.options.ResponseStatusTypeName,  null, properties);
			}
			else
			{
				foreach (var property in properties.Where(property => 
					!this.serviceModel
					.GetServiceClassHiearchy(retval)
					.SelectMany(c => c.Properties)
					.Any(c => String.Equals(c.Name, property.Name, StringComparison.InvariantCultureIgnoreCase))))
				{
					retval.Properties.Add(property);
				}
			}

			return retval;
		}

		public virtual ServiceModel Ammend()
		{
			bool containsResponseStatus;
			var returnTypes = serviceModel.Gateways.SelectMany(c => c.Methods).Select(c => serviceModel.GetTypeFromName(c.Returns)).ToHashSet();
			var returnServiceClasses = returnTypes.Where(c => TypeSystem.IsNotPrimitiveType(c) && !(c is FickleListType)).Select(serviceModel.GetServiceClass);
			var additionalClasses = new HashSet<ServiceClass>();

			try
			{
				serviceModel.GetServiceClass(options.ResponseStatusTypeName);

				containsResponseStatus = true;
            }
			catch
			{
				containsResponseStatus = false;
			}

			var responseStatusClass = this.AmmendOrCreateResponseStatusClass();

			if (!containsResponseStatus)
			{
				additionalClasses.Add(responseStatusClass);
			} 
			
			foreach (var valueResponse in returnTypes
				.Where(c => TypeSystem.IsPrimitiveType(c) || c is FickleListType)
				.Distinct()
				.Select(this.CreateValueResponseServiceClass))
			{
				additionalClasses.Add(valueResponse);
			}

			foreach (var returnTypeClass in returnServiceClasses.OrderBy(c => serviceModel.GetDepth(c)))
			{
				if (!serviceModel.GetServiceClassHiearchy(returnTypeClass).SelectMany(c => c.Properties).Any(c => string.Equals(c.Name, options.ResponseStatusPropertyName, StringComparison.CurrentCultureIgnoreCase)))
				{
					returnTypeClass.Properties.Add(new ServiceProperty
					{
						Name = options.ResponseStatusPropertyName,
						TypeName = options.ResponseStatusTypeName
					});
				}
			}

			return new ServiceModel(this.serviceModel.ServiceModelInfo, serviceModel.Enums, serviceModel.Classes.Concat(additionalClasses), serviceModel.Gateways);
		}
	}
}
