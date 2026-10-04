using UnityEngine;


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

    // DEĞİŞTİ(Adim4.b): override → üst sınıfın Update'ini genişletiyoruz.
    protected override void Update()
    {
        base.Update();   // ÖNEMLİ: Tamir mantığı üst sınıfta. Bunu unutursan tamir ilerlemez!

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

    }
}
