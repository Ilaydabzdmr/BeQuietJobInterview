using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Kompozisyon ibresi. Mülakatçının KENDİ bilgisayarında simüle edilir (anlık tepki için).
// Sadece "kenara çarptım" sonucu sunucuya gider.
public class NeedleMinigame : MonoBehaviour
{
    [Header("Arayüz")]
    [SerializeField] private GameObject root;        // IbreAlani (göster/gizle)
    [SerializeField] private RectTransform track;    // IbreAlani (genişliği için)
    [SerializeField] private RectTransform needle;   // Ibre (hareket eden)
    [SerializeField] private Image needleImage;      // Ibre (renk için)

    [Header("Fizik")]
    [SerializeField] private float driftStrength = 1.2f;   // Kendi kendine kayma gücü
    [SerializeField] private float driftSpeed = 0.5f;      // Kaymanın yön değiştirme hızı
    [SerializeField] private float playerForce = 3f;       // A/D'nin itme gücü
    [SerializeField] private float damping = 2f;           // Sürtünme (büyük = daha çabuk durur)

    [Header("Kenar")]
    [SerializeField] private float bounceBackTo = 0.6f;    // Çarpınca nereye geri seksin
    [SerializeField] private float flashDuration = 0.3f;   // Kırmızı yanıp sönme süresi

    private float position;    // -1 = sol uç, 0 = orta, +1 = sağ uç
    private float velocity;    // Saniyede ne kadar hareket ediyor
    private float noiseSeed;   // Her oyunda farklı bir gürültü deseni için
    private float flashTimer;

    void Awake()
    {
        noiseSeed = Random.Range(0f, 100f);
    }

    void Update()
    {
        bool active = IsLocalInterviewerSeated();
        root.SetActive(active);

        if (!active)
        {
            // Oturmuyorsa ibre ortada bekler
            position = 0f;
            velocity = 0f;
            return;
        }

        float dt = Time.deltaTime;

        // 1) KENDİLİĞİNDEN KAYMA: Perlin gürültüsü 0-1 arası yumuşak değer verir.
        //    -1 ile 1 arasına çeviriyoruz → sağa ya da sola yavaşça değişen bir itme.
        float drift = (Mathf.PerlinNoise(noiseSeed, Time.time * driftSpeed) * 2f - 1f) * driftStrength;

        // 2) OYUNCUNUN İTMESİ
        float input = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input += 1f;
        }

        // 3) FİZİK: kuvvet → hız → konum. Sürtünme hızı yavaşça azaltır.
        velocity += (drift + input * playerForce) * dt;
        velocity -= velocity * damping * dt;
        position += velocity * dt;

        // 4) KENARA ÇARPTI MI?
        if (Mathf.Abs(position) >= 1f)
        {
            // Sonucu sunucuya bildir. Cezaya sunucu karar verir.
            SuspicionMeter.Instance.ReportNeedleHitRpc();

            position = Mathf.Sign(position) * bounceBackTo;   // Sign: işaretini al (+1 ya da -1)
            velocity = 0f;
            flashTimer = flashDuration;
        }

        // 5) GÖRSEL: -1..1 konumu piksele çevir
        float halfWidth = track.rect.width / 2f;
        needle.anchoredPosition = new Vector2(position * halfWidth, needle.anchoredPosition.y);

        if (flashTimer > 0f) flashTimer -= dt;
        needleImage.color = flashTimer > 0f ? Color.red : Color.white;
    }

    // Bu bilgisayardaki oyuncu mülakatçı mı ve laptopta mı oturuyor?
    private bool IsLocalInterviewerSeated()
    {
        var nm = NetworkManager.Singleton;
        var meter = SuspicionMeter.Instance;
        if (nm == null || meter == null || !meter.IsSpawned) return false;
        if (meter.Stage.Value == SuspicionStage.Bitti) return false;   // Görüşme bitti

        var player = nm.SpawnManager.GetLocalPlayerObject();
        if (player == null) return false;
        if (player.GetComponent<PlayerRole>().CurrentRole.Value != Role.Mulakatci) return false;

        return player.GetComponent<PlayerInteraction>().IsSeated;
    }
}
