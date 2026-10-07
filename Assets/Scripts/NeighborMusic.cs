using Unity.Netcode;
using UnityEngine;

// Komşu müziği. Susturmak için duvara vurmak gerekir, ama vurmak da gürültü!
public class NeighborMusic : ChaosEvent
{
    public override string DisplayName => "Komsu muzigi";
    public override string ActionPrompt => "duvara vur";   // "E basili tut: duvara vur"

    [Header("Duvara vurma")]
    [SerializeField] private float bangInterval = 0.5f;   // Kaç saniyede bir "GÜM"
    [SerializeField] private float bangSuspicion = 1f;    // Her vuruşta kişi başı ani şüphe
    [SerializeField] private float needlePushPerBang = 0.3f;   // Her vuruşta kişi başı ibre itmesi

    [Header("Görsel")]
    [SerializeField] private float bpm = 120f;            // Müziğin temposu
    [SerializeField] private float beatPop = 1.4f;        // Her vuruşta (müzik) büyüme
    [SerializeField] private float bangPop = 1.8f;        // Duvara vurulunca büyüme

    [Header("Altyazılar")]
    [SerializeField] private string startLine = "Komşudan bas sesi: UMPF UMPF UMPF...";
    [SerializeField] private string endLine = "Komşu: Tamam be, kıstım!";

    private float bangTimer;          // Sunucu: bir sonraki vuruşa kalan süre
    private float bangVisualTimer;    // Her bilgisayar: vuruş görselinin kalan süresi
    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    // Kancalar: Başlayınca ve bitince herkese altyazı (bunlar "an", o yüzden RPC)
    protected override void OnTriggered() { SubtitleRpc(true); }
    protected override void OnResolved() { SubtitleRpc(false); }

    protected override void Update()
    {
        base.Update();   // Kötüleşme ve tamir atada

        // SUNUCU: Biri vuruyorsa her yarım saniyede bir "GÜM" ve gürültü cezası
        if (IsServer && IsActive.Value && RepairerCount > 0)
        {
            bangTimer -= Time.deltaTime;
            if (bangTimer <= 0f)
            {
                // Kaç kişi vuruyorsa o kadar gürültü
                SuspicionMeter.Instance.AddSuspicion(bangSuspicion * RepairerCount);
                BangRpc(needlePushPerBang * RepairerCount);   // İki kişi vurursa iki kat itme
                bangTimer = bangInterval;
            }
        }
        else
        {
            bangTimer = 0f;   // Bir sonraki vurmaya başlayışta ilk "GÜM" hemen olsun
        }

        // GÖRSEL: Müziğin ritmi + duvara vuruş (her bilgisayar kendisi)
        float scale = 1f;
        if (IsActive.Value)
        {
            // Mathf.Repeat: 0'dan 1'e gidip başa dönen sayı. Her müzik vuruşunda sıfırlanır.
            float beat = Mathf.Repeat(Time.time * bpm / 60f, 1f);
            scale = Mathf.Lerp(beatPop, 1f, beat);   // Vuruş anında büyük, sonra küçülür
        }
        if (bangVisualTimer > 0f)
        {
            bangVisualTimer -= Time.deltaTime;
            scale = Mathf.Max(scale, bangPop);       // Duvar vuruşu her zaman en belirgin
        }
        transform.localScale = baseScale * scale;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BangRpc(float needlePush)
    {
        bangVisualTimer = 0.1f;

        if (NeedleMinigame.Instance != null)
            NeedleMinigame.Instance.Push(needlePush, transform.position);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SubtitleRpc(bool started)
    {
        if (InterviewReactionUI.Instance != null)
            InterviewReactionUI.Instance.ShowSubtitle(started ? startLine : endLine);
    }
}
