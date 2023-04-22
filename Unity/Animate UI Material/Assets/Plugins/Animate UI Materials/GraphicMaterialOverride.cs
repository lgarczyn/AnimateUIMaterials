using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.Animate_UI_Materials
{
  public class GraphicMaterialOverride : MonoBehaviour, IMaterialModifier
  {
    /// <summary>
    /// Recreate the modified material using each active IMaterialPropertyModifier on this GameObject or its children
    /// </summary>
    public void SetMaterialDirty()
    {
      if (TryGetComponent<Graphic>(out Graphic graphic))
      {
        graphic.SetMaterialDirty();
      }
    }

    /// <summary>
    /// A buffer list to accelerate GetComponents requests
    /// </summary>
    List<IMaterialPropertyModifier> _modifiers;

    /// <summary>
    /// Retrieves all enabled IMaterialPropertyModifiers belonging to direct children
    /// </summary>
    /// <returns>An iterator over all enabled modifiers, avoid storing this value</returns>
    public IEnumerable<IMaterialPropertyModifier> GetModifiers(bool includeInactive = false)
    {
      // Ensure the buffer list is available
      if (_modifiers == null) _modifiers = new List<IMaterialPropertyModifier>();

      // Load all IPropertyModifiers belonging the direct children of this GameObject
      foreach (Transform child in transform)
      {
        // skip this GameObject if disabled
        if (!child.gameObject.activeSelf && !includeInactive) continue;
        // disabled children will be ignored
        child.GetComponents<IMaterialPropertyModifier>(_modifiers);
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
    /// From IMaterialModifier
    /// Receives a material to be modified before display, and returns a new material
    /// </summary>
    /// <param name="baseMaterial"></param>
    /// <returns>A new material object</returns>
    public Material GetModifiedMaterial(Material baseMaterial)
    {
      // Return the base material if invalid or if this component is disabled
      if (!enabled || baseMaterial == null) return baseMaterial;

      // Create a child material of the original
      // This allows any later modifications to the original material to be preserved 
      Material modifiedMaterial = new Material(baseMaterial.shader);
      #if UNITY_2022_1_OR_NEWER
      modifiedMaterial.parent = baseMaterial;
      #endif
      modifiedMaterial.CopyPropertiesFromMaterial(baseMaterial);

      // Iterate over all active modifiers
      foreach (IMaterialPropertyModifier modifier in GetModifiers())
      {
        // Apply the property to the new material
        modifier.ApplyModifiedProperty(modifiedMaterial);
      }
      // Return the child material
      return modifiedMaterial;
    }
  }
}