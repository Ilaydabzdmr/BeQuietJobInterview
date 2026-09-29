using Unity.Netcode;
using UnityEngine;

// Şüphe barının GERÇEK girdileri. Tamamen SUNUCUDA çalışır.
// Her karede "şu an durum ne?" diye bakar ve bara ekler.
public class SuspicionInputs : NetworkBehaviour
{
    [Header("Saniye başına şüphe artışı (playtest ile ayarlanacak)")]
    [SerializeField] private float emptySeatPerSecond = 3f;        // Koltuk boş
    [SerializeField] private float wrongPersonPerSecond = 1.5f;    // Koltukta ev arkadaşı
    [SerializeField] private float noEyeContactPerSecond = 0.12f;  // Ekrana bakmıyor (~7/dk)

    // Her aktif kaos için temel artış. Plan: "saniyede +0.5, kaos sayısıyla çarpanlı"
    [SerializeField] private float chaosPerSecond = 0.5f;

    private LaptopSeat laptop;

    public override void OnNetworkSpawn()
    {
        laptop = FindFirstObjectByType<LaptopSeat>();
    }

    void Update()
    {
        // Sadece sunucu hesaplar. Client'lar sadece sonucu (barı) görür.
        if (!IsServer || laptop == null) return;

        var meter = SuspicionMeter.Instance;
        if (meter == null) return;

        // Koltuk ve göz kuralları: en kötüsü geçerli.
        float rate = GetSeatRate();

        // Kaos kuralı ÜSTÜNE eklenir (farklı kategori).
        // Her aktif kaos 0.5 × aktif kaos sayısı → 1:0.5, 2:2.0, 3:4.5
        int active = ChaosEvent.ActiveCount();
        rate += chaosPerSecond * active * active;

        // rate saniye başına. deltaTime ile çarparak bu karenin payını buluruz.
        // (1b'deki hareket hızıyla aynı mantık: FPS'ten bağımsız.)
        if (rate > 0f)
            meter.AddSuspicion(rate * Time.deltaTime);
    }

    // Şu anki duruma göre saniye başına artış. Kurallar toplanmaz,
    // yukarıdan aşağıya ilk uyan (en kötüsü) geçerli olur.
    private float GetSeatRate()
    {
        ulong seatedId = laptop.SeatedClientId.Value;

        // Kural 1: Koltuk boş
        if (seatedId == LaptopSeat.Bos)
            return emptySeatPerSecond;

        // Oturan oyuncuyu bul. TryGetValue: sözlükte varsa verir, yoksa false döner.
        // (O an bağlantısı kopmuş olabilir; bu durumu da "boş" sayıyoruz.)
        if (!NetworkManager.ConnectedClients.TryGetValue(seatedId, out var client) ||
            client.PlayerObject == null)
            return emptySeatPerSecond;

        var seatedPlayer = client.PlayerObject;

        // Kural 2: Koltukta mülakatçı değil, ev arkadaşı var
        if (seatedPlayer.GetComponent<PlayerRole>().CurrentRole.Value != Role.Mulakatci)
            return wrongPersonPerSecond;

        // Kural 3: Mülakatçı oturuyor ama ekrana bakmıyor.
        // Bu bilgiyi mülakatçının kendi bilgisayarı yazıyor (Owner NetworkVariable).
        if (!seatedPlayer.GetComponent<PlayerInteraction>().LookingAtScreen.Value)
            return noEyeContactPerSecond;

        // Her şey yolunda
        return 0f;
    }
}
