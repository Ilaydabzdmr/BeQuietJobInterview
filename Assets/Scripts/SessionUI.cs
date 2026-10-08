using TMPro;
using UnityEngine;

// Ekranın üstünde aşama ve kalan süre. Herkes görür (ev arkadaşları da "kaç dakika kaldı?" bilsin).
public class SessionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;   // OturumYazi

    void Update()
    {
        var s = InterviewSession.Instance;
        if (s == null || !s.IsSpawned)
        {
            label.text = "";
            return;
        }

        switch (s.Phase.Value)
        {
            case SessionPhase.Hazirlik:
                label.text = "Hazırlık: Mülakatçı laptoptan Meet'e katılınca görüşme başlar";
                break;

            case SessionPhase.Gorusmede:
                int t = Mathf.CeilToInt(s.TimeRemaining.Value);   // Yukarı yuvarla: 0.3 sn → 1
                label.text = $"Görüşme: {t / 60}:{t % 60:00}";      // 125 → "2:05"
                break;

            case SessionPhase.Basarili:
                label.text = "GÖRÜŞME BAŞARILI! (host: R ile yeni tur)";
                break;

            case SessionPhase.Basarisiz:
                label.text = "";   // Büyük "GÖRÜŞME BİTTİ" paneli zaten konuşuyor
                break;
        }
    }
}