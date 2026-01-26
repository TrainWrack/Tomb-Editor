using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TombLib.LevelData.Properties
{
    /// <summary>
    /// Represents the type of a property
    /// </summary>
    public enum PropertyType
    {
        Integer,
        Float,
        Boolean,
        Dropdown,
        Checkbox
    }

    /// <summary>
    /// Base class for property definitions loaded from XML
    /// </summary>
    [XmlRoot("Property")]
    public class PropertyDefinition
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public PropertyType Type { get; set; }

        [XmlElement("Default")]
        public string Default { get; set; }

        [XmlElement("Description")]
        public string Description { get; set; }

        [XmlArray("Options")]
        [XmlArrayItem("Option")]
        public List<string> Options { get; set; }

        [XmlElement("Min")]
        public string Min { get; set; }

        [XmlElement("Max")]
        public string Max { get; set; }

        public PropertyDefinition()
        {
            Options = new List<string>();
        }
    }

    /// <summary>
    /// Root element for moveable properties XML
    /// </summary>
    [XmlRoot("MoveableProperties")]
    public class MoveablePropertySet
    {
        [XmlElement("Property")]
        public List<PropertyDefinition> Properties { get; set; }

        public MoveablePropertySet()
        {
            Properties = new List<PropertyDefinition>();
        }
    }

    /// <summary>
    /// Root element for static properties XML
    /// </summary>
    [XmlRoot("StaticProperties")]
    public class StaticPropertySet
    {
        [XmlElement("Property")]
        public List<PropertyDefinition> Properties { get; set; }

        public StaticPropertySet()
        {
            Properties = new List<PropertyDefinition>();
        }
    }

    /// <summary>
    /// Stores the actual property values for an instance
    /// </summary>
    [Serializable]
    public class PropertyValue
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public PropertyValue()
        {
        }

        public PropertyValue(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }

    /// <summary>
    /// Container for property values
    /// </summary>
    [Serializable]
    public class PropertyCollection
    {
        private Dictionary<string, object> _properties;

        public PropertyCollection()
        {
            _properties = new Dictionary<string, object>();
        }

        public void SetProperty(string name, object value)
        {
            if (_properties.ContainsKey(name))
                _properties[name] = value;
            else
                _properties.Add(name, value);
        }

        public object GetProperty(string name, object defaultValue = null)
        {
            if (_properties.ContainsKey(name))
                return _properties[name];
            return defaultValue;
        }

        public T GetProperty<T>(string name, T defaultValue = default(T))
        {
            if (_properties.ContainsKey(name))
            {
                try
                {
                    return (T)Convert.ChangeType(_properties[name], typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public bool HasProperty(string name)
        {
            return _properties.ContainsKey(name);
        }

        public void Clear()
        {
            _properties.Clear();
        }

        public Dictionary<string, object> GetAll()
        {
            return new Dictionary<string, object>(_properties);
        }

        public void SetAll(Dictionary<string, object> properties)
        {
            _properties = new Dictionary<string, object>(properties);
        }
    }
}
