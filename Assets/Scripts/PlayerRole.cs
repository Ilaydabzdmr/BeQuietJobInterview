using Unity.Netcode;
using UnityEngine;

// enum: Sabit seçenekler listesi. String ("mulakatci") yerine enum kullanıyoruz çünkü
// yazım hatası imkânsız, network'te de tek bir sayı olarak gider (0, 1). Hem hızlı hem güvenli.
public enum Role { EvArkadasi, Mulakatci }

public class PlayerRole : NetworkBehaviour
{
    // NetworkVariable: Değeri otomatik olarak tüm oyunculara senkronize edilen değişken.
    // Server değiştirir → Netcode herkese yollar → herkesin kopyası güncellenir.
    // Sonradan katılan oyuncu da güncel değeri otomatik alır, ekstra kod gerekmez.
    public NetworkVariable<Role> CurrentRole = new NetworkVariable<Role>(
        Role.EvArkadasi,                          // Başlangıç değeri
        NetworkVariableReadPermission.Everyone,   // Herkes okuyabilir (herkes rolleri görmeli)
        NetworkVariableWritePermission.Server);   // Sadece server yazabilir.
                                                  // Neden? Rol oyunun kuralıdır. Client "ben mülakatçıyım" diye kendini atayamasın.
                                                  // Kural koyan değer = server'da. Bu prensibi şüphe barında da aynen uygulayacağız.

    // Inspector'dan sürükleyip bırakacağın iki renk (gri kutu aşamasında rol = renk).
    [SerializeField] private Material mulakatciMaterial;
    [SerializeField] private Material evArkadasiMaterial;
    private MeshRenderer meshRenderer;   // Objenin görünümünü çizen component

    public override void OnNetworkSpawn()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        // OnValueChanged: Değer değiştiğinde (server'da da, client'ta da) çağrılacak event.
        // ÖNCE abone oluyoruz, SONRA değeri set ediyoruz. Ters sırada ilk değişikliği kaçırabilirdik.
        CurrentRole.OnValueChanged += OnRoleChanged;

        // Rol atamayı sadece server yapar (yazma izni zaten sadece onda).
        if (IsServer)
        {
            // ServerClientId = host'un ID'si (0). Bu karakterin sahibi host'sa mülakatçı, değilse ev arkadaşı.
            CurrentRole.Value = OwnerClientId == NetworkManager.ServerClientId
                ? Role.Mulakatci
                : Role.EvArkadasi;
        }

        // Event sadece DEĞİŞİMDE tetiklenir. Sonradan katılan client değeri hazır alır,
        // "değişim" görmez, event de çalışmaz. O yüzden mevcut değeri bir kez elle uyguluyoruz.
        // Bu satır olmasa geç katılanlar herkesi yanlış renkte görürdü.
        ApplyVisual(CurrentRole.Value);
    }

    // Obje network'ten kalkarken (oyuncu çıktı, sahne değişti) aboneliği bırak.
    // Unutulursa silinmiş objeye event gitmeye çalışır → hata ya da bellek sızıntısı.
    public override void OnNetworkDespawn()
    {
        CurrentRole.OnValueChanged -= OnRoleChanged;
    }

    // Event imzası sabittir: (eski değer, yeni değer). Eskiyi şimdilik kullanmıyoruz,
    // ama ileride "ev arkadaşından mülakatçıya geçti" gibi geçiş efektleri için işe yarar.
    private void OnRoleChanged(Role oldRole, Role newRole)
    {
        ApplyVisual(newRole);
    }

    // Görseli tek bir yerde topladık. Hem ilk açılışta hem değişimde aynı fonksiyon çalışıyor.
    private void ApplyVisual(Role role)
    {
        // sharedMaterial: Mevcut materyali doğrudan değiştirir, kopya oluşturmaz.
        // .material kullansaydık Unity her seferinde gizlice yeni bir kopya yaratırdı (bellek israfı).
        // Burada materyali düzenlemiyor, sadece hazır birini takıyoruz. Bu yüzden sharedMaterial doğru seçim.
        meshRenderer.sharedMaterial = role == Role.Mulakatci ? mulakatciMaterial : evArkadasiMaterial;
    }
}