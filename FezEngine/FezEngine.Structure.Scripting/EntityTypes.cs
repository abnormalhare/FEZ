using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common;
using FezEngine.Services.Scripting;

namespace FezEngine.Structure.Scripting;

public static class EntityTypes
{
	private const string Namespace = "FezEngine.Services.Scripting";

	public static IDictionary<string, EntityTypeDescriptor> Types { get; private set; }

	static EntityTypes()
	{
		Types = new Dictionary<string, EntityTypeDescriptor>();
		foreach (Type item in from x in Assembly.GetExecutingAssembly().GetTypes()
			where x.Namespace == "FezEngine.Services.Scripting" && x.IsInterface && x.Implements(typeof(IScriptingBase))
			select x)
		{
			string text = item.Name.Substring(1, item.Name.IndexOf("Service") - 1);
			EntityAttribute attribute = item.GetCustomAttributes(typeof(EntityAttribute), inherit: false).FirstOrDefault() as EntityAttribute;
			if (attribute == null)
			{
				Logger.Log("Engine.Scripting.EntityTypes", LogSeverity.Warning, "Entity type '" + text + "' did not contain any metadata, thus was not loaded.");
				continue;
			}
			Types.Add(text, new EntityTypeDescriptor(text, attribute.Static, attribute.Model, attribute.RestrictTo, item, from m in item.GetMethods()
				where !m.IsSpecialName && !m.Name.StartsWith("On") && !m.Name.StartsWith("get_")
				select new OperationDescriptor(m.Name, GetDescription(m), ReflectionHelper.CreateDelegate(m), from p in m.GetParameters().Skip((!attribute.Static) ? 1 : 0)
					select new ParameterDescriptor(p.Name, p.ParameterType)), (from p in item.GetProperties()
				select new PropertyDescriptor(p.Name, GetDescription(p), p.PropertyType, ReflectionHelper.CreateDelegate(p.GetGetMethod()))).Union(from m in item.GetMethods()
				where !m.IsSpecialName && m.Name.StartsWith("get_")
				select new PropertyDescriptor(m.Name.Substring(4), GetDescription(m), m.ReturnType, ReflectionHelper.CreateDelegate(m))), from e in item.GetEvents()
				select new EventDescriptor(e.Name, GetDescription(e), ReflectionHelper.CreateDelegate(e.GetAddMethod()), GetEndTrigger(e))));
		}
	}

	private static string GetDescription(MemberInfo info)
	{
		if (info.GetCustomAttributes(typeof(DescriptionAttribute), inherit: false).FirstOrDefault() is DescriptionAttribute descriptionAttribute)
		{
			return descriptionAttribute.Description;
		}
		return null;
	}

	private static DynamicMethodDelegate GetEndTrigger(EventInfo info)
	{
		if (!(info.GetCustomAttributes(typeof(EndTriggerAttribute), inherit: false).FirstOrDefault() is EndTriggerAttribute endTriggerAttribute))
		{
			return null;
		}
		return ReflectionHelper.CreateDelegate(info.DeclaringType.GetEvent(endTriggerAttribute.Trigger).GetAddMethod());
	}
}
