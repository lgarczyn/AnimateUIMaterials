using UnityEngine;
using UnityEngine.UI;

namespace Plugins.Animate_UI_Materials
{
  /// <summary>
  /// Reuse the modified material of a GraphicMaterialOverride with the same base material
  /// </summary>
  [ExecuteAlways]
  [DisallowMultipleComponent]
  [AddComponentMenu("UI/Animate UI Material/GraphicMaterialOverrideMirror")]
  public class GraphicMaterialOverrideMirror : MonoBehaviour, IMaterialModifier
  {
    bool _pendingDirty;

    Material _lastReturnedOverride;

    public Material GetModifiedMaterial(Material baseMaterial)
    {
      if (!enabled || !baseMaterial) return baseMaterial;
      Material overrideMaterial = MaterialOverrideRegistry.Get(baseMaterial);
      _lastReturnedOverride = overrideMaterial;
      return overrideMaterial ? overrideMaterial : baseMaterial;
    }

    void OnEnable()
    {
      MaterialOverrideRegistry.OverrideChanged += OnOverrideChanged;
      Canvas.willRenderCanvases += DrainPending;
      SetMaterialDirty();
    }

    void OnDisable()
    {
      MaterialOverrideRegistry.OverrideChanged -= OnOverrideChanged;
      Canvas.willRenderCanvases -= DrainPending;
      _pendingDirty = false;
      SetMaterialDirty();
    }

    void OnOverrideChanged(Material source, Material overrideMaterial)
    {
      if (!TryGetComponent(out Graphic g)) return;

      // Read from renderer, since materialForRendering reruns all modifiers
      Material rendering = g.canvasRenderer ? g.canvasRenderer.GetMaterial() : null;

      bool relevant =
        g.material == source                      // source matches our unwrapped material
        || rendering == source                    // we're rendering the source's wrapped variant
        || rendering == overrideMaterial          // we're already rendering this override
        || _lastReturnedOverride != null          // OR we were using SOME override that may now be invalid
           && (overrideMaterial == null || _lastReturnedOverride == overrideMaterial);
      if (!relevant) return;

      // Defer, since CanvasUpdateRegistry ignores dirty calls during rebuild
      if (CanvasUpdateRegistry.IsRebuildingGraphics())
        _pendingDirty = true;
      else
        SetMaterialDirty();
    }

    void DrainPending()
    {
      if (!this) return;
      if (!_pendingDirty) return;
      _pendingDirty = false;
      SetMaterialDirty();
    }

    void SetMaterialDirty()
    {
      if (TryGetComponent(out Graphic g)) g.SetMaterialDirty();
    }
  }
}
