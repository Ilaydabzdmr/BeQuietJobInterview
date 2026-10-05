using TMPro;
using UnityEngine;

// İK Uzmanı'nın tepkilerini HERKESE gösterir. Tamamen yerel.
public class InterviewReactionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text subtitleText;      // Altyazi
    [SerializeField] private GameObject gameOverPanel;   // GorusmeBittiPanel
    [SerializeField] private float subtitleDuration = 3f;

    private SuspicionStage lastStage = SuspicionStage.Sakin;   // En son gördüğümüz aşama
    private float subtitleTimer; // Altyazının kalan süresi

    // Başka scriptler (kapı ziyaretçileri) altyazı gösterebilsin diye.
    public static InterviewReactionUI Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        subtitleText.text = "";
        gameOverPanel.SetActive(false);
    }

    void Update()
    {
        var meter = SuspicionMeter.Instance;
        if (meter == null || !meter.IsSpawned) return;

        // Polling: Aşama değişti mi?
        var stage = meter.Stage.Value;
        if (stage != lastStage)
        {
            // Sadece KÖTÜYE giderken konuşur. Düşüşte sessiz kalır.
            if (stage > lastStage) OnStageRaised(stage);

            // Panel: Bitti ise açık, değilse kapalı (R ile sıfırlanınca kapanır).
            gameOverPanel.SetActive(stage == SuspicionStage.Bitti);

            lastStage = stage;
         
        }

        // Altyazı süresi dolunca temizle.
        if (subtitleTimer > 0f)
        {
            subtitleTimer -= Time.deltaTime;
            if (subtitleTimer <= 0f) subtitleText.text = "";
        }
    }

    private void OnStageRaised(SuspicionStage stage)
    {
        switch (stage)
        {
            case SuspicionStage.Suphelendi:
                ShowSubtitle("İK Uzmanı: Hmm... Arkada bir şey mi oluyor?");
                break;
            case SuspicionStage.Uyardi:
                ShowSubtitle("İK Uzmanı: Başka bir zaman mı arasak?");
                break;
                // Bitti için altyazı yok, büyük panel yeterince konuşuyor.
        }
    }

    public void ShowSubtitle(string line)
    {
        subtitleText.text = line;
        subtitleTimer = subtitleDuration;
    }
}