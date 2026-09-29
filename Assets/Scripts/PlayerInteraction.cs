using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Oyuncunun E tuşuyla etkileşimi. Player prefab'ına takılır.
public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float interactDistance = 1.5f;

    // "Laptop ekranına bakıyor muyum?" Bunu sadece bu oyuncunun kendi
    // bilgisayarı bilir (kamera yerel). O yüzden OWNER yazar, herkes okur.
    public NetworkVariable<bool> LookingAtScreen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);



    private LaptopSeat laptop;
    private PlayerMovement movement;
    private CharacterController cc;
    private CameraController cam;   // Yerel kamera

    // "Koltukta oturan benim mi?" Gerçeğin tek kaynağı sunucudaki değer.
    public bool IsSeated => laptop != null && laptop.SeatedClientId.Value == OwnerClientId;

    public override void OnNetworkSpawn()
    {
        // Sadece KENDİ oyuncum için çalışır.
        if (!IsOwner) return;

        laptop = FindFirstObjectByType<LaptopSeat>();
        movement = GetComponent<PlayerMovement>();
        cc = GetComponent<CharacterController>();
        cam = FindFirstObjectByType<CameraController>();   // Bu bilgisayardaki kamera

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

        // Her karede "ekrana bakıyor muyum?" bilgisini güncelle.
        // Sadece değer DEĞİŞTİĞİNDE yazıyoruz. Aynı değeri tekrar yazmak zararsız
        // olsa da gereksiz; bu kontrol niyetimizi netleştiriyor.
        bool looking = IsSeated && cam.IsLookingAtScreen;
        if (LookingAtScreen.Value != looking)
            LookingAtScreen.Value = looking;

        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        if (IsSeated)
        {
            // Sunucu cevabı gelene kadar IsSeated hâlâ true görünür.
            // "Zaten kalktıysam ikinci kez kalkma" kontrolü.
            if (!movement.enabled)
            {
                StandUpLocally();          // İyimser: önce kendi ekranımda hemen kalk
                laptop.RequestStandRpc();  // Sonra sunucuya haber ver
            }
        }
        else if (Vector3.Distance(transform.position, laptop.SeatPoint.position) <= interactDistance)
        {
            // Oturmak iyimser DEĞİL: koltuk dolu olabilir, sunucunun onayını bekleriz.
            laptop.RequestSitRpc();
        }
    }

    // Sunucu koltuk değerini değiştirdiğinde çalışır.
    private void OnSeatChanged(ulong oldId, ulong newId)
    {
        if (newId == OwnerClientId)
        {
            // Koltuk artık benim: dur, ışınlan, oturma bakışına geç.
            movement.enabled = false;
            cc.enabled = false;   // CC açıkken ışınlama geri sıçrar
            transform.position = laptop.SeatPoint.position;
            cam.SetMode(CameraMode.Seated);   // DEĞİŞTİ: SetLaptopFocus(true) yerine
        }
        else if (oldId == OwnerClientId && !movement.enabled)
        {
            // İyimser kalkma olmadan koltuktan düştüysem (ileride: sunucu zorla kaldırırsa).
            StandUpLocally();
        }
    }

    private void StandUpLocally()
    {
        // Masadan oturma noktasına doğru olan yön = "geri" yönü.
        Vector3 away = laptop.SeatPoint.position - laptop.transform.position;
        away.y = 0;

        transform.position = laptop.SeatPoint.position + away.normalized * 0.5f;
        cc.enabled = true;
        movement.enabled = true;
        cam.SetMode(CameraMode.Overview);   // DEĞİŞTİ: SetLaptopFocus(false) yerine
    }
}