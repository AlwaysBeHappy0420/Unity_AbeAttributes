# AbeAttributes

A lightweight, extensible Inspector framework for Unity.

AbeAttributes builds its own **Property Model** and processes properties through a modular Editor pipeline.

The goal is simple:

> Make Inspector features easy to extend without turning the Inspector into a monolithic system.

The repository also contains **PopToConsole**, a separate runtime debugging tool for observing method calls and value access/mutation during gameplay.

> **Status:** Actively developed. APIs and internal architecture may change.

---

## Features

### Inspector Framework

* Custom `AbeInspector` entry point
* `AbePropertyTree` / `AbeProperty` property model
* Reflection-based member discovery
* Unified value access through `AbeValueEntry`
* Serialized and reflection-based value access
* Nested object inspection
* Collection and collection-element inspection
* Per-element attributes for collections
* Method invocation support
* Extensible property processors
* Property state updaters
* Validation
* Extensible drawer chain
* Custom Inspector rendering

### Built-in Attributes

#### Drawing

* `Dropdown`
* `EnumFlags`
* `MinMaxSlider`
* `ProgressBar`
* `ShowPreview`
* `TableMatrix`

#### Validation

* `Required`
* `MinValue`
* `MaxValue`
* `ValidateInput`

Attributes are intentionally kept lightweight.

From the user's perspective, an Attribute is simply metadata describing intent.

Internally, that Attribute may be consumed by a:

* Processor
* State Updater
* Validator
* Drawer
* Or multiple systems

---

# Quick Start

```csharp
using UnityEngine;
using AbeAttributes;

public class CharacterData : MonoBehaviour
{
    [ProgressBar(100f)]
    public float Health = 75f;

    [MinValue(0)]
    public int Level = 1;

    [Required]
    public GameObject Target;

    [ValidateInput(nameof(IsValid))]
    public string Name;

    private bool IsValid(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}
```

AbeAttributes reads the metadata from these Attributes and applies the corresponding Editor-side behavior to each property.

---

# Examples

## Dropdown

A basic Dropdown can use a method or member as its value source.

```csharp
[Dropdown(nameof(GetWeapons))]
public int SelectedWeapon;

private DropdownList<int> GetWeapons()
{
    var list = new DropdownList<int>();

    list.Add("Sword", 0);
    list.Add("Bow", 1);
    list.Add("Staff", 2);

    return list;
}
```

The selected value is written back through `AbeValueEntry`.

---

## Dropdown on Collections

Dropdown also works on collection elements.

```csharp
private List<string> labelList = new();

[Dropdown(nameof(labelList))]
[SerializeField]
private List<string> labels = new();
```

Instead of treating the entire `List<string>` as one Dropdown, AbeAttributes represents the collection as child properties:

```text
labels
├── Element 0   [Dropdown]
├── Element 1   [Dropdown]
├── Element 2   [Dropdown]
└── ...
```

Each element uses its actual element type:

```text
List<string>
    ↓
Element 0 → string
Element 1 → string
Element 2 → string
```

Selecting a value therefore writes directly to:

```csharp
labels[index] = selectedValue;
```

The same property model is used for both native and serialized collections.

### Attribute Propagation

Collection elements inherit the parent's Abe Attributes.

For example:

```csharp
[Dropdown(nameof(labelList))]
public List<string> labels;
```

is internally represented approximately as:

```text
List<string>
    │
    ├── Element 0
    │      └── DropdownAttribute
    │
    ├── Element 1
    │      └── DropdownAttribute
    │
    └── Element 2
           └── DropdownAttribute
```

This allows an Attribute to operate naturally on individual collection elements without special collection-specific drawer logic.

---

# Architecture

AbeAttributes is centered around a custom property model.

```text
Unity Inspector
      │
      ▼
AbeInspector
      │
      ▼
AbePropertyTree
      │
      ▼
AbeProperty
      │
      ├── Member Information
      ├── Attributes
      ├── ValueEntry
      ├── State
      ├── Context
      └── Children
            │
            ▼
      Property Processing
            │
            ├── Processors
            ├── State Updaters
            └── Validators
            │
            ▼
      Drawer Resolution
            │
            ▼
        Drawer Chain
            │
            ▼
       Unity Editor GUI
```

The important idea is that the Inspector does not need to know how every Attribute works.

The property model provides a common representation that the rest of the framework can consume.

---

# AbeInspector

`AbeInspector` is the Unity Inspector entry point.

Its responsibility is to connect Unity's Inspector lifecycle to the AbeAttributes property system.

Individual Attributes and their behaviors remain outside the Inspector itself.

---

# AbePropertyTree

`AbePropertyTree` manages the property hierarchy for an inspected object.

A property tree can contain:

```text
Property
├── Child
├── Child
└── Child
    ├── Child
    └── Child
```

Nested objects and collections are represented as child `AbeProperty` instances.

This means the same property systems can operate on:

* Normal fields
* Properties
* Nested objects
* Serialized properties
* Collection elements
* Methods

---

# AbeProperty

`AbeProperty` is the central property model.

It brings together:

* Member metadata
* Attributes
* Value access
* Property state
* Context
* Parent/child relationships
* Collection information

A property can therefore represent more than just a field.

For example:

```text
AbeProperty
├── Normal Field
├── Serialized Property
├── Native Property
├── Method
├── Collection Element
└── Group
```

The goal is to keep property information centralized instead of repeatedly resolving reflection and serialization information in every subsystem.

---

# AbeValueEntry

`AbeValueEntry` provides a common interface for reading and writing values.

```text
AbeProperty
      │
      ▼
AbeValueEntry
├── GetValue()
├── GetValues()
├── SetValue(...)
├── Collection Access
└── Method Invocation
```

The rest of the framework does not need to know whether a value came from:

* Unity serialization
* Reflection
* A collection element
* A nested property
* A method

The appropriate accessor handles that internally.

---

# Collection Value Model

Collections are represented as properties with child properties.

For:

```csharp
List<int> Values;
```

the property tree can be:

```text
Values
├── Element 0
├── Element 1
└── Element 2
```

Each element has its own `AbeProperty` and `AbeValueEntry`.

The element's value type is the actual element type:

```text
List<int>
    ↓
Element
    ↓
int
```

rather than:

```text
Element
    ↓
List<int>
```

This is important because element-level Attributes should operate on the element itself.

For example:

```csharp
[Dropdown(nameof(Options))]
public List<string> Values;
```

creates individual string Dropdowns.

When an element changes, the existing property chain resolves the collection and writes the value back to its index.

---

# Property Pipeline

Processors, State Updaters, Validators, and Drawers remain separate internal responsibilities.

The public-facing concept is simply an Attribute.

```text
Attribute
   │
   ├── Processor
   ├── State Updater
   ├── Validator
   └── Drawer
```

A feature may use one system or several depending on what it needs to do.

---

## Processors

Processors operate on the property model itself.

They are useful when a feature needs to modify or prepare property information before drawing.

The project currently contains processor systems such as:

```text
Property Processor
Validator Processor
Group Processor
```

---

## State Updaters

State Updaters convert Attribute or condition information into `AbePropertyState`.

For example, a state-related feature can conceptually work like:

```text
Attribute / Condition
        │
        ▼
   State Updater
        │
        ▼
AbePropertyState
        │
        ▼
      Drawer
```

The drawer does not need to know why a property became disabled, hidden, or otherwise modified.

---

## Validators

Validators check whether a property's current value satisfies a rule.

Examples:

```text
Required
MinValue
MaxValue
ValidateInput
RequiredType
```

Validation is deliberately kept separate from rendering.

```text
AbeProperty
     ↓
 Validator
     ↓
Validation Result
     ↓
Inspector Feedback
```

---

# Drawers

Drawers are responsible for Inspector rendering.

AbeAttributes uses an extensible drawer chain.

```text
AbeProperty
     │
     ▼
 Drawer 1
     │
     ▼
 Drawer 2
     │
     ▼
 Drawer 3
     │
     ▼
Default Drawer
```

This allows multiple systems to participate in drawing the same property.

For example, a collection property with a Dropdown Attribute can be handled conceptually as:

```text
Dropdown Drawer
      │
      └── collection detected
              │
              ▼
        next drawer
              │
              ▼
       Collection Drawer
              │
              ▼
        Element 0
              └── Dropdown Drawer
        Element 1
              └── Dropdown Drawer
        Element 2
              └── Dropdown Drawer
```

This keeps collection rendering and element rendering separate.

---

# Adding a New Attribute

A new feature normally starts with a lightweight Attribute that stores configuration.

```csharp
using System;

namespace AbeAttributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MyFeatureAttribute
        : AbeAttribute
    {
        public int Value { get; }

        public MyFeatureAttribute(int value)
        {
            Value = value;
        }
    }
}
```

Then decide which Editor-side system should consume it.

### Drawer

Use a Drawer when the feature changes how the property is rendered.

### Validator

Use a Validator when the feature checks the current value or configuration.

### State Updater

Use a State Updater when the feature changes property state such as enabled/disabled or visibility.

### Processor

Use a Processor when the feature needs to modify or prepare the property model itself.

### Combination

Some features legitimately require multiple systems.

The Attribute remains a lightweight description of intent; the Editor implementation owns the behavior.

---

# PopToConsole

PopToConsole is a separate runtime debugging feature included in the repository.

Instead of relying on repeated `Debug.Log` calls, it instruments compiled members so that selected runtime behavior can be observed during gameplay.

Conceptually:

```text
[PopToConsole]
       │
       ▼
Instrumentation
       │
       ▼
Instrumented Method / Accessor
       │
       ▼
PopToConsoleRuntime
       │
       ▼
Unity Console
```

The goal is to answer questions such as:

> When was this method called?

> When was this value accessed?

> When was this value changed?

This complements a traditional step debugger rather than trying to replace it.

---

# Project Structure

The repository is broadly divided into public/runtime-facing definitions and Editor implementation.

A simplified structure is:

```text
Core/
├── Attributes/
├── PopToConsole/
│   ├── Runtime/
│   └── Editor/
└── Shared Types/

Editor/
├── AbeInspector/
│   ├── Property Tree
│   ├── AbeProperty
│   ├── Value Access
│   ├── Drawers
│   └── Processing
└── Other Editor Systems
```

The exact folder organization may continue to evolve while the architecture is refined.

The important separation is between:

```text
Attribute / Runtime Definition
            │
            ▼
       Editor Pipeline
```

rather than putting Inspector behavior directly into the Attribute definition.

---

# Installation

AbeAttributes is currently distributed as Unity source code rather than as a Unity Package Manager package.

1. Clone or download this repository.
2. Copy the project into your Unity project's `Assets` folder.
3. A dedicated location such as `Assets/AbeAttributes/` can be used.
4. Open the Unity project and allow the Editor scripts to compile.

There is currently no required package manifest or minimum Unity version declared by this repository, so Unity version compatibility should be considered based on the version used to develop and test the framework.

---

# Design Goals

### Custom Property Model

Keep property information centralized and reusable.

### Modular Pipeline

Keep processing, state, validation, value access, and drawing responsibilities separate.

### Lightweight Attributes

Attributes describe intent instead of implementing the entire feature.

### Extensibility

New features should be addable without turning the Inspector into a large collection of special cases.

### Unified Property Handling

Nested objects, collections, serialized values, reflection values, and methods should be represented through the same property model wherever practical.

### Unity Integration

Unity-specific types such as Addressables references should integrate cleanly without exposing irrelevant internal implementation details.

### Runtime Debugging

PopToConsole addresses runtime behavior tracing, which is a different problem from Inspector presentation.

---

# Status

AbeAttributes is an actively developed open-source project.

The architecture is still evolving, and APIs or internal implementation details may change.

The current focus is on keeping the framework:

* Modular
* Lightweight
* Extensible
* Easy to understand
* Practical for real Unity projects

Expect changes to APIs, folder organization, and implementation details as the project continues to evolve.

---

# License

MIT License.
