using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public abstract class DefaultEffect : BaseEffect
{
	public class Textured : DefaultEffect
	{
		private readonly SemanticMappedTexture texture;

		private readonly SemanticMappedBoolean textureEnabled;

		private bool groupTextureDirty;

		public Textured()
			: base("DefaultEffect_Textured")
		{
			textureEnabled = new SemanticMappedBoolean(effect.Parameters, "TextureEnabled");
			texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		}

		public override BaseEffect Clone()
		{
			return new Textured
			{
				Fullbright = base.Fullbright,
				Emissive = base.Emissive,
				AlphaIsEmissive = base.AlphaIsEmissive
			};
		}

		public override void Prepare(Mesh mesh)
		{
			base.Prepare(mesh);
			textureEnabled.Set(mesh.TexturingType == TexturingType.Texture2D);
			texture.Set(mesh.Texture);
			groupTextureDirty = false;
		}

		public override void Prepare(Group group)
		{
			base.Prepare(group);
			if (group.TexturingType == TexturingType.Texture2D)
			{
				textureEnabled.Set(value: true);
				texture.Set(group.Texture);
				groupTextureDirty = true;
			}
			else if (groupTextureDirty)
			{
				textureEnabled.Set(group.Mesh.TexturingType == TexturingType.Texture2D);
				texture.Set(group.Mesh.Texture);
			}
		}
	}

	public class VertexColored : DefaultEffect
	{
		public VertexColored()
			: base("DefaultEffect_VertexColored")
		{
		}

		public override BaseEffect Clone()
		{
			return new VertexColored
			{
				Fullbright = base.Fullbright,
				Emissive = base.Emissive,
				AlphaIsEmissive = base.AlphaIsEmissive
			};
		}
	}

	public class LitVertexColored : DefaultEffect
	{
		private readonly SemanticMappedBoolean specularEnabled;

		public bool Specular
		{
			get
			{
				return specularEnabled.Get();
			}
			set
			{
				specularEnabled.Set(value);
			}
		}

		public LitVertexColored()
			: base("DefaultEffect_LitVertexColored")
		{
			specularEnabled = new SemanticMappedBoolean(effect.Parameters, "SpecularEnabled");
		}

		public override BaseEffect Clone()
		{
			return new LitVertexColored
			{
				Fullbright = base.Fullbright,
				Emissive = base.Emissive,
				Specular = Specular,
				AlphaIsEmissive = base.AlphaIsEmissive
			};
		}

		public override void Prepare(Group group)
		{
			base.Prepare(group);
			if (IgnoreCache || !group.EffectOwner || group.InverseTransposeWorldMatrix.Dirty)
			{
				matrices.WorldInverseTranspose = group.InverseTransposeWorldMatrix;
				group.InverseTransposeWorldMatrix.Clean();
			}
		}
	}

	public class LitTextured : DefaultEffect
	{
		private readonly SemanticMappedTexture texture;

		private readonly SemanticMappedBoolean textureEnabled;

		private readonly SemanticMappedBoolean specularEnabled;

		private bool groupTextureDirty;

		public bool Specular
		{
			get
			{
				return specularEnabled.Get();
			}
			set
			{
				specularEnabled.Set(value);
			}
		}

		public LitTextured()
			: base("DefaultEffect_LitTextured")
		{
			texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
			textureEnabled = new SemanticMappedBoolean(effect.Parameters, "TextureEnabled");
			specularEnabled = new SemanticMappedBoolean(effect.Parameters, "SpecularEnabled");
		}

		public override BaseEffect Clone()
		{
			return new LitTextured
			{
				Specular = Specular,
				AlphaIsEmissive = base.AlphaIsEmissive,
				Emissive = base.Emissive,
				Fullbright = base.Fullbright
			};
		}

		public override void Prepare(Mesh mesh)
		{
			base.Prepare(mesh);
			textureEnabled.Set(mesh.TexturingType == TexturingType.Texture2D);
			texture.Set(mesh.Texture);
			groupTextureDirty = false;
		}

		public override void Prepare(Group group)
		{
			base.Prepare(group);
			if (IgnoreCache || !group.EffectOwner || group.InverseTransposeWorldMatrix.Dirty)
			{
				matrices.WorldInverseTranspose = group.InverseTransposeWorldMatrix;
				group.InverseTransposeWorldMatrix.Clean();
			}
			if (group.TexturingType == TexturingType.Texture2D)
			{
				textureEnabled.Set(value: true);
				texture.Set(group.Texture);
				groupTextureDirty = true;
			}
			else if (groupTextureDirty)
			{
				textureEnabled.Set(group.Mesh.TexturingType == TexturingType.Texture2D);
				texture.Set(group.Mesh.Texture);
			}
		}
	}

	public class TexturedVertexColored : DefaultEffect
	{
		private readonly SemanticMappedTexture texture;

		private readonly SemanticMappedBoolean textureEnabled;

		private bool groupTextureDirty;

		public TexturedVertexColored()
			: base("DefaultEffect_TexturedVertexColored")
		{
			texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
			textureEnabled = new SemanticMappedBoolean(effect.Parameters, "TextureEnabled");
		}

		public override BaseEffect Clone()
		{
			return new TexturedVertexColored
			{
				AlphaIsEmissive = base.AlphaIsEmissive,
				Fullbright = base.Fullbright,
				Emissive = base.Emissive
			};
		}

		public override void Prepare(Mesh mesh)
		{
			base.Prepare(mesh);
			textureEnabled.Set(mesh.TexturingType == TexturingType.Texture2D);
			texture.Set(mesh.Texture);
			groupTextureDirty = false;
		}

		public override void Prepare(Group group)
		{
			base.Prepare(group);
			if (group.TexturingType == TexturingType.Texture2D)
			{
				textureEnabled.Set(value: true);
				texture.Set(group.Texture);
				groupTextureDirty = true;
			}
			else if (groupTextureDirty)
			{
				textureEnabled.Set(group.Mesh.TexturingType == TexturingType.Texture2D);
				texture.Set(group.Mesh.Texture);
			}
		}
	}

	public class LitTexturedVertexColored : DefaultEffect
	{
		private readonly SemanticMappedTexture texture;

		private readonly SemanticMappedBoolean textureEnabled;

		private readonly SemanticMappedBoolean specularEnabled;

		private bool groupTextureDirty;

		public bool Specular
		{
			get
			{
				return specularEnabled.Get();
			}
			set
			{
				specularEnabled.Set(value);
			}
		}

		public LitTexturedVertexColored()
			: base("DefaultEffect_LitTexturedVertexColored")
		{
			texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
			textureEnabled = new SemanticMappedBoolean(effect.Parameters, "TextureEnabled");
			specularEnabled = new SemanticMappedBoolean(effect.Parameters, "SpecularEnabled");
		}

		public override BaseEffect Clone()
		{
			return new LitTexturedVertexColored
			{
				Fullbright = base.Fullbright,
				Emissive = base.Emissive,
				Specular = Specular,
				AlphaIsEmissive = base.AlphaIsEmissive
			};
		}

		public override void Prepare(Mesh mesh)
		{
			base.Prepare(mesh);
			textureEnabled.Set(mesh.TexturingType == TexturingType.Texture2D);
			texture.Set(mesh.Texture);
			groupTextureDirty = false;
		}

		public override void Prepare(Group group)
		{
			base.Prepare(group);
			if (IgnoreCache || !group.EffectOwner || group.InverseTransposeWorldMatrix.Dirty)
			{
				matrices.WorldInverseTranspose = group.InverseTransposeWorldMatrix;
				group.InverseTransposeWorldMatrix.Clean();
			}
			if (group.TexturingType == TexturingType.Texture2D)
			{
				textureEnabled.Set(value: true);
				texture.Set(group.Texture);
				groupTextureDirty = true;
			}
			else if (groupTextureDirty)
			{
				textureEnabled.Set(group.Mesh.TexturingType == TexturingType.Texture2D);
				texture.Set(group.Mesh.Texture);
			}
		}
	}

	private readonly SemanticMappedBoolean alphaIsEmissive;

	private readonly SemanticMappedBoolean fullbright;

	private readonly SemanticMappedSingle emissive;

	private Vector3 lastDiffuse;

	public LightingEffectPass Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value == LightingEffectPass.Pre) ? 1 : 0];
		}
	}

	public bool AlphaIsEmissive
	{
		get
		{
			return alphaIsEmissive.Get();
		}
		set
		{
			alphaIsEmissive.Set(value);
		}
	}

	public bool Fullbright
	{
		get
		{
			return fullbright.Get();
		}
		set
		{
			fullbright.Set(value);
		}
	}

	public float Emissive
	{
		get
		{
			return emissive.Get();
		}
		set
		{
			emissive.Set(value);
		}
	}

	private DefaultEffect(string effectName)
		: base(effectName)
	{
		alphaIsEmissive = new SemanticMappedBoolean(effect.Parameters, "AlphaIsEmissive");
		fullbright = new SemanticMappedBoolean(effect.Parameters, "Fullbright");
		emissive = new SemanticMappedSingle(effect.Parameters, "Emissive");
		Pass = LightingEffectPass.Main;
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		if (group.Material != null)
		{
			if (lastDiffuse != group.Material.Diffuse)
			{
				material.Diffuse = group.Material.Diffuse;
				lastDiffuse = group.Material.Diffuse;
			}
		}
		else
		{
			material.Diffuse = group.Mesh.Material.Diffuse;
			lastDiffuse = group.Mesh.Material.Diffuse;
		}
	}
}
