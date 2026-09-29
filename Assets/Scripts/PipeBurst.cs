using UnityEngine;
using UnityEngine.InputSystem;

// Boru patlaması. ChaosEvent'in tüm ağ davranışını miras alır,
// sadece kendine özgü görseli ekler.
public class PipeBurst : ChaosEvent
{
    public override string DisplayName => "Boru patlamasi";

    [SerializeField] private float pulseSpeed = 8f;    // Titreme hızı
    [SerializeField] private float pulseAmount = 0.3f; // Titreme büyüklüğü

    private Vector3 baseScale;   // Normal boyut

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        // GÖRSEL: Her bilgisayar kendisi hesaplar. Ağdan sadece IsActive gelir.
        if (IsActive.Value)
        {
            // Sin: -1 ile 1 arasında gidip gelen dalga → "nefes alan" titreme
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * s;
        }
        else
        {
            transform.localScale = baseScale;
        }

        // DEBUG: 1 = patlat, 2 = onar. 4b'de gerçek onarım gelince 2 silinecek,
        // 4c'de zamanlayıcı gelince 1 silinecek.
        if (!IsServer) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) Trigger();
        if (kb.digit2Key.wasPressedThisFrame) Resolve();
    }
}
