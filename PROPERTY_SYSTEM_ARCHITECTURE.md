# Property System Architecture and Data Flow

This document provides a comprehensive overview of how the property system is implemented in Tomb Editor, showing file names, component relationships, and data flow.

## Table of Contents
1. [System Overview](#system-overview)
2. [File Structure](#file-structure)
3. [Core Components](#core-components)
4. [Data Flow - WadTool](#data-flow---wadtool)
5. [Data Flow - TombEditor](#data-flow---tombeditor)
6. [Serialization Flow](#serialization-flow)
7. [Component Interaction Diagram](#component-interaction-diagram)

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    Property System Architecture                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────┐         ┌──────────────┐                      │
│  │   WadTool    │         │  TombEditor  │                      │
│  │              │         │              │                      │
│  │ Edit WAD     │         │ Edit Level   │                      │
│  │ Defaults     │         │ Instances    │                      │
│  └──────┬───────┘         └──────┬───────┘                      │
│         │                        │                               │
│         │                        │                               │
│    ┌────▼────────────────────────▼────┐                         │
│    │   PropertyEditorWindow.xaml.cs   │                         │
│    │   (Unified Property Editor UI)    │                         │
│    └────┬────────────────────┬─────────┘                         │
│         │                    │                                   │
│    ┌────▼────────┐      ┌───▼──────────┐                        │
│    │ PropertyMan-│      │ Property     │                        │
│    │ ager.cs     │      │ Collection.cs│                        │
│    │ (XML Load)  │      │ (Storage)    │                        │
│    └─────────────┘      └──────────────┘                        │
│                                                                   │
│  ┌──────────────┐         ┌──────────────┐                      │
│  │   WAD2 File  │         │   PRJ2 File  │                      │
│  │  (Wad data)  │         │ (Level data) │                      │
│  └──────────────┘         └──────────────┘                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## File Structure

### Core Property System Files

```
TombLib/TombLib/LevelData/Properties/
├── PropertyDefinition.cs          # Property metadata (name, type, min, max, etc.)
├── PropertyCollection.cs          # Key-value storage for property values
└── PropertyManager.cs             # XML file loading and caching

TombEditor/Windows/
├── PropertyEditorWindow.xaml      # Property editor UI (XAML)
├── PropertyEditorWindow.xaml.cs   # Property editor logic
└── ColorPickerWindow.xaml(.cs)    # Color picker dialog

TombEditor/Forms/TombEngine/
├── FormMoveable.cs                # Wrapper form for moveable properties
└── FormStatic.cs                  # Wrapper form for static properties

TombLib/TombLib/Wad/
├── WadMoveable.cs                 # Moveable class with CustomProperties
├── WadStatic.cs                   # Static class with CustomProperties
├── Wad2Writer.cs                  # Serialize properties to WAD2
└── Wad2Loader.cs                  # Deserialize properties from WAD2

TombLib/TombLib/LevelData/IO/
├── Prj2Writer.cs                  # Serialize properties to PRJ2
└── Prj2Loader.cs                  # Deserialize properties from PRJ2

TombLib/TombLib/LevelData/Instances/
├── MoveableInstance.cs            # Level instance with CustomProperties
├── StaticInstance.cs              # Level instance with CustomProperties
└── ItemInstance.cs                # Base class, FromItemType() copies from WAD

WadTool/Forms/
└── FormMain.cs                    # WadTool main form, PropertyManager init

TombEditor/
├── EditorActions.cs               # Property editing actions, bulk operations
└── Command.cs                     # Command registration for menu items
```

### XML Property Definition Files

```
Resources/Properties/
├── Moveables/
│   ├── Default.xml                # Default properties (OCB, HP)
│   ├── HORSEMAN.xml               # Object-specific properties
│   ├── ENEMY.xml                  # Object-specific properties
│   └── ...                        # Other moveable XMLs
└── StaticProperties.xml           # Static object properties
```

---

## Core Components

### 1. PropertyDefinition.cs
**Purpose:** Defines metadata for a single property
```
Properties:
- Name: string (e.g., "HP", "OCB", "Damage")
- Type: string ("Integer", "Float", "Boolean", "Dropdown", "Checkbox", "Color")
- DefaultValue: object
- Min, Max: For numeric types
- Values: For Dropdown/Checkbox (list of options)
- Description: string (shown in tooltip)
```

### 2. PropertyCollection.cs
**Purpose:** Stores actual property values (key-value pairs)
```
Methods:
- SetProperty(string key, object value)
- GetProperty<T>(string key, T defaultValue)
- GetAll() → Dictionary<string, object>
- Clear()
```

### 3. PropertyManager.cs
**Purpose:** Loads and caches XML property definitions
```
Key Methods:
- SetPropertiesDirectory(string path)
- LoadProperties()
- GetMoveableProperties(string name) → MoveablePropertySet
- GetStaticProperties() → StaticPropertySet
- GetDefaultMoveableProperties() → MoveablePropertySet

Lifecycle:
1. Initialize with properties directory
2. Scan and load all XML files
3. Cache in memory (_moveableProperties, _staticProperties)
4. Return merged Default.xml + object-specific properties
```

### 4. PropertyEditorWindow.xaml.cs
**Purpose:** Unified UI for editing properties
```
Constructors:
- Single object: PropertyEditorWindow(ItemInstance, bool, context, gameVersion, level)
- Batch editing: PropertyEditorWindow(List<ItemInstance>, bool, context, gameVersion, level)

Key Methods:
- BuildPropertyControls() - Creates UI dynamically from PropertyDefinition
- SaveProperties() - Extracts values from controls
- ResetToDefaults() - Context-aware reset (WAD vs XML)
- SetPropertyValueOnInstance() - Stores value in CustomProperties
- GetPropertyValueFromInstance() - Reads value from CustomProperties

Contexts:
- Wadtool: Edit WAD default values, reset to XML
- TombEditor: Edit instance values, reset to WAD or XML
```

---

## Data Flow - WadTool

### Workflow: Edit Moveable Properties in WadTool

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Startup                                                       │
└───────────────────────────────────────────────────┬─────────────┘
                                                    │
                                                    ▼
                    ┌──────────────────────────────────────────┐
                    │ FormMain.cs (Constructor)                │
                    │ PropertyManager.Instance.SetProperties   │
                    │ Directory("Resources/Properties")        │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────────────────┐
                    │ PropertyManager.LoadProperties()         │
                    │ - Scan Moveables/*.xml                   │
                    │ - Parse and cache in memory              │
                    └──────────────┬───────────────────────────┘
                                   │
┌──────────────────────────────────┴─────────────────────────────┐
│ 2. User Action: Right-click moveable → "Edit Properties"       │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ FormMain.EditPropertiesMoveable_Click()  │
                    │ - Get selected WadMoveable               │
                    │ - Create WadMoveableWrapper              │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────────────────┐
                    │ WadMoveableWrapper (constructor)         │
                    │ - Inherits from MoveableInstance         │
                    │ - CustomProperties = moveable.Custom     │
                    │   Properties (shared reference)          │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────────────────┐
                    │ PropertyEditorWindow (constructor)       │
                    │ - context = Wadtool                      │
                    │ - gameVersion = wad.GameVersion          │
                    │ - Load properties from PropertyManager   │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────┐
        │ PropertyManager.GetMoveableProperties(objName)   │
        │ 1. Try object-specific XML (e.g., HORSEMAN.xml)  │
        │ 2. Merge with Default.xml (OCB, HP)              │
        │ 3. Return combined MoveablePropertySet           │
        └──────────────┬───────────────────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────────────────────────┐
        │ PropertyEditorWindow.BuildPropertyControls()     │
        │ - For each PropertyDefinition:                   │
        │   - Create control (TextBox, CheckBox, etc.)     │
        │   - Set initial value from CustomProperties      │
        │   - Add to UI                                    │
        └──────────────┬───────────────────────────────────┘
                       │
┌──────────────────────┴──────────────────────────────────────────┐
│ 3. User edits values and clicks OK                              │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ PropertyEditorWindow.SaveProperties()    │
                    │ - For each control:                      │
                    │   - Extract value                        │
                    │   - Call SetPropertyValueOnInstance()    │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │ SetPropertyValueOnInstance()                         │
        │ - instance.CustomProperties.SetProperty(name, value) │
        │ - Stores in WadMoveable.CustomProperties (shared!)   │
        └──────────────┬─────────────────────────────────────┘
                       │
┌──────────────────────┴──────────────────────────────────────────┐
│ 4. User saves WAD file                                           │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ Wad2Writer.WriteMoveables()              │
                    │ - For each moveable:                     │
                    │   - Write standard data                  │
                    │   - Call WriteCustomProperties()         │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │ Wad2Writer.WriteCustomProperties()                   │
        │ - Write chunk: MoveableProperties                    │
        │ - Write count (ushort)                               │
        │ - For each property in CustomProperties:             │
        │   - Write key (UTF8 string)                          │
        │   - If List<string>: Serialize as JSON array         │
        │   - Else: Write value.ToString() (UTF8 string)       │
        └──────────────┬─────────────────────────────────────┘
                       │
                       ▼
            ┌─────────────────────────┐
            │ WAD2 File (on disk)     │
            │ Contains CustomProp     │
            │ data in binary format   │
            └─────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 5. Reload WAD file                                               │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ Wad2Loader.LoadMoveables()               │
                    │ - For each moveable:                     │
                    │   - Load standard data                   │
                    │   - Check for MoveableProperties chunk   │
                    │   - Call ReadCustomProperties()          │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │ Wad2Loader.ReadCustomProperties()                    │
        │ - Read count (ushort)                                │
        │ - For each property:                                 │
        │   - Read key (UTF8 string)                           │
        │   - Read value (UTF8 string)                         │
        │   - If value is JSON array: Parse to List<string>    │
        │   - properties.SetProperty(key, value)               │
        └──────────────┬─────────────────────────────────────┘
                       │
                       ▼
            ┌─────────────────────────────────────┐
            │ WadMoveable.CustomProperties        │
            │ populated with values from file     │
            └─────────────────────────────────────┘
```

---

## Data Flow - TombEditor

### Workflow 1: Place Object from WAD

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User places moveable in level                                │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ Command.cs "AddItem"                     │
                    │ - ItemInstance.FromItemType(level, item) │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │ ItemInstance.FromItemType(Level level, ItemType item)│
        │ 1. Create new MoveableInstance                       │
        │ 2. Look up WadMoveable from level.Settings           │
        │ 3. Copy CustomProperties from WAD to instance:       │
        │    foreach (kvp in wadMoveable.CustomProperties)     │
        │        instance.CustomProperties.SetProperty(...)    │
        │ 4. Return instance                                   │
        └──────────────┬─────────────────────────────────────┘
                       │
                       ▼
            ┌─────────────────────────────────────┐
            │ MoveableInstance created with       │
            │ CustomProperties from WAD           │
            │ (HP=500, OCB=20, Damage=50, etc.)  │
            └─────────────────────────────────────┘
```

### Workflow 2: Edit Object Properties

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User right-clicks object → "Edit Object"                     │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ EditorActions.EditObject()               │
                    │ - Get selected MoveableInstance          │
                    │ - Create FormMoveable                    │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────────────────┐
                    │ FormMoveable (constructor)               │
                    │ - Accepts Level parameter                │
                    │ - Opens PropertyEditorWindow             │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │ PropertyEditorWindow (constructor)                   │
        │ - context = TombEditor                               │
        │ - level = passed from FormMoveable                   │
        │ - Look up WAD object from level.Settings:            │
        │   wadMoveable = level.Settings.WadTryGetMoveable()   │
        │ - Save WAD CustomProperties to _savedWad2Properties  │
        └──────────────┬─────────────────────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────────────────────────────┐
        │ PropertyManager.GetMoveableProperties()              │
        │ - Merge Default.xml + object-specific XML            │
        │ - Return property definitions                        │
        └──────────────┬─────────────────────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────────────────────────────┐
        │ BuildPropertyControls()                              │
        │ - Create UI from PropertyDefinitions                 │
        │ - Load values from instance.CustomProperties         │
        │   (current instance values, may differ from WAD)     │
        └──────────────┬─────────────────────────────────────┘
                       │
┌──────────────────────┴──────────────────────────────────────────┐
│ 2. User clicks "Reset" button                                    │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ ResetToDefaults() [TombEditor context]   │
                    │ - Check if _savedWad2Properties has data│
                    │ - If yes: Restore WAD values             │
                    │ - If no: Use XML defaults                │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │ SetControlToValue() or SetControlToDefault()         │
        │ - For each control:                                  │
        │   - Set to WAD value or XML default                  │
        │ - User sees original values restored                 │
        └──────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 3. User edits values and clicks OK                              │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ SaveProperties()                         │
                    │ - Extract values from controls           │
                    │ - Store in instance.CustomProperties     │
                    │ - editor.ObjectChange() marks changed    │
                    └──────────────┬───────────────────────────┘
                                   │
┌──────────────────────────────────┴─────────────────────────────┐
│ 4. User saves level                                             │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ Prj2Writer.WriteLevel()                  │
                    │ - Write all level data                   │
                    │ - For moveables: WriteCustomProperties() │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │ Prj2Writer.WriteCustomProperties()                   │
        │ - Write chunk: ObjectMovableTombEngine3              │
        │ - Same format as WAD2:                               │
        │   - Count (ushort)                                   │
        │   - Key-value pairs (UTF8 strings)                   │
        │   - List<string> as JSON arrays                      │
        └──────────────┬─────────────────────────────────────┘
                       │
                       ▼
            ┌─────────────────────────┐
            │ PRJ2 File (on disk)     │
            │ Contains instance       │
            │ CustomProperties        │
            └─────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 5. Reload level                                                  │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ Prj2Loader.LoadLevel()                   │
                    │ - For each moveable instance:            │
                    │   - Read standard data                   │
                    │   - Check for ObjectMovableTombEngine3   │
                    │   - Call ReadCustomProperties()          │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │ Prj2Loader.ReadCustomProperties()                    │
        │ - Same format as WAD2                                │
        │ - Populate instance.CustomProperties                 │
        └──────────────┬─────────────────────────────────────┘
                       │
                       ▼
            ┌─────────────────────────────────────┐
            │ MoveableInstance.CustomProperties   │
            │ restored from PRJ2 file             │
            └─────────────────────────────────────┘
```

### Workflow 3: Reload All Properties from WAD

```
┌─────────────────────────────────────────────────────────────────┐
│ User: Items → "Reload all properties from WAD"                  │
└───────────────────────────────────────────────┬─────────────────┘
                                                │
                                                ▼
                    ┌──────────────────────────────────────────┐
                    │ EditorActions.ReloadAllPropertiesFromWad │
                    │ (Editor editor, IWin32Window owner)      │
                    └──────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │ For each MoveableInstance in level:                  │
        │ 1. wadMoveable = level.Settings.WadTryGetMoveable()  │
        │ 2. Clear instance.CustomProperties                   │
        │ 3. Copy from wadMoveable.CustomProperties            │
        │ 4. editor.ObjectChange(instance, Change)             │
        │                                                       │
        │ For each StaticInstance in level:                    │
        │ 1. wadStatic = level.Settings.WadTryGetStatic()      │
        │ 2. Clear instance.CustomProperties                   │
        │ 3. Copy from wadStatic.CustomProperties              │
        │ 4. editor.ObjectChange(instance, Change)             │
        └──────────────┬─────────────────────────────────────┘
                       │
                       ▼
            ┌─────────────────────────────────────┐
            │ All instances updated with          │
            │ current WAD CustomProperties        │
            │ Level marked as changed             │
            └─────────────────────────────────────┘
```

---

## Serialization Flow

### WAD2 Format (Binary)

```
Moveable Data Structure:
┌────────────────────────────┐
│ Moveable TypeId            │ ← Standard moveable data
│ Mesh count                 │
│ Meshes...                  │
│ Skeleton...                │
│ Animations...              │
├────────────────────────────┤
│ [MoveableProperties Chunk] │ ← NEW: Custom properties chunk
│   - Chunk ID              │
│   - Count (ushort)         │
│   - Properties:            │
│     - Key (UTF8 string)    │
│     - Value:               │
│       - If List<string>:   │
│         ["item1","item2"]  │ (JSON array)
│       - Else:              │
│         "value"            │ (ToString())
└────────────────────────────┘

Static Data Structure:
┌────────────────────────────┐
│ Static TypeId              │ ← Standard static data
│ Mesh                       │
│ Collision...               │
├────────────────────────────┤
│ [StaticProperties Chunk]   │ ← NEW: Custom properties chunk
│   (Same format as above)   │
└────────────────────────────┘
```

### PRJ2 Format (Binary)

```
Level Object Data:
┌────────────────────────────────┐
│ [ObjectMovableTombEngine3]     │ ← Chunk for moveable instance
│   - Position                   │
│   - Rotation                   │
│   - WadObjectId                │
│   - ... (other properties)     │
│   - CustomProperties:          │
│     - Count (ushort)           │
│     - Key-value pairs          │
│       (same format as WAD2)    │
└────────────────────────────────┘

┌────────────────────────────────┐
│ [ObjectStaticTombEngine3]      │ ← Chunk for static instance
│   - Position                   │
│   - Rotation                   │
│   - WadObjectId                │
│   - ... (other properties)     │
│   - CustomProperties:          │
│     - Count (ushort)           │
│     - Key-value pairs          │
│       (same format as WAD2)    │
└────────────────────────────────┘
```

### Type Serialization

```
Property Types → Serialization:

Integer (42)           → "42"
Float (3.14)           → "3.14"
Boolean (true)         → "True"
String ("value")       → "value"
List<string>           → ["item1","item2","item3"]  (JSON)
  ["AI_Patrol",...]
Color (RGB)            → Store as Integer, display as color

Deserialization:
- Read as string
- Convert.ChangeType for primitive types
- Parse JSON array for List<string>
- GetProperty<T> handles conversion
```

---

## Component Interaction Diagram

### Class Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                         Core Classes                             │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────────┐
│ PropertyDefinition   │ ← Metadata (Name, Type, Min, Max, Values)
└──────────┬───────────┘
           │
           │ 1..* (List<PropertyDefinition>)
           │
           ▼
┌──────────────────────────┐
│ MoveablePropertySet      │
│ - Properties: List       │
└──────────────────────────┘
           ▲
           │ Contains
           │
┌──────────┴────────────────────────────────────────────┐
│ PropertyManager (Singleton)                           │
│ - _moveableProperties: Dict<string, MoveablePS>       │
│ - _staticProperties: StaticPropertySet                │
│                                                        │
│ + GetMoveableProperties(name) → MoveablePropertySet   │
│ + GetStaticProperties() → StaticPropertySet           │
│ + GetDefaultMoveableProperties() → MoveablePropertySet│
└────────────────────────────────────────────────────────┘
           ▲
           │ Uses
           │
┌──────────┴─────────────────────────────────────┐
│ PropertyEditorWindow                           │
│ - _propertyDefinitions: List<PropertyDef>      │
│ - _propertyControls: Dict<string, Control>     │
│ - _savedWad2Properties: PropertyCollection     │
│ - _context: PropertyEditorContext              │
│                                                 │
│ + BuildPropertyControls()                      │
│ + SaveProperties()                             │
│ + ResetToDefaults()                            │
└─────────────────────────────────────────────────┘
           │
           │ Reads/Writes
           ▼
┌─────────────────────────────────────────────────┐
│ PropertyCollection                              │
│ - _properties: Dict<string, object>             │
│                                                  │
│ + SetProperty(key, value)                       │
│ + GetProperty<T>(key, default)                  │
│ + GetAll() → Dictionary                         │
└─────────────────────────────────────────────────┘
           ▲
           │ Has (CustomProperties field)
           │
    ┌──────┴───────┬─────────────────────────┐
    │              │                         │
┌───▼──────────┐ ┌─▼────────────────┐ ┌─────▼──────────────┐
│ WadMoveable  │ │ MoveableInstance │ │ WadStatic          │
│              │ │                  │ │                    │
│ CustomProp   │ │ CustomProp       │ │ CustomProp         │
└──────────────┘ └──────────────────┘ └────────────────────┘
```

### Data Flow Between Components

```
User Action → Component Chain → Storage

1. WadTool Edit Flow:
   User edit
      → PropertyEditorWindow (UI)
      → WadMoveableWrapper.CustomProperties (shared reference)
      → WadMoveable.CustomProperties (actual storage)
      → Wad2Writer.WriteCustomProperties()
      → WAD2 File

2. TombEditor Edit Flow:
   User edit
      → PropertyEditorWindow (UI)
      → MoveableInstance.CustomProperties (storage)
      → Prj2Writer.WriteCustomProperties()
      → PRJ2 File

3. Place Object Flow:
   User places object
      → ItemInstance.FromItemType()
      → WadMoveable.CustomProperties (read)
      → MoveableInstance.CustomProperties (copy)
      → Object in level with WAD properties

4. Reset Flow (TombEditor):
   User clicks Reset
      → PropertyEditorWindow.ResetToDefaults()
      → _savedWad2Properties (WAD values saved at init)
      → Restore to controls
      → User sees WAD values
```

---

## Summary

### Key Design Principles

1. **Unified UI:** Single PropertyEditorWindow handles both WadTool and TombEditor
2. **Context-Aware:** Behavior changes based on PropertyEditorContext
3. **XML-Driven:** All property definitions come from XML files
4. **Merge Strategy:** Object-specific XML merged with Default.xml
5. **Shared References:** WadWrapper classes share CustomProperties with WAD objects
6. **Inheritance:** Instances inherit CustomProperties from WAD when placed
7. **Reset Distinction:** 
   - Wadtool: Reset to XML defaults
   - TombEditor: Reset to WAD saved values
8. **Type Safety:** PropertyCollection handles type conversions
9. **Serialization:** Consistent format between WAD2 and PRJ2
10. **Bulk Operations:** Reload/Reset all properties from menu

### Property Lifecycle

```
XML File → PropertyManager (load) → PropertyEditorWindow (display)
   ↓
WadTool Edit → WadMoveable.CustomProperties → WAD2 File
   ↓
TombEditor Place → MoveableInstance.CustomProperties (copy from WAD)
   ↓
TombEditor Edit → Instance.CustomProperties → PRJ2 File
   ↓
TombEditor Reset → Restore from WAD2 saved values
```

---

## File Checklist

✅ Core Property Classes:
- PropertyDefinition.cs
- PropertyCollection.cs
- PropertyManager.cs

✅ UI Components:
- PropertyEditorWindow.xaml.cs
- ColorPickerWindow.xaml.cs
- FormMoveable.cs / FormStatic.cs

✅ Data Models:
- WadMoveable.cs / WadStatic.cs
- MoveableInstance.cs / StaticInstance.cs
- ItemInstance.cs

✅ Serialization:
- Wad2Writer.cs / Wad2Loader.cs
- Prj2Writer.cs / Prj2Loader.cs

✅ Integration:
- EditorActions.cs
- Command.cs
- FormMain.cs (WadTool)
- FormMain.Designer.cs (TombEditor menu)

✅ Documentation:
- PROPERTY_SYSTEM.md
- PROPERTY_SYSTEM_ARCHITECTURE.md (this file)

---

*Last Updated: 2026-01-29*
*Property System Version: 2.0*
