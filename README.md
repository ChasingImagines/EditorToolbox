# Editor Toolbox for Unity

[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE)
[![Unity 2021.3–Unity 6](https://img.shields.io/badge/unity-2021.3%E2%80%936-blue.svg)](https://unity.com/)
[![Version 2.0.0](https://img.shields.io/badge/version-2.0.0-orange.svg)](package.json)

A modular, lightweight, high-performance Inspector enhancement toolkit for Unity (Unity 2021.3 through Unity 6). It supercharges your editor workflow with type-safe asset/prefab/interface pickers, in-place `ScriptableObject` inline editors, and essential custom attributes—**zero bloat, zero dependencies**.

[🇹🇷 Türkçe Dokümantasyon için buraya tıklayın (README.tr.md)](README.tr.md)

---

## ✨ Key Features & Real-World Use Cases

### 1. 🔍 Smart Asset, Prefab & Interface Finder (`[Find]`)
Never lose time dragging objects across nested inspector windows or digging through folders.

- **All `UnityEngine.Object` Asset Types Supported:**
  Pick `ScriptableObject`, `AudioClip`, `Material`, `Texture2D`, `Sprite` (including sub-assets), `Mesh`, `AnimationClip`, `Shader`, `TextAsset`, prefabs, components, and more.
  ```csharp
  [Find(Source = FindSource.Assets)]
  public AudioClip impactClip;

  [Find(Source = FindSource.Assets)]
  public Sprite itemIcon;
  ```

- **Filter Prefabs by Component or Interface (Storing `GameObject`):**
  Want a `GameObject` slot in the Inspector, but only want prefabs that implement `IInteractable` or have a `DamageDealer` component?
  ```csharp
  // Only prefabs with an IInteractable component are listed (checks all components on the GameObject).
  [Find(typeof(IInteractable), Source = FindSource.Prefabs)]
  public GameObject interactablePrefab;

  // Only prefabs with a DamageDealer component.
  [Find(typeof(DamageDealer), Source = FindSource.Prefabs)]
  public GameObject weaponPrefab;
  ```

- **Pick Specific Components Directly from Prefabs or Scene:**
  Bind `Component` references directly from Prefabs or Scene GameObjects (including multiple matching components on a single GameObject):
  ```csharp
  [Find(Source = FindSource.Prefabs | FindSource.Scene)]
  public NavMeshAgent agentTemplate;
  ```

- **Interface Reference Support:**
  Enforce C# interface contracts directly on serialized `UnityEngine.Object` fields:
  ```csharp
  [Find(typeof(IItemPickup))]
  public UnityEngine.Object itemPickup;

  public IItemPickup Item => itemPickup as IItemPickup;
  ```

- **Combined Find + Inline Editing:**
  ```csharp
  [Find(Source = FindSource.Assets, Inline = true, StartInlineExpanded = true)]
  public BuildingDef targetBuilding;
  ```

- **Rich Picker Window (`FindPickerWindow`):**
  - **Smart Source Tabs**: Automatically enables only tabs compatible with the target field type and opens on the first populated tab.
  - **Cached Live Search**: Diacritic/Turkish normalization (`İ`/`i`, `ş`/`s`, `ğ`/`g`, etc.), token & fuzzy matching with zero per-frame allocations.
  - **Quick Actions**: Single-button clear (`×`), type-safe drag & drop with component/sub-asset extraction, single-click Ping, double-click Select/Frame/Open.

---

### 2. 📝 In-Place ScriptableObject Editor (`[Inline]`)
Edit nested `ScriptableObject` references directly inside the parent Inspector without opening separate windows.
- **`[Inline(startExpanded, MaxDepth = 4)]`**: Adds a clean foldout toggle next to any `ScriptableObject` field with configurable nesting depth.
- **Cycle Detection & Depth Limits**: True graph cycle guards prevent circular loops while allowing multi-level non-circular chains up to `MaxDepth`.
- **Managed Cache Lifecycle**: Reuses `SerializedObject` wrappers cleanly and disposes them on selection change, assembly reload, or play-mode transitions.
- **`[InlineReadOnly]`**: Renders fields disabled inside the inline view (`System.Attribute`-based, so it combines cleanly with `[Range]`, `[Find]`, etc.).
- **`[InlineIgnore]`**: Hides internal or boilerplate fields from the inline view.

---

### 3. 🔒 Custom Property Attributes
- **`[ReadOnlyField]`**: Renders fields disabled in the Inspector while preserving serialization (avoids naming collisions with `Unity.Collections.ReadOnly`).

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
```json
{
  "dependencies": {
    "com.yasin.editortoolbox": "https://github.com/ChasingImagines/EditorToolbox.git"
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

    [Header("3. Asset & Sub-Asset Picking")]
    [Find(Source = FindSource.Assets)]
    public AudioClip spawnSfx;

    [Find(Source = FindSource.Assets)]
    public Sprite enemyIcon;

    [Header("4. ScriptableObject In-Place Editing")]
    [Inline(startExpanded: false, MaxDepth = 4)]
    public EnemyStatsSO enemyStats;

    [Header("5. Read-Only Field")]
    [ReadOnlyField]
    public int currentSpawnCount = 0;
}
```

---

## 👥 Credits & Acknowledgments
- **Author:** [Yasin Öztürk](https://github.com/ChasingImagines)
- **AI Pair-Programmer:** Antigravity (Google DeepMind)

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
