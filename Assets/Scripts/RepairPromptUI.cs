using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Yakında aktif kaos varsa "E basılı tut" ipucu ve ilerleme barı. Tamamen yerel.
public class RepairPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;       // TamirPanel
    [SerializeField] private TMP_Text label;         // TamirYazi
    [SerializeField] private Image progressFill;     // TamirDolgu

    void Update()
    {
        var nm = NetworkManager.Singleton;
        var player = nm != null ? nm.SpawnManager?.GetLocalPlayerObject() : null;
        var interaction = player != null ? player.GetComponent<PlayerInteraction>() : null;
        var chaos = interaction != null ? interaction.NearbyChaos : null;

        panel.SetActive(chaos != null);
        if (chaos == null) return;

        label.text = interaction.IsRepairing
            ? "Onariliyor..."
            : "E basili tut: " + chaos.DisplayName;

        // İlerleme sunucudan gelir: iki kişi tamir ediyorsa ikisi de aynı barı görür.
        progressFill.fillAmount = chaos.RepairProgress.Value;
    }
}
