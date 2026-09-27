using UnityEngine;
using Unity.Netcode; // Import the Unity Netcode namespace for networking functionality

// MonoBehaviour: Bu script'in bir GameObject'e "component" olarak takılabilmesini sağlar.
// Takılmadıkça hiçbir şey çalışmaz. Sahnede bir objeye eklemen şart
public class NetworkTestButtons : MonoBehaviour
{
    // OnGUI: Unity'nin eski, "kodla anında UI çizme" sistemi (IMGUI).
    // her karede (hatta karede birkaç kez) otomatik çağrılır ve ekranı sıfırdan çizer.
    // Neden bunu kullandık? Canvas, buton prefab'ı vb. kurmadan hızlıca test paneli yapmak için
    // Gerçek menüde bunu atıp Canvas UI kullanacağız. Bu sadece test iskelesi
    void OnGUI()
    {
        // Ekranın sol üstünde (x:10, y:10) 250x200 piksellik bir alan açıyoruz.
        // Aşağıdaki tüm butonlar/yazılar bu kutunun içine alt alta dizilir.
        GUILayout.BeginArea(new Rect(10, 10, 250, 200));

        // NetworkManager.Singleton: Sahnedeki tek NetworkManager'a her yerden ulaşma kısayolu.
        // (Singleton = "bundan sadece bir tane var, işte o"). Kısa isimle tutuyoruz ki kod okunaklı olsun.
        var nm = NetworkManager.Singleton;

        // Henüz ne client ne server'sak = hiçbir oturuma bağlı değiliz.
        // Bu durumda seçim butonlarını gösteriyoruz
        if (!nm.IsClient && !nm.IsServer)
        {
            // StartHost: Hem server hem client ol.Oyunu "kuran" oyuncu budur.
            // Server tarafı otoritedir: ileride şüphe barı gibi kritik değerler burada tutulacak.
            if (GUILayout.Button("Host Ol")) nm.StartHost();

            // StartClient: Mevcut bir host'a katıl. Sadece client'sın, otorite sende değil.
            if (GUILayout.Button("Client Ol")) nm.StartClient();

        }
        else
        {
            // Bağlandıysak butonları gizleyip durumu gösteriyoruz.
            // IsHost true ise hem server hem client'ız; değilse düz client'ız.
            GUILayout.Label(nm.IsHost ? "Mod: HOST" : "Mod: CLIENT");

            // LocalClientId: Netcode'un bu bilgisayara verdiği kimlik numarası.
            // Host her zaman 0 alır, katılanlar 1, 2, 3... İleride "kim mülakatçı" ayrımını bununla yapacağız.
            GUILayout.Label("ID Numarası: " + nm.LocalClientId);
        }
        // BeginArea ile açtığımız alanı kapatıyoruz. Açıp kapatmazsan Unity hata fırlatır.
        GUILayout.EndArea();

    }

}
