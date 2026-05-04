using UnityEngine;
using UnityEngine.UI;

namespace Plugins.Animate_UI_Materials
{
  /// <summary>
  /// Simply replaces a Graphic component's material
  /// Does not create or modify materials
  /// Useful for debugging, or switching the material of a Graphic that does not offer this option in some context
  /// </summary>
  [ExecuteAlways]
  [AddComponentMenu("UI/Animate UI Material/GraphicMaterialReplacer")]
  public class GraphicMaterialReplacer : MonoBehaviour, IMaterialModifier
  {
    [SerializeField] Material material;

    public Material Material
    {
      get => material;
      set {
        material = value;
        SetMaterialDirty();
      }
    }

    /// <summary>
    /// Request Graphic to regenerate materials
    /// </summary>
    void SetMaterialDirty()
    {
      if (TryGetComponent(out Graphic graphic))
      {
        graphic.SetMaterialDirty();
      }
    }

    // On enable and disable, update the target graphic
    void OnEnable() => SetMaterialDirty();

    void OnDisable() => SetMaterialDirty();

    /// <summary>
    /// From IMaterialModifier
    /// Receives the current material before display, and returns another material if enabled
    /// Here, simply sends the "material" field if enabled
    /// </summary>
    /// <param name="baseMaterial"></param>
    /// <returns>A new material object</returns>
    public Material GetModifiedMaterial(Material baseMaterial)
    {
      // Return the base material if invalid or if this component is disabled
      if (!enabled || baseMaterial == null) return baseMaterial;
      return material;
    }
  }
}