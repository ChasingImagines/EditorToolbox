# Editor Toolbox for Unity (Türkçe)

Unity için modüler, hafif ve yüksek performanslı Inspector geliştirme ve üretkenlik kiti. Akıllı bileşen/arayüz filtreli prefab seçici, yerinde ScriptableObject editörü ve temel özellik nitelikleriyle editör iş akışınızı hızlandırır.

---

## ✨ Öne Çıkan Yetenekler ve Gerçek Kullanım Senaryoları

### 1. 🔍 Akıllı Varlık, Prefab ve Arayüz Seçici (`[Find]`)
Upuzun proje klasörlerinde kaybolmaya veya karmaşık pencereler arasında sürükleyip bırakmaya son:

- **Bileşen veya Arayüze (Interface) Sahip Prefabları Filtreleme (`GameObject` Saklama):**
  Inspector'da `GameObject` olarak bir prefab atamak istiyorsunuz, ancak **yalnızca üzerinde belirli bir bileşen veya interface (`IInteractable`, `Health`, vb.) olan prefab'ların seçilebilmesini** mi istiyorsunuz?
  ```csharp
  // Arama penceresi yalnızca üzerinde IInteractable olan prefabları listeler!
  [Find(typeof(IInteractable), Source = FindSource.Prefabs)]
  public GameObject interactablePrefab;

  // Yalnızca DamageDealer bileşeni bulunan prefabları listeler
  [Find(typeof(DamageDealer), Source = FindSource.Prefabs)]
  public GameObject weaponPrefab;
  ```

- **Prefab veya Sahneden Doğrudan Bileşen (`Component`) Seçip Alma:**
  `GameObject` alıp runtime'da `GetComponent` çağırmak yerine, doğrudan prefab'ın üzerindeki bileşeni değişkene bağlayın:
  ```csharp
  // Sahne ve prefabları tarar, üzerindeki NavMeshAgent bileşenini doğrudan bağlar
  [Find(Source = FindSource.Prefabs | FindSource.Scene)]
  public NavMeshAgent agentTemplate;
  ```

- **Arayüz (Interface) Sözleşmesi Zorlama:**
  Unity saf C# arayüzlerini serileştiremez. `UnityEngine.Object` ve `[Find]` ile Inspector seviyesinde arayüz desteği sağlayabilirsiniz:
  ```csharp
  [Find(typeof(IItemPickup))]
  public UnityEngine.Object itemPickup;

  public IItemPickup Item => itemPickup as IItemPickup;
  ```

- **İsteğe Bağlı Alt Nesne (Child) Taraması (`IncludeChildren`):**
  Varsayılan olarak `[Find]` kök (root) nesneyi inceler. İhtiyaç olduğunda `IncludeChildren = true` diyerek prefab hiyerarşisinin derinliklerine inebilirsiniz:
  ```csharp
  // Kök ve alt çocuk nesneleri (children) tarar
  [Find(typeof(HitboxCollider), IncludeChildren = true)]
  public HitboxCollider targetHitbox;
  ```

- **ScriptableObject ve Varlık Seçimi:**
  ```csharp
  [Find(typeof(BuildingDef), Source = FindSource.Assets)]
  public BuildingDef targetBuilding;
  ```

- **Gelişmiş Arama Penceresi (`FindPickerWindow`):**
  - **Sekmeler:** Sahne Nesneleri (Scene), Prefablar (Prefabs) ve Varlıklar (Assets).
  - **Canlı Arama:** Türkçe ve aksanlı karakterleri normalleştirerek hızlı arama (örn. `ı`/`i`, `ş`/`s`).
  - **Sürükle & Bırak:** Uygun nesneyi veya bileşeni otomatik ayıklar ve bağlar.
  - **Odaklanma (Ping/Focus):** Tek tıkla Hierarchy/Project'te gösterir, çift tıkla Scene görünümünde odaklar.

---

### 2. 📝 Yerinde ScriptableObject Düzenleme (`[Inline]`)
ScriptableObject alanlarını başka pencereye gitmeden doğrudan mevcut Inspector içinde açın ve düzenleyin.
- **`[Inline]`**: Alanın yanına katlanabilir (foldout) açma/kapatma oku ekler.
- **Döngüsel Referans Koruması**: İç içe geçmiş nesnelerin birbirini referans vermesi durumunda sonsuz döngüyü ve editör kilitlenmesini engeller.
- **`[InlineReadOnly]`**: Inline panelinde alanın sadece okunabilir (gri/disabled) gösterilmesini sağlar.
- **`[InlineIgnore]`**: Belirli alanların inline görünümünde gizlenmesini sağlar.

---

### 3. 🔒 Inspector Yardımcı Nitelikleri
- **`[ReadOnlyField]`**: Alanları Inspector üzerinde gri (düzenlenemez) gösterir. Serileştirmeyi bozmaz ve `Unity.Collections.ReadOnly` ile isim çakışması yaşamaz.

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
    [Header("1. Arayüze göre Prefab Filtreleme (GameObject saklar)")]
    [Find(typeof(IEnemy), Source = FindSource.Prefabs)]
    public GameObject enemyPrefab;

    [Header("2. Prefab/Sahneden Doğrudan Bileşen Bağlama")]
    [Find(Source = FindSource.Prefabs | FindSource.Scene)]
    public NavMeshAgent agentReference;

    [Header("3. ScriptableObject Yerinde Düzenleme")]
    [Inline]
    public EnemyStatsSO enemyStats;

    [Header("4. Salt-Okunur Bilgi Alanı")]
    [ReadOnlyField]
    public int currentSpawnCount = 0;
}
```

---

## 📄 Lisans
Bu proje MIT Lisansı altında sunulmaktadır - detaylar için [LICENSE](LICENSE) dosyasına göz atabilirsiniz.
