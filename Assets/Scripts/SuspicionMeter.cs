using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Oyunun "can" sistemi. Değer SUNUCUDA tutulur, herkes okur.
public class SuspicionMeter : NetworkBehaviour
{
    // Singleton: Her yerden SuspicionMeter.Instance ile ulaşılır.
    // "private set": dışarıdan okunabilir ama sadece bu sınıf atayabilir.
    public static SuspicionMeter Instance { get; private set; }

    public const float Max = 100f;

    // Şüphe değeri (0-100). Sunucu yazar, herkes okur.
    public NetworkVariable<float> Suspicion = new NetworkVariable<float>(
        0f,
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

        // Clamp: 0'ın altına ya da 100'ün üstüne çıkmasın.
        Suspicion.Value = Mathf.Clamp(Suspicion.Value + amount, 0f, Max);
    }

    // DEBUG: Test tuşları. Görev 3c'de gerçek girdiler gelince silinecek.
    void Update()
    {
        if (!IsServer) return;   // Sadece host test edebilir

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.kKey.wasPressedThisFrame) AddSuspicion(10f);    // K: +10
        if (kb.lKey.wasPressedThisFrame) AddSuspicion(-10f);   // L: -10
    }
}