using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Oyuncunun E tuşuyla etkileşimi. Player prefab'ına takılır.
public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float interactDistance = 1.5f;

    private LaptopSeat laptop;
    private PlayerMovement movement;
    private CharacterController cc;

    // "Koltukta oturan benim mi?" Ayrı bir bool tutmuyoruz, her seferinde
    // sunucunun değerinden hesaplıyoruz. Gerçeğin tek kaynağı sunucu.
    public bool IsSeated => laptop != null && laptop.SeatedClientId.Value == OwnerClientId;

    public override void OnNetworkSpawn()
    {
        // Bu scriptin işi sadece KENDİ oyuncum için. Başkasının kapsülü çıkar.
        if (!IsOwner) return;

        // Sahnedeki laptopu bir kez bul ve sakla (bu arama yavaş, her karede yapılmaz).
        laptop = FindFirstObjectByType<LaptopSeat>();
        movement = GetComponent<PlayerMovement>();
        cc = GetComponent<CharacterController>();

        // "Koltukta oturan değişirse bana haber ver"
        laptop.SeatedClientId.OnValueChanged += OnSeatChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (laptop != null)
            laptop.SeatedClientId.OnValueChanged -= OnSeatChanged;
    }

    void Update()
    {
        if (!IsOwner || laptop == null) return;

        // wasPressedThisFrame: Sadece basıldığı karede true.
        // (isPressed olsaydı basılı tuttukça her kare otur-kalk yapardı.)
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        if (IsSeated)
        {
            // Sunucunun cevabı gelene kadar IsSeated hâlâ true görünür.
            // movement.enabled kontrolü: "Zaten kalktıysam ikinci kez kalkma" demek.
            if (!movement.enabled)
            {
                StandUpLocally();          // Önce kendi ekranımda hemen kalk (iyimser)
                laptop.RequestStandRpc();  // Sonra sunucuya haber ver
            }
        }
        else if (Vector3.Distance(transform.position, laptop.SeatPoint.position) <= interactDistance)
        {
            // Yakınsam: oturmak istiyorum. (Uzaktan boşuna istek atmayalım diye
            // burada da bakıyoruz, ama asıl kararı sunucu verir.)
            laptop.RequestSitRpc();
        }
    }

    // Sunucu koltuk değerini değiştirdiğinde çalışır. oldId = önceki, newId = yeni.
    private void OnSeatChanged(ulong oldId, ulong newId)
    {
        if (newId == OwnerClientId)
        {
            // Koltuk artık BENİM: hareketi durdur ve koltuğa ışınlan.
            movement.enabled = false;   // PlayerMovement'ın Update'i artık çalışmaz
            cc.enabled = false;         // CC açıkken ışınlama geri sıçrar (1b'deki tuzak)
            transform.position = laptop.SeatPoint.position;
        }
        else if (oldId == OwnerClientId && !movement.enabled)
        {
            // Buraya sadece iyimser kalkmadan GEÇMEDEN koltuktan düştüysem gelirim
            // (ileride: sunucu beni zorla kaldırırsa). Zaten kalktıysam bir şey yapma,
            // yoksa yürürken tekrar masanın önüne ışınlanırım.
            StandUpLocally();
        }
    }

    // Yerel olarak oyuncuyu kalkmış hale getirir (sunucudan onay beklenirken UI/kontroller için).
    private void StandUpLocally()
    {
        // Masadan oturma noktasına doğru olan yön = "geri" yönü.
        Vector3 away = laptop.SeatPoint.position - laptop.transform.position;
        away.y = 0;   // Sadece yatay yön, yukarı/aşağı değil

        // cc kapalıyken ışınla (1b'deki tuzak), sonra aç.
        transform.position = laptop.SeatPoint.position + away.normalized * 0.5f;
        cc.enabled = true;
        movement.enabled = true;
    }
}