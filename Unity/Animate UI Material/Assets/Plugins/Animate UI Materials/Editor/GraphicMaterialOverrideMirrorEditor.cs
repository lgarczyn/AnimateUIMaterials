using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.Animate_UI_Materials.Editor
{
  [CustomEditor(typeof(GraphicMaterialOverrideMirror))]
  [CanEditMultipleObjects]
  public class GraphicMaterialOverrideMirrorEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      GraphicMaterialOverrideMirror mirror = (GraphicMaterialOverrideMirror)target;
      if (!mirror) return;
      if (!mirror.TryGetComponent(out Graphic graphic) || graphic.material == null) return;

      Material baseMaterial = graphic.material;
      int mirrorDepth = GetStencilDepth(mirror.transform);

      int totalMatching = 0;
      int sameDepthMatching = 0;
      GraphicMaterialOverride solePublisher = null;
      foreach (GraphicMaterialOverride pub in FindAllPublishers())
      {
        if (!pub.isActiveAndEnabled) continue;
        if (!pub.TryGetComponent(out Graphic pubGraphic)) continue;
        if (pubGraphic.material != baseMaterial) continue;
        totalMatching++;
        if (GetStencilDepth(pub.transform) == mirrorDepth)
        {
          sameDepthMatching++;
          if (solePublisher == null) solePublisher = pub;
        }
      }

      if (totalMatching == 0)
      {
        EditorGUILayout.HelpBox(
          "No active GraphicMaterialOverride uses this material. Showing the base material.",
          MessageType.Info);
        return;
      }

      // Reject other Mask depths, since StencilMaterial.Add copies per depth
      if (sameDepthMatching == 0)
      {
        EditorGUILayout.HelpBox(
          "The matching GraphicMaterialOverride is under a different Mask. " +
          "Put both under the same Mask, or neither. RectMask2D works either way.",
          MessageType.Error);
        return;
      }

      if (sameDepthMatching > 1)
      {
        EditorGUILayout.HelpBox(
          "Several active GraphicMaterialOverride use this material at this Mask depth. " +
          "The last one to render wins.",
          MessageType.Warning);
        return;
      }

      EditorGUI.BeginDisabledGroup(true);
      EditorGUILayout.ObjectField("Mirroring", solePublisher, typeof(GraphicMaterialOverride), true);
      EditorGUI.EndDisabledGroup();
    }

    static int GetStencilDepth(Transform t)
    {
      if (!t) return 0;
      Transform stopAfter = MaskUtilities.FindRootSortOverrideCanvas(t);
      return MaskUtilities.GetStencilDepth(t, stopAfter);
    }

    static GraphicMaterialOverride[] FindAllPublishers()
    {
#if UNITY_2023_1_OR_NEWER
      return Object.FindObjectsByType<GraphicMaterialOverride>(FindObjectsSortMode.None);
#else
      return Object.FindObjectsOfType<GraphicMaterialOverride>();
#endif
    }
  }
}
