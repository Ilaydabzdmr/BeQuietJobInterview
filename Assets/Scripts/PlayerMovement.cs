using Unity.Netcode;              // NetworkBehaviour, IsOwner, OwnerClientId buradan
using UnityEngine;
using UnityEngine.InputSystem;    // Yeni Input System (Keyboard.current vb.)

// NetworkBehaviour: MonoBehaviour'un network'lü versiyonu.
// Bize IsOwner, IsServer, OwnerClientId gibi "bu obje kimin?" bilgilerini verir.
// Çalışması için aynı objede bir NetworkObject component'i olmalı.
public class PlayerMovement : NetworkBehaviour
{
    // [SerializeField]: private olsa bile Inspector'da görünür ve oradan ayarlanabilir.
    // Neden public değil? Başka script'ler kurcalayamasın, ama sen editörde hızı deneyebilesin.
    [SerializeField] private float moveSpeed = 4f;

    // CharacterController: Fizik motoruna (Rigidbody) girmeden, duvarlara çarpan basit hareket sağlar.
    // Karakter kontrolü için Rigidbody'den daha öngörülebilir, o yüzden tercih ettik.
    private CharacterController cc;

    // OnNetworkSpawn: Obje network'te "doğduğunda" çağrılır.
    // Neden Start() değil? Start'ta IsOwner gibi network bilgileri henüz hazır olmayabilir.
    // Network'le ilgili ilk kurulum her zaman burada yapılır.
    public override void OnNetworkSpawn()
    {
        cc = GetComponent<CharacterController>();   // Aynı objedeki CharacterController'ı bul

        // IsOwner: "Bu karakter benim mi?" Her bilgisayarda 4 karakter de var,
        // ama her biri sadece birinin. Benim olmayanın controller'ını kapatıyorum.
        // Neden? Onların pozisyonu network'ten gelecek; kendi collider'ım araya girip
        // pozisyonu itip kakmasın.
        if (!IsOwner)
        {
            cc.enabled = false;
            return;   // Başkasının karakteriyse kurulum burada biter
        }

        // Kendi karakterimi başlangıç noktasına ışınlıyorum.
        // Kapat-taşı-aç hilesi: CharacterController açıkken transform.position'ı
        // değiştirirsen, kendi iç pozisyonuyla ezip geri alabilir. Klasik Unity tuzağı.
        cc.enabled = false;
        // OwnerClientId (0,1,2,3) ile x'i kaydırıyoruz ki oyuncular üst üste doğmasın.
        transform.position = new Vector3(1f + OwnerClientId * 1.2f, 1f, -2f);
        cc.enabled = true;
    }

    // Update: Her karede çalışır. Input okumanın doğru yeri.
    void Update()
    {
        // En kritik satır: Başkasının karakterini ben hareket ettirmiyorum.
        // Bu olmasa W'ya bastığında 4 karakter birden yürürdü.
        if (!IsOwner) return;

        var kb = Keyboard.current;   // Bağlı klavye
        if (kb == null) return;       // Klavye yoksa (ör. sadece gamepad) çökmesin

        // WASD'yi yön vektörüne çeviriyoruz. z = ileri/geri, x = sağ/sol.
        Vector3 input = Vector3.zero;
        if (kb.wKey.isPressed) input.z += 1;
        if (kb.sKey.isPressed) input.z -= 1;
        if (kb.aKey.isPressed) input.x -= 1;
        if (kb.dKey.isPressed) input.x += 1;

        // normalized: Çaprazda (W+D) vektör uzunluğu ~1.41 olur, yani daha hızlı gidersin.
        //   Normalize ederek her yönde aynı hızı garantiliyoruz.
        // Time.deltaTime: Kareler arası geçen süre. Bununla çarpınca hız FPS'ten bağımsız olur
        //   (60 FPS'teki oyuncu 30 FPS'tekinden hızlı koşmaz).
        cc.Move(input.normalized * moveSpeed * Time.deltaTime);
    }
}