# Property System Documentation

## Overview

The Tomb Editor property system provides a dynamic, XML-driven approach to managing properties for **Moveables** and **Statics** in Tomb Engine levels. This system allows level designers to define custom properties without modifying code, using WPF-based property editors.

## Architecture

### Key Components

1. **Property Models** (`TombLib/TombLib/LevelData/Properties/`)
   - `PropertyDefinition.cs` - Defines property metadata (name, type, default, options)
   - `PropertyManager.cs` - Manages loading and caching of property definitions
   - `PropertyCollection.cs` - Stores actual property values for instances

2. **WPF Editors** (`TombEditor/Windows/`)
   - `PropertyEditorWindow.xaml` - Single object property editor
   - `BatchPropertyEditorWindow.xaml` - Multi-object batch editor

3. **Integration Forms** (`TombEditor/Forms/TombEngine/`)
   - `FormMoveable.cs` - Wrapper for moveable property editing
   - `FormStatic.cs` - Wrapper for static property editing

### Data Flow

```
XML Property Files
        ↓
PropertyManager (loads and caches definitions)
        ↓
PropertyEditorWindow (creates dynamic controls)
        ↓
PropertyCollection (stores values in instance)
        ↓
Level File (serialized with level data)
```

## XML Property File Format

### Property Types

The system supports six property types:

| Type | Description | Example Controls |
|------|-------------|-----------------|
| `Integer` | Whole numbers | TextBox with validation |
| `Float` | Decimal numbers | TextBox with validation |
| `Boolean` | True/false values | Radio buttons (True/False) |
| `Dropdown` | Single selection from list | ComboBox |
| `Checkbox` | Multiple selections | CheckBox list |
| `Color` | Color selection | Color preview + hex input + picker |

### XML Schema

#### Moveable Properties

Moveable properties are stored in individual XML files under:
```
TombEditor/Resources/Properties/Moveables/{MoveableName}.xml
```

Example: `Enemy.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<MoveableProperties>
  <Property name="HP" type="Integer">
    <Default>100</Default>
    <Description>Hit Points - enemy health</Description>
    <Min>0</Min>
    <Max>32767</Max>
  </Property>
  
  <Property name="OCB" type="Integer">
    <Default>0</Default>
    <Description>Object Combination Block identifier</Description>
    <Min>-32768</Min>
    <Max>32767</Max>
  </Property>
  
  <Property name="DamageType" type="Dropdown">
    <Default>None</Default>
    <Description>Type of damage dealt by this moveable</Description>
    <Options>
      <Option>None</Option>
      <Option>Burn</Option>
      <Option>Freeze</Option>
      <Option>Electric</Option>
      <Option>Poison</Option>
    </Options>
  </Property>
  
  <Property name="AggroRange" type="Float">
    <Default>5.0</Default>
    <Description>Distance at which enemy becomes aggressive</Description>
    <Min>0.0</Min>
    <Max>50.0</Max>
  </Property>
  
  <Property name="IsBoss" type="Boolean">
    <Default>false</Default>
    <Description>Marks this enemy as a boss encounter</Description>
  </Property>
  
  <Property name="GlowColor" type="Color">
    <Default>#FF0000</Default>
    <Description>Color of the enemy's glow effect (hex format)</Description>
  </Property>
</MoveableProperties>
```

#### Static Properties

All statics share a single XML file:
```
TombEditor/Resources/Properties/StaticProperties.xml
```

Example:
```xml
<?xml version="1.0" encoding="utf-8"?>
<StaticProperties>
  <Property name="ShatterSound" type="Dropdown">
    <Default>None</Default>
    <Description>Sound played when object shatters</Description>
    <Options>
      <Option>None</Option>
      <Option>Stone</Option>
      <Option>Wood</Option>
      <Option>Glass</Option>
      <Option>Custom</Option>
    </Options>
  </Property>
  
  <Property name="HP" type="Integer">
    <Default>150</Default>
    <Description>Hit Points - object health</Description>
    <Min>0</Min>
    <Max>32767</Max>
  </Property>
  
  <Property name="Shatter" type="Boolean">
    <Default>false</Default>
    <Description>Whether the object can shatter when destroyed</Description>
  </Property>
  
  <Property name="TintColor" type="Color">
    <Default>#FFFFFF</Default>
    <Description>Color tint applied to the object</Description>
  </Property>
  
  <Property name="Color" type="Checkbox">
    <Description>Available color tints for this object</Description>
    <Options>
      <Option>Red</Option>
      <Option>Blue</Option>
      <Option>Green</Option>
      <Option>Yellow</Option>
    </Options>
  </Property>
</StaticProperties>
```

### XML Element Reference

#### Property Element

| Attribute | Required | Type | Description |
|-----------|----------|------|-------------|
| `name` | Yes | string | Unique identifier for the property |
| `type` | Yes | enum | One of: Integer, Float, Boolean, Dropdown, Checkbox, Color |

#### Child Elements

| Element | Required | Type | Description |
|---------|----------|------|-------------|
| `Default` | Yes | string | Default value for the property |
| `Description` | No | string | Human-readable description shown in UI |
| `Min` | No | string | Minimum value (Integer/Float only) |
| `Max` | No | string | Maximum value (Integer/Float only) |
| `Options` | Conditional | list | Required for Dropdown and Checkbox types |

#### Options Element

Used for Dropdown and Checkbox types:
```xml
<Options>
  <Option>Value1</Option>
  <Option>Value2</Option>
  <Option>Value3</Option>
</Options>
```

## Usage Guide

### For Level Designers

#### Editing Single Object Properties

1. Select an object in the level editor (Moveable or Static)
2. Right-click and choose "Properties" or press the properties hotkey
3. The WPF Property Editor window will open
4. Edit properties as needed:
   - Text fields for numbers
   - Radio buttons for boolean values
   - Dropdowns for single selections
   - Checkboxes for multiple selections
5. Click "OK" to apply changes or "Cancel" to discard
6. Use "Reset to Default" to restore all properties to defaults

#### Batch Editing Multiple Objects

1. Select multiple objects of the same type
2. Open the property editor
3. The Batch Property Editor will display:
   - `<Mixed Values>` for properties that differ between objects
   - Common values for properties that are the same
4. Only modified fields will be updated across all selected objects
5. Unchanged fields retain their individual values

### For Developers

#### Adding Custom Properties

1. **For Moveables**: Create a new XML file in `Resources/Properties/Moveables/`
   - Name it after the moveable type (e.g., `LARA.xml`, `BADDY_1.xml`)
   - Include at least HP and OCB properties (mandatory)

2. **For Statics**: Edit `Resources/Properties/StaticProperties.xml`
   - Add new `<Property>` elements as needed
   - Include at least ShatterSound, HP, and Shatter properties (mandatory)

3. **Accessing Properties in Code**:
   ```csharp
   // For Moveables
   var moveable = instance as MoveableInstance;
   int hp = moveable.CustomProperties.GetProperty<int>("HP", 100);
   string damageType = moveable.CustomProperties.GetProperty<string>("DamageType", "None");
   
   // For Statics
   var staticMesh = instance as StaticInstance;
   string shatterSound = staticMesh.CustomProperties.GetProperty<string>("ShatterSound", "None");
   bool canShatter = staticMesh.CustomProperties.GetProperty<bool>("Shatter", false);
   ```

4. **Setting Properties Programmatically**:
   ```csharp
   moveable.CustomProperties.SetProperty("HP", 200);
   moveable.CustomProperties.SetProperty("DamageType", "Burn");
   ```

## Property System Features

### Dynamic Control Generation

The WPF editor automatically creates appropriate controls based on property type:
- **Integer/Float**: Text box with input validation
- **Boolean**: True/False radio button group
- **Dropdown**: ComboBox populated with options
- **Checkbox**: Multiple checkboxes for multi-select
- **Color**: WPF color picker with RGB sliders and live preview

#### Color Property Features
- Large visual color preview rectangle (40px height)
- Three RGB sliders (Red, Green, Blue) with value displays
- Real-time color preview as you adjust sliders
- Hex color display (read-only) showing current value
- Each slider ranges from 0-255
- Smooth gradient rendering on sliders
- Batch mode: Shows gradient pattern for mixed colors, sliders start at average values

### Default Values

All properties must define default values. Defaults are applied when:
- A new object is placed in the level
- "Reset to Default" button is clicked
- Property is not found in the instance

### Validation

- Integer fields only accept whole numbers
- Float fields only accept decimal numbers
- Min/Max constraints are enforced (when specified)
- Required fields must have values

### Batch Updates

The batch editor supports editing multiple objects simultaneously:
- Shows `<Mixed Values>` when properties differ
- Only updates properties that are explicitly changed
- Preserves individual values for unchanged properties
- "Reset All to Default" resets all selected objects

## Integration with Tomb Engine

### Mandatory Properties

#### Moveables
- **HP** (Integer): Hit points, health
- **OCB** (Integer): Object Combination Block

#### Statics
- **ShatterSound** (Dropdown): Sound when object breaks
- **HP** (Integer): Hit points, structural integrity
- **Shatter** (Boolean): Can the object shatter

### Property Persistence

Properties are stored in the level file within the CustomProperties collection:
- Serialized as Dictionary<string, object>
- Saved with the level (.prj2 format)
- Loaded when level opens
- Preserved during level operations (copy, paste, etc.)

### Version Compatibility

- Property system is **Tomb Engine specific**
- Only activated when `Level.IsTombEngine == true`
- Legacy levels use existing FormMoveable/FormStatic dialogs
- Properties are ignored in non-TEN levels

## Best Practices

### Creating Property Files

1. **Be Descriptive**: Use clear property names and descriptions
2. **Set Sensible Defaults**: Choose defaults that work in most cases
3. **Group Related Properties**: Use similar naming conventions
4. **Document Options**: Explain what each dropdown/checkbox option does
5. **Use Appropriate Types**: Choose the simplest type that fits your needs

### Property Naming Conventions

- Use PascalCase for property names: `DamageType`, `AggroRange`
- Avoid spaces and special characters
- Keep names short but meaningful
- Don't duplicate standard property names (HP, OCB, etc.)

### Performance Considerations

- Property files are cached after first load
- Batch operations are optimized for multiple objects
- Changes are only serialized when level is saved
- No performance impact during gameplay

## Troubleshooting

### Properties Not Showing

**Problem**: Property editor opens but shows no custom properties.

**Solutions**:
1. Verify XML files are in correct location:
   - Moveables: `Resources/Properties/Moveables/{Name}.xml`
   - Statics: `Resources/Properties/StaticProperties.xml`
2. Check XML syntax is valid
3. Ensure property file matches moveable name exactly
4. Verify `Level.IsTombEngine` is true

### XML Parse Errors

**Problem**: Properties fail to load from XML.

**Solutions**:
1. Validate XML syntax (use XML validator)
2. Ensure all required elements are present
3. Check property type names are correct (case-sensitive)
4. Verify `<Options>` elements are properly formatted

### Batch Editor Issues

**Problem**: Batch editor not updating all objects.

**Solutions**:
1. Ensure all selected objects are same type
2. Verify properties were actually modified (not left as `<Mixed Values>`)
3. Check for undo/redo issues
4. Confirm Level.IsTombEngine is true for all objects

## Future Enhancements

Planned improvements for future versions:
- **Validation Rules**: Custom validation expressions in XML
- **Undo/Redo**: Full undo/redo support for property changes
- **Property Templates**: Save/load property presets
- **Localization**: Multi-language support for descriptions
- **Advanced Types**: Color pickers, file browsers, vector inputs
- **Property Dependencies**: Show/hide properties based on other values

## API Reference

### PropertyManager

```csharp
// Get moveable properties
MoveablePropertySet GetMoveableProperties(string moveableName)

// Get static properties
StaticPropertySet GetStaticProperties()

// Get default moveables properties
static MoveablePropertySet GetDefaultMoveableProperties()

// Get default static properties
static StaticPropertySet GetDefaultStaticProperties()

// Create defaults from definitions
static PropertyCollection CreateDefaultProperties(List<PropertyDefinition> definitions)
```

### PropertyCollection

```csharp
// Set a property value
void SetProperty(string name, object value)

// Get a property value
object GetProperty(string name, object defaultValue = null)

// Get a typed property value
T GetProperty<T>(string name, T defaultValue = default(T))

// Check if property exists
bool HasProperty(string name)

// Clear all properties
void Clear()

// Get all properties
Dictionary<string, object> GetAll()

// Set all properties
void SetAll(Dictionary<string, object> properties)
```

## Support

For issues, questions, or feature requests related to the property system:
1. Check this documentation first
2. Review example XML files in `Resources/Properties/`
3. Consult the source code in `TombLib/TombLib/LevelData/Properties/`
4. Open an issue on the Tomb Editor GitHub repository

---

**Version**: 1.0  
**Last Updated**: 2026-01-26  
**Applicable to**: Tomb Engine levels only
