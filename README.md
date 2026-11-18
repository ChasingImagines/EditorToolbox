# Editor Toolbox for Unity

[![openupm](https://img.shields.io/npm/v/com.yasin.editortoolbox?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.yasin.editortoolbox/)
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE)
[![Unity 2021.3+](https://img.shields.io/badge/unity-2021.3%2B-blue.svg)](https://unity.com/)

A modular, lightweight, high-performance Inspector enhancement toolkit for Unity. It supercharges your editor workflow with type-safe asset finders, in-place ScriptableObject inline editors, and essential custom attributes—**zero bloat, zero dependencies**.

[🇹🇷 Türkçe Dokümantasyon için buraya tıklayın (README.tr.md)](README.tr.md)

---

## ✨ Key Features

### 1. 🔍 Asset & Reference Finder (`[Find]`)
Say goodbye to scrolling through endless asset lists or dragging items across inspector panes.
- **`[Find]` Attribute**: Decorate any `UnityEngine.Object`, `ScriptableObject`, `Component`, or `GameObject` field.
- **Rich Search Window (`FindPickerWindow`)**:
  - Live filtering with Turkish/diacritic character normalization.
  - Tabbed browsing: **Scene Objects**, **Prefabs**, and **Assets**.
  - Type-safe drag & drop area and double-click to ping/focus in Project or Hierarchy.
  - Keyboard navigation (Arrow keys + Enter).

### 2. 📝 In-Place ScriptableObject Editor (`[Inline]`)
Edit nested ScriptableObjects directly within the parent Inspector without opening separate windows.
- **`[Inline]`**: Adds a foldout toggle next to any ScriptableObject field.
- **Recursion-Safe**: Built-in circular reference guards prevent editor freezes or infinite loops.
- **`[InlineReadOnly]`**: Mark fields that should be displayed as disabled inside the inline view.
- **`[InlineIgnore]`**: Hide internal or boilerplate fields from the inline view.

### 3. 🔒 Custom Property Attributes
- **`[ReadOnlyField]`**: Mark fields as read-only / disabled in the Inspector while keeping them serialized. (Conflict-free name that avoids collisions with `Unity.Collections.ReadOnly`).

---

## 📦 Installation

### Option 1: Via Git URL (Unity Package Manager)
1. In Unity, open **Window > Package Manager**.
2. Click **+** > **Add package from git URL...**
3. Enter:
   ```text
   https://github.com/ChasingImagines/EditorToolbox.git
   ```

### Option 2: Via `manifest.json`
Add the package to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.yasin.editortoolbox": "https://github.com/ChasingImagines/EditorToolbox.git",
    ...
  }
}
```

---

## 🚀 Quick Start

```csharp
using UnityEngine;
using Yasin.EditorToolbox;

public class QuestGiver : MonoBehaviour
{
    [Header("Asset Search & Filter")]
    [Find(typeof(AudioClip), Source = FindSource.Assets)]
    public AudioClip greetingSound;

    [Find(FindSource.Scene)]
    public Transform questTarget;

    [Header("Inline ScriptableObject")]
    [Inline]
    public QuestDefinition questDef;

    [Header("Read Only")]
    [ReadOnlyField]
    public string uniqueSessionId = "QS-10029";
}
```

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
