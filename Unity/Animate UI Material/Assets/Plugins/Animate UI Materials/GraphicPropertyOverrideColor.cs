using UnityEditor;
using UnityEngine;

namespace Plugins.Animate_UI_Materials
{
  using PropertyType = ShaderUtil.ShaderPropertyType;

  /// <summary>
  ///   Used in with GraphicMaterialOverride to modify a material without creating a variant
  ///   Added to a child of the Graphic element
  ///   This variant only applies to Color properties
  /// </summary>
  [AddComponentMenu("UI/Animate UI Material/GraphicPropertyOverrideColor")]
  public class GraphicPropertyOverrideColor : GraphicPropertyOverride<Color>
  {
    public override PropertyType GetPropertyType()
    {
      return PropertyType.Color;
    }

    /// <summary>
    /// Apply the modified property to the material
    /// </summary>
    /// <param name="material"></param>
    public override void ApplyModifiedProperty(Material material)
    {
      material.SetColor(PropertyId, propertyValue);
    }

    /// <summary>
    /// Retrieve the default Color value from the source material
    /// </summary>
    /// <param name="material">The source material</param>
    /// <param name="defaultValue">The Color value from the material</param>
    /// <returns>True if the value could be retrieved</returns>
    public override bool GetDefaultValue(Material material, out Color defaultValue)
    {
      bool hasProperty = material.HasColor(PropertyId);
      if (hasProperty) defaultValue = material.GetColor(PropertyId);
      else defaultValue = default;
      return hasProperty;
    }
  }
}