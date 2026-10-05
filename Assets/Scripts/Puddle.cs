using Unity.Netcode;
using UnityEngine;

// Su birikintisi. Sunucu boyutunu yazar, her client kendisi ölçekler.
public class Puddle : NetworkBehaviour
{
    // 0 = en küçük, 1 = en büyük. Sunucu yazar.
    public NetworkVariable<float> Size = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] private float minDiameter = 0.5f;   // İlk an (metre)
    [SerializeField] private float maxDiameter = 3.5f;   // En büyük hali (metre)

    void Update()
    {
        // Lerp: Size 0 iken min, 1 iken max, arada orantılı.
        float d = Mathf.Lerp(minDiameter, maxDiameter, Size.Value);
        transform.localScale = new Vector3(d, 0.01f, d);
    }
}
