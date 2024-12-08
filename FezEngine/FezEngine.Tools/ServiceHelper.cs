using System;
using System.Collections.Generic;
using System.Reflection;
using Common;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public static class ServiceHelper
{
	private static readonly List<object> services = new List<object>();

	private static readonly object Mutex = new object();

	public static bool IsFull;

	public static bool FirstLoadDone;

	public static Game Game { get; set; }

	public static void Clear()
	{
		foreach (object service in services)
		{
			Type[] interfaces = service.GetType().GetInterfaces();
			foreach (Type type in interfaces)
			{
				Game.Services.RemoveService(type);
			}
			if (service is IDisposable)
			{
				(service as IDisposable).Dispose();
			}
		}
		services.Clear();
	}

	public static void AddComponent(IGameComponent component)
	{
		AddComponent(component, addServices: false);
	}

	public static void AddComponent(IGameComponent component, bool addServices)
	{
		if (!addServices)
		{
			InjectServices(component);
		}
		lock (Mutex)
		{
			Game.Components.Add(component);
		}
		if (addServices)
		{
			AddService(component);
		}
		if (TraceFlags.TraceContentLoad)
		{
			Logger.Log("ServiceHelper", LogSeverity.Information, component.GetType().Name + " loaded");
		}
	}

	public static T Get<T>() where T : class
	{
		return Game.Services.GetService(typeof(T)) as T;
	}

	public static object Get(Type type)
	{
		return Game.Services.GetService(type);
	}

	public static void AddService(object service)
	{
		Type[] interfaces = service.GetType().GetInterfaces();
		foreach (Type type in interfaces)
		{
			if (type != typeof(IDisposable) && type != typeof(IUpdateable) && type != typeof(IDrawable) && type != typeof(IGameComponent) && !type.Name.StartsWith("IComparable") && type.GetCustomAttributes(typeof(DisabledServiceAttribute), inherit: false).Length == 0)
			{
				Game.Services.AddService(type, service);
			}
		}
		services.Add(service);
	}

	public static void InitializeServices()
	{
		foreach (object service in services)
		{
			InjectServices(service);
		}
	}

	public static void InjectServices(object componentOrService)
	{
		Type type = componentOrService.GetType();
		do
		{
			MemberInfo[] settableProperties = ReflectionHelper.GetSettableProperties(type);
			for (int i = 0; i < settableProperties.Length; i++)
			{
				PropertyInfo propertyInfo = (PropertyInfo)settableProperties[i];
				ServiceDependencyAttribute firstAttribute = ReflectionHelper.GetFirstAttribute<ServiceDependencyAttribute>(propertyInfo);
				if (firstAttribute == null)
				{
					continue;
				}
				Type propertyType = propertyInfo.PropertyType;
				object obj = ((Game == null) ? null : Game.Services.GetService(propertyType));
				if (obj == null)
				{
					if (!firstAttribute.Optional)
					{
						throw new MissingServiceException(type, propertyType);
					}
				}
				else
				{
					propertyInfo.GetSetMethod(nonPublic: true).Invoke(componentOrService, new object[1] { obj });
				}
			}
			type = type.BaseType;
		}
		while (type != typeof(object));
	}

	public static void RemoveComponent<T>(T component) where T : IGameComponent
	{
		if (component is IDisposable)
		{
			(component as IDisposable).Dispose();
		}
		lock (Mutex)
		{
			Game.Components.Remove(component);
		}
	}
}
