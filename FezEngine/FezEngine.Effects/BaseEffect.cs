using System;
using System.Collections.Generic;
using System.Diagnostics;
using Common;
using FezEngine.Effects.Structures;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects;

public abstract class BaseEffect : IDisposable
{
	internal FogEffectStructure fog;

	protected SemanticMappedSingle aspectRatio;

	protected SemanticMappedSingle time;

	protected SemanticMappedVector3 eye;

	protected SemanticMappedVector3 baseAmbient;

	protected SemanticMappedVector2 texelOffset;

	protected SemanticMappedVector3 diffuseLight;

	protected SemanticMappedVector3 eyeSign;

	protected SemanticMappedVector3 levelCenter;

	protected Matrix viewProjection;

	private Stopwatch stopWatch;

	internal readonly MatricesEffectStructure matrices;

	internal readonly MaterialEffectStructure material;

	protected Effect effect;

	protected EffectPass currentPass;

	protected EffectTechnique currentTechnique;

	protected bool SimpleGroupPrepare;

	protected bool SimpleMeshPrepare;

	public bool IsDisposed;

	public static readonly object DeviceLock = new object();

	private static Vector3 sharedEyeSign;

	private static Vector3 sharedLevelCenter;

	private static bool useHardwareInstancing = false;

	protected bool textureMatrixDirty;

	public bool IgnoreCache;

	public static bool UseHardwareInstancing
	{
		get
		{
			return useHardwareInstancing;
		}
		set
		{
			bool num = useHardwareInstancing != value && BaseEffect.InstancingModeChanged != null;
			useHardwareInstancing = value;
			if (num)
			{
				Logger.Log("Instancing", LogSeverity.Information, "Hardware instancing is now " + (useHardwareInstancing ? "enabled" : "disabled"));
				BaseEffect.InstancingModeChanged();
			}
		}
	}

	public static Vector3 EyeSign
	{
		set
		{
			sharedEyeSign = value;
		}
	}

	public static Vector3 LevelCenter
	{
		set
		{
			sharedLevelCenter = value;
		}
	}

	public EffectPass CurrentPass => currentPass;

	public Matrix? ForcedViewMatrix { get; set; }

	public Matrix? ForcedProjectionMatrix { get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { protected get; set; }

	[ServiceDependency]
	public IGraphicsDeviceService GraphicsDeviceService { protected get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraProvider { protected get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { protected get; set; }

	[ServiceDependency]
	public IFogManager FogProvider { protected get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	public static event Action InstancingModeChanged;

	protected BaseEffect(string effectName)
		: this(effectName, skipClone: false)
	{
	}

	protected BaseEffect(string effectName, bool skipClone)
	{
		ServiceHelper.InjectServices(this);
		effect = CMProvider.Global.Load<Effect>("Effects/" + effectName);
		if (!skipClone)
		{
			TryCloneEffect(effect);
			while (currentPass == null)
			{
				Logger.Log("Effect", LogSeverity.Warning, "Could not validate effect " + effectName);
				TryCloneEffect(effect);
			}
		}
		else
		{
			currentTechnique = effect.Techniques[0];
			currentPass = currentTechnique.Passes[0];
		}
		matrices = new MatricesEffectStructure(effect.Parameters);
		material = new MaterialEffectStructure(effect.Parameters);
		Initialize();
	}

	private void TryCloneEffect(Effect sharedEffect)
	{
		effect = sharedEffect.Clone();
		using List<EffectTechnique>.Enumerator enumerator = effect.Techniques.GetEnumerator();
		if (enumerator.MoveNext())
		{
			currentPass = (currentTechnique = enumerator.Current).Passes[0];
		}
	}

	public virtual BaseEffect Clone()
	{
		throw new NotImplementedException();
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			effect = null;
			currentPass = null;
			currentTechnique = null;
			IsDisposed = true;
			EngineState.PauseStateChanged -= CheckPause;
			CameraProvider.ViewChanged -= RefreshViewProjection;
			CameraProvider.ProjectionChanged -= RefreshViewProjection;
			CameraProvider.ViewChanged -= RefreshCenterPosition;
			CameraProvider.ProjectionChanged -= RefreshAspectRatio;
			FogProvider.FogSettingsChanged -= RefreshFog;
			LevelManager.LightingChanged -= RefreshLighting;
			GraphicsDeviceService.DeviceReset -= RefreshTexelSize;
			if (stopWatch != null)
			{
				stopWatch.Stop();
			}
			stopWatch = null;
		}
	}

	private void Initialize()
	{
		fog = new FogEffectStructure(effect.Parameters);
		aspectRatio = new SemanticMappedSingle(effect.Parameters, "AspectRatio");
		texelOffset = new SemanticMappedVector2(effect.Parameters, "TexelOffset");
		time = new SemanticMappedSingle(effect.Parameters, "Time");
		baseAmbient = new SemanticMappedVector3(effect.Parameters, "BaseAmbient");
		eye = new SemanticMappedVector3(effect.Parameters, "Eye");
		diffuseLight = new SemanticMappedVector3(effect.Parameters, "DiffuseLight");
		eyeSign = new SemanticMappedVector3(effect.Parameters, "EyeSign");
		levelCenter = new SemanticMappedVector3(effect.Parameters, "LevelCenter");
		stopWatch = Stopwatch.StartNew();
		EngineState.PauseStateChanged += CheckPause;
		CameraProvider.ViewChanged += RefreshViewProjection;
		CameraProvider.ProjectionChanged += RefreshViewProjection;
		RefreshViewProjection();
		CameraProvider.ViewChanged += RefreshCenterPosition;
		RefreshCenterPosition();
		CameraProvider.ProjectionChanged += RefreshAspectRatio;
		RefreshAspectRatio();
		FogProvider.FogSettingsChanged += RefreshFog;
		RefreshFog();
		LevelManager.LightingChanged += RefreshLighting;
		RefreshLighting();
		GraphicsDeviceService.DeviceReset += RefreshTexelSize;
		RefreshTexelSize();
		eyeSign.Set(sharedEyeSign);
		levelCenter.Set(sharedLevelCenter);
	}

	private void RefreshTexelSize(object sender, EventArgs ea)
	{
		RefreshTexelSize();
	}

	private void RefreshTexelSize()
	{
		int width = GraphicsDeviceService.GraphicsDevice.Viewport.Width;
		int height = GraphicsDeviceService.GraphicsDevice.Viewport.Height;
		texelOffset.Set(new Vector2(-0.5f / (float)width, 0.5f / (float)height));
	}

	private void RefreshLighting()
	{
		baseAmbient.Set(LevelManager.ActualAmbient.ToVector3());
		diffuseLight.Set(LevelManager.ActualDiffuse.ToVector3());
	}

	private void CheckPause()
	{
		if (stopWatch.IsRunning && EngineState.Paused)
		{
			stopWatch.Stop();
		}
		else if (!stopWatch.IsRunning && !EngineState.Paused)
		{
			stopWatch.Start();
		}
	}

	private void RefreshFog()
	{
		fog.FogType = FogProvider.Type;
		fog.FogColor = FogProvider.Color;
		if (EngineState.InEditor)
		{
			fog.FogDensity = FogProvider.Density;
		}
		else
		{
			fog.FogDensity = FogProvider.Density * 1.25f;
		}
	}

	private void RefreshViewProjection()
	{
		viewProjection = CameraProvider.View * CameraProvider.Projection;
		matrices.ViewProjection = viewProjection;
	}

	private void RefreshCenterPosition()
	{
		eye.Set(CameraProvider.InverseView.Forward);
	}

	private void RefreshAspectRatio()
	{
		aspectRatio.Set(CameraProvider.AspectRatio);
	}

	public virtual void Prepare(Mesh mesh)
	{
		eyeSign.Set(sharedEyeSign);
		levelCenter.Set(sharedLevelCenter);
		time.Set((float)stopWatch.Elapsed.TotalSeconds);
		if (mesh.TextureMatrix.Dirty || IgnoreCache)
		{
			matrices.TextureMatrix = mesh.TextureMatrix;
			mesh.TextureMatrix.Clean();
		}
		if (SimpleMeshPrepare)
		{
			matrices.WorldViewProjection = viewProjection;
		}
		else if (SimpleGroupPrepare)
		{
			Matrix matrix = viewProjection;
			if (ForcedViewMatrix.HasValue && !ForcedProjectionMatrix.HasValue)
			{
				matrix = ForcedViewMatrix.Value * CameraProvider.Projection;
			}
			else if (!ForcedViewMatrix.HasValue && ForcedProjectionMatrix.HasValue)
			{
				matrix = CameraProvider.View * ForcedProjectionMatrix.Value;
			}
			else if (ForcedViewMatrix.HasValue && ForcedProjectionMatrix.HasValue)
			{
				matrix = ForcedViewMatrix.Value * ForcedProjectionMatrix.Value;
			}
			matrices.WorldViewProjection = mesh.WorldMatrix * matrix;
		}
		else
		{
			material.Opacity = mesh.Material.Opacity;
		}
	}

	public virtual void Prepare(Group group)
	{
		if (!SimpleGroupPrepare)
		{
			Matrix matrix = viewProjection;
			if (ForcedViewMatrix.HasValue && !ForcedProjectionMatrix.HasValue)
			{
				matrix = ForcedViewMatrix.Value * CameraProvider.Projection;
			}
			else if (!ForcedViewMatrix.HasValue && ForcedProjectionMatrix.HasValue)
			{
				matrix = CameraProvider.View * ForcedProjectionMatrix.Value;
			}
			else if (ForcedViewMatrix.HasValue && ForcedProjectionMatrix.HasValue)
			{
				matrix = ForcedViewMatrix.Value * ForcedProjectionMatrix.Value;
			}
			matrices.WorldViewProjection = group.WorldMatrix.Value * matrix;
			material.Opacity = ((group.Material != null) ? group.Material.Opacity : group.Mesh.Material.Opacity);
			if (group.TextureMatrix.Value.HasValue)
			{
				matrices.TextureMatrix = group.TextureMatrix.Value.Value;
				textureMatrixDirty = true;
			}
			else if (textureMatrixDirty)
			{
				matrices.TextureMatrix = Matrix.Identity;
				textureMatrixDirty = false;
			}
		}
	}

	public void Apply()
	{
		currentPass.Apply();
	}
}
