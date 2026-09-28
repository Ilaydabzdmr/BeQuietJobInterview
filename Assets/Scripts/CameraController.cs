using UnityEngine;

// Kameranın iki durumu var: genel bakış ve laptop odağı.
// Tamamen yerel, ağa hiç dokunmaz: herkesin kendi kamerası.
public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform laptopView;        // Odaklanınca gideceği nokta (LaptopKamerasi)
    [SerializeField] private float transitionSpeed = 6f;  // Geçiş hızı: büyüdükçe daha çabuk

    private Vector3 defaultPos;       // Genel bakış konumu (oyun başındaki konum)
    private Quaternion defaultRot;    // Genel bakış açısı
    private bool focusLaptop = false; // Şu an laptopa mı odaklanmalı?

    void Start()
    {
        // Sahnede ayarladığın konumu "ev" olarak hatırla.
        defaultPos = transform.position;
        defaultRot = transform.rotation;
    }

    // PlayerInteraction bunu çağırır: true = ekrana gömül, false = geri çekil.
    public void SetLaptopFocus(bool focus)
    {
        focusLaptop = focus;
    }

    // LateUpdate: Tüm Update'lerden SONRA çalışır, kamera titremez.
    void LateUpdate()
    {
        // Hedefi seç: odak varsa laptop, yoksa genel bakış.
        Vector3 targetPos = focusLaptop ? laptopView.position : defaultPos;
        Quaternion targetRot = focusLaptop ? laptopView.rotation : defaultRot;

        // Her karede hedefe doğru mesafenin bir kısmını kapat → yumuşak yavaşlayan geçiş.
        // Mathf.Exp'li formül: 30 FPS'te de 144 FPS'te de geçiş aynı hızda olsun diye.
        float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);

        // Lerp: iki konum arasında t oranında bir nokta. Slerp: aynısı, dönüşler için.
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }
}
