using Unity.Netcode;
using UnityEngine;

// Laptop = sandalye. Tek işi: "koltukta kim oturuyor?" bilgisini tutmak.
// Laptop_Masa nesnesine takılır.
public class LaptopSeat : NetworkBehaviour
{
    // "Koltuk boş" anlamına gelen özel değer.
    // Client ID'leri 0'dan başlar, hiçbir oyuncu ulong.MaxValue alamaz.
    public const ulong Bos = ulong.MaxValue;

    // Koltukta oturan oyuncunun ID'si. Sadece sunucu yazar, herkes okur.
    public NetworkVariable<ulong> SeatedClientId = new NetworkVariable<ulong>(
        Bos,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] private Transform seatPoint;          // Oturanın ışınlanacağı nokta
    [SerializeField] private float interactDistance = 1.5f; // Ne kadar yakından oturulabilir

    public Transform SeatPoint => seatPoint;   // Diğer scriptler okuyabilsin diye

    public override void OnNetworkSpawn()
    {
        // Oturan oyuncunun bağlantısı koparsa koltuk sonsuza kadar dolu kalmasın.
        // Sadece sunucu takip eder, çünkü koltuğa sadece sunucu yazabilir.
        if (IsServer)
            NetworkManager.OnClientDisconnectCallback += OnClientLeft;
    }

    public override void OnNetworkDespawn()
    {
        // Aboneliği iptal et (WinForms'taki event -= ile aynı mantık)
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= OnClientLeft;
    }

    // [Rpc(SendTo.Server)]: Bu metodu kim çağırırsa çağırsın, içi SUNUCUDA çalışır.
    // Metodun adı mutlaka "Rpc" ile bitmeli.
    [Rpc(SendTo.Server)]
    public void RequestSitRpc(RpcParams rpcParams = default)
    {
        // Talebi kim gönderdi? Bu bilgiyi NGO ekler, client taklit edemez.
        ulong sender = rpcParams.Receive.SenderClientId;

        // Soru 1: Koltuk boş mu? Değilse sessizce reddet.
        if (SeatedClientId.Value != Bos) return;

        // Soru 2: Gerçekten yanında mı? Sunucu oyuncunun konumunu hafif gecikmeli
        // gördüğü için 0.5 m tolerans var.
        var player = NetworkManager.ConnectedClients[sender].PlayerObject;
        float dist = Vector3.Distance(player.transform.position, seatPoint.position);
        if (dist > interactDistance + 0.5f) return;

        // İki kontrol de geçti: koltuk artık onun. NGO bunu herkese duyurur.
        SeatedClientId.Value = sender;
    }

    [Rpc(SendTo.Server)]
    public void RequestStandRpc(RpcParams rpcParams = default)
    {
        // Sadece şu an oturan kişi kalkabilir. Başkası seni kaldıramaz.
        if (SeatedClientId.Value == rpcParams.Receive.SenderClientId)
            SeatedClientId.Value = Bos;
    }

    private void OnClientLeft(ulong clientId)
    {
        // Çıkan oyuncu oturuyorsa koltuğu boşalt.
        if (SeatedClientId.Value == clientId)
            SeatedClientId.Value = Bos;
    }
}