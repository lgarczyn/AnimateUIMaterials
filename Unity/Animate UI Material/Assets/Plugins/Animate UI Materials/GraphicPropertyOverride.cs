using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Plugins.Animate_UI_Materials
{
  using PropertyType = ShaderUtil.ShaderPropertyType;

  /// <summary>
  ///   Used in combination with GraphicMaterialOverride to modify and animate shader properties
  ///   The base class is used for the shared Editor script, and reacting to OnValidate events
  /// </summary>
  [ExecuteInEditMode]
  public abstract class GraphicPropertyOverride : MonoBehaviour, IMaterialPropertyModifier
  {
    /// <summary>
    /// The name of the shader property, serialized
    /// </summary>
    [SerializeField] protected string propertyName;
    /// <summary>
    /// The id of the shader property
    /// </summary>
    /// <remarks> Should not be serialized, as it can change between game runs</remarks>
    protected int PropertyId;

    // Request a material update whenever the parent changes
    void OnEnable()
    {
      SetMaterialDirty();
    }

    void OnDisable()
    {
      SetMaterialDirty();
    }

    void OnValidate()
    {
      SetMaterialDirty();
    }

    public abstract PropertyType GetPropertyType();

    public void SetMaterialDirty()
    {
      PropertyId = Shader.PropertyToID(propertyName);
      if (!transform.parent) return;
      if (transform.parent.TryGetComponent(out GraphicMaterialOverride parent)) parent.SetMaterialDirty();
    }

    public abstract void ApplyModifiedProperty(Material material);

    public string PropertyName
    {
      get => propertyName;
      set
      {
        propertyName = value;
        SetMaterialDirty();
      }
    }
  }

  public abstract class GraphicPropertyOverride<T>: GraphicPropertyOverride
  {
    [SerializeField] protected T propertyValue;

    T _previousValue;

    void LateUpdate()
    {
      if (EqualityComparer<T>.Default.Equals(propertyValue, _previousValue) == false)
      {
        SetMaterialDirty();
      }
    }

    public T PropertyValue
    {
      get => propertyValue;
      set
      {
        propertyValue = value;
        SetMaterialDirty();
      }
    }
  }
}