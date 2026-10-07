using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

//Görüşmenin aşamaları. Sayısal sırası önemli (büyük = daha kötü).
public enum SuspicionStage { Sakin = 0, Suphelendi = 1, Uyardi = 2, Bitti = 3 }

// Oyunun "can" sistemi. Değer SUNUCUDA tutulur, herkes okur.
public class SuspicionMeter : NetworkBehaviour
{
    // Singleton: Her yerden SuspicionMeter.Instance ile ulaşılır.
    // "private set": dışarıdan okunabilir ama sadece bu sınıf atayabilir.
    public static SuspicionMeter Instance { get; private set; }

    public const float Max = 100f;

    [Header("İbre cezası")]  
    [SerializeField] private float needleHitSuspicion = 5f;   // Kenara çarpınca ani şüphe
    [SerializeField] private float needleHitCooldown = 1f;    // En fazla saniyede bir ceza

    private float lastNeedleHitTime = -999f;   // YENİ: Son cezanın zamanı (sadece sunucu)


    // Şüphe değeri (0-100). Sunucu yazar, herkes okur.
    public NetworkVariable<float> Suspicion = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Şu anki aşama. Sunucu hesaplar ve yazar, herkes okur.
    public NetworkVariable<SuspicionStage> Stage = new NetworkVariable<SuspicionStage>(
        SuspicionStage.Sakin,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // 0 ile 1 arası oran. UI doluluğu bununla çalışır.
    public float Normalized => Suspicion.Value / Max;

    void Awake()
    {
        Instance = this;   // Yaşam döngüsünün en başında kendimizi kaydet
    }

    // TEK KAPI: Şüpheyi değiştirmenin tek yolu. Negatif değer azaltır.
    // Kaoslar, koltuk, ibre... hepsi ileride bunu çağıracak.
    public void AddSuspicion(float amount)
    {
        if (!IsServer)
        {
            // Yanlışlıkla client'tan çağrılırsa sessizce bozulmasın, bize söylesin.
            Debug.LogWarning("AddSuspicion sadece sunucuda cagrilabilir!");
            return;
        }

        // Görüşme bittiyse bar kilitli. Artık hiçbir şey değiştiremez.
        if (Stage.Value == SuspicionStage.Bitti) return;

        // Clamp: 0'ın altına ya da 100'ün üstüne çıkmasın.
        Suspicion.Value = Mathf.Clamp(Suspicion.Value + amount, 0f, Max);

        // Değer her değiştiğinde aşamayı yeniden hesapla.
        // Aynı değer tekrar yazılırsa NGO bunu değişiklik saymaz, ağa boşuna gönderilmez.
        Stage.Value = CalculateStage(Suspicion.Value);
    }

    // Değerden aşamaya çeviri. Eşikler tek yerde, ayarlaması kolay.
    private SuspicionStage CalculateStage(float value)
    {
        if (value >= 100f) return SuspicionStage.Bitti;
        if (value >= 75f) return SuspicionStage.Uyardi;
        if (value >= 50f) return SuspicionStage.Suphelendi;
        return SuspicionStage.Sakin;
    }

    // Barı sıfırla. Şimdilik test için, Görev 8'de tur başında çağrılacak.
    public void ResetMeter()
    {
        if (!IsServer) return;
        Suspicion.Value = 0f;
        Stage.Value = SuspicionStage.Sakin;
    }

    // Mülakatçının bilgisayarı "ibre kenara çarptı" der. Sunucu doğrular ve cezalandırır.
    [Rpc(SendTo.Server)]
    public void ReportNeedleHitRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        // Kontrol 1: Gönderen gerçekten var ve gerçekten mülakatçı mı?
        if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client) ||
            client.PlayerObject == null)
            return;
        if (client.PlayerObject.GetComponent<PlayerRole>().CurrentRole.Value != Role.Mulakatci)
            return;

        // Kontrol 2: Çok sık mı? (Hata ya da hile ile spam'e karşı)
        if (Time.time - lastNeedleHitTime < needleHitCooldown) return;
        lastNeedleHitTime = Time.time;

        AddSuspicion(needleHitSuspicion);
        Debug.Log($"[Ibre] Kenara carpti! +{needleHitSuspicion}");
    }

    // DEBUG: Test tuşları. Görev 3c'de gerçek girdiler gelince silinecek.
    void Update()
    {
        if (!IsServer) return;   // Sadece host test edebilir

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.kKey.wasPressedThisFrame) AddSuspicion(10f);    // K: +10
        if (kb.lKey.wasPressedThisFrame) AddSuspicion(-10f);   // L: -10
        if (kb.rKey.wasPressedThisFrame) ResetMeter();         // R: sıfırla
    }
}