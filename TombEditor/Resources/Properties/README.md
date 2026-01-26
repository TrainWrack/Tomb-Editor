# Property XML Files

This directory contains XML property definitions for Tomb Engine objects.

## Directory Structure

```
Properties/
├── StaticProperties.xml        # Shared properties for ALL static meshes
└── Moveables/                  # Individual property files for moveables
    ├── Default.xml             # Default properties (HP, OCB)
    ├── Enemy.xml               # Example enemy properties
    └── {MoveableName}.xml      # Add more as needed
```

## Quick Start

### Creating Properties for a New Moveable

1. Create a new XML file in the `Moveables/` folder
2. Name it after the moveable (e.g., `LARA.xml`, `BADDY_1.xml`)
3. Start with the minimum required properties:

```xml
<?xml version="1.0" encoding="utf-8"?>
<MoveableProperties>
  <Property name="HP" type="Integer">
    <Default>100</Default>
    <Description>Hit Points</Description>
    <Min>0</Min>
    <Max>32767</Max>
  </Property>
  
  <Property name="OCB" type="Integer">
    <Default>0</Default>
    <Description>Object Combination Block identifier</Description>
    <Min>-32768</Min>
    <Max>32767</Max>
  </Property>
</MoveableProperties>
```

4. Add custom properties as needed

### Editing Static Properties

Edit `StaticProperties.xml` to add new properties for ALL static meshes. These properties are mandatory:

```xml
<Property name="ShatterSound" type="Dropdown">...</Property>
<Property name="HP" type="Integer">...</Property>
<Property name="Shatter" type="Boolean">...</Property>
```

## Property Types Reference

### Integer
Whole numbers (-32768 to 32767)
```xml
<Property name="Damage" type="Integer">
  <Default>10</Default>
  <Description>Damage dealt per hit</Description>
  <Min>0</Min>
  <Max>1000</Max>
</Property>
```

### Float
Decimal numbers
```xml
<Property name="Speed" type="Float">
  <Default>1.5</Default>
  <Description>Movement speed multiplier</Description>
  <Min>0.0</Min>
  <Max>10.0</Max>
</Property>
```

### Boolean
True/False values
```xml
<Property name="IsHostile" type="Boolean">
  <Default>true</Default>
  <Description>Will the enemy attack on sight</Description>
</Property>
```

### Dropdown
Single selection from a list
```xml
<Property name="Behavior" type="Dropdown">
  <Default>Patrol</Default>
  <Description>AI behavior mode</Description>
  <Options>
    <Option>Patrol</Option>
    <Option>Guard</Option>
    <Option>Flee</Option>
    <Option>Berserker</Option>
  </Options>
</Property>
```

### Checkbox
Multiple selections from a list
```xml
<Property name="Immunities" type="Checkbox">
  <Description>Types of damage this enemy is immune to</Description>
  <Options>
    <Option>Fire</Option>
    <Option>Ice</Option>
    <Option>Electric</Option>
    <Option>Poison</Option>
  </Options>
</Property>
```

### Color
Color picker with RGB sliders
```xml
<Property name="GlowColor" type="Color">
  <Default>#FF0000</Default>
  <Description>Color of the glow effect (hex format #RRGGBB)</Description>
</Property>
```
Supports hex format: `#RRGGBB` (e.g., `#FF0000` for red, `#00FF00` for green)
UI features RGB sliders (0-255 for each channel) with live preview

## Examples

See the included files for complete examples:
- `Default.xml` - Minimal moveable properties
- `Enemy.xml` - Full example with multiple property types
- `StaticProperties.xml` - Static mesh property template

For complete documentation, see `/PROPERTY_SYSTEM.md` in the root directory.
