using UnityEngine;

namespace Plugins.Animate_UI_Materials
{
  /// <summary>
  ///   Used in with GraphicMaterialOverride to modify a material without creating a variant
  ///   Added to a child of the Graphic element
  ///   This variant only applies to Texture properties
  /// </summary>
  [AddComponentMenu("UI/Animate UI Material/GraphicPropertyOverrideScaleAndOffset")]
  public class GraphicPropertyOverrideScaleAndOffset : GraphicPropertyOverride<Vector4>
  {
    /// <summary>
    ///   Apply the modified property to the material
    /// </summary>
    /// <param name="material"></param>
    public override void ApplyModifiedProperty(Material material)
    {
      material.SetTextureOffset(PropertyId, new Vector2(propertyValue.x, propertyValue.y));
      material.SetTextureScale(PropertyId, new Vector2(propertyValue.z, propertyValue.w));
    }

    /// <summary>
    /// Retrieve the default Texture value from the source material
    /// </summary>
    /// <param name="material">The source material</param>
    /// <param name="defaultValue">The Texture value from the material</param>
    /// <returns>True if the value could be retrieved</returns>
    public override bool GetDefaultValue(Material material, out Vector4 defaultValue)
    {
      bool hasProperty = material.HasTexture(PropertyId);
      Vector2 scale = hasProperty ? material.GetTextureOffset(PropertyId) : Vector2.zero;
      Vector2 offset = hasProperty ? material.GetTextureScale(PropertyId) : Vector2.one;
      defaultValue = new Vector4(scale.x, scale.y, offset.x, offset.y);
      return hasProperty;
    }
  }
}