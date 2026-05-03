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

      // No publisher anywhere uses this material — Mirror has nothing to reflect.
      if (totalMatching == 0)
      {
        EditorGUILayout.HelpBox(
          "No active GraphicMaterialOverride uses this material. " +
          "The Mirror has nothing to reflect and will display the unmodified base material.",
          MessageType.Info);
        return;
      }

      // A publisher exists, but at a different stencil-Mask depth. MaskableGraphic wraps the
      // material per-depth via StencilMaterial.Add, producing distinct Material references for
      // different depths, so the runtime lookup can never match. RectMask2D is unaffected since
      // it clips at CanvasRenderer level without touching the material.
      if (sameDepthMatching == 0)
      {
        EditorGUILayout.HelpBox(
          "The matching GraphicMaterialOverride is inside a different stencil Mask hierarchy. " +
          "For the Mirror to share its material, both Graphics must be inside the same Mask, " +
          "or both outside any Mask. RectMask2D doesn't have this restriction.",
          MessageType.Error);
        return;
      }

      // More than one same-depth publisher claims this material — runtime registry is
      // last-writer-wins, so which one drives the Mirror is unpredictable.
      if (sameDepthMatching > 1)
      {
        EditorGUILayout.HelpBox(
          "Multiple active GraphicMaterialOverride components share this material at this Mask depth. " +
          "The Mirror reflects whichever one rendered most recently — assignment is unstable.",
          MessageType.Warning);
        return;
      }

      // Exactly one same-depth publisher. Mirror should be reflecting it; show a clickable
      // read-only object field so the user can ping/select it from the inspector.
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
