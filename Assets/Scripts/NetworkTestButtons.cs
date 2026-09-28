using System.Threading.Tasks;               // Task: async metotların dönüş tipi
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;         // UnityTransport: verinin kablodaki taşıyıcısı
using Unity.Services.Authentication;        // Unity hesabına (anonim) giriş
using Unity.Services.Core;                  // Unity Services'ı başlatma
using Unity.Services.Relay;                 // Relay sunucusu API'si
using Unity.Services.Relay.Models;          // Allocation gibi veri tipleri
using UnityEngine;
using System.Text.RegularExpressions;   // Regex: metnin bir kurala uyup uymadığını kontrol eder

public class NetworkTestButtons : MonoBehaviour
{
    private string joinCodeInput = "";   // Client'ın yazdığı kod
    private string myJoinCode = "";      // Host'un aldığı ve paylaşacağı kod
    private string status = "";          // Ekrana basılan durum/hata mesajı
    private bool busy = false;           // İşlem sürerken butonlara tekrar basılmasın diye kilit

    // Relay'i kullanmak için önce Unity'nin sunucularına "ben kimim" demek gerekiyor.
    // async/await: İnternet işlemi saniyeler sürebilir. await, oyunu dondurmadan
    // "cevap gelince buradan devam et" demek. (WinForms'taki async ile aynı mantık.)
    async Task SignIn()
    {
        // Servisler sadece bir kez başlatılır. Zaten başlatıldıysa tekrar yapma.
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            var options = new InitializationOptions();
            // Rastgele profil adı: Aynı bilgisayarda iki oyun penceresi açtığında
            // ikisi de aynı anonim hesapla girip çakışmasın diye. TEST HİLESİ.
            // Yan etkisi: Her açılışta yeni bir kimlik oluşur. Gerçek sürümde Steam
            // kimliğine geçince bu satır gidecek.
            options.SetProfile("p" + Random.Range(0, 1000000));
            await UnityServices.InitializeAsync(options);
        }

        // Anonim giriş: Kullanıcı adı/şifre yok, Unity bize geçici bir kimlik verir.
        // Relay bu kimlik olmadan istek kabul etmez.
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // async void: Normalde kaçınılır (hataları yutulabilir), ama buton tıklaması gibi
    // "ateşle ve unut" olaylarında kabul edilir. try/catch koyduğumuz için güvendeyiz.
    async void StartHostRelay()
    {
        busy = true;
        status = "Relay'e baglaniliyor...";
        try
        {
            await SignIn();

            // Allocation: Relay sunucusunda bizim için ayrılan "oda".
            // 3 = host HARİÇ en fazla bağlanacak kişi sayısı → toplam 4 oyuncu. Oyunun kuralı burada.
            var allocation = await RelayService.Instance.CreateAllocationAsync(3);

            // NetworkManager'ın taşıyıcısını al ve "doğrudan IP'ye değil, bu Relay odasına bağlan" de.
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            // "dtls" = şifreli bağlantı. Alternatif "udp" şifresizdir. Güvenli olan varsayılan bu.
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            // Odanın kısa kodunu al (ör. "7XK2PQ"). Arkadaşlarına bunu göndereceksin.
            myJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("JOIN CODE: " + myJoinCode);

            // Sıralama önemli: Transport Relay'e ayarlandıktan SONRA host başlatılır.
            // Önce başlatırsan "SetRelayServerData çağrılmadı" hatası alırsın (dün yaşadık).
            NetworkManager.Singleton.StartHost();
            status = "";
        }
        catch (System.Exception e)
        {
            // İnternet yok, servis kapalı, proje Unity Cloud'a bağlı değil vb.
            status = "Hata: " + e.Message;   // Oyuncuya kısa mesaj
            Debug.LogException(e);            // Sana Console'da tam detay
        }
        busy = false;
    }

    async void StartClientRelay()
    {
        // YENİ: Biçim kontrolü. Açıkça yanlış bir kodu Relay'e hiç göndermiyoruz.
        // Asıl kontrolü yine Relay yapar; bu sadece internete çıkmadan hızlı uyarı.
        // Trim(): Kopyala-yapıştırda gelen boşlukları temizler.
        string code = joinCodeInput.Trim();
        // Relay'in kendi hata mesajında verdiği kural: izin verilen harfler, 6-12 uzunluk.
        if (!Regex.IsMatch(code, "^[6789BCDFGHJKLMNPQRTWbcdfghjklmnpqrtw]{6,12}$"))
        {
            status = "Gecersiz kod. Host ekranindaki kodu aynen yapistir.";
            return;
        }

        busy = true;
        status = "Odaya katiliniyor...";
        try
        {
            await SignIn();

            // Host'un odası yerine, verilen kodla var olan odaya katıl.
            // YENİ: joinCodeInput.Trim() yerine yukarıda temizlediğimiz "code" kullanılıyor.
            var allocation = await RelayService.Instance.JoinAllocationAsync(code);

            // Host'takiyle aynı: Taşıyıcıyı Relay'e yönlendir, sonra başlat.
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            NetworkManager.Singleton.StartClient();
            status = "";
        }
        catch (System.Exception e)
        {
            status = "Hata: " + e.Message;
            Debug.LogException(e);
        }
        busy = false;
    }

    void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;   // Sahne yüklenirken NetworkManager henüz yoksa çökme

        GUILayout.BeginArea(new Rect(10, 10, 300, 250));

        if (!nm.IsClient && !nm.IsServer)
        {
            if (busy)
            {
                GUILayout.Label(status);   // Bekleme sırasında sadece durum yazısı
            }
            else
            {
                if (GUILayout.Button("Host Ol (Relay)")) StartHostRelay();
                GUILayout.Label("Join code:");
                // TextField: Her karede eski değeri alır, yazılan yeni değeri döndürür.
                // Bu yüzden sonucu aynı değişkene geri atıyoruz. OnGUI'de metin kutusu böyle çalışır.
                joinCodeInput = GUILayout.TextField(joinCodeInput);
                if (GUILayout.Button("Temizle"))
                {
                    GUIUtility.keyboardControl = 0;   // Odağı metin kutusundan al, yoksa Unity eski metni tutar
                    joinCodeInput = "";               // Artık silme görünür hale gelir
                    status = "";                      // Eski hata mesajını da temizle
                }
                if (GUILayout.Button("Client Ol (Relay)")) StartClientRelay();
                if (status != "") GUILayout.Label(status);   // Varsa hata mesajını göster
            }
        }
        else
        {
            GUILayout.Label(nm.IsHost ? "Mod: HOST" : "Mod: CLIENT");
            GUILayout.Label("ID: " + nm.LocalClientId);
            // Kodu Label yerine TextField'da gösteriyoruz: seçip kopyalanabilsin diye.
            // Dönüş değerini kullanmadığımız için kullanıcı değiştirse bile bir şey olmaz.
            if (nm.IsHost) GUILayout.TextField(myJoinCode);
            var player = nm.SpawnManager?.GetLocalPlayerObject();
            if (player != null)
            {
                var role = player.GetComponent<PlayerRole>().CurrentRole.Value;
                GUILayout.Label("Rolun: " + (role == Role.Mulakatci ? "MULAKATCI" : "Ev arkadasi"));
            }
        }

        GUILayout.EndArea();
    }
}