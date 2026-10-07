using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Plugins.Animate_UI_Materials
{
  /// <summary>
  /// Map source materials to their current override
  /// </summary>
  public static class  MaterialOverrideRegistry
  {
    static readonly ConditionalWeakTable<Material, Material> _overrides = new();

    /// <summary>
    /// Fired on set and clear with null when cleared
    /// </summary>
    public static event Action<Material, Material> OverrideChanged;

    public static Material Get(Material source)
    {
      if (!source) return null;
      if (_overrides.TryGetValue(source, out Material modified) && modified) return modified;
      return null;
    }

    /// <summary>
    /// Pass null to clear the override
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
