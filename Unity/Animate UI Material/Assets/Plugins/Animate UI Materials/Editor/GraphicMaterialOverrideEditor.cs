using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Plugins.Animate_UI_Materials.EditorExtensions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Plugins.Animate_UI_Materials.Editor
{
  using PropertyType = ShaderUtil.ShaderPropertyType;

  [CustomEditor(typeof(GraphicMaterialOverride), true)]
  public class GraphicMaterialOverrideEditor : UnityEditor.Editor
  {
    /// <summary>
    ///   The scroll position in the modifiers ScrollView
    ///   Usually not needed, but good to have
    /// </summary>
    Vector2 _scrollPosition;

    /// <summary>
    /// A fake material used to create an inspector
    /// </summary>
    Material _editorMaterial;

    Object[] _editorMaterialArray;

    /// <summary>
    /// The editor of the fake material
    /// </summary>
    MaterialEditor _editorMaterialEditor;

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

    /// <summary>
    /// Ask the Graphic component to reload the modified material
    /// </summary>
    [MenuItem("CONTEXT/GraphicMaterialOverride/Reload Source Material")]
    static void ReloadSourceMaterial(MenuCommand b)
    {
      if (b.context is not GraphicMaterialOverride materialOverride) return;
      materialOverride.SetMaterialDirty();
      EditorUtility.SetDirty(materialOverride);
    }

    [MenuItem("CONTEXT/MonoBehaviour/Bake Material Variant", true)]
    static bool BakeMaterialVariantValidator(MenuCommand b)
    {
      return b.context is IMaterialModifier;
    }

    /// <summary>
    /// Ask the Graphic component to reload the modified material
    /// </summary>
    [MenuItem("CONTEXT/MonoBehaviour/Bake Material Variant")]
    static void BakeMaterialVariant(MenuCommand b)
    {
      if (b.context is not IMaterialModifier) return;
      if (b.context is not Component materialModifier) return;

      if (materialModifier.TryGetComponent(out Graphic materialSource) == false)
      {
        Debug.LogWarning("Cannot find associated Graphic");
        return;
      }

      Material original = materialSource.material;
      Material modified = materialSource.materialForRendering;

      Material asset = new(modified);

  #if UNITY_2022_1_OR_NEWER && UNITY_EDITOR
        asset.parent = original;
  #endif
      asset.hideFlags = HideFlags.None;

      string path = GetMaterialVariantPath(original);

      AssetDatabase.CreateAsset(asset, path);
      EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Material>(path));
    }

    static string GetMaterialVariantPath(Material original)
    {
      string path = null;
      string name = original ? original.name : "Material";

  #if UNITY_2022_1_OR_NEWER && UNITY_EDITOR
        {

          Material current = original;
          while (path == null && current)
          {
            path = AssetDatabase.GetAssetPath(current);
            current = current.parent;
          }
        }
  #else
      if (original != null) path = AssetDatabase.GetAssetPath(original);
  #endif
      path = Path.GetDirectoryName(path);
      path ??= Application.dataPath;

      path += $"/{name} Override.asset";

      return AssetDatabase.GenerateUniqueAssetPath(path);
    }

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      // Get the materialOverride component
      GraphicMaterialOverride materialOverride = (GraphicMaterialOverride)target;

      if (materialOverride.GetComponent<Graphic>() == null)
        EditorGUILayout.HelpBox(
          "Cannot find any sibling UI element. Add a UI element to use this component",
          MessageType.Warning);
      else if (GetTargetMaterial() == null)
        EditorGUILayout.HelpBox(
          "Cannot find any material. Add a material to the UI element to use this component",
          MessageType.Warning);

      Material baseMaterial = GetTargetMaterial();
      if (baseMaterial == null) return;

      if (!_editorMaterial)
      {
        _editorMaterial = materialOverride.GetEditorMaterial(GetTargetMaterial());
        _editorMaterialArray = new Object[] { _editorMaterial };
      }

      InternalEditorUtility.SetIsInspectorExpanded(_editorMaterial, true);

      if (!_editorMaterialEditor || _editorMaterialEditor.target != _editorMaterial)
        _editorMaterialEditor = CreateEditor(_editorMaterial) as MaterialEditor;

      var properties = ShaderPropertyInfo.GetMaterialProperties(baseMaterial);
      var names = properties.Select(p => p.name).ToList();

      // Get the active modifiers
      List<IMaterialPropertyModifier> modifiers = materialOverride
                                                 .GetModifiers(true)
                                                 .OrderBy(m => names.IndexOf(m.PropertyName))
                                                 .ToList();

      // Display the current modifier values
      DisplayModifiers(modifiers);
      // Use a popup to create new modifiers

      if (DisplayCreationPopup(modifiers, properties) is { } toCreate)
        CreateNewModifier(materialOverride.transform, toCreate);
    }

    /// <summary>
    ///   Display all added modifiers and their value
    /// </summary>
    /// <param name="modifiers"></param>
    void DisplayModifiers(List<IMaterialPropertyModifier> modifiers)
    {
      EditorGUILayout.LabelField("Modifiers");

      if (modifiers.Count == 0)
        EditorGUILayout.HelpBox("Select a value from the dropdown to add a property modifier", MessageType.Info);

      using GUILayout.ScrollViewScope scrollViewScope = new(_scrollPosition);
      using GUILayout.HorizontalScope horizontalScope = new();

      ForEachParameterVertical(modifiers, DrawModifier);
    }

    /// <summary>
    /// Returns the heights of each modifiers as a list
    /// </summary>
    /// <param name="modifiers"> The modifiers to get the heights of</param>
    /// <returns></returns>
    List<float> GetModifiersHeights(List<IMaterialPropertyModifier> modifiers)
    {
      Object[] targetMat = _editorMaterialArray;

      return modifiers.Select(m => m.PropertyName)
                      .Select(n => MaterialEditor.GetMaterialProperty(targetMat, n))
                      .Select(_editorMaterialEditor.GetPropertyHeight)
                      .ToList();
    }

    /// <summary>
    ///   Begin a vertical group, and call a draw function on each modifier
    /// </summary>
    /// <param name="modifiers">The modifiers to draw</param>
    /// <param name="action">The draw function for a modifier property</param>
    static void ForEachParameterVertical(
      List<IMaterialPropertyModifier>   modifiers,
      Action<IMaterialPropertyModifier> action
    )
    {
      using EditorGUILayout.VerticalScope scope = new();
      foreach (IMaterialPropertyModifier modifier in modifiers)
      {
        using GUILayout.HorizontalScope hScope = new();
        action(modifier);
      }
    }

    void DrawModifier(IMaterialPropertyModifier modifier)
    {
      DrawModifierToggle(modifier);
      DrawModifierKebabMenu(modifier);
      DrawModifierValue(modifier);
    }

    // The cached style of the kebab menu button
    GUIStyle _kebabMenuStyle;

    /// <summary>
    ///   Draw a button that activate the context menu
    /// </summary>
    /// <param name="modifier"></param>
    void DrawModifierKebabMenu(IMaterialPropertyModifier modifier)
    {
      if (_kebabMenuStyle == null)
      {
        _kebabMenuStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
        // Force the height of the button
        _kebabMenuStyle.fixedHeight = EditorGUIUtility.singleLineHeight;
        _kebabMenuStyle.margin = new RectOffset(0, 0, 3, 0);
      }

      if (GUILayout.Button("", _kebabMenuStyle)) DrawModifierContextMenu(modifier);
    }

    /// <summary>
    ///   Draw a toggle to enable or disable the target modifier component
    /// </summary>
    /// <param name="modifier">The modifier component</param>
    void DrawModifierToggle(IMaterialPropertyModifier modifier)
    {
      GameObject targetObject = modifier.gameObject;
      SerializedObject targetSO = new(targetObject);
      SerializedProperty activeProp = targetSO.FindProperty("m_IsActive");
      EditorGUI.ChangeCheckScope scope = new();
      EditorGUILayout.PropertyField(activeProp, GUIContent.none, false, GUILayout.MaxWidth(16f));
      if (scope.changed) targetSO.ApplyModifiedProperties();
    }

    /// <summary>
    ///   Draw the value field from the property modifier
    /// </summary>
    /// <param name="modifier">The target IMaterialPropertyModifier</param>
    protected virtual void DrawModifierValue(IMaterialPropertyModifier modifier)
    {
      MonoBehaviour modifierComponent = (MonoBehaviour)modifier;
      // Add change checks to the property field
      EditorGUI.BeginChangeCheck();
      // Create a serialized object on the modifier, to display it properly
      SerializedObject obj = new(modifierComponent);
      // Get the serialized property
      SerializedProperty property = obj.FindProperty("propertyValue");

      try
      {
        EditorGUIUtility.fieldWidth = 64f;
        DrawMaterialProperty(modifier, property);
        EditorGUIUtility.fieldWidth = -1;
      }
      catch (ExitGUIException e)
      {
        throw;
      }
      // e is used for debugging purposes
#pragma warning disable CS0168 // Variable is declared but never used
      catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
      {
        // Put breakpoint here
        EditorGUIUtility.fieldWidth = -1;
        DrawFallbackProperty(modifier, property);
      }

      // If no change was applied, ignore storage
      if (!EditorGUI.EndChangeCheck()) return;
      // Set the serialized property from the current prop
      // Record an undo
      Undo.RecordObject(modifierComponent, $"Modified property override {modifier.PropertyName}");
      // If we are in a prefab, ensure unity knows about the modification
      PrefabUtility.RecordPrefabInstancePropertyModifications(modifierComponent);
      // Apply the modified property
      obj.ApplyModifiedProperties();
    }

    void DrawFallbackProperty(IMaterialPropertyModifier modifier, SerializedProperty property)
    {
      GUI.backgroundColor = new Color(1, 0.5f, 0);
      EditorGUILayout.PropertyField(property, new GUIContent(modifier.PropertyName));
      GUI.backgroundColor = Color.white;
    }

    FieldInfo _materialPropertyFlagsField =
      typeof(MaterialProperty).GetField("m_Flags", BindingFlags.NonPublic | BindingFlags.Instance);

    void DrawMaterialProperty(
      IMaterialPropertyModifier modifier,
      SerializedProperty        property
    )
    {
      // Get the actual Shader Property
      MaterialProperty materialProperty = MaterialEditor.GetMaterialProperty(_editorMaterialArray, modifier.PropertyName);

      MaterialProperty.PropFlags oldFlags = materialProperty.flags;
      MaterialProperty.PropFlags flags = oldFlags;

      // Hide the scale offset in the texture property drawer
      if (modifier is GraphicPropertyOverrideTexture or GraphicPropertyOverrideScaleAndOffset)
      {
        bool wantsScaleOffset = modifier is GraphicPropertyOverrideScaleAndOffset;

        flags &= ~MaterialProperty.PropFlags.NoScaleOffset;

        if (!wantsScaleOffset)
          flags |= MaterialProperty.PropFlags.NoScaleOffset;
      }

      flags &= ~MaterialProperty.PropFlags.PerRendererData;

      if (oldFlags != flags)
      {
        _materialPropertyFlagsField.SetValue(materialProperty, (int)flags);
      }

      // Asset correct property type
      SerializedMaterialPropertyUtility.AssertTypeEqual(property, materialProperty);
      // Set the buffer shader property to our current value
      SerializedMaterialPropertyUtility.CopyProperty(materialProperty, property);
      // Get the height needed to render
      float height = _editorMaterialEditor.GetPropertyHeight(materialProperty);

      // Get the control rect
      Rect rect = EditorGUILayout.GetControlRect(true, height);
      using EditorGUI.PropertyScope scope = new(rect, new GUIContent(modifier.DisplayName), property);

      // Set the animator colored backgrounds
      if (GraphicMaterialOverrideHelper.OverridePropertyColor(materialProperty, (Object)modifier, out Color background))
      {
        GUI.backgroundColor = background;
      }

      using EditorGUI.ChangeCheckScope changes = new();
      // Draw the property using the hidden editor
      _editorMaterialEditor.ShaderProperty(rect, materialProperty, scope.content, 0);

      // Reset the background color
      GUI.backgroundColor = Color.white;

      if (changes.changed)
      {
        // Place the result in the SerializedProperty
        SerializedMaterialPropertyUtility.CopyProperty(property, materialProperty);
      }
    }

    /// <summary>
    ///   Draw the context menu for one modifier
    /// </summary>
    /// <param name="modifier"></param>
    void DrawModifierContextMenu(IMaterialPropertyModifier modifier)
    {
      MonoBehaviour modifierComponent = (MonoBehaviour)modifier;
      GenericMenu menu = new();
      menu.AddItem(new GUIContent("Select"),      false, () => Selection.activeGameObject = modifierComponent.gameObject);
      menu.AddItem(new GUIContent("Set Default"), false, () => ResetModifier(modifier));
      if (modifierComponent.isActiveAndEnabled)
        menu.AddItem(new GUIContent("Disable"), false, () => ModifierSetActive(modifier, false));
      else
        menu.AddItem(new GUIContent("Enable"), false, () => ModifierSetActive(modifier, true));

      menu.AddItem(new GUIContent("Delete"), false, () => DeleteModifier(modifier));
      menu.ShowAsContext();
    }

    /// <summary>
    /// Reset a modifier object to the default material value and record an undo
    /// </summary>
    /// <param name="modifier"></param>
    void ResetModifier(IMaterialPropertyModifier modifier)
    {
      Undo.RecordObject(modifier as Object, "Reset modifier component");

      modifier.ResetPropertyToDefault();

      PrefabUtility.RecordPrefabInstancePropertyModifications(modifier as Object);
    }

    /// <summary>
    /// Delete the GameObject of a modifier and record an Undo
    /// </summary>
    /// <param name="modifier"></param>
    void DeleteModifier(IMaterialPropertyModifier modifier)
    {
      Undo.DestroyObjectImmediate(modifier.gameObject);
    }

    /// <summary>
    /// Set the active state of a modifier and its GameObject
    /// Records an undo
    /// </summary>
    /// <param name="modifier"></param>
    /// <param name="isActive"></param>
    void ModifierSetActive(IMaterialPropertyModifier modifier, bool isActive)
    {
      // Make sure any modifications are properly propagated to unity
      Undo.RecordObjects(
        new[] { modifier as Object, modifier.gameObject },
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

      PrefabUtility.RecordPrefabInstancePropertyModifications(modifier as Object);
      PrefabUtility.RecordPrefabInstancePropertyModifications(modifier.gameObject);
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
      Material           material,
      int                propertyIndex,
      SerializedProperty property,
      GUIContent         label
    )
    {
      if (!material || propertyIndex < 0)
      {
        EditorGUILayout.PropertyField(property);
        return;
      }

      Rect rect = GUILayoutUtility.GetRect(
        EditorGUIUtility.fieldWidth,
        EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + 110f,
        18f,
        18f);

      Shader shader = material.shader;
      float min = ShaderUtil.GetRangeLimits(shader, propertyIndex, 1);
      float max = ShaderUtil.GetRangeLimits(shader, propertyIndex, 2);

      using EditorGUI.PropertyScope scope = new(rect, label, property);
      EditorGUI.BeginChangeCheck();
      float newValue = EditorGUI.Slider(rect, "⇔", property.floatValue, min, max);
      // Only assign the value back if it was actually changed by the user.
      // Otherwise a single value will be assigned to all objects when multi-object editing,
      // even when the user didn't touch the control.
      if (EditorGUI.EndChangeCheck())
        property.floatValue = newValue;
    }

    /// <summary>
    ///   Draw a color field for a shader property, hdr if required
    ///   If the information cannot be found, draw a color property
    /// </summary>
    /// <param name="material">The material holding the shader property</param>
    /// <param name="property">The serialized property in the modifier</param>
    /// <param name="isHdr">If the property should be drawn as HDR</param>
    /// <param name="label">The label of the property</param>
    public static void DrawColorPropertyAsHdr(
      Material           material,
      SerializedProperty property,
      bool               isHdr,
      GUIContent         label
    )
    {
      if (!material)
      {
        EditorGUILayout.PropertyField(property);
        return;
      }

      property.colorValue = EditorGUILayout.ColorField(label, property.colorValue, true, true, isHdr);
    }

    static readonly (string, string)[] LowerPropertyStrings =
    {
      ("_StencilComp", "UI Hidden Properties/StencilComp"),
      ("_Stencil", "UI Hidden Properties/Stencil"),
      ("_StencilOp", "UI Hidden Properties/StencilOp"),
      ("_StencilWriteMask", "UI Hidden Properties/StencilWriteMask"),
      ("_StencilReadMask", "UI Hidden Properties/StencilReadMask"),
      ("_ColorMask", "UI Hidden Properties/ColorMask"),
      ("_UseUIAlphaClip", "UI Hidden Properties/UseUIAlphaClip"),
    };

    static string GetPropertyName(ShaderPropertyInfo prop)
    {
      foreach (var (lower, upper) in LowerPropertyStrings)
      {
        if (prop.name == lower) return upper;
      }

      return prop.name;
    }

    struct PropertyEntry
    {
      public string Name;
      public string DisplayName;
      public Type ComponentType;
    }

    /// <summary>
    ///   Display a dropdown to select a modifier
    ///   Filters out modifiers that are already added
    /// </summary>
    /// <returns></returns>
    PropertyEntry? DisplayCreationPopup(List<IMaterialPropertyModifier> modifiers, List<ShaderPropertyInfo> properties)
    {
      // Create a set to filter out modifiers that are already added
      HashSet<string> namesAlreadyUsed = modifiers
                                        .Select(p => p.DisplayName)
                                        .ToHashSet();

      List<PropertyEntry> entries = new();

      foreach (ShaderPropertyInfo info in properties)
      {
        if (!namesAlreadyUsed.Contains(info.name))
        {
          entries.Add(
            new PropertyEntry
            {
              Name = info.name,
              DisplayName = GetPropertyName(info),
              ComponentType = info.type switch
              {
                PropertyType.Color  => typeof(GraphicPropertyOverrideColor),
                PropertyType.Float  => typeof(GraphicPropertyOverrideFloat),
                PropertyType.Range  => typeof(GraphicPropertyOverrideRange),
                PropertyType.Vector => typeof(GraphicPropertyOverrideVector),
                PropertyType.TexEnv => typeof(GraphicPropertyOverrideTexture),
                _                   => throw new ArgumentOutOfRangeException(),
              },
            });
        }

        if (info.type == PropertyType.TexEnv)
        {
          string displayName = GetPropertyName(info) + " Scale Offset";
          if (!namesAlreadyUsed.Contains(displayName))
          {
            entries.Add(
              new PropertyEntry
              {
                Name = info.name,
                DisplayName = displayName,
                ComponentType = typeof(GraphicPropertyOverrideScaleAndOffset),
              });
          }
        }
      }

      string[] propertyNames = entries.Select(e => e.DisplayName).ToArray();

      int selectedIndex = EditorGUILayout.Popup(new GUIContent("Add Override"), -1, propertyNames);

      if (selectedIndex >= 0) return entries[selectedIndex];
      return null;
    }

    /// <summary>
    ///   Create a new GraphicPropertyOverride in a new child GameObject
    /// </summary>
    /// <param name="parent">The transform of the GraphicMaterialOverride</param>
    /// <param name="propertyInfo">The property to override</param>
    /// <exception cref="ArgumentOutOfRangeException">thrown when ShaderPropertyType is invalid</exception>
    void CreateNewModifier(Transform parent, PropertyEntry propertyInfo)
    {
      // Increment undo group
      Undo.IncrementCurrentGroup();
      GameObject child = new($"{propertyInfo.DisplayName} Override");

      Undo.RegisterCreatedObjectUndo(child, $"Added override GameObject");
      child.layer = parent.gameObject.layer;
      Undo.SetTransformParent(child.transform, parent, false, "Moved override GameObject");

      GraphicPropertyOverride propertyOverride =
        Undo.AddComponent(child, propertyInfo.ComponentType) as GraphicPropertyOverride;
      propertyOverride!.PropertyName = propertyInfo.Name;
      propertyOverride.ResetPropertyToDefault();

      Undo.RegisterCompleteObjectUndo(child, "Added override component");
      Undo.SetCurrentGroupName($"Override ${propertyInfo.DisplayName}");
    }

    /// <summary>
    ///   Try to get the material from the Graphic component
    /// </summary>
    /// <returns></returns>
    Material GetTargetMaterial()
    {
      GraphicMaterialOverride graphicMaterialOverride = (GraphicMaterialOverride)target;

      return graphicMaterialOverride.TryGetComponent(out Graphic graphic) ? graphic.material : null;
    }
  }
}