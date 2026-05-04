using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Plugins.Animate_UI_Materials
{
  /// <summary>
  /// Central registry mapping source materials to their currently-published override versions.
  /// BufferedMaterialModifier writes here when its buffered material is created or destroyed;
  /// GraphicMaterialOverrideMirror subscribes to OverrideChanged to track the override for its
  /// own Graphic.material. Keys are weakly held so a destroyed source material auto-evicts.
  /// </summary>
  public static class  MaterialOverrideRegistry
  {
    static readonly ConditionalWeakTable<Material, Material> _overrides = new();

    /// <summary>
    /// Fires whenever an override is set or cleared for a source material.
    /// Payload: <c>(source, overrideMaterial)</c>. <c>overrideMaterial</c> is null when cleared.
    /// </summary>
    public static event Action<Material, Material> OverrideChanged;

    /// <summary>
    /// Look up the current override for <paramref name="source"/>. Returns null if no override
    /// is registered or the previously-registered override has been destroyed.
    /// </summary>
    public static Material Get(Material source)
    {
      if (!source) return null;
      if (_overrides.TryGetValue(source, out Material modified) && modified) return modified;
      return null;
    }

    /// <summary>
    /// Register <paramref name="overrideMaterial"/> as the current override for
    /// <paramref name="source"/>, or clear it by passing null. Last writer wins. Always fires
    /// OverrideChanged so subscribers can re-evaluate.
    /// </summary>
    public static void Set(Material source, Material overrideMaterial)
    {
      if (!source) return;
      _overrides.Remove(source);
      if (overrideMaterial) _overrides.Add(source, overrideMaterial);
      OverrideChanged?.Invoke(source, overrideMaterial);
    }
  }
}
