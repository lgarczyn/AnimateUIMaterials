using UnityEngine;

namespace Plugins.Animate_UI_Materials
{
  [System.Serializable]
  public struct TextureScaleOffset
  {
    public Texture Texture;
    public Vector4 ScaleOffset;
    public Vector2 Scale => new Vector2(ScaleOffset.x, ScaleOffset.y);
    public Vector2 Offset => new Vector2(ScaleOffset.z, ScaleOffset.w);

    public TextureScaleOffset(Texture texture, Vector2 scale, Vector2 offset)
    {
      Texture = texture;
      ScaleOffset = new Vector4(scale.x, scale.y, offset.x, offset.y);
    }
  }

  /// <summary>
  ///   Used in with GraphicMaterialOverride to modify a material without creating a variant
  ///   Added to a child of the Graphic element
  ///   This variant only applies to Texture properties
  /// </summary>
  [AddComponentMenu("UI/Animate UI Material/GraphicPropertyOverrideScaleAndOffset")]
  public class GraphicPropertyOverrideScaleAndOffset : GraphicPropertyOverride<TextureScaleOffset>
  {
    public override string DisplayName => $"{propertyName} Scale Offset";

    /// <summary>
    ///   Apply the modified property to the material
    /// </summary>
    /// <param name="material"></param>
    public override void ApplyModifiedProperty(Material material)
    {
      material.SetTextureOffset(PropertyId, propertyValue.Offset);
      material.SetTextureScale(PropertyId, propertyValue.Scale);
      material.SetTexture(PropertyId, propertyValue.Texture);
    }

    /// <summary>
    /// Retrieve the default Texture value from the source material
    /// </summary>
    /// <param name="material">The source material</param>
    /// <param name="defaultValue">The Texture value from the material</param>
    /// <returns>True if the value could be retrieved</returns>
    public override bool GetDefaultValue(Material material, out TextureScaleOffset defaultValue)
    {
      bool hasProperty = material.HasTexture(PropertyId);
      Vector2 scale = hasProperty ? material.GetTextureOffset(PropertyId) : Vector2.one;
      Vector2 offset = hasProperty ? material.GetTextureScale(PropertyId) : Vector2.zero;
      Texture texture = hasProperty ? material.GetTexture(PropertyId) : null;
      defaultValue = new TextureScaleOffset(texture, scale, offset);
      return hasProperty;
    }
  }
}