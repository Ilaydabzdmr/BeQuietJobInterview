using Unity.Netcode;
using UnityEngine;

public class PipeBurst : ChaosEvent
{
    public override string DisplayName => "Boru patlamasi";

    [Header("Görsel")]
    [SerializeField] private float pulseSpeed = 8f;
    [SerializeField] private float pulseAmount = 0.3f;

    [Header("Su birikintisi")]
    [SerializeField] private NetworkObject puddlePrefab;
    [SerializeField] private Transform puddlePoint;
    [SerializeField] private float burstNeedlePush = 1.5f;   // Patlama anında ibreye sert darbe

    private Vector3 baseScale;
    private Puddle puddle;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    protected override void OnTriggered()
    {
        NetworkObject obj = Instantiate(puddlePrefab, puddlePoint.position, Quaternion.identity);
        obj.Spawn();
        puddle = obj.GetComponent<Puddle>();

        BurstRpc(burstNeedlePush);   // AN: "Boru şu an patladı!"
    }

    protected override void OnResolved()
    {
        if (puddle != null && puddle.IsSpawned)
            puddle.NetworkObject.Despawn();
        puddle = null;
    }

    protected override void Update()
    {
        base.Update();   // Kötüleşme ve tamir artık atada

        // Birikintinin boyutu = borunun kötüleşmesi
        if (IsServer && IsActive.Value && puddle != null)
            puddle.Size.Value = Severity.Value;

        if (IsActive.Value)
        {
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * s;
        }
        else
        {
            transform.localScale = baseScale;
        }
    }

    // AN: Patlama. Faz 4'te patlama sesi de burada çalacak.
    [Rpc(SendTo.ClientsAndHost)]
    private void BurstRpc(float needlePush)
    {
        if (NeedleMinigame.Instance != null)
            NeedleMinigame.Instance.Push(needlePush, transform.position);
    }
}