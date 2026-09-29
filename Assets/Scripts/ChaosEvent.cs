using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Bütün kaosların ortak atası. abstract: tek başına kullanılamaz,
// PipeBurst gibi alt sınıflar bundan türer.
public abstract class ChaosEvent : NetworkBehaviour
{
    // Sahnedeki bütün kaosların listesi. static: tek bir liste, tüm kaoslar paylaşır.
    // Şüphe sistemi "kaç kaos aktif?" sorusunu buradan cevaplar.
    public static readonly List<ChaosEvent> All = new List<ChaosEvent>();

    // Kaos şu an aktif mi? Sunucu yazar, herkes okur.
    public NetworkVariable<bool> IsActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Her alt sınıf kendi adını söylemek ZORUNDA (abstract özellik).
    public abstract string DisplayName { get; }

    public override void OnNetworkSpawn()
    {
        All.Add(this);   // Ağda doğunca listeye kaydol (her bilgisayarda)
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);   // Yok olurken listeden çık
    }

    // Kaosu başlat. Sadece sunucu, zaten aktifse bir şey yapma.
    public void Trigger()
    {
        if (!IsServer || IsActive.Value) return;
        IsActive.Value = true;
        Debug.Log($"[Kaos] {DisplayName} basladi");
    }

    // Kaosu çöz. Sadece sunucu, zaten pasifse bir şey yapma.
    public void Resolve()
    {
        if (!IsServer || !IsActive.Value) return;
        IsActive.Value = false;
        Debug.Log($"[Kaos] {DisplayName} cozuldu");
    }

    // Şu an kaç kaos aktif? Her bilgisayarda çalışır (liste ve IsActive her yerde var).
    public static int ActiveCount()
    {
        int count = 0;
        foreach (var e in All)
            if (e.IsActive.Value) count++;
        return count;
    }
}
