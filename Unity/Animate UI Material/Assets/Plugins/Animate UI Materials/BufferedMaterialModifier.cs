using UnityEngine;
using UnityEngine.UI;

namespace Plugins.Animate_UI_Materials
{
  /// <summary>
  /// Implements IMaterialModifier while avoiding the creation of too many garbage materials
  /// WARNING: will destroy the modified material if the source material changes shader or on its own destruction
  /// </summary>
  public abstract class BufferedMaterialModifier : MonoBehaviour, IMaterialModifier
  {
    /// <summary>
    /// Hold the last modified material, to be re-used if possible
    /// </summary>
    Material _bufferedMaterial;

    /// <summary>
    /// From IMaterialModifier
    /// Receives a material to be modified before display, and returns a new material
    /// Only called once per frame per Graphic if changed, as Graphic is well optimized
    /// </summary>
    /// <param name="baseMaterial"></param>
    /// <returns>A new material object, or the reset previous return value if possible</returns>
    public Material GetModifiedMaterial(Material baseMaterial)
    {
      // Return the base material if invalid or if this component is disabled
      if (!enabled || baseMaterial == null) return baseMaterial;

      if (!_bufferedMaterial || _bufferedMaterial.shader != baseMaterial.shader)
      {
        DestroyBuffer();

          // Create a child material of the original
        Material modifiedMaterial = new (baseMaterial.shader)
        {
          // Set a new name, to warn about editor modifications
          name = $"{baseMaterial.name} OVERRIDE",
          hideFlags = HideFlags.HideAndDontSave & HideFlags.NotEditable,
        };
#if UNITY_2022_1_OR_NEWER && UNITY_EDITOR
        modifiedMaterial.parent = baseMaterial;
#endif
        _bufferedMaterial = modifiedMaterial;
      }
      _bufferedMaterial.CopyPropertiesFromMaterial(baseMaterial);
      ModifyMaterial(_bufferedMaterial);
      return _bufferedMaterial;
    }

    void DestroyBuffer()
    {
      if (Application.isPlaying) Destroy(_bufferedMaterial);
      else DestroyImmediate(_bufferedMaterial);
    }

    /// <summary>
    /// Child class implement this class, modifying directly the buffered material
    /// </summary>
    /// <param name="modifiedMaterial"></param>
    protected abstract void ModifyMaterial(Material modifiedMaterial);

    /// <summary>
    /// Destroy the buffered material
    /// </summary>
    void OnDestroy()
    {
      DestroyBuffer();
    }
  }
}