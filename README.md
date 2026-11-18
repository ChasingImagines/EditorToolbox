# Editor Toolbox for Unity

[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE)
[![Unity 2021.3+](https://img.shields.io/badge/unity-2021.3%2B-blue.svg)](https://unity.com/)

A modular, lightweight, high-performance Inspector enhancement toolkit for Unity. It supercharges your editor workflow with type-safe asset/prefab finders, interface & component pickers, in-place ScriptableObject inline editors, and essential custom attributes—**zero bloat, zero dependencies**.

[🇹🇷 Türkçe Dokümantasyon için buraya tıklayın (README.tr.md)](README.tr.md)

---

## ✨ Key Features & Real-World Use Cases

### 1. 🔍 Smart Asset, Prefab & Interface Finder (`[Find]`)
Never lose time dragging objects across nested inspector windows or digging through folders.

- **Filter Prefabs by Component or Interface (Storing GameObject):**
  Want a `GameObject` slot in Inspector, but only want users to pick prefabs that implement `IInteractable` or have a `Health` component?
  ```csharp
  // Only prefabs/objects with IInteractable are shown in the search window!
  [Find(typeof(IInteractable), Source = FindSource.Prefabs)]
  public GameObject interactablePrefab;

  // Only prefabs with DamageDealer component
  [Find(typeof(DamageDealer), Source = FindSource.Prefabs)]
  public GameObject weaponPrefab;
  ```

- **Pick Specific Components Directly from Prefabs or Scene:**
  Instead of getting a GameObject and calling `GetComponent` at runtime, pick and bind the `Component` directly:
  ```csharp
  // Automatically inspects prefabs/scene, extracts NavMeshAgent, and binds the component
  [Find(Source = FindSource.Prefabs | FindSource.Scene)]
  public NavMeshAgent agentTemplate;
  ```

- **Interface Reference Support:**
  Unity cannot natively serialize pure C# interfaces. With `UnityEngine.Object` and `[Find]`, you can enforce interface contracts in the editor:
  ```csharp
  [Find(typeof(IItemPickup))]
  public UnityEngine.Object itemPickup;

  public IItemPickup Item => itemPickup as IItemPickup;
  ```

- **ScriptableObjects & Assets:**
  ```csharp
  [Find(typeof(BuildingDef), Source = FindSource.Assets)]
  public BuildingDef targetBuilding;
  ```

- **Rich Picker Window (`FindPickerWindow`):**
  - **Tabs**: Scene Objects, Prefabs, and Assets.
  - **Live Search**: Normalized search ignoring accents/Turkish diacritics (e.g. `ı`/`i`, `ö`/`o`).
  - **Drag & Drop**: Automatically validates and extracts required component when dropped.
  - **Ping & Frame**: Single click pings in Project/Hierarchy, double-click focuses in Scene view or opens asset.

---

### 2. 📝 In-Place ScriptableObject Editor (`[Inline]`)
Edit nested ScriptableObjects directly within the parent Inspector without opening separate windows.
- **`[Inline]`**: Adds a foldout toggle next to any ScriptableObject field.
- **Recursion-Safe**: Built-in circular reference guards prevent editor freezes or infinite loops.
- **`[InlineReadOnly]`**: Mark fields that should be displayed as disabled inside the inline view.
- **`[InlineIgnore]`**: Hide internal or boilerplate fields from the inline view.

---

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

## 🚀 Complete Example

```csharp
using UnityEngine;
using UnityEngine.AI;
using Yasin.EditorToolbox;

public class SpawnerExample : MonoBehaviour
{
    [Header("1. Filter Prefab by Interface (stores GameObject)")]
    [Find(typeof(IEnemy), Source = FindSource.Prefabs)]
    public GameObject enemyPrefab;

    [Header("2. Bind Component directly from Prefab/Scene")]
    [Find(Source = FindSource.Prefabs | FindSource.Scene)]
    public NavMeshAgent agentReference;

    [Header("3. ScriptableObject In-Place Editing")]
    [Inline]
    public EnemyStatsSO enemyStats;

    [Header("4. Read-Only Field")]
    [ReadOnlyField]
    public int currentSpawnCount = 0;
}
```

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
