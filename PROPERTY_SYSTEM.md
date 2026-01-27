# Property System Documentation

## Overview

The Tomb Editor property system provides a dynamic, XML-driven approach to managing properties for **Moveables** and **Statics** in Tomb Engine. This system allows level designers to define custom properties without modifying code, using a unified WPF-based property editor that works in both Tomb Editor and Wadtool.

## Architecture

### Key Components

1. **Property Models** (`TombLib/TombLib/LevelData/Properties/`)
   - `PropertyDefinition.cs` - Defines property metadata (name, type, default, options)
   - `PropertyManager.cs` - Manages loading and caching of property definitions
   - `PropertyCollection.cs` - Stores actual property values for instances

2. **Data Storage**
   - `WadMoveable.CustomProperties` - Property storage in WAD files
   - `WadStatic.CustomProperties` - Property storage in WAD files
   - `MoveableInstance.CustomProperties` - Property storage in level files
   - `StaticInstance.CustomProperties` - Property storage in level files

3. **WPF Editor** (`TombEditor/Windows/`)
   - `PropertyEditorWindow.xaml` - Unified property editor (handles both single and batch editing)
   - `ColorPickerWindow.xaml` - Dedicated color picker with RGB sliders and hex input

4. **Wadtool Integration** (`WadTool/Controls/ContextMenus/`)
   - `MoveableContextMenu.cs` - "Edit Properties" menu for moveables
   - `StaticContextMenu.cs` - "Edit Properties" menu for statics

5. **Serialization**
   - `Wad2Writer.cs` / `Wad2Loader.cs` - WAD2 format property serialization
   - `Prj2Writer.cs` / `Prj2Loader.cs` - PRJ2 format property serialization

### Data Flow

```
                    XML Property Files
                           ↓
        PropertyManager (loads and caches definitions)
                           ↓
          ┌────────────────┴────────────────┐
          ↓                                 ↓
    WADTOOL WORKFLOW              TOMB EDITOR WORKFLOW
          ↓                                 ↓
  Edit Properties Menu              Place Object in Level
          ↓                                 ↓
  PropertyEditorWindow              ItemInstance.FromItemType()
   (Wadtool Context)                  copies WAD2 properties
          ↓                                 ↓
  Save to WAD2 file         ─────→  MoveableInstance/StaticInstance
  (MoveableProperties/              (CustomProperties loaded from WAD2)
   StaticProperties chunks)                ↓
          ↓                         PropertyEditorWindow
          └─────────────────────→    (TombEditor Context)
                                            ↓
                                    Save to Level File
                                    (PRJ2: ObjectMovableTombEngine3/
                                     ObjectStaticTombEngine3 chunks)
```

**Key Points:**
- XML defines property schemas (types, defaults, constraints)
- WAD2 files store property values for moveables/statics in Wadtool
- Objects placed in TombEditor inherit properties from WAD2 (take precedence over XML)
- PRJ2 files store property values for instances in levels
- Reset button behavior varies by context (XML defaults in Wadtool, WAD2 values in TombEditor)

## XML Property File Format

### Property Types

The system supports six property types:

| Type | Description | Control Type | Notes |
|------|-------------|--------------|-------|
| `Integer` | Whole numbers | TextBox with validation | Supports Min/Max constraints |
| `Float` | Decimal numbers | TextBox with validation | Supports Min/Max constraints |
| `Boolean` | True/false values | Single checkbox | Checked=true, Unchecked=false, Indeterminate=mixed (batch) |
| `Dropdown` | Single selection from list | ComboBox | Shows `<Mixed>` option in batch mode |
| `Checkbox` | Multiple selections | CheckBox list | Shows `<Mixed values>` label in batch mode |
| `Color` | Color selection | Color button → ColorPickerWindow | RGB sliders, hex input, live preview |

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

#### Editing Properties in Tomb Editor

1. **Single Object**:
   - Select an object in the level editor (Moveable or Static)
   - Right-click and choose "Edit Object" (or double-click)
   - The Property Editor window will open
   - Edit properties as needed
   - Click "OK" to apply changes or "Cancel" to discard
   - Use "Reset to Defaults" to restore:
     - WAD2 saved values (if object was defined in Wadtool)
     - XML defaults (if no WAD2 values exist)

2. **Batch Editing Multiple Objects**:
   - Ctrl+Click to select multiple objects of the same type
   - Right-click on any selected object → "Edit Object"
   - The Property Editor opens in batch mode showing:
     - Regular values for properties that are the same across all objects
     - `<Mixed>` indicators for properties that differ
   - Only modified fields will be updated across all selected objects
   - Unchanged fields retain their individual values
   - Title shows "Batch Edit X Moveables" or "Batch Edit X Statics"

3. **Description Panel**:
   - At the bottom of the window is a scrollable description panel
   - Shows property description when you focus on or hover over a control
   - Helps understand what each property does without cluttering the UI

#### Editing Properties in Wadtool

1. **For Moveables**:
   - Right-click on a moveable in the moveable list
   - Select "Edit Properties" (only available for TombEngine WADs)
   - The Property Editor window opens
   - Edit default property values for this moveable
   - Click "OK" to save to the WAD file
   - These values will be used as defaults when placing the object in levels

2. **For Statics**:
   - Right-click on a static in the static list
   - Select "Edit Properties" (only available for TombEngine WADs)
   - Edit default property values for this static
   - Values are saved to the WAD file

3. **Reset Behavior in Wadtool**:
   - "Reset to Defaults" restores XML defaults (not WAD2 values)
   - This allows you to revert custom WAD properties back to schema defaults

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

### Unified Property Editor

The PropertyEditorWindow handles both single and batch editing modes:

**Single Mode**:
- Title shows object name (e.g., "ENEMY_KNIGHT Properties")
- All controls show current values
- Reset button restores WAD2 or XML defaults

**Batch Mode**:
- Title shows "Batch Edit X Moveables/Statics"
- Info message explains batch behavior
- Mixed values shown with appropriate indicators:
  - Text fields: `<Mixed>` placeholder
  - Checkbox (Boolean): Indeterminate state (gray checkmark)
  - Dropdown: `<Mixed>` option at top
  - Checkbox list: `<Mixed values>` label
  - Color: Gray color with `<Mixed>` text
- Only modified properties are applied to all objects

### Dynamic Control Generation

The editor automatically creates appropriate controls based on property type:

**Integer/Float**: 
- Text box with input validation
- Real-time validation on text input
- Shows validation errors for invalid input
- Enforces Min/Max constraints if specified

**Boolean**: 
- Single checkbox control
- Checked = true, Unchecked = false
- In batch mode: Indeterminate (gray) = mixed values
- Three-state checkbox automatically handles batch scenarios

**Dropdown**: 
- ComboBox populated with options from XML
- In batch mode: Adds `<Mixed>` option if values differ
- Selecting a new value applies to all objects

**Checkbox**: 
- Multiple checkboxes for multi-select
- Each option can be checked independently
- In batch mode: Shows `<Mixed values>` label if selections differ

**Color**: 
- Button showing color preview and hex value
- Opens dedicated ColorPickerWindow when clicked
- Features:
  - Large color preview (80px height)
  - RGB sliders (0-255) with gradient backgrounds
  - RGB text inputs next to each slider
  - Hex text input (#RRGGBB format)
  - All inputs synchronized in real-time
  - OK/Cancel buttons

### Description Panel

At the bottom of the property window:
- Scrollable text area (max height: 100px)
- Shows property description when control gains focus or mouse hovers
- Default message: "Select a property to see its description"
- Helps users understand properties without cluttering the main UI
- Updates dynamically as you navigate between properties

### Default Values and Reset Behavior

All properties must define default values. The system uses context-aware reset behavior:

**In Tomb Editor (PropertyEditorContext.TombEditor)**:
- When "Reset to Defaults" is clicked:
  1. First tries to restore WAD2 saved values (if object was defined in Wadtool)
  2. Falls back to XML defaults if no WAD2 values exist
  3. Shows appropriate message indicating the source
- This allows level designers to revert to the values defined in the WAD
- Useful when you've modified an object and want to restore its WAD defaults

**In Wadtool (PropertyEditorContext.Wadtool)**:
- When "Reset to Defaults" is clicked:
  1. Always resets to XML schema defaults
  2. Clears any custom values set for this WAD object
- This allows WAD creators to restore properties to their original XML definitions
- Useful when experimenting with different default values

**Auto-Applied Defaults**:
- When a new object is placed in Tomb Editor, it inherits CustomProperties from the WAD2 file
- If the WAD has no custom properties, XML defaults are used
- WAD2 properties always take precedence over XML defaults

### Validation

- Integer fields only accept whole numbers
- Float fields only accept decimal numbers
- Min/Max constraints are enforced (when specified)
- Required fields must have values

### Batch Updates

The unified property editor supports editing multiple objects simultaneously:

**Behavior**:
- Window title shows "Batch Edit X Moveables" or "Batch Edit X Statics"
- Info message explains that only changed properties will be updated
- Mixed value detection:
  - Automatically detects when selected objects have different values
  - Shows appropriate indicators (`<Mixed>`, indeterminate checkbox, etc.)
- **Selective updates**: Only properties you explicitly modify are changed
- **Preservation**: Unchanged properties retain their individual values across objects
- **Smart reset**: "Reset to Defaults" considers each object's context individually

**Example**:
```
Selected: 3 ENEMY_KNIGHT instances
- Instance 1: HP=100, DamageType="Fire"
- Instance 2: HP=200, DamageType="Fire"  
- Instance 3: HP=100, DamageType="Ice"

Editor shows:
- HP: <Mixed> (100, 200, 100)
- DamageType: <Mixed> (Fire, Fire, Ice)

User changes HP to 150, leaves DamageType unchanged:
- Instance 1: HP=150, DamageType="Fire" (Fire unchanged)
- Instance 2: HP=150, DamageType="Fire" (Fire unchanged)
- Instance 3: HP=150, DamageType="Ice" (Ice unchanged)
```

## Integration with Tomb Engine

### WAD2 File Format

Properties are stored in TombEngine WAD2 files using dedicated chunks:

**Chunk IDs**:
- `MoveableProperties` - Stores CustomProperties for each WadMoveable
- `StaticProperties` - Stores CustomProperties for each WadStatic

**Serialization Format**:
```
[ushort: property_count]
For each property:
  [UTF8 string: property_name]
  [UTF8 string: property_value]
```

**Features**:
- Only written for TombEngine game version WADs
- Properties serialized as key-value pairs (strings)
- Type conversion happens at runtime when properties are accessed
- Backward compatible: Old WAD2 files without properties load correctly
- Properties are stored per-moveable and per-static in the WAD

### PRJ2 Level Format

Properties are stored in level files using dedicated chunks:

**Chunk IDs**:
- `ObjectMovableTombEngine3` - Moveable instances with CustomProperties
- `ObjectStaticTombEngine3` - Static instances with CustomProperties

**Serialization Format**:
```
[Existing instance data...]
[ushort: property_count]
For each property:
  [UTF8 string: property_name]
  [UTF8 string: property_value]
```

**Features**:
- Extends existing moveable/static chunks with property data
- Backward compatible with v2 chunks (without properties)
- Only written for TombEngine levels
- Properties persist across save/load/copy/paste operations

### Property Loading Priority

When placing an object in Tomb Editor:

1. **Check WAD2 file**: Does this moveable/static have CustomProperties in the WAD?
   - If YES: Copy CustomProperties from WadMoveable/WadStatic to instance
   - If NO: Use empty PropertyCollection

2. **Load XML defaults**: PropertyManager loads XML schema for this object type
   - Defines available properties and their types
   - Provides default values for missing properties

3. **Merge**: Instance has CustomProperties from WAD, XML provides schema
   - WAD2 values take precedence
   - XML fills in any missing properties with defaults

**Result**: Objects inherit property values from WAD2, giving Wadtool creators full control over default values.

### Mandatory Properties

#### Moveables
- **HP** (Integer): Hit points, health
- **OCB** (Integer): Object Combination Block

#### Statics
- **ShatterSound** (Dropdown): Sound when object breaks
- **HP** (Integer): Hit points, structural integrity
- **Shatter** (Boolean): Can the object shatter

### Property Persistence

**In Wadtool (WAD2 files)**:
- Properties stored as CustomProperties in WadMoveable and WadStatic objects
- Serialized to MoveableProperties and StaticProperties chunks
- Format: Dictionary<string, string> as count + key-value pairs
- Saved when WAD is saved
- Loaded when WAD opens
- Preserved during WAD operations (clone, copy, etc.)

**In Tomb Editor (PRJ2 files)**:
- Properties stored as CustomProperties in MoveableInstance and StaticInstance
- Serialized to ObjectMovableTombEngine3 and ObjectStaticTombEngine3 chunks
- Same format as WAD2: count + key-value pairs
- Saved with the level (.prj2 format)
- Loaded when level opens
- Preserved during level operations (copy, paste, undo, redo)

**Synchronization**:
- Edit properties in Wadtool → Save WAD → Properties stored in WAD2
- Open Tomb Editor with that WAD → Place object → CustomProperties copied from WAD to instance
- Edit properties in Tomb Editor → Save level → Properties stored in PRJ2
- Reload level → Properties loaded from PRJ2
- Reset in Tomb Editor → Restores WAD2 values (or XML if no WAD2 values)

### Version Compatibility

- Property system is **Tomb Engine specific**
- Only activated when `Level.IsTombEngine == true`
- Only available in TombEngine WAD2 files (version check in Wadtool)
- Legacy levels use existing dialogs
- Properties are ignored in non-TEN levels
- Old WAD2 files without property chunks load correctly
- Old PRJ2 files with v2 chunks (no properties) load correctly

### Wadtool Context Menu Requirements

The "Edit Properties" menu option appears when:
1. WAD file has `GameVersion == TRVersion.Game.TombEngine`
2. PropertyManager finds XML definitions for the moveable/static
3. If no XML file exists, menu shows helpful message

### Tomb Editor Integration

Properties integrate seamlessly:
1. Objects placed in level inherit WAD2 CustomProperties automatically
2. Edit Object opens PropertyEditorWindow in TombEditor context
3. Properties save to PRJ2 file with level
4. Undo/redo system tracks property changes
5. Copy/paste preserves CustomProperties

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

### Properties Not Showing in Tomb Editor

**Problem**: Property editor opens but shows no custom properties.

**Solutions**:
1. Verify XML files are in correct location:
   - Moveables: `Resources/Properties/Moveables/{Name}.xml`
   - Statics: `Resources/Properties/StaticProperties.xml`
2. Check XML syntax is valid
3. Ensure property file matches moveable name exactly
4. Verify `Level.IsTombEngine` is true
5. Check if object has properties defined in XML schema

### Properties Not Showing in Wadtool

**Problem**: "Edit Properties" menu doesn't appear or shows "No properties defined".

**Solutions**:
1. Verify WAD is TombEngine version (`GameVersion == TRVersion.Game.TombEngine`)
2. Check that XML file exists for this moveable/static
3. Ensure XML file is named correctly (matches moveable name)
4. Verify PropertyManager can find XML files
5. Check console/logs for XML parsing errors

### WAD2 Properties Not Loading in Tomb Editor

**Problem**: Objects placed in editor don't have WAD2 property values.

**Solutions**:
1. Verify properties were saved in Wadtool (open WAD, check values)
2. Ensure Tomb Editor is using the correct WAD file
3. Check that `ItemInstance.FromItemType(Level, ItemType)` overload is being called
4. Verify `Level.Settings.WadTryGetMoveable()` finds the WAD object
5. Check if properties actually exist in WAD (may need to re-edit in Wadtool)

### Reset Button Not Working Correctly

**Problem**: Reset doesn't restore expected values.

**Solutions**:
1. Check PropertyEditorContext:
   - Wadtool should use `PropertyEditorContext.Wadtool`
   - Tomb Editor should use `PropertyEditorContext.TombEditor` (default)
2. In Tomb Editor: Verify object has WAD2 properties (check `_savedWad2Properties`)
3. In Wadtool: Ensure XML defaults are defined correctly
4. Check if properties were actually modified (some may show as `<Mixed>`)

### XML Parse Errors

**Problem**: Properties fail to load from XML.

**Solutions**:
1. Validate XML syntax (use XML validator)
2. Ensure all required elements are present
3. Check property type names are correct (case-sensitive)
4. Verify `<Options>` elements are properly formatted
5. Check for special characters that need escaping

### Batch Editor Issues

**Problem**: Batch editor not updating all objects correctly.

**Solutions**:
1. Ensure all selected objects are same type (all moveables or all statics)
2. Verify properties were actually modified (not left as `<Mixed>`)
3. Check that all objects support the property being changed
4. Verify selections are still valid (objects not deleted)
5. Test with smaller selection to isolate issue

## Future Enhancements

Potential improvements for future versions:
- **Validation Rules**: Custom validation expressions in XML
- **Property Templates**: Save/load property presets
- **Localization**: Multi-language support for descriptions
- **Advanced Types**: File browsers, vector inputs, more specialized controls
- **Property Dependencies**: Show/hide properties based on other values
- **Property Groups**: Organize properties into collapsible sections
- **Search/Filter**: Find properties by name in large property sets

## API Reference

### PropertyManager

```csharp
// Get moveable properties (from XML)
MoveablePropertySet GetMoveableProperties(string moveableName)

// Get static properties (from XML)
StaticPropertySet GetStaticProperties()

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

// Get all properties as dictionary
Dictionary<string, object> GetAll()

// Set all properties from dictionary
void SetAll(Dictionary<string, object> properties)
```

### PropertyEditorWindow

```csharp
// Constructor for single object (Tomb Editor or Wadtool)
PropertyEditorWindow(ItemInstance instance, bool isTombEngine, 
                     PropertyEditorContext context = PropertyEditorContext.TombEditor)

// Constructor for batch editing (Tomb Editor only)
PropertyEditorWindow(List<ItemInstance> instances, bool isTombEngine,
                     PropertyEditorContext context = PropertyEditorContext.TombEditor)

// Context enum
enum PropertyEditorContext
{
    TombEditor,  // Reset uses WAD2 values or XML defaults
    Wadtool      // Reset uses XML defaults only
}
```

### ItemInstance

```csharp
// Create instance from ItemType (copies WAD2 properties)
static ItemInstance FromItemType(Level level, ItemType item)

// Original overload (no WAD2 property loading)
static ItemInstance FromItemType(ItemType item)
```

### WadMoveable / WadStatic

```csharp
// Each has CustomProperties field
public PropertyCollection CustomProperties { get; set; }

// Initialized in constructor, preserved in Clone()
```

## Support

For issues, questions, or feature requests related to the property system:
1. Check this documentation first
2. Review example XML files in `Resources/Properties/`
3. Consult the source code in `TombLib/TombLib/LevelData/Properties/`
4. Check serialization code in `Wad2Writer.cs`, `Wad2Loader.cs`, `Prj2Writer.cs`, `Prj2Loader.cs`
5. Review UI code in `TombEditor/Windows/PropertyEditorWindow.xaml.cs`
6. Open an issue on the Tomb Editor GitHub repository

## Version History

**Version 2.0** (2026-01-27)
- Consolidated PropertyEditorWindow and BatchPropertyEditorWindow into single unified window
- Added Wadtool integration with "Edit Properties" context menu
- Implemented WAD2 property serialization (MoveableProperties, StaticProperties chunks)
- Implemented PRJ2 property serialization (ObjectMovableTombEngine3, ObjectStaticTombEngine3 chunks)
- Added context-aware Reset button (Wadtool vs TombEditor)
- WAD2 properties now take precedence over XML defaults
- Changed Boolean control from radio buttons to single checkbox
- Added description panel at bottom of property window
- Implemented three-state checkbox for mixed boolean values in batch mode
- Added ColorPickerWindow with RGB sliders and hex input

**Version 1.0** (2026-01-26)
- Initial property system implementation
- XML-driven property definitions
- Dynamic WPF property editor
- Six property types supported
- Batch editing capability

---

**Last Updated**: 2026-01-27  
**Applicable to**: Tomb Engine levels and WAD files only
