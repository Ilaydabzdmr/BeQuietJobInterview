using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Oyunun aşamaları. Sırası önemli değil ama sayıları sabit kalsın.
public enum SessionPhase { Hazirlik = 0, Gorusmede = 1, Basarili = 2, Basarisiz = 3 }

// Görüşmenin yöneticisi. Ne zaman başlar, ne kadar sürer, nasıl biter? SUNUCUDA karar verilir.
public class InterviewSession : NetworkBehaviour
{
    public static InterviewSession Instance { get; private set; }

    [SerializeField] private float callDuration = 180f;   // Görüşme süresi (saniye). 3 dakika.

    public NetworkVariable<SessionPhase> Phase = new NetworkVariable<SessionPhase>(
        SessionPhase.Hazirlik,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Herkesin soracağı soru: "Şu an görüşmede miyiz?"
    public bool IsInCall => IsSpawned && Phase.Value == SessionPhase.Gorusmede;

    private LaptopSeat laptop;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        laptop = FindFirstObjectByType<LaptopSeat>();
    }

    // Mülakatçı Meet ikonuna tıklayınca çağrılır.
    [Rpc(SendTo.Server)]
    public void StartCallRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        // Sunucunun üç sorusu:
        if (Phase.Value != SessionPhase.Hazirlik) return;                        // 1) Hazırlıkta mıyız?
        if (laptop == null || laptop.SeatedClientId.Value != sender) return;     // 2) Laptopta oturan o mu?
        if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client) ||
            client.PlayerObject == null) return;
        if (client.PlayerObject.GetComponent<PlayerRole>().CurrentRole.Value != Role.Mulakatci)
            return;                                                              // 3) Mülakatçı mı?

        SuspicionMeter.Instance.ResetMeter();   // Her görüşme temiz bir sayfayla başlar
        TimeRemaining.Value = callDuration;
        Phase.Value = SessionPhase.Gorusmede;
        Debug.Log("[Oturum] Gorusme basladi");
    }

    void Update()
    {
        if (!IsServer) return;

        // DEBUG: R = her şeyi sıfırla, Hazırlık'a dön. (Görev 8'de gerçek tur akışı gelecek.)
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame) ResetSession();

        if (Phase.Value != SessionPhase.Gorusmede) return;

        // Kaybetme: şüphe 100'e ulaştı
        if (SuspicionMeter.Instance.Stage.Value == SuspicionStage.Bitti)
        {
            EndCall(false);
            return;
        }

        // Kazanma: süre doldu ve hâlâ ayaktayız
        TimeRemaining.Value = Mathf.Max(0f, TimeRemaining.Value - Time.deltaTime);
        if (TimeRemaining.Value <= 0f)
            EndCall(true);
    }

    private void EndCall(bool success)
    {
        ResolveAllChaos();   // Görüşme bitince ev de sakinleşir
        Phase.Value = success ? SessionPhase.Basarili : SessionPhase.Basarisiz;
        Debug.Log(success ? "[Oturum] Gorusme BASARILI" : "[Oturum] Gorusme BASARISIZ");
    }

    private void ResetSession()
    {
        ResolveAllChaos();
        SuspicionMeter.Instance.ResetMeter();
        TimeRemaining.Value = 0f;
        Phase.Value = SessionPhase.Hazirlik;
    }

    private void ResolveAllChaos()
    {
        foreach (var c in ChaosEvent.All)
            c.Resolve();   // Zaten pasifse Resolve hiçbir şey yapmaz
    }
}
