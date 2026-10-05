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

    //Tamir ilerlemesi (0 = başlanmadı, 1 = bitti). Sunucu yazar, herkes görür.
    public NetworkVariable<float> RepairProgress = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    //(PipeBurst'ten taşındı): Ne kadar kötüleşti? 0 = yeni başladı, 1 = en kötü.
    public NetworkVariable<float> Severity = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Tamir")]
    [SerializeField] private float repairDuration = 3f;      // YENİ: Tek kişiyle kaç saniye
    [SerializeField] private float interactDistance = 1.5f;  // YENİ: Ne kadar yakından

    [Header("Kötüleşme")]   //(PipeBurst'ten taşındı)
    [SerializeField] private float timeToMaxSeverity = 40f;   // 0 = hiç kötüleşmez
    [SerializeField] private float maxMultiplier = 2f;        // En kötü halde şüphe çarpanı

    public float InteractDistance => interactDistance;

    //Alanları doğrudan değil, bu özellikler üzerinden kullanıyoruz.
    // Varsayılan olarak Inspector'daki değeri döner, ama alt sınıf override edip
    // başka bir değer verebilir (kapı: o anki ziyaretçinin değeri).
    protected virtual float RepairDuration => repairDuration;
    protected virtual float TimeToMaxSeverity => timeToMaxSeverity;
    protected virtual float MaxMultiplier => maxMultiplier;

    //Tamir ipucunda yazacak eylem. Varsayılan: kaosun adı.
    public virtual string ActionPrompt => DisplayName;

    // Şu an tamir eden oyuncuların ID'leri. SADECE SUNUCUDA dolu.
    // HashSet: aynı ID iki kez eklenemez (bir oyuncu iki kez sayılmasın).
    private readonly HashSet<ulong> repairers = new HashSet<ulong>();

    // Alt sınıflar "şu an kaç kişi tamir ediyor?" bilsin. (Sadece sunucuda dolu.)
    protected int RepairerCount => repairers.Count;

    // Her alt sınıf kendi adını söylemek ZORUNDA (abstract özellik).
    public abstract string DisplayName { get; }

    //Artık varsayılan olarak Severity'ye göre hesaplanıyor (her kaos için).
    public virtual float PressureMultiplier => Mathf.Lerp(1f, maxMultiplier, Severity.Value);

    // Alt sınıfların "başladım / çözüldüm" anlarına özel iş eklemesi için kancalar.
    // Boş gövdeli virtual metot: alt sınıf isterse override eder, istemezse hiçbir şey olmaz.
    protected virtual void OnTriggered() { }
    protected virtual void OnResolved() { }

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
        RepairProgress.Value = 0f;   // Her patlama sıfırdan başlar
        Severity.Value = 0f;   
        IsActive.Value = true;
        OnTriggered();   // Alt sınıfa haber ver
        Debug.Log($"[Kaos] {DisplayName} basladi");
    }

    // Kaosu çöz. Sadece sunucu, zaten pasifse bir şey yapma.
    public void Resolve()
    {
        if (!IsServer || !IsActive.Value) return;
        IsActive.Value = false;
        RepairProgress.Value = 0f;
        Severity.Value = 0f;   
        repairers.Clear();//Tamirciler dağılsın
        OnResolved();    // Alt sınıfa haber ver
        Debug.Log($"[Kaos] {DisplayName} cozuldu");
    }

    // Client "tamire başladım" der.
    [Rpc(SendTo.Server)]
    public void StartRepairRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (!IsActive.Value) return;          // Çözülmüş kaos tamir edilmez
        if (!IsInRange(sender)) return;       // Uzaktan tamir yok
        repairers.Add(sender);
    }

    // Client "bıraktım" der.
    [Rpc(SendTo.Server)]
    public void StopRepairRpc(RpcParams rpcParams = default)
    {
        repairers.Remove(rpcParams.Receive.SenderClientId);
        if (repairers.Count == 0) RepairProgress.Value = 0f;   // Kimse tutmuyorsa sıfırla
    }

    // DEĞİŞTİ: Tamir mantığı kendi metoduna alındı (Update okunaklı kalsın diye).
    protected virtual void Update()
    {
        if (!IsServer || !IsActive.Value) return;

        // Açık kaldıkça kötüleş.
        if (timeToMaxSeverity > 0f)
            Severity.Value = Mathf.Min(1f, Severity.Value + Time.deltaTime / timeToMaxSeverity);

        UpdateRepair();
    }
    // Tamir mantığı kendi metodunda (Update okunaklı kalsın diye).
    private void UpdateRepair()
    {
        if (repairers.Count == 0) return;

        repairers.RemoveWhere(id => !IsInRange(id));
        if (repairers.Count == 0)
        {
            RepairProgress.Value = 0f;
            return;
        }

        RepairProgress.Value += Time.deltaTime / repairDuration * repairers.Count;

        if (RepairProgress.Value >= 1f)
            Resolve();
    }

    // Bu oyuncu kaosun yeterince yakınında mı? (Sadece sunucuda anlamlı)
    private bool IsInRange(ulong clientId)
    {
        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) ||
            client.PlayerObject == null)
            return false;

        float dist = Vector3.Distance(client.PlayerObject.transform.position, transform.position);
        return dist <= interactDistance + 0.5f;   // Gecikme toleransı (koltuktaki gibi)
    }

    // Şu an kaç kaos aktif? Her bilgisayarda çalışır (liste ve IsActive her yerde var).
    public static int ActiveCount()
    {
        int count = 0;
        foreach (var e in All)
            if (e.IsActive.Value) count++;
        return count;
    }

    // Aktif kaosların çarpanlarının toplamı.
    // Hepsi 1 ise ActiveCount() ile aynı sonucu verir (eski formül bozulmaz).
    public static float TotalPressure()
    {
        float total = 0f;
        foreach (var e in All)
            if (e.IsActive.Value) total += e.PressureMultiplier;
        return total;
    }
}
