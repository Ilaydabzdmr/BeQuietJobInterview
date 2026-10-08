using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Kaosları rastgele zamanlarda başlatan "yönetmen". SADECE SUNUCUDA çalışır.
public class ChaosDirector : NetworkBehaviour
{
    [Header("Zamanlama (saniye)")]
    [SerializeField] private float firstDelay = 10f;    // Oyun başladıktan sonra ilk kaos
    [SerializeField] private float minInterval = 15f;   // Kaoslar arası en kısa süre
    [SerializeField] private float maxInterval = 35f;   // Kaoslar arası en uzun süre

    private Coroutine loop;          // Çalışan döngünün "kumandası" (durdurmak için)
    private float nextChaosTime;     // Bir sonraki kaosun zamanı (debug için)

    // Host'un debug ekranı için: bir sonraki kaosa kaç saniye kaldı?
    public float NextChaosIn => Mathf.Max(0f, nextChaosTime - Time.time);

    public override void OnNetworkSpawn()
    {
        // Sadece sunucu döngüyü başlatır.
        if (IsServer)
            loop = StartCoroutine(ChaosLoop());
    }

    public override void OnNetworkDespawn()
    {
        if (loop != null) StopCoroutine(loop);
    }

    // IEnumerator: Coroutine'lerin dönüş tipi. "Duraklayabilen metot" demek.
    private IEnumerator ChaosLoop()
    {
        while (true)   // Her görüşme için bir tur
        {
            // Görüşme başlayana kadar bekle.
            // WaitUntil: içindeki koşul true olana kadar her karede kontrol eder.
            yield return new WaitUntil(() =>
                InterviewSession.Instance != null && InterviewSession.Instance.IsInCall);

            // Görüşme başladı: ilk kaostan önce kısa bir nefes
            nextChaosTime = Time.time + firstDelay;
            yield return new WaitForSeconds(firstDelay);

            // Görüşme sürdükçe kaos üret
            while (InterviewSession.Instance.IsInCall)
            {
                TriggerRandomChaos();

                float wait = Random.Range(minInterval, maxInterval);
                nextChaosTime = Time.time + wait;
                yield return new WaitForSeconds(wait);
            }
            // Görüşme bitti → en baştaki WaitUntil'e dön, bir sonraki görüşmeyi bekle
        }
    }

    // Aktif OLMAYAN kaoslardan birini rastgele seç ve başlat.
    private void TriggerRandomChaos()
    {
        var candidates = new List<ChaosEvent>();
        foreach (var c in ChaosEvent.All)
            if (!c.IsActive.Value) candidates.Add(c);

        if (candidates.Count == 0) return;   // Hepsi zaten aktif, bu tur pas

        // Random.Range(int, int): üst sınır DAHİL DEĞİL → 0 ile Count-1 arası.
        candidates[Random.Range(0, candidates.Count)].Trigger();
    }
}