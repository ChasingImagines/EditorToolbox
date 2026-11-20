# Editor Toolbox for Unity (Türkçe)

[![Lisans: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE)
[![Unity 2021.3–Unity 6](https://img.shields.io/badge/unity-2021.3%E2%80%936-blue.svg)](https://unity.com/)
[![Sürüm 2.0.0](https://img.shields.io/badge/version-2.0.0-orange.svg)](package.json)

Unity (2021.3 – Unity 6) için modüler, hafif ve yüksek performanslı Inspector geliştirme ve üretkenlik kiti. Akıllı varlık/bileşen/arayüz seçicisi, yerinde `ScriptableObject` editörü ve temel özellik nitelikleriyle editör iş akışınızı hızlandırır—**sıfır gereksiz yük, sıfır harici bağımlılık**.

---

## ✨ Öne Çıkan Yetenekler ve Gerçek Kullanım Senaryoları

### 1. 🔍 Akıllı Varlık, Prefab ve Arayüz Seçici (`[Find]`)
Upuzun proje klasörlerinde kaybolmaya veya pencereler arasında sürükle-bırak yapmaya son:

- **Tüm `UnityEngine.Object` Varlık Tipleriyle Tam Uyumluluk:**
  `ScriptableObject`, `AudioClip`, `Material`, `Texture2D`, `Sprite` (alt-varlık / sub-asset dahil), `Mesh`, `AnimationClip`, `Shader`, `TextAsset`, prefab ve bileşenleri doğrudan seçin:
  ```csharp
  [Find(Source = FindSource.Assets)]
  public AudioClip impactClip;

  [Find(Source = FindSource.Assets)]
  public Sprite itemIcon;
  ```

- **Bileşen veya Arayüze (Interface) Sahip Prefabları Filtreleme (`GameObject` Saklama):**
  Inspector'da `GameObject` olarak bir prefab atamak, ancak **yalnızca üzerinde belirli bir bileşen veya interface (`IInteractable`, `DamageDealer` vb.) bulunan prefab'ların seçilebilmesini** mi istiyorsunuz?
  ```csharp
  // GameObject üzerindeki tüm bileşenleri tarar, yalnızca IInteractable içeren prefabları listeler
  [Find(typeof(IInteractable), Source = FindSource.Prefabs)]
  public GameObject interactablePrefab;

  // Yalnızca DamageDealer bileşeni bulunan prefabları listeler
  [Find(typeof(DamageDealer), Source = FindSource.Prefabs)]
  public GameObject weaponPrefab;
  ```

- **Prefab veya Sahneden Doğrudan Bileşen (`Component`) Bağlama:**
  ```csharp
  [Find(Source = FindSource.Prefabs | FindSource.Scene)]
  public NavMeshAgent agentTemplate;
  ```

- **Arayüz (Interface) Sözleşmesi Zorlama:**
  ```csharp
  [Find(typeof(IItemPickup))]
  public UnityEngine.Object itemPickup;

  public IItemPickup Item => itemPickup as IItemPickup;
  ```

- **`[Find]` + Yerinde Düzenleme Birlikte:**
  ```csharp
  [Find(Source = FindSource.Assets, Inline = true, StartInlineExpanded = true)]
  public BuildingDef targetBuilding;
  ```

- **Gelişmiş Arama Penceresi (`FindPickerWindow`):**
  - **Akıllı Sekmeler:** Alan tipine uymayan gereksiz sekmeleri gizler ve ilk dolu sekmeyi otomatik açar.
  - **Önbellekli Canlı Arama:** Türkçe ve aksanlı karakterleri (`İ`/`i`, `ş`/`s`, `ğ`/`g` vb.) normalleştirir, sıfır frame tahsisiyle hızlı ve bulanık (fuzzy) arama yapar.
  - **Hızlı İşlemler:** Tek tıkla temizleme (`×`), sürükle-bırak ile bileşen/`Sprite` ayıklama, tek tıkla Ping, çift tıkla Odaklanma/Açma.

---

### 2. 📝 Yerinde ScriptableObject Düzenleme (`[Inline]`)
`ScriptableObject` alanlarını başka pencere açmadan doğrudan mevcut Inspector içinde düzenleyin.
- **`[Inline(startExpanded, MaxDepth = 4)]`**: Katlanabilir açma/kapatma oku ve yapılandırılabilir derinlik sınırı sunar.
- **Döngü Tespiti ve Derinlik Limiti**: Gerçek döngüsel referansları keserken çok katmanlı normal hiyerarşilere `MaxDepth`'e kadar izin verir.
- **Güvenli Önbellek Yaşam Döngüsü**: `SerializedObject` örneklerini yeniden kullanır; seçim, derleme ve Play Mode değişimlerinde otomatik temizler (`Dispose`).
- **`[InlineReadOnly]`**: Inline panelinde alanı salt-okunur gösterir (`System.Attribute` tabanlı olduğu için `[Range]`, `[Find]` vb. ile çakışmaz).
- **`[InlineIgnore]`**: Belirli alanları inline görünümde gizler.

---

### 3. 🔒 Inspector Yardımcı Nitelikleri
- **`[ReadOnlyField]`**: Alanları Inspector'da salt-okunur gösterir ve serileştirmeyi korur (`Unity.Collections.ReadOnly` ile isim çakışması yaşamaz).

---

## 📦 Kurulum

### Unity Package Manager (Git URL)
1. Unity'de **Window > Package Manager** penceresini açın.
2. **+** butonuna basıp **Add package from git URL...** seçeneğini seçin.
3. Adresi yapıştırın:
   ```text
   https://github.com/ChasingImagines/EditorToolbox.git
   ```

---

## 🚀 Kapsamlı Örnek

```csharp
using UnityEngine;
using UnityEngine.AI;
using Yasin.EditorToolbox;

public class SpawnerExample : MonoBehaviour
{
    [Header("1. Arayüze Göre Prefab Filtreleme (GameObject saklar)")]
    [Find(typeof(IEnemy), Source = FindSource.Prefabs)]
    public GameObject enemyPrefab;

    [Header("2. Prefab/Sahneden Doğrudan Bileşen Bağlama")]
    [Find(Source = FindSource.Prefabs | FindSource.Scene)]
    public NavMeshAgent agentReference;

    [Header("3. Ses ve Görsel Varlık Seçimi")]
    [Find(Source = FindSource.Assets)]
    public AudioClip spawnSfx;

    [Find(Source = FindSource.Assets)]
    public Sprite enemyIcon;

    [Header("4. ScriptableObject Yerinde Düzenleme")]
    [Inline(startExpanded: false, MaxDepth = 4)]
    public EnemyStatsSO enemyStats;

    [Header("5. Salt-Okunur Bilgi Alanı")]
    [ReadOnlyField]
    public int currentSpawnCount = 0;
}
```

---

## 👥 Emeği Geçenler (Credits)
- **Geliştirici:** [Yasin Öztürk](https://github.com/ChasingImagines)
- **AI Pair-Programmer:** Antigravity (Google DeepMind)

---

## 📄 Lisans
Bu proje MIT Lisansı altında sunulmaktadır - detaylar için [LICENSE](LICENSE) dosyasına göz atabilirsiniz.
