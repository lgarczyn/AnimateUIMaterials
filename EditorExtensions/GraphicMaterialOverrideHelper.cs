using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugins.Animate_UI_Materials.EditorExtensions
{
  public static class GraphicMaterialOverrideHelper
  {
    public static bool OverridePropertyColor(
      MaterialProperty materialProp,
      Object           target,
      out Color        color)
    {
      List<string> stringList = new();
      string str = "propertyValue";
      if (materialProp.type == MaterialProperty.PropType.Texture)
      {
        stringList.Add(str);
        stringList.Add(str + "_ST.x");
        stringList.Add(str + "_ST.y");
        stringList.Add(str + "_ST.z");
        stringList.Add(str + "_ST.w");
      }
      else if (materialProp.type == MaterialProperty.PropType.Color)
      {
        stringList.Add(str + ".r");
        stringList.Add(str + ".g");
        stringList.Add(str + ".b");
        stringList.Add(str + ".a");
      }
      else if (materialProp.type == MaterialProperty.PropType.Vector)
      {
        stringList.Add(str + ".x");
        stringList.Add(str + ".y");
        stringList.Add(str + ".z");
        stringList.Add(str + ".w");
      }
      else
      {
        stringList.Add(str);
      }

      bool found = false;
      foreach (string propName in stringList)
      {
        if (AnimationMode.IsPropertyAnimated(target, propName))
        {
          found = true;
          break;
        }
      }

      if (!found)
      {
        color = Color.white;
        return false;
      }

      if (AnimationMode.InAnimationRecording())
      {
        color = AnimationMode.recordedPropertyColor;
        return true;
      }

      foreach (string propName in stringList)
      {
        if (AnimationMode.IsPropertyCandidate(target, propName))
        {
          color = AnimationMode.candidatePropertyColor;
          return true;
        }
      }

      color = AnimationMode.animatedPropertyColor;
      return true;
    }
  }
}