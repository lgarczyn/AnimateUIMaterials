using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.Animate_UI_Materials
{
  [ExecuteAlways]
  [AddComponentMenu("UI/Animate UI Material/GraphicMaterialOverride")]
  public class GraphicMaterialOverride : BufferedMaterialModifier
  {
    /// <summary>
    /// Recreate the modified material using each active IMaterialPropertyModifier on this GameObject or its children
    /// </summary>
    public void SetMaterialDirty()
    {
      if (TryGetComponent(out Graphic graphic))
      {
        graphic.SetMaterialDirty();
      }
    }

    /// <summary>
    /// A buffer list to accelerate GetComponents requests
    /// </summary>
    [NonSerialized] List<IMaterialPropertyModifier> _modifiers;

    /// <summary>
    /// Retrieves all enabled IMaterialPropertyModifiers belonging to direct children
    /// </summary>
    /// <returns>An iterator over all enabled modifiers, avoid storing this value</returns>
    public IEnumerable<IMaterialPropertyModifier> GetModifiers(bool includeInactive = false)
    {
      // Ensure the buffer list is available
      _modifiers ??= new List<IMaterialPropertyModifier>();

      // Load all IPropertyModifiers belonging the direct children of this GameObject
      foreach (Transform child in transform)
      {
        // skip this GameObject if disabled
        if (!child.gameObject.activeSelf && !includeInactive) continue;
        // disabled children will be ignored
        child.GetComponents(_modifiers);
        // Call the children to apply their modified properties
        foreach (IMaterialPropertyModifier propertyModifier in _modifiers)
        {
          // Check if the modifier is enabled (skip if not)
          if (propertyModifier.enabled || includeInactive)
          {
            yield return propertyModifier;
          }
        }
      }

      // Ensure no ref is kept
      _modifiers.Clear();
    }

    // On enable and disable, update the target graphic
    void OnEnable() => SetMaterialDirty();

    void OnDisable() => SetMaterialDirty();

    /// <summary>
    /// Called by Graphic using the IMaterialModifier interface through the parent class
    /// Modifies the buffered material to match all children component specifications
    /// </summary>
    /// <param name="modifiedMaterial">A copy of the Graphic base material, buffered for reuse</param>
    protected override void ModifyMaterial(Material modifiedMaterial)
    {
      // Iterate over all active modifiers
      foreach (IMaterialPropertyModifier modifier in GetModifiers())
      {
        // Apply the property to the new material
        modifier.ApplyModifiedProperty(modifiedMaterial);
      }
    }
  }
}