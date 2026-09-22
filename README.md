# AbeAttributes

A lightweight, extensible Inspector framework for Unity.

AbeAttributes builds its own **Property Model** and processes properties through a modular Editor pipeline. The goal is to make Inspector features easy to extend without turning the Inspector itself into a monolithic system.

The repository also contains **PopToConsole**, an IL-instrumented runtime debugging tool for observing method calls and value access/mutation during gameplay.

> **Status:** Actively developed. APIs and internal architecture may change.

---

## Features

### Inspector Framework

* Custom `AbeInspector` entry point
* `AbePropertyTree` / `AbeProperty` property model
* Reflection-based member discovery
* Unified value access through `AbeValueEntry`
* Method invocation support
* Nested properties and collections
* Extensible property processors
* Property state updaters
* Attribute-driven validation
* Extensible drawer chain
* Custom Inspector rendering

### Built-in Attributes

**Drawing**

* `Dropdown`
* `EnumFlags`
* `MinMaxSlider`
* `ProgressBar`
* `ShowAssetPreview`
* `TableMatrix`

**State / Layout / Behaviour**

* `ShowIf`
* `HideIf`
* `EnableIf`
* `DisableIf`
* `ReadOnly`
* `HideIn`
* `HideLabel`
* `LabelText`
* `PropertySpace`
* `GroupStart`
* `GroupEnd`
* `OnValueChanged`

**Validation**

* `Required`
* `MinValue`
* `MaxValue`
* `ValidateInput`

These are all simply **Abe Attributes** from the user's point of view. Internally, an Attribute can be consumed by a Processor, State Updater, Validator, Drawer, or more than one of them.

---

## Quick Start

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

    [ShowIf(nameof(IsAdvanced))]
    public float AdvancedValue;

    [ReadOnly]
    public int RuntimeID;

    private bool IsAdvanced => Level >= 10;
}
```

AbeAttributes reads the metadata from these attributes and applies the corresponding Editor-side behavior to each property.

---

## More Examples

### Dropdown

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

### Conditional State

```csharp
public bool IsEditable;

[EnableIf(nameof(IsEditable))]
public int Value;

[HideIf(nameof(IsEditable))]
public int HiddenValue;
```

### Validation

```csharp
[Required]
public GameObject Target;

[MinValue(0)]
public int Health;

[ValidateInput(nameof(IsValid))]
public string Name;

private bool IsValid(string value)
{
    return !string.IsNullOrWhiteSpace(value);
}
```

### Table Matrix

```csharp
[TableMatrix]
public bool[,] Grid;
```

`TableMatrixAttribute` can also specify titles and a custom element-drawing method.

---

# Architecture

The framework is centered around a custom property model:

```text
Unity Editor
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
    ├── Member information
    ├── Attributes
    ├── ValueEntry
    ├── State
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

### AbeInspector

`AbeInspector` is the Unity Inspector entry point. It connects Unity's Inspector lifecycle to the AbeAttributes property system.

It is intentionally not responsible for individual Attribute implementations.

### AbePropertyTree

`AbePropertyTree` manages the property hierarchy for an inspected object:

```text
AbePropertyTree
├── Property
├── Property
└── Property
     ├── Child
     ├── Child
     └── Child
```

Nested objects and collection elements can therefore be represented as child `AbeProperty` instances.

### AbeProperty

`AbeProperty` is the central property model. It brings together information needed by the rest of the framework, including member metadata, Attributes, value access, state, and child properties.

The goal is to avoid repeatedly resolving the same Unity serialization and reflection information in every subsystem.

### Value Access

`AbeValueEntry` provides a common way to read and write values without forcing the rest of the framework to know whether a value came from serialization, reflection, a collection element, or another supported accessor.

```text
AbeProperty
    │
    ▼
AbeValueEntry
    ├── GetValue()
    └── SetValue(...)
```

### Reflection and Method Invocation

Reflection helpers resolve fields, properties, and methods used by Inspector features such as conditional attributes, dropdown providers, validation callbacks, and value-change callbacks.

---

# Property Pipeline

Processors, State Updaters, Validators, and Drawers remain separate **internal responsibilities** even though the public-facing concept is simply an Attribute.

### Processors

Processors are extension points for preparing or modifying property information.

They are useful when a feature needs to operate on the property model itself rather than merely draw a control.

Current processor-related systems include:

```text
PropertyProcessor
ValidatorProcessor
GroupProcessor
```

### State Updaters

State updaters convert Attribute/condition information into `AbePropertyState`.

For example:

```text
ShowIf / HideIf
EnableIf / DisableIf
ReadOnly
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

The drawer consumes the resulting state without needing to know why the state changed.

### Validators

Validators check whether the current property/value satisfies a rule.

Examples include:

* `Required`
* `MinValue`
* `MaxValue`
* `ValidateInput`

Validation is deliberately separate from rendering:

```text
AbeProperty
    ↓
Validator
    ↓
Validation feedback
```

### Drawers

Drawers are responsible for Inspector rendering.

AbeAttributes supports an extensible drawer chain, allowing multiple behaviors to participate in drawing the same property.

```text
AbeProperty
    ↓
Drawer 1
    ↓
Drawer 2
    ↓
Drawer 3
    ↓
Default Drawer
```

---

# Adding a New Attribute

A new feature normally starts with an Attribute that stores the feature's configuration.

```csharp
using System;

namespace AbeAttributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MyFeatureAttribute : AbeAttribute
    {
        public int Value { get; }

        public MyFeatureAttribute(int value)
        {
            Value = value;
        }
    }
}
```

Then decide which Editor-side system should consume it:

* **Drawer** — changes how the property is rendered.
* **Validator** — checks the current value or configuration.
* **State Updater** — changes visibility, enabled state, read-only state, etc.
* **Processor** — prepares or modifies property information.
* **Combination** — some features legitimately need more than one of the above.

The Attribute remains a lightweight description of intent; the Editor implementation owns the behavior.

---

# PopToConsole

PopToConsole is a separate debugging feature included in the repository.

Instead of relying on repeated `Debug.Log` calls, it instruments compiled members through Unity's IL Post Processing pipeline.

```text
[PopToConsole]
       │
       ▼
IL Post Processor
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

### Modes

`PopToConsoleMode` currently supports:

* `Invoke` — observe method invocation
* `Get` — observe reads where supported
* `Set` — observe writes where supported
* `Manual` — explicitly trigger a pop

Example:

```csharp
[PopToConsole(PopToConsoleMode.Invoke)]
private void Refresh()
{
    // ...
}
```

The Attribute also supports optional configuration such as `BreakPoint`, `ValuePath`, and `CustomDebugMethod`.

PopToConsole is intended to answer runtime behavior questions such as:

> When was this method called?

> When was this value accessed or changed?

This complements a traditional step debugger rather than trying to replace it.

---

# Project Structure

The repository is broadly divided into runtime-facing Attributes and Editor implementation:

```text
Core/
├── Attributes/              # Abe Attributes
├── PopToConsole/
│   ├── Runtime/             # Runtime API
│   └── Editor/              # IL Post Processor
└── Shared types

Editor/
├── AbeInspector/            # Inspector, Property Tree, property model
├── Drawers/                 # Inspector rendering
└── Validators/              # Editor-side validation
```

The Attribute definitions are intentionally treated as one public concept. Older checkouts may still contain directories such as `DrawerAttributes`, `MetaAttributes`, and `ValidatorAttributes`; these can be consolidated into a single `Attributes` folder without changing the internal pipeline.

---

# Installation

AbeAttributes is currently distributed as Unity source code rather than a Unity Package Manager package.

1. Clone or download this repository.
2. Copy it into your Unity project's `Assets` folder, preferably under a dedicated folder such as `Assets/AbeAttributes/`.
3. Open the project in Unity and allow the Editor scripts to compile.

The repository does not currently declare a package manifest or a minimum Unity version, so version support should be considered tied to the Unity version used while developing/testing the framework.

---

# Design Goals

* **Custom Property Model** — keep property information centralized and reusable.
* **Modular Pipeline** — separate processing, state, validation, and drawing responsibilities.
* **Lightweight Attributes** — Attributes describe intent; Editor systems implement behavior.
* **Extensibility** — new features should be addable without turning the core Inspector into a large switch statement.
* **Runtime Debugging** — PopToConsole addresses a different problem from Inspector presentation: tracing behavior during gameplay.

---

# Status

AbeAttributes is an actively developed open-source project and the architecture is still evolving.

Expect changes to APIs, folder organization, and internal implementation details as the framework is refined.

---

# License

MIT License. But technically, do whatever you want!
