using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Kompozisyon ibresi. Mülakatçının KENDİ bilgisayarında simüle edilir.
public class NeedleMinigame : MonoBehaviour
{
    // YENİ: Kaoslar ibreyi itebilsin diye
    public static NeedleMinigame Instance { get; private set; }

    [Header("Arayüz")]
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform track;
    [SerializeField] private RectTransform needle;
    [SerializeField] private Image needleImage;

    [Header("Fizik")]
    [SerializeField] private float driftStrength = 1.2f;
    [SerializeField] private float driftSpeed = 0.5f;
    [SerializeField] private float playerForce = 3f;
    [SerializeField] private float damping = 2f;

    [Header("Kaos etkisi")]   // YENİ
    [SerializeField] private float pressureDriftFactor = 0.5f;   // Aktif kaos baskısı kaymayı ne kadar güçlendirir

    [Header("Kenar")]
    [SerializeField] private float bounceBackTo = 0.6f;
    [SerializeField] private float flashDuration = 0.3f;

    private float position;
    private float velocity;
    private float noiseSeed;
    private float flashTimer;
    private LaptopSeat laptop;   // YENİ: Sesin hangi taraftan geldiğini hesaplamak için

    void Awake()
    {
        Instance = this;   // YENİ
        noiseSeed = Random.Range(0f, 100f);
    }

    void Update()
    {
        bool active = IsLocalInterviewerSeated();
        root.SetActive(active);

        if (!active)
        {
            position = 0f;
            velocity = 0f;
            return;
        }

        float dt = Time.deltaTime;

        // YENİ: Aktif kaoslar kaymayı güçlendirir. TotalPressure her bilgisayarda hesaplanabilir
        // (IsActive ve Severity zaten senkronize). Hiç kaos yoksa çarpan 1.
        float chaosBoost = 1f + ChaosEvent.TotalPressure() * pressureDriftFactor;
        float drift = (Mathf.PerlinNoise(noiseSeed, Time.time * driftSpeed) * 2f - 1f)
                      * driftStrength * chaosBoost;

        float input = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input += 1f;
        }

        velocity += (drift + input * playerForce) * dt;
        velocity -= velocity * damping * dt;
        position += velocity * dt;

        if (Mathf.Abs(position) >= 1f)
        {
            SuspicionMeter.Instance.ReportNeedleHitRpc();
            position = Mathf.Sign(position) * bounceBackTo;
            velocity = 0f;
            flashTimer = flashDuration;
        }

        float halfWidth = track.rect.width / 2f;
        needle.anchoredPosition = new Vector2(position * halfWidth, needle.anchoredPosition.y);

        if (flashTimer > 0f) flashTimer -= dt;
        needleImage.color = flashTimer > 0f ? Color.red : Color.white;
    }

    // YENİ: Bir kaos "anı" ibreyi iter. strength = itme gücü, sourceWorldPos = sesin kaynağı.
    // Bu metot her bilgisayarda çağrılır ama sadece oturan mülakatçıda bir etkisi olur.
    public void Push(float strength, Vector3 sourceWorldPos)
    {
        if (!root.activeSelf) return;   // İbre çalışmıyorsa (mülakatçı değil ya da oturmuyor) yok say

        if (laptop == null) laptop = FindFirstObjectByType<LaptopSeat>();

        // InverseTransformPoint: Dünyadaki bir noktayı laptopun KENDİ bakış açısına çevirir.
        // Sonuç x > 0 ise ses mülakatçının sağında, x < 0 ise solunda.
        float side = 1f;
        if (laptop != null)
            side = Mathf.Sign(laptop.transform.InverseTransformPoint(sourceWorldPos).x);

        velocity += side * strength;   // Ani darbe: hız bir anda değişir
    }

    private bool IsLocalInterviewerSeated()
    {
        var nm = NetworkManager.Singleton;
        var meter = SuspicionMeter.Instance;
        if (nm == null || meter == null || !meter.IsSpawned) return false;
        if (meter.Stage.Value == SuspicionStage.Bitti) return false;

        var player = nm.SpawnManager.GetLocalPlayerObject();
        if (player == null) return false;
        if (player.GetComponent<PlayerRole>().CurrentRole.Value != Role.Mulakatci) return false;

        return player.GetComponent<PlayerInteraction>().IsSeated;
    }
}