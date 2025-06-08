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
    private static readonly int Stencil = Shader.PropertyToID("_Stencil");

    /// <summary>
    /// Hold the last modified material, to be re-used if possible
    /// </summary>
    Material _bufferedMaterial;

    /// <summary>
    /// Holds the base material used to create _bufferedMaterial
    /// </summary>
    Material _bufferedMaterialSource;

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

      if (!_bufferedMaterial || _bufferedMaterial.shader != baseMaterial.shader || baseMaterial != _bufferedMaterialSource)
      {
        DestroyBuffer();

        // Create a child material of the original
        _bufferedMaterial = CreateNewMaterial(baseMaterial, "OVERRIDE");
        _bufferedMaterialSource = baseMaterial;
      }

      _bufferedMaterial.CopyPropertiesFromMaterial(baseMaterial);
      ModifyMaterial(_bufferedMaterial);
      return _bufferedMaterial;
    }

    private int? GetStencilId(Material baseMaterial)
    {
      if (baseMaterial == null) return null;

      // Check if material has stencil prop to avoid warning
      if (!baseMaterial.HasInt(Stencil)) return null;
      int id = baseMaterial.GetInt(Stencil);
      return id > 0 ? id : null;
    }

    /// <summary>
    /// Create a new material variant of the base material.
    /// Tries to set parent value from the source material for prettier editing.
    /// Sets flags to avoid saving the material in assets.
    /// Used for creating new buffered material or for the fake editor screen.
    /// </summary>
    /// <param name="baseMaterial">IMaterialModifier argument</param>
    /// <param name="suffix">Suffix to append to the original material name</param>
    /// <returns></returns>
    private Material CreateNewMaterial(Material baseMaterial, string suffix)
    {
      Material realSource;
      // Try to retrieve real base Material
      if (TryGetComponent(out Graphic graphic))
      {
        realSource = graphic.material ? graphic.material : Canvas.GetDefaultCanvasMaterial();
      }
      else
      {
        Debug.LogWarning("No graphic found");
        realSource = baseMaterial;
      }

      // Add mask info to the Material
      if (GetStencilId(baseMaterial) is {} stencilId)
      {
        suffix = $"{suffix} MASKED {stencilId}";
      }

      Material modifiedMaterial = new (baseMaterial.shader)
      {
        // Set a new name, to warn about editor modifications
        name = $"{realSource.name} {suffix}",
        hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor,
      };
      // Set parent if supported
#if UNITY_2022_1_OR_NEWER && UNITY_EDITOR
      modifiedMaterial.parent = realSource;
#endif
      return modifiedMaterial;
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

    public Material GetEditorMaterial(Material baseMaterial)
    {
      // Create a child material of the original
      Material modifiedMaterial = CreateNewMaterial(baseMaterial, "EDITOR");
      modifiedMaterial.CopyPropertiesFromMaterial(baseMaterial);
      ModifyMaterial(modifiedMaterial);
      return modifiedMaterial;
    }
  }
}