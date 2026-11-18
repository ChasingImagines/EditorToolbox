# Editor Toolbox for Unity (Türkçe)

Unity için modüler, hafif ve yüksek performanslı Inspector genişletme ve üretkenlik kiti. Tip-güvenli varlık seçici, yerinde ScriptableObject editörü ve temel özellik nitelikleriyle editör iş akışınızı hızlandırır.

---

## ✨ Öne Çıkan Özellikler

### 1. 🔍 Varlık ve Referans Seçici (`[Find]`)
Upuzun proje klasörlerinde kaybolmaya veya karmaşık pencereler arasında sürükleyip bırakmaya son:
- **`[Find]` Niteliği**: `UnityEngine.Object`, `ScriptableObject`, `Component` veya `GameObject` alanlarını işaretler.
- **Gelişmiş Arama Penceresi (`FindPickerWindow`)**:
  - Türkçe ve özel karakter normalizasyonu ile canlı filtreleme (örn. 'ı'/'i' ayrımı yapmadan hızlı arama).
  - Sekmeli yapı: **Sahne Nesneleri (Scene)**, **Prefablar (Prefabs)** ve **Proje Varlıkları (Assets)**.
  - Sürükle & Bırak ve çift tıklamayla Project/Hierarchy üzerinde nesneye odaklanma (Ping/Focus).
  - Klavye desteği (Ok tuşları ve Enter).

### 2. 📝 Yerinde ScriptableObject Düzenleme (`[Inline]`)
ScriptableObject alanlarını başka pencereye gitmeden doğrudan mevcut Inspector içinde açın ve düzenleyin.
- **`[Inline]`**: Alanın yanına katlanabilir (foldout) açma/kapatma oku ekler.
- **Döngüsel Referans Koruması**: İç içe geçmiş nesnelerin birbirini referans vermesi durumunda sonsuz döngüyü ve editör kilitlenmesini engeller.
- **`[InlineReadOnly]`**: Inline panelinde alanın sadece okunabilir (gri/disabled) gösterilmesini sağlar.
- **`[InlineIgnore]`**: Belirli alanların inline görünümünde gizlenmesini sağlar.

### 3. 🔒 Inspector Yardımcı Nitelikleri
- **`[ReadOnlyField]`**: Alanları Inspector üzerinde gri (düzenlenemez) gösterir. Serileştirmeyi bozmaz ve `Unity.Collections.ReadOnly` ile isim çakışması yaşamaz.

---

## 📦 Kurulum

### Unity Package Manager (Git URL)
1. Unity'de **Window > Package Manager** penceresini açın.
2. **+** butonuna basıp **Add package from git URL...** seçeneğini seçin.
3. Şu adresi yapıştırın:
   ```text
   https://github.com/ChasingImagines/EditorToolbox.git
   ```

---

## 🚀 Örnek Kullanım

```csharp
using UnityEngine;
using Yasin.EditorToolbox;

public class BuildingManager : MonoBehaviour
{
    [Header("Referans Arama")]
    [Find(typeof(BuildingDef), Source = FindSource.Assets)]
    public BuildingDef selectedBuilding;

    [Header("Yerinde Düzenleme")]
    [Inline]
    public BuildingSettings defaultSettings;

    [Header("Bilgi")]
    [ReadOnlyField]
    public int activeWorkersCount = 5;
}
```

---

## 📄 Lisans
Bu proje MIT Lisansı altında sunulmaktadır - detaylar için [LICENSE](LICENSE) dosyasına göz atabilirsiniz.
