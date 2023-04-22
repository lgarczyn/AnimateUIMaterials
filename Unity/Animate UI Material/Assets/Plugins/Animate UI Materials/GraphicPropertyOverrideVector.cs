using UnityEditor;
using UnityEngine;

namespace Plugins.Animate_UI_Materials
{
  using PropertyType = ShaderUtil.ShaderPropertyType;

  /// <summary>
  ///   Used in with GraphicMaterialOverride to modify a material without creating a variant
  ///   Added to a child of the Graphic element
  ///   This variant only applies to Vector properties
  /// </summary>
  public class GraphicPropertyOverrideVector : GraphicPropertyOverride<Vector4>
  {
    public override PropertyType GetPropertyType()
    {
      return PropertyType.Vector;
    }

    /// <summary>
    ///   Apply the modified property to the material
    /// </summary>
    /// <param name="material"></param>
    public override void ApplyModifiedProperty(Material material)
    {
      material.SetVector(PropertyId, propertyValue);
    }
  }
}