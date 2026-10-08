using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Laptop masaüstündeki Meet ikonu. Sadece Hazırlık'ta ve sadece mülakatçıya görünür.
public class MeetAppUI : MonoBehaviour
{
    [SerializeField] private Button meetButton;   // MeetIkon

    void Awake()
    {
        // AddListener: "Bu düğmeye tıklanınca şu metodu çağır." (WinForms'taki button.Click += ...)
        meetButton.onClick.AddListener(OnMeetClicked);
    }

    void Update()
    {
        var session = InterviewSession.Instance;
        bool show = session != null && session.IsSpawned &&
                    session.Phase.Value == SessionPhase.Hazirlik &&
                    IsLocalInterviewer();
        meetButton.gameObject.SetActive(show);
    }

    private void OnMeetClicked()
    {
        // İstek gönder, karar sunucuda.
        if (InterviewSession.Instance != null)
            InterviewSession.Instance.StartCallRpc();
    }

    private bool IsLocalInterviewer()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return false;
        var player = nm.SpawnManager.GetLocalPlayerObject();
        return player != null &&
               player.GetComponent<PlayerRole>().CurrentRole.Value == Role.Mulakatci;
    }
}