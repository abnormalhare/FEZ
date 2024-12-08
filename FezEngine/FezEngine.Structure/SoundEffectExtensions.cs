using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezEngine.Structure;

public static class SoundEffectExtensions
{
	public static ISoundManager SoundManager;

	public static SoundEmitter Emit(this SoundEffect soundEffect)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, 0f, 1f, paused: false, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, bool loop)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, 0f, 1f, paused: false, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, bool loop, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, 0f, 1f, paused, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, float pitch)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, pitch, 1f, paused: false, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, float pitch, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, pitch, 1f, paused, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, bool loop, float pitch)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, pitch, 1f, paused: false, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, bool loop, float pitch, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, pitch, 1f, paused, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, float pitch, float volume)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, pitch, volume, paused: false, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, float pitch, float volume, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, pitch, volume, paused, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, bool loop, float pitch, float volume)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, pitch, volume, paused: false, null));
	}

	public static SoundEmitter Emit(this SoundEffect soundEffect, bool loop, float pitch, float volume, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, pitch, volume, paused, null));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, 0f, 1f, paused: false, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, bool loop)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, 0f, 1f, paused: false, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, bool loop, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, 0f, 1f, paused, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, float pitch)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, pitch, 1f, paused: false, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, float pitch, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, pitch, 1f, paused, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, bool loop, float pitch)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, pitch, 1f, paused: false, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, bool loop, float pitch, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, pitch, 1f, paused, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, float pitch, float volume)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, pitch, volume, paused: false, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, float pitch, float volume, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, looped: false, pitch, volume, paused, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, bool loop, float pitch, float volume)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, pitch, volume, paused: false, position));
	}

	public static SoundEmitter EmitAt(this SoundEffect soundEffect, Vector3 position, bool loop, float pitch, float volume, bool paused)
	{
		if (SoundManager == null)
		{
			SoundManager = ServiceHelper.Get<ISoundManager>();
		}
		return SoundManager.AddEmitter(new SoundEmitter(soundEffect, loop, pitch, volume, paused, position));
	}
}
