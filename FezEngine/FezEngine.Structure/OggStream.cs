using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezEngine.Structure;

public class OggStream : IDisposable
{
	private const int MAX_SAMPLES = 192000;

	private static byte[] vorbisBuffer = new byte[192000];

	private static GCHandle bufferHandle = GCHandle.Alloc(vorbisBuffer, GCHandleType.Pinned);

	private static IntPtr bufferPtr = bufferHandle.AddrOfPinnedObject();

	private static readonly Dictionary<int, OggStream> Streams = new Dictionary<int, OggStream>();

	private static int NextStreamId = 1;

	private static Vorbisfile.ov_callbacks VorbisCallbacks = new Vorbisfile.ov_callbacks
	{
		read_func = ReadCallback,
		close_func = CloseCallback,
		tell_func = TellCallback,
		seek_func = SeekCallback
	};

	private DynamicSoundEffectInstance soundEffect;

	private int streamId;

	private MemoryStream memoryStream;

	private long streamOffset;

	private GCHandle streamHandle;

	private IntPtr vorbisFile;

	private readonly object PrecacheLock = new object();

	private static readonly ConcurrentQueue<OggStream> ToPrecache = new ConcurrentQueue<OggStream>();

	private static readonly AutoResetEvent WakeUpPrecacher = new AutoResetEvent(initialState: false);

	private static Thread ThreadedPrecacher;

	private static bool PrecacherAborted;

	private bool hitEof;

	private float volume;

	private float globalVolume = 1f;

	private string category;

	public string Name { get; private set; }

	public string RealName { get; set; }

	public bool IsDisposed { get; private set; }

	public bool LowPass { get; set; }

	public float Volume
	{
		get
		{
			return volume;
		}
		set
		{
			float num = ((SoundEffect.MasterVolume == 0f) ? 1f : (1f / SoundEffect.MasterVolume));
			soundEffect.Volume = MathHelper.Clamp((volume = value) * globalVolume, 0f, 1f) * num;
		}
	}

	public float GlobalVolume
	{
		set
		{
			globalVolume = value;
			Volume = volume;
		}
	}

	public string Category
	{
		get
		{
			return category;
		}
		set
		{
			category = value;
			SyncVolume();
		}
	}

	public bool IsLooped { get; set; }

	public bool IsStopped
	{
		get
		{
			if (soundEffect.State != SoundState.Stopped)
			{
				return soundEffect.PendingBufferCount == 0;
			}
			return true;
		}
	}

	public bool IsPlaying => soundEffect.State == SoundState.Playing;

	public int QueuedBuffers => soundEffect.PendingBufferCount;

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	private OggStream()
	{
		ServiceHelper.InjectServices(this);
		streamId = NextStreamId;
		Streams.Add(streamId, this);
		if (NextStreamId == int.MaxValue)
		{
			NextStreamId = 0;
		}
		else
		{
			NextStreamId++;
		}
		LowPass = true;
	}

	public OggStream(MemoryStream stream)
		: this()
	{
		memoryStream = stream;
		streamHandle = GCHandle.Alloc(stream.GetBuffer(), GCHandleType.Pinned);
		Vorbisfile.ov_open_callbacks(new IntPtr(streamId), out vorbisFile, IntPtr.Zero, IntPtr.Zero, VorbisCallbacks);
		Initialize();
	}

	public OggStream(string filename)
		: this()
	{
		Name = filename;
		Vorbisfile.ov_fopen(filename, out vorbisFile);
		Initialize();
	}

	private void Initialize()
	{
		Vorbisfile.vorbis_info vorbis_info = Vorbisfile.ov_info(vorbisFile, -1);
		soundEffect = new DynamicSoundEffectInstance((int)vorbis_info.rate, (vorbis_info.channels == 1) ? AudioChannels.Mono : AudioChannels.Stereo);
		Volume = 1f;
		ToPrecache.Enqueue(this);
		if (ThreadedPrecacher == null)
		{
			ThreadedPrecacher = new Thread(PrecacheStreams)
			{
				Priority = ThreadPriority.Lowest
			};
			ThreadedPrecacher.Start();
		}
		WakeUpPrecacher.Set();
	}

	public static void AbortPrecacher()
	{
		PrecacherAborted = true;
		WakeUpPrecacher.Set();
	}

	private static void PrecacheStreams()
	{
		while (!PrecacherAborted)
		{
			Precache();
			WakeUpPrecacher.WaitOne();
		}
	}

	private static void Precache()
	{
		OggStream result;
		while (ToPrecache.TryDequeue(out result))
		{
			lock (result.PrecacheLock)
			{
				while (result.soundEffect != null && !result.soundEffect.IsDisposed && result.vorbisFile != IntPtr.Zero && result.QueuedBuffers < 3 && !result.hitEof)
				{
					result.QueueBuffer(null, EventArgs.Empty);
				}
			}
		}
	}

	private unsafe static IntPtr ReadCallback(IntPtr ptr, IntPtr size, IntPtr nmemb, IntPtr datasource)
	{
		if (!Streams.TryGetValue(datasource.ToInt32(), out var value))
		{
			return new IntPtr(0);
		}
		byte* source = (byte*)value.streamHandle.AddrOfPinnedObject().ToPointer() + value.streamOffset;
		byte* destination = (byte*)ptr.ToPointer();
		int val = nmemb.ToInt32() * size.ToInt32();
		int num = Math.Min((int)(value.memoryStream.Length - value.streamOffset), val);
		memcpy(destination, source, (IntPtr)num);
		value.streamOffset += num;
		return new IntPtr(num);
	}

	[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern IntPtr memcpy(byte* destination, byte* source, IntPtr num);

	private static int SeekCallback(IntPtr datasource, long offset, Vorbisfile.SeekWhence whence)
	{
		if (!Streams.TryGetValue(datasource.ToInt32(), out var value))
		{
			return -1;
		}
		switch (whence)
		{
		case Vorbisfile.SeekWhence.SEEK_CUR:
			value.streamOffset += offset;
			break;
		case Vorbisfile.SeekWhence.SEEK_END:
			value.streamOffset = value.memoryStream.Length;
			break;
		case Vorbisfile.SeekWhence.SEEK_SET:
			value.streamOffset = offset;
			break;
		}
		return 0;
	}

	private static long TellCallback(IntPtr datasource)
	{
		if (!Streams.TryGetValue(datasource.ToInt32(), out var value))
		{
			return 0L;
		}
		return value.streamOffset;
	}

	private static int CloseCallback(IntPtr datasource)
	{
		if (!Streams.TryGetValue(datasource.ToInt32(), out var value))
		{
			return 0;
		}
		value.streamOffset = 0L;
		return 0;
	}

	~OggStream()
	{
		Dispose(disposing: true);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (disposing)
		{
			Stop();
			lock (PrecacheLock)
			{
				if (soundEffect != null)
				{
					soundEffect.Dispose();
					soundEffect = null;
				}
				Vorbisfile.ov_clear(ref vorbisFile);
			}
			if (memoryStream != null)
			{
				streamHandle.Free();
				memoryStream = null;
			}
			Streams.Remove(streamId);
		}
		IsDisposed = true;
	}

	public void Play()
	{
		soundEffect.BufferNeeded += OnBufferNeeded;
		while (soundEffect.PendingBufferCount == 0)
		{
			Thread.Yield();
		}
		if (soundEffect.State == SoundState.Paused)
		{
			soundEffect.Resume();
			return;
		}
		soundEffect.Play();
		(SoundManager as SoundManager).RegisterLowPass(soundEffect);
	}

	private void OnBufferNeeded(object sender, EventArgs e)
	{
		ToPrecache.Enqueue(this);
		WakeUpPrecacher.Set();
	}

	private void QueueBuffer(object source, EventArgs ea)
	{
		int num = 0;
		int num2 = 0;
		do
		{
			num = (int)Vorbisfile.ov_read(vorbisFile, bufferPtr + num2, 4096, 0, 2, 1, out var _);
			num2 += num;
		}
		while (num > 0 && num2 < 187904);
		if (num2 == 0)
		{
			if (IsLooped)
			{
				Vorbisfile.ov_time_seek(vorbisFile, 0.0);
				QueueBuffer(source, ea);
			}
			else
			{
				hitEof = true;
				soundEffect.BufferNeeded -= OnBufferNeeded;
			}
		}
		else
		{
			soundEffect.SubmitBuffer(vorbisBuffer, 0, num2);
		}
	}

	public void Pause()
	{
		soundEffect.Pause();
	}

	public void Resume()
	{
		soundEffect.Resume();
	}

	public void Stop()
	{
		if (soundEffect != null)
		{
			soundEffect.Stop();
			soundEffect.BufferNeeded -= OnBufferNeeded;
		}
	}

	private void SyncVolume()
	{
		if (category == "Ambience")
		{
			GlobalVolume = SoundEffect.MasterVolume * SoundManager.GlobalVolumeFactor * SoundManager.MusicVolumeFactor;
		}
		else
		{
			GlobalVolume = FezMath.Saturate(Easing.EaseIn(SoundManager.MusicVolume * SoundManager.GlobalVolumeFactor, EasingType.Quadratic) * SoundManager.MusicVolumeFactor);
		}
	}

	public static void SyncAllVolume()
	{
		foreach (OggStream value in Streams.Values)
		{
			value.SyncVolume();
		}
	}

	public static void SyncAllFilter(MethodInfo applyFilter, object[] gainContainer)
	{
		foreach (OggStream value in Streams.Values)
		{
			if (value.LowPass)
			{
				applyFilter.Invoke(value.soundEffect, gainContainer);
			}
		}
	}
}
