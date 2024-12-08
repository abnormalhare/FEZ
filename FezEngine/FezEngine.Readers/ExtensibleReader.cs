using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class ExtensibleReader<T> : IDisposable
{
	private readonly Func<int> mReadHeader;

	private readonly Action<int> mReadSharedResources;

	private readonly FieldInfo fTypeReaders;

	private readonly ContentReader BaseReader;

	private readonly Stream Stream;

	public readonly Dictionary<string, ContentTypeReader> ReaderReplacements = new Dictionary<string, ContentTypeReader>();

	public ExtensibleReader(ContentManager manager, Stream stream, string assetName)
	{
		Stream = stream;
		Type typeFromHandle = typeof(ContentReader);
		ConstructorInfo constructor = typeFromHandle.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[5]
		{
			typeof(ContentManager),
			typeof(Stream),
			typeof(string),
			typeof(Action<IDisposable>),
			typeof(int)
		}, null);
		BaseReader = (ContentReader)constructor.Invoke(new object[5]
		{
			manager,
			Stream,
			assetName,
			(Action<IDisposable>)delegate
			{
			},
			1
		});
		MethodInfo method = typeFromHandle.GetMethod("ReadHeader", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[0], null);
		mReadHeader = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), BaseReader, method);
		method = typeFromHandle.GetMethod("ReadSharedResources", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[1] { typeof(int) }, null);
		mReadSharedResources = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), BaseReader, method);
		fTypeReaders = typeFromHandle.GetField("typeReaders", BindingFlags.Instance | BindingFlags.NonPublic);
	}

	private void ReplaceReaders()
	{
		ContentTypeReader[] array = (ContentTypeReader[])fTypeReaders.GetValue(BaseReader);
		for (int i = 0; i < array.Length; i++)
		{
			Type type = array[i].GetType();
			if (ReaderReplacements.TryGetValue(type.Name, out var value))
			{
				array[i] = value;
			}
		}
	}

	public T Read()
	{
		int obj = mReadHeader();
		ReplaceReaders();
		T result = BaseReader.ReadObject<T>();
		mReadSharedResources(obj);
		return result;
	}

	public void Dispose()
	{
		BaseReader.Close();
		Stream.Dispose();
	}
}
