namespace UnityEditor
{
 /// <summary>
 /// Broken implementation of MaterialEditor meant to be injected in a standard Editor
 /// Hacky, but only way to get custom shader editors to work with this packages
 /// </summary>
  public class CustomMaterialEditor: MaterialEditor
  {
    public override void OnInspectorGUI()
    {
      // Commented lines are those removed from the base implementation
      serializedObject.Update();
      // Remove most checks
      // this.CheckSetup();
      // this.DetectShaderEditorNeedsUpdate();
      // this.isVisible && (UnityEngine.Object) this.m_Shader != (UnityEngine.Object) null && !this.HasMultipleMixedShaderValues() && this.
      // Draw properties
      if (PropertiesGUI())
      {
        // Do not validate materials since we don't have access to their editor scripts
        // foreach (Material target in this.targets)
        // {
        //   if (this.m_CustomShaderGUI != null)
        //     this.m_CustomShaderGUI.ValidateMaterial(target);
        // }
        //
        PropertiesChanged();
      }
      // this.DetectTextureStackValidationIssues();
    }


  }
}