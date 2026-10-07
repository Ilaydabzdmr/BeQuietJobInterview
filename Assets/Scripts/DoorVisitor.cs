using Unity.Netcode;
using UnityEngine;

// Kapıya gelen ziyaretçiler. Her patlamada listeden rastgele biri seçilir.
public class DoorVisitor : ChaosEvent
{
    [Header("Ziyaretçiler")]
    [SerializeField] private VisitorType[] visitorTypes;   // Inspector'dan sürüklenen 4 dosya

    [Header("Görsel")]
    [SerializeField] private float popScale = 1.6f;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float needlePushPerSuspicion = 0.3f;   // Her şüphe puanı için ibre itme gücü

    // Şu anki ziyaretçinin listedeki sırası. Sunucu seçer, herkes okur.
    // Herkeste aynı liste olduğu için bu sayı yeterli.
    public NetworkVariable<int> CurrentVisitor = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private VisitorType Current =>
        visitorTypes[Mathf.Clamp(CurrentVisitor.Value, 0, visitorTypes.Length - 1)];

    // Ata sınıfın özelliklerini o anki ziyaretçinin verisiyle eziyoruz.
    public override string DisplayName => Current.displayName;
    public override string ActionPrompt => Current.actionPrompt;
    protected override float RepairDuration => Current.repairDuration;
    protected override float TimeToMaxSeverity => Current.timeToMaxSeverity;
    protected override float MaxMultiplier => Current.maxMultiplier;

    private float ringTimer;
    private float popTimer;
    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    protected override void OnTriggered()
    {
        int index = PickWeighted();
        CurrentVisitor.Value = index;
        ringTimer = 0f;          // İlk zil hemen
        ArrivalRpc(index);       // AN: "Kim geldi" altyazısı herkese
    }

    protected override void Update()
    {
        base.Update();

        if (IsServer && IsActive.Value)
        {
            ringTimer -= Time.deltaTime;
            if (ringTimer <= 0f)
            {
                SuspicionMeter.Instance.AddSuspicion(Current.ringSuspicion);
                RingRpc(Current.ringSuspicion * needlePushPerSuspicion);   // Polis sert, takılan zil hafif
                ringTimer = Mathf.Lerp(Current.ringIntervalStart, Current.ringIntervalMin, Severity.Value);
            }
        }

        if (popTimer > 0f)
        {
            popTimer -= Time.deltaTime;
            float t = popTimer / popDuration;
            transform.localScale = baseScale * Mathf.Lerp(1f, popScale, t);
        }
        else
        {
            transform.localScale = baseScale;
        }
    }

    // Ağırlıklı rastgele seçim: weight büyük olan daha sık gelir.
    // Mantık: Ağırlıkları uç uca dizilmiş bir çizgi gibi düşün, rastgele bir nokta seç,
    // hangi parçaya düştüyse o ziyaretçi gelir.
    private int PickWeighted()
    {
        float total = 0f;
        foreach (var v in visitorTypes) total += v.weight;

        float r = Random.Range(0f, total);
        for (int i = 0; i < visitorTypes.Length; i++)
        {
            r -= visitorTypes[i].weight;
            if (r <= 0f) return i;
        }
        return visitorTypes.Length - 1;   // Kayan nokta hatalarına karşı güvenlik
    }

    // AN: Ziyaretçi geldi. Index'i parametre olarak yolluyoruz, çünkü RPC,
    // CurrentVisitor NetworkVariable'ı client'a ulaşmadan ÖNCE varabilir.
    [Rpc(SendTo.ClientsAndHost)]
    private void ArrivalRpc(int index)
    {
        if (InterviewReactionUI.Instance != null)
            InterviewReactionUI.Instance.ShowSubtitle(visitorTypes[index].arrivalLine);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RingRpc(float needlePush)
    {
        popTimer = popDuration;

        // Aynı "an"ı ibre için de kullan. Sadece oturan mülakatçıda etki eder.
        if (NeedleMinigame.Instance != null)
            NeedleMinigame.Instance.Push(needlePush, transform.position);
    }
}