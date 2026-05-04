using System;
using UnityEditor;
using UnityEngine;

namespace Plugins.Animate_UI_Materials.Editor
{
  public static class SerializedMaterialPropertyUtility
  {
    public static void AssertTypeEqual(SerializedProperty a, MaterialProperty b)
    {
      AssertTypeEqual(b, a);
    }

    public static void AssertTypeEqual(MaterialProperty a, SerializedProperty b)
    {
      switch (a.type)
      {
        case MaterialProperty.PropType.Color when b.propertyType == SerializedPropertyType.Color: return;
        case MaterialProperty.PropType.Float when b.propertyType == SerializedPropertyType.Float: return;
        case MaterialProperty.PropType.Vector when b.propertyType == SerializedPropertyType.Vector4: return;
        case MaterialProperty.PropType.Range when b.propertyType == SerializedPropertyType.Float: return;
        case MaterialProperty.PropType.Int when b.propertyType == SerializedPropertyType.Integer: return;
        case MaterialProperty.PropType.Texture when b.propertyType == SerializedPropertyType.ObjectReference: return;
        case MaterialProperty.PropType.Texture when b.propertyType == SerializedPropertyType.Generic: return;
        default: throw new Exception("Wrong Material Override Type");
      }
    }

    public static void CopyProperty(SerializedProperty to, MaterialProperty from)
    {
      switch (from.type)
      {
        case MaterialProperty.PropType.Color:
          to.colorValue = from.colorValue;
          return;
        case MaterialProperty.PropType.Float:
          to.floatValue = from.floatValue;
          return;
        case MaterialProperty.PropType.Vector:
          to.vector4Value = from.vectorValue;
          return;
        case MaterialProperty.PropType.Range:
          to.floatValue = from.floatValue;
          return;
        case MaterialProperty.PropType.Int:
          to.intValue = from.intValue;
          return;
        case MaterialProperty.PropType.Texture when to.propertyType == SerializedPropertyType.Generic:
          to.FindPropertyRelative(nameof(TextureScaleOffset.ScaleOffset)).vector4Value = from.textureScaleAndOffset;
          to.FindPropertyRelative(nameof(TextureScaleOffset.Texture)).objectReferenceValue = from.textureValue;
          return;
        case MaterialProperty.PropType.Texture when to.propertyType == SerializedPropertyType.ObjectReference:
          to.objectReferenceValue = from.textureValue;
          return;
        default:
          Debug.LogWarning($"WEIRD TYPES {to.type} {from.type}");
          return;
      }
    }

    public static void CopyProperty(MaterialProperty to, SerializedProperty from)
    {
      switch (to.type)
      {
        case MaterialProperty.PropType.Color:
          to.colorValue = from.colorValue;
          return;
        case MaterialProperty.PropType.Float:
          to.floatValue = from.floatValue;
          return;
        case MaterialProperty.PropType.Vector:
          to.vectorValue = from.vector4Value;
          return;
        case MaterialProperty.PropType.Range:
          to.floatValue = from.floatValue;
          return;
        case MaterialProperty.PropType.Int:
          to.intValue = from.intValue;
          return;
        case MaterialProperty.PropType.Texture when from.propertyType == SerializedPropertyType.Generic:
          to.textureScaleAndOffset = from.FindPropertyRelative(nameof(TextureScaleOffset.ScaleOffset)).vector4Value;
          to.textureValue = from.FindPropertyRelative(nameof(TextureScaleOffset.Texture)).objectReferenceValue as Texture;
          return;
        case MaterialProperty.PropType.Texture when from.propertyType == SerializedPropertyType.ObjectReference:
          to.textureValue = from.objectReferenceValue as Texture;
          return;
        default:
          Debug.LogWarning("WEIRD TYPES");
          return;
      }
    }
  }
}