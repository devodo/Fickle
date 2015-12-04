﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace Fickle.Model
{
	public class ServiceModel
	{
		public ServiceModelInfo ServiceModelInfo { get; private set; }
		public ReadOnlyCollection<ServiceEnum> Enums { get; private set; }
		public ReadOnlyCollection<ServiceClass> Classes { get; private set; }
		public ReadOnlyCollection<ServiceGateway> Gateways { get; private set; }

		private Dictionary<string, Type> serviceTypesByName;

		public ServiceModel(ServiceModelInfo serviceModelInfo, IEnumerable<ServiceEnum> enums, IEnumerable<ServiceClass> classes, IEnumerable<ServiceGateway> gateways)
		{
			this.ServiceModelInfo = serviceModelInfo ?? new ServiceModelInfo();
			this.Enums = enums.ToReadOnlyCollection();
			this.Classes = classes.ToReadOnlyCollection();
			this.Gateways = gateways.ToReadOnlyCollection();
		}

		public int GetDepth(ServiceClass serviceClass)
		{
			if (serviceClass.BaseTypeName == null)
			{
				return 1;
			}

			return 1 + this.GetDepth(this.GetServiceClass(serviceClass.BaseTypeName));
		}

		public IEnumerable<ServiceClass> GetServiceClassHiearchy(ServiceClass serviceClass)
		{
			yield return serviceClass;

			if (!string.IsNullOrEmpty(serviceClass.BaseTypeName))
			{
				serviceClass = this.GetServiceClass(serviceClass.BaseTypeName);

				foreach (var value in this.GetServiceClassHiearchy(serviceClass))
				{
					yield return value;
				}
			}
		}

		public virtual Type GetTypeFromName(string name)
		{
			var list = false;

			if (name.EndsWith("[]"))
			{
				list = true;
				name = name.Substring(0, name.Length - 2);
			}

			var type = TypeSystem.GetPrimitiveType(name);

			if (type == null)
			{
				type = this.GetServiceType(name);
			}

			if (list)
			{
				return MakeListType(type);
			}
			else
			{
				return type;
			}
		}

		private readonly Dictionary<Type, Type> listTypesByElementType = new Dictionary<Type, Type>();

		private Type MakeListType(Type elementType)
		{
			Type value;

			if (!listTypesByElementType.TryGetValue(elementType, out value))
			{
				value = new FickleListType(elementType);
			}

			return value;
		}

		public virtual ServiceClass GetServiceClass(string name)
		{
			return this.GetServiceClass(this.GetServiceType(name));
		}

		public virtual ServiceClass GetServiceClass(Type type)
		{
			var fickleType = type as FickleType;

			if (fickleType == null)
			{
				return null;
			}

			return fickleType.ServiceClass;
		}

		private void CreateIndex()
		{
			this.serviceTypesByName = this.Classes.Select(c => (object)c).Concat(this.Enums ?? Enumerable.Empty<object>()).Select(c => c is ServiceEnum ? (Type)new FickleType((ServiceEnum)c, this) : (Type)new FickleType((ServiceClass)c, this)).Distinct().ToDictionary(c => c.Name, c => c, StringComparer.InvariantCultureIgnoreCase);

			foreach (FickleType type in this.serviceTypesByName.Values)
			{
				if (type.ServiceClass != null && !string.IsNullOrEmpty(type.ServiceClass.BaseTypeName))
				{
					Type baseType;

					if (this.serviceTypesByName.TryGetValue(type.ServiceClass.BaseTypeName, out baseType))
					{
						type.SetBaseType(baseType);
					}
					else
					{
						type.SetBaseType(new FickleType(type.ServiceClass.BaseTypeName));
					}
				}
				else if (type.ServiceEnum != null)
				{
					type.SetBaseType(typeof(Enum));
				}
				else
				{
					type.SetBaseType(typeof(object));
				}
			}

			var enums = this.serviceTypesByName.Where(c => c.Value.BaseType == typeof(Enum)).ToList();

			foreach (var kv in enums)
			{
				if (kv.Value.BaseType == typeof(Enum))
				{
					this.serviceTypesByName[kv.Key + "?"] = new FickleNullable(kv.Value);
				}
			}
		}

		public virtual Type GetServiceType(string name)
		{
			if (this.serviceTypesByName == null)
			{
				this.CreateIndex();
			}

			Type retval;

			if (this.serviceTypesByName.TryGetValue(name, out retval))
			{
				return retval;
			}

			throw new InvalidOperationException("Can't find service type: " + name);
		}
	}
}
