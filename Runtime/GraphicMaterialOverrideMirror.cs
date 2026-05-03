using UnityEngine;
using UnityEngine.UI;

namespace Plugins.Animate_UI_Materials
{
  /// <summary>
  /// Lets a Graphic share the modified material instance produced by another GraphicMaterialOverride
  /// elsewhere in the project, as long as both Graphics use the same base material. The source is
  /// looked up by base material — no direct reference, no channel asset — so the link survives across
  /// prefabs and additively-loaded scenes. To differentiate two elements that should NOT share, give
  /// each a distinct material variant (Material.parent in 2022.1+).
  /// </summary>
  /// <remarks>
  /// Falls back to the unmodified base material when no override is registered for our material,
  /// so a mirror pointing at a disabled or destroyed source does nothing.
  /// </remarks>
  [ExecuteAlways]
  [DisallowMultipleComponent]
  [AddComponentMenu("UI/Animate UI Material/GraphicMaterialOverrideMirror")]
  public class GraphicMaterialOverrideMirror : MonoBehaviour, IMaterialModifier
  {
    /// <summary>
    /// Set when OverrideChanged fires from inside the canvas graphic-rebuild loop, where
    /// CanvasUpdateRegistry refuses to enqueue further Graphics. Drained from
    /// Canvas.willRenderCanvases on the next frame, where m_PerformingGraphicUpdate is false.
    /// </summary>
    bool _pendingDirty;

    /// <summary>
    /// The override we last returned from GetModifiedMaterial. Used by the event handler to
    /// detect "the override I'm using just got cleared/changed" — a case the source-based
    /// filter alone misses, since the CanvasRenderer's current material is the override
    /// itself, not the source key the publisher registered under.
    /// </summary>
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

    /// <summary>
    /// An override was set or cleared somewhere in the project. If it's for our Graphic.material
    /// and its baked stencil ref is compatible with our mask depth, store it. Mismatched depths
    /// are silently rejected so the Graphic falls back to its base material instead of rendering
    /// nothing (which is what would happen if we cached an override with foreign stencil bits).
    /// SetMaterialDirty is short-circuited when we're already inside a graphic-rebuild loop —
    /// CanvasUpdateRegistry refuses re-entry there — and deferred to willRenderCanvases instead.
    /// </summary>
    void OnOverrideChanged(Material source, Material overrideMaterial)
    {
      if (!TryGetComponent(out Graphic g)) return;

      // Use the cached material the CanvasRenderer is currently drawing with — this is what
      // materialForRendering returned on the most recent rebuild, but reading it from the
      // renderer is a direct field load, not a fresh IMaterialModifier chain walk.
      Material rendering = g.canvasRenderer ? g.canvasRenderer.GetMaterial() : null;

      bool relevant =
        g.material == source                      // source matches our unwrapped material
        || rendering == source                    // we're rendering the source's wrapped variant
        || rendering == overrideMaterial          // we're already rendering this override
        || _lastReturnedOverride != null          // OR we were using SOME override that may now be invalid
           && (overrideMaterial == null || _lastReturnedOverride == overrideMaterial);
      if (!relevant) return;

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
