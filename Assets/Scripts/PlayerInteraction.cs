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

    private ChaosEvent repairTarget;   // Şu an tamir ettiğim kaos (yoksa null)

    // HUD okusun diye. Yakındaki aktif kaos ve tamir ediyor muyum?
    public ChaosEvent NearbyChaos { get; private set; }
    public bool IsRepairing => repairTarget != null;

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

        var kb = Keyboard.current;
        if (kb == null) return;

        // ÖNCELİK 1: Oturuyorsam E sadece "kalk" demek.
        if (IsSeated)
        {
            NearbyChaos = null;
            if (kb.eKey.wasPressedThisFrame && !movement.enabled)
            {
                StandUpLocally();
                laptop.RequestStandRpc();
            }
            return;
        }

        // ÖNCELİK 2: Yakında aktif kaos varsa E = tamir.
        NearbyChaos = FindNearbyActiveChaos();
        UpdateRepair(kb);
        if (IsRepairing) return;   // Tamir ederken başka bir şey yapma

        // ÖNCELİK 3: Laptopa otur.
        if (kb.eKey.wasPressedThisFrame && NearbyChaos == null &&
            Vector3.Distance(transform.position, laptop.SeatPoint.position) <= interactDistance)
        {
            laptop.RequestSitRpc();
        }
    }

    // Basılı tutma mantığı
    private void UpdateRepair(Keyboard kb)
    {
        if (repairTarget != null)
        {
            // E'yi bıraktıysam, uzaklaştıysam ya da kaos çözüldüyse: dur.
            // (Kaos çözülünce NearbyChaos null olur, böylece bu şart da tutar.)
            if (!kb.eKey.isPressed || repairTarget != NearbyChaos)
            {
                repairTarget.StopRepairRpc();
                repairTarget = null;
            }
        }
        else if (NearbyChaos != null && kb.eKey.wasPressedThisFrame)
        {
            repairTarget = NearbyChaos;
            repairTarget.StartRepairRpc();
        }
    }

    // Menzildeki ilk aktif kaosu bul.
    private ChaosEvent FindNearbyActiveChaos()
    {
        foreach (var c in ChaosEvent.All)
        {
            if (c.IsActive.Value &&
                Vector3.Distance(transform.position, c.transform.position) <= c.InteractDistance)
                return c;
        }
        return null;
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