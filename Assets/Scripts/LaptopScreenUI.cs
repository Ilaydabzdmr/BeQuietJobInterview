using UnityEngine;
using UnityEngine.UI;

// Laptop arayüzünün iki modu arasında geçiş:
// - Normal: UI kamerası dokuya çizer → 3D laptop ekranında canlı görünür
// - Tam ekran: doğrudan monitörün üstüne çizilir
// Tamamen yerel.
[RequireComponent(typeof(Canvas))]
public class LaptopScreenUI : MonoBehaviour
{
    [SerializeField] private Camera uiCamera;   // LaptopUICamera

    private Canvas canvas;
    private GraphicRaycaster raycaster;   // Fare tıklamalarını arayüze ileten component

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        raycaster = GetComponent<GraphicRaycaster>();
        SetFullscreen(false);   // Oyun her zaman 3D ekran modunda başlar
    }

    public void SetFullscreen(bool full)
    {
        if (full)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;   // Bu kamera çizecek
            canvas.planeDistance = 1f;       // Kameranın 1 metre önüne yerleştir
        }

        // Sadece tam ekrandayken tıklanabilir olsun. Yoksa ayaktayken farenin
        // nereye tıkladığına göre görünmez düğmelere basılabilirdi.
        raycaster.enabled = full;
    }
}
