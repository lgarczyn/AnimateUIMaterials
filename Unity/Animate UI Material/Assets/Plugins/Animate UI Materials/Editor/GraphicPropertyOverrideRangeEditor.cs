using UnityEditor;
using UnityEngine;

namespace Plugins.Animate_UI_Materials.Editor
{
  using PropertyType = ShaderUtil.ShaderPropertyType;

  /// <summary>
  /// A special editor for Range properties
  /// Retrieves the range min and max from the shader, and draws the slider field
  /// In case of failure to retrieve, it will simply display a float property
  /// </summary>
  [CustomEditor(typeof(GraphicPropertyOverrideRange), true)]
  public class GraphicPropertyOverrideRangeEditor : GraphicPropertyOverrideEditor
  {
    protected override void DrawValueProperty()
    {
      Material material = GetTargetMaterial();
      int propertyIndex = GetPropertyIndex();

      EditorGUI.BeginChangeCheck();
      GraphicMaterialOverrideEditor.DrawFloatPropertyAsRange(material, propertyIndex, _propertyValue,
        new GUIContent(""));

      if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
    }
  }
}