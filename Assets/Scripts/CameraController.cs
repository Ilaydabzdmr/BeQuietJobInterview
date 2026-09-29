using UnityEngine;
using UnityEngine.InputSystem;

// Kameranın üç modu. Tamamen yerel, ağa dokunmaz.
public enum CameraMode { Overview, Seated, Screen }

public class CameraController : MonoBehaviour
{
    [Header("Bakış noktaları")]
    [SerializeField] private Transform seatedView;   // OturmaKamerasi: oturan birinin gözü
    [SerializeField] private GameObject screenUI;    // DEĞİŞTİ: LaptopEkranUI (tam ekran arayüzü)

    [Header("Ayarlar")]
    [SerializeField] private float transitionSpeed = 10f;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float maxYaw = 100f;
    [SerializeField] private float maxPitchUp = 30f;
    [SerializeField] private float maxPitchDown = 40f;
    [SerializeField] private float screenLookAngle = 20f;

    private Vector3 defaultPos;
    private Quaternion defaultRot;
    private CameraMode mode = CameraMode.Overview;
    private float yaw;
    private float pitch;

    public CameraMode Mode => mode;

    // Görev 3 için hazırlık: Mülakatçı şu an ekrana bakıyor mu?
    public bool IsLookingAtScreen =>
        mode == CameraMode.Screen ||
        (mode == CameraMode.Seated && Mathf.Abs(yaw) < screenLookAngle && Mathf.Abs(pitch) < screenLookAngle);

    void Start()
    {
        defaultPos = transform.position;
        defaultRot = transform.rotation;
        screenUI.SetActive(false);   // YENİ: oyun her zaman ekran kapalı başlar
    }

    public void SetMode(CameraMode newMode)
    {
        if (newMode == CameraMode.Seated && mode == CameraMode.Overview)
        {
            yaw = 0f;
            pitch = 0f;
        }

        mode = newMode;

        // YENİ: Tam ekran arayüzünü sadece Screen modunda göster.
        // SetActive: bir GameObject'i (ve tüm alt nesnelerini) açar/kapatır.
        screenUI.SetActive(mode == CameraMode.Screen);

        UpdateCursor();
    }

    private void UpdateCursor()
    {
        bool lockCursor = mode == CameraMode.Seated;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    void Update()
    {
        if (mode == CameraMode.Overview) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (mode == CameraMode.Screen)
            {
                // Tam ekrandan çıkmak her zaman serbest.
                SetMode(CameraMode.Seated);
            }
            else if (IsLookingAtScreen)
            {
                // Tam ekrana SADECE laptopa bakarken girilebilir.
                SetMode(CameraMode.Screen);
            }
            // Başka yere bakarken sağ tık hiçbir şey yapmaz.
        }

        if (mode == CameraMode.Seated)
        {
            Vector2 d = mouse.delta.ReadValue() * mouseSensitivity;
            yaw = Mathf.Clamp(yaw + d.x, -maxYaw, maxYaw);
            pitch = Mathf.Clamp(pitch - d.y, -maxPitchUp, maxPitchDown);
        }
    }

    void LateUpdate()
    {
        Vector3 targetPos;
        Quaternion targetRot;

        switch (mode)
        {
            // DEĞİŞTİ: Tam ekranda kamera oturma bakışında kalır. Arayüz zaten her şeyi örtüyor.
            case CameraMode.Seated:
            case CameraMode.Screen:
                targetPos = seatedView.position;
                Vector3 e = seatedView.eulerAngles;
                targetRot = Quaternion.Euler(e.x + pitch, e.y + yaw, 0f);
                break;

            default: // Overview
                targetPos = defaultPos;
                targetRot = defaultRot;
                break;
        }

        float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }
}