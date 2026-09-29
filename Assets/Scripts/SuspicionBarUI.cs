using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Şüphe barını ekranda gösterir. Tamamen yerel.
// Değer herkese senkronize olur ama SADECE mülakatçıya gösterilir.
public class SuspicionBarUI : MonoBehaviour
{
    [SerializeField] private GameObject barRoot;   // SupheBar (tamamen açılıp kapanacak kısım)
    [SerializeField] private Image fillImage;      // SupheDolgu (doluluk)

    void Update()
    {
        var meter = SuspicionMeter.Instance;
        // Henüz ağda değilse (bağlanmadan önce) hiçbir şey yapma.
        if (meter == null || !meter.IsSpawned) return;

        // Bu bilgisayardaki oyuncu mülakatçı mı?
        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        bool isInterviewer = player != null &&
            player.GetComponent<PlayerRole>().CurrentRole.Value == Role.Mulakatci;

        // Ev arkadaşı laptopa otursa bile barı göremez.
        barRoot.SetActive(isInterviewer);

        fillImage.fillAmount = meter.Normalized;   // 0-1 arası doluluk
    }
}