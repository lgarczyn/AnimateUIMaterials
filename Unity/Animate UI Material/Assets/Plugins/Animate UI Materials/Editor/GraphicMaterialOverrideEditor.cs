using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Plugins.Animate_UI_Materials.Editor
{
  using PropertyType = ShaderUtil.ShaderPropertyType;

  [CustomEditor(typeof(GraphicMaterialOverride), true)]
  public class GraphicMaterialOverrideEditor : UnityEditor.Editor
  {
    Vector2 _scrollPosition;

    // Get all the 
    static List<IMaterialPropertyModifier> GetPropertyModifiers(Transform parent)
    {
      List<IMaterialPropertyModifier> modifiers = new();
      List<IMaterialPropertyModifier> modifiersInOneChild = new();

      foreach (Transform child in parent)
      {
        child.GetComponents(modifiersInOneChild);
        modifiers.AddRange(modifiersInOneChild);
      }

      return modifiers;
    }

    /// <summary>
    ///   Override the reset context menu to implement the reset function
    ///   Needed instead of "MonoBehavior.Reset" on GraphicMaterialOverride because we need to record an Undo
    /// </summary>
    [MenuItem("CONTEXT/GraphicMaterialOverride/Reset")]
    static void ResetMaterialModifiers(MenuCommand b)
    {
      GraphicMaterialOverride materialOverride = (GraphicMaterialOverride)b.context;
      if (!materialOverride) return;

      List<IMaterialPropertyModifier> modifiers = materialOverride.GetModifiers().ToList();

      Object[] modifiersAsObjects = modifiers.Select(m => (Object)m).ToArray();
      Undo.RecordObjects(modifiersAsObjects, "Reset material modifiers");

      foreach (IMaterialPropertyModifier modifier in modifiers)
      {
        modifier.ResetPropertyToDefault();
        PrefabUtility.RecordPrefabInstancePropertyModifications((Object)modifier);
      }
    }

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      // Get the materialOverride component
      GraphicMaterialOverride materialOverride = (GraphicMaterialOverride)target;

      if (materialOverride.GetComponent<Graphic>() == null)
        EditorGUILayout.HelpBox("Cannot find any sibling UI element. Add a UI element to use this component",
          MessageType.Warning);
      else if (GetTargetMaterial() == null)
        EditorGUILayout.HelpBox("Cannot find any material. Add a material to the UI element to use this component",
          MessageType.Warning);

      // Get the active modifiers
      List<IMaterialPropertyModifier> modifiers = materialOverride.GetModifiers(true).ToList();
      // Display the current modifier values
      DisplayModifiers(modifiers);
      // Use a popup to create new modifiers
      ShaderPropertyInfo newProperty = DisplayCreationPopup(modifiers);

      if (newProperty != null)
        CreateNewModifier(
          materialOverride.transform,
          GetTargetMaterial(),
          newProperty);
    }

    /// <summary>
    ///   Display all added modifiers and their value
    /// </summary>
    /// <param name="modifiers"></param>
    void DisplayModifiers(List<IMaterialPropertyModifier> modifiers)
    {
      EditorGUILayout.BeginScrollView(_scrollPosition);

      EditorGUILayout.LabelField("Modifiers");

      if (modifiers.Count == 0)
        EditorGUILayout.HelpBox("Select a value from the dropdown to add a property modifier", MessageType.Info);

      // Change the label width to allow a float slider with a small width
      EditorGUIUtility.labelWidth = 16f;

      // Draw every active modifiers
      foreach (IMaterialPropertyModifier modifier in modifiers)
      {
        // Start the horizontal group
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
        DrawModifierToggle(modifier);
        DrawModifierReadOnlyValues(modifier);
        DrawModifierValue(modifier);
        EditorGUILayout.EndHorizontal();
        DrawModifierContextMenu(modifier);
      }

      EditorGUILayout.EndScrollView();
      // Reset the label width
      EditorGUIUtility.labelWidth = 0;
    }

    void DrawModifierContextMenu(IMaterialPropertyModifier modifier)
    {
      if (Event.current.type == EventType.MouseDown
          && Event.current.button == 1
          && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
      {
        MonoBehaviour modifierComponent = (MonoBehaviour)modifier;
        GenericMenu menu = new();
        if (modifierComponent.isActiveAndEnabled)
        {
          menu.AddItem(new GUIContent("Disable"), false, () => ModifierSetActive(modifierComponent, false));
        }
        else
        {
          menu.AddItem(new GUIContent("Enable"), false, () => ModifierSetActive(modifierComponent, true));
        }
        menu.AddItem(new GUIContent("Reset"), false, () => ResetModifier(modifierComponent));
        menu.AddItem(new GUIContent("Delete"), false, () => DeleteModifier(modifierComponent));
        menu.ShowAsContext();
        Event.current.Use();
      }
    }

    void ResetModifier(MonoBehaviour modifier)
    {
      IMaterialPropertyModifier modifierInterface = (IMaterialPropertyModifier)modifier;
      Undo.RecordObject(modifier, "Reset modifier component");

      modifierInterface.ResetPropertyToDefault();

      PrefabUtility.RecordPrefabInstancePropertyModifications(modifier);
    }

    void DeleteModifier(MonoBehaviour modifier)
    {
      Undo.DestroyObjectImmediate(modifier.gameObject);
    }

    void ModifierSetActive(MonoBehaviour modifier, bool isActive)
    {
      // Make sure any modifications are properly propagated to unity
      Undo.RecordObjects(new Object[] { modifier, modifier.gameObject },
        "Toggled modifier component");
      // If enabling, set the component and GameObject as active
      if (isActive)
      {
        modifier.enabled = true;
        modifier.gameObject.SetActive(true);
      }
      // If disabling, disable the GameObject only
      else
      {
        modifier.gameObject.SetActive(false);
      }

      PrefabUtility.RecordPrefabInstancePropertyModifications(modifier);
      PrefabUtility.RecordPrefabInstancePropertyModifications(modifier.gameObject);
    }

    /// <summary>
    ///   Draw a toggle to enable or disable the target modifier component
    /// </summary>
    /// <param name="modifier">The modifier component</param>
    void DrawModifierToggle(IMaterialPropertyModifier modifier)
    {
      MonoBehaviour modifierComponent = (MonoBehaviour)modifier;
      // Start checking for changes
      EditorGUI.BeginChangeCheck();
      // Draw the toggle with limited width
      bool isActive = EditorGUILayout.Toggle(modifierComponent.isActiveAndEnabled, GUILayout.Width(16f));
      // If changes happened
      if (EditorGUI.EndChangeCheck()) ModifierSetActive(modifierComponent, isActive);
    }

    /// <summary>
    ///   Draw the reference to the modifier and the property name
    /// </summary>
    /// <param name="modifier">The target property modifier</param>
    void DrawModifierReadOnlyValues(IMaterialPropertyModifier modifier)
    {
      MonoBehaviour modifierComponent = (MonoBehaviour)modifier;
      // Disable read-only fields, as they should not be modified here
      EditorGUI.BeginDisabledGroup(true);
      // Create a "link" field to the modifier object
      EditorGUILayout.ObjectField(modifierComponent, typeof(IMaterialPropertyModifier), true);
      // Display the modifier property name
      EditorGUILayout.TextField(modifier.PropertyName);
      // Stop the disabled group
      EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    ///   Draw the value field from the property modifier
    /// </summary>
    /// <param name="modifier">The target IMaterialPropertyModifier</param>
    void DrawModifierValue(IMaterialPropertyModifier modifier)
    {
      MonoBehaviour modifierComponent = (MonoBehaviour)modifier;
      // Add change checks to the property field
      EditorGUI.BeginChangeCheck();
      // Create a serialized object on the modifier, to display it properly
      SerializedObject obj = new(modifierComponent);
      // For floats, add a label to allow "sliding" the cursor
      string propertyLabel = modifier.GetPropertyType() == PropertyType.Float ? "⇔" : "";
      // Get the serialized property
      SerializedProperty property = obj.FindProperty("propertyValue");
      // If property is of type range, display a custom drawer
      if (modifier.GetPropertyType() == PropertyType.Range) DrawModifierRange(modifier, property);
      // Otherwise, just use the property field
      else EditorGUILayout.PropertyField(property, new GUIContent(propertyLabel));
      // If any change was applied
      if (EditorGUI.EndChangeCheck())
      {
        // Record an undo
        Undo.RecordObject(modifierComponent, $"Modified property override {modifier.PropertyName}");
        // If we are in a prefab, ensure unity knows about the modification
        PrefabUtility.RecordPrefabInstancePropertyModifications(modifierComponent);
        // Apply the modified property
        obj.ApplyModifiedProperties();
      }
    }

    /// <summary>
    ///   Retrieve shader property information and try to draw a range field
    /// </summary>
    /// <param name="modifier">The target IMaterialPropertyModifier</param>
    /// <param name="property">The serialized property of the value</param>
    void DrawModifierRange(IMaterialPropertyModifier modifier, SerializedProperty property)
    {
      Material mat = GetTargetMaterial();
      string propName = modifier.PropertyName;
      int index = ShaderPropertyInfo.GetMaterialProperties(mat)
        .Find(p => p.name == propName)
        ?.index ?? -1;

      DrawFloatPropertyAsRange(mat, index, property, new GUIContent(""));
    }

    /// <summary>
    ///   Try to draw a range field for a shader property
    ///   If the information cannot be found, draw a float property
    /// </summary>
    /// <param name="material">The material holding the shader property</param>
    /// <param name="propertyIndex">The index of the shader property</param>
    /// <param name="property">The serialized property in the modifier</param>
    /// <param name="label">The label of the property</param>
    public static void DrawFloatPropertyAsRange(
      Material material,
      int propertyIndex,
      SerializedProperty property,
      GUIContent label)
    {
      if (!material || propertyIndex <= 0)
      {
        EditorGUILayout.PropertyField(property);
        return;
      }

      Shader shader = material.shader;
      float min = ShaderUtil.GetRangeLimits(shader, propertyIndex, 1);
      float max = ShaderUtil.GetRangeLimits(shader, propertyIndex, 2);

      EditorGUILayout.Slider(property, min, max, label);
    }

    /// <summary>
    ///   Display a dropdown to select a modifier
    ///   Filters out modifiers that are already added
    /// </summary>
    /// <param name="modifiers"></param>
    /// <returns></returns>
    ShaderPropertyInfo DisplayCreationPopup(List<IMaterialPropertyModifier> modifiers)
    {
      // Create a set to filter out modifiers that are already added
      HashSet<string> namesAlreadyUsed = modifiers
        .Select(p => p.PropertyName)
        .ToHashSet();

      // Display a creation popup
      Material material = GetTargetMaterial();
      List<ShaderPropertyInfo> properties = ShaderPropertyInfo.GetMaterialProperties(material)
        .Where(p => !namesAlreadyUsed.Contains(p.name))
        .ToList();

      string[] propertyNames = properties.Select(p => p.name).ToArray();
      int selectedIndex = EditorGUILayout.Popup(new GUIContent("Add Override"), -1, propertyNames);

      if (selectedIndex >= 0) return properties[selectedIndex];
      return null;
    }

    /// <summary>
    ///   Create a new GraphicPropertyOverride in a new child GameObject
    /// </summary>
    /// <param name="parent">The transform of the GraphicMaterialOverride</param>
    /// <param name="material">The material to get the default value from</param>
    /// <param name="propertyInfo">The property to override</param>
    /// <exception cref="ArgumentOutOfRangeException">thrown when ShaderPropertyType is invalid</exception>
    void CreateNewModifier(Transform parent, Material material, ShaderPropertyInfo propertyInfo)
    {
      GameObject child = new();
      child.name = $"{propertyInfo.name} Override";
      child.transform.parent = parent;

      Undo.RegisterCreatedObjectUndo(child, $"Added override for property ${propertyInfo.name}");

      GraphicPropertyOverride propertyOverride;

      // For every possible property type
      switch (propertyInfo.type)
      {
        case PropertyType.Color:
          // Add the appropriate component
          propertyOverride = child.AddComponent<GraphicPropertyOverrideColor>();
          // Set the override value to the current value of the material
          ((GraphicPropertyOverrideColor)propertyOverride).PropertyValue = material.GetColor(propertyInfo.name);
          break;
        case PropertyType.Vector:
          propertyOverride = child.AddComponent<GraphicPropertyOverrideVector>();
          ((GraphicPropertyOverrideVector)propertyOverride).PropertyValue = material.GetVector(propertyInfo.name);
          break;
        case PropertyType.Float:
          propertyOverride = child.AddComponent<GraphicPropertyOverrideFloat>();
          ((GraphicPropertyOverrideFloat)propertyOverride).PropertyValue = material.GetFloat(propertyInfo.name);
          break;
        case PropertyType.Range:
          propertyOverride = child.AddComponent<GraphicPropertyOverrideRange>();
          ((GraphicPropertyOverrideRange)propertyOverride).PropertyValue = material.GetFloat(propertyInfo.name);
          break;
        case PropertyType.TexEnv:
          propertyOverride = child.AddComponent<GraphicPropertyOverrideTexture>();
          ((GraphicPropertyOverrideTexture)propertyOverride).PropertyValue = material.GetTexture(propertyInfo.name);
          break;
        case PropertyType.Int:
          propertyOverride = child.AddComponent<GraphicPropertyOverrideInt>();
          ((GraphicPropertyOverrideInt)propertyOverride).PropertyValue = material.GetInteger(propertyInfo.name);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      propertyOverride.PropertyName = propertyInfo.name;
    }

    /// <summary>
    ///   Try to get the material from the Graphic component
    /// </summary>
    /// <returns></returns>
    Material GetTargetMaterial()
    {
      GraphicMaterialOverride graphicMaterialOverride = (GraphicMaterialOverride)target;

      if (!graphicMaterialOverride.TryGetComponent(out Graphic graphic)) return null;

      return graphic.material;
    }
  }
}