using UnityEngine;

// Bir ziyaretçi türünün VERİSİ. Kod değil, ayar dosyası.
// CreateAssetMenu: Project'te sağ tık → Create → BeQuiet → Ziyaretci Turu menüsünü ekler.
[CreateAssetMenu(fileName = "YeniZiyaretci", menuName = "BeQuiet/Ziyaretci Turu")]
public class VisitorType : ScriptableObject
{
    [Header("Kimlik")]
    public string displayName = "Ziyaretci";
    [TextArea] public string arrivalLine = "Kapi caliyor!";   // Gelince herkese altyazı
    public string actionPrompt = "kapiyi ac";                  // "E basili tut: ..." kısmı

    [Header("Ne sıklıkla gelir (diğerlerine göre ağırlık)")]
    public float weight = 1f;

    [Header("Kapıda davranış")]
    public float ringIntervalStart = 6f;    // Başta kaç saniyede bir çalar
    public float ringIntervalMin = 2f;      // En ısrarcı halinde
    public float ringSuspicion = 3f;        // Her çalışta ani şüphe
    public float timeToMaxSeverity = 30f;   // Kaç saniyede en ısrarcı hale gelir
    public float maxMultiplier = 1.5f;      // En kötü halde sürekli baskı çarpanı

    [Header("Çözüm")]
    public float repairDuration = 2f;       // E'yi kaç saniye tutmak gerekir
}