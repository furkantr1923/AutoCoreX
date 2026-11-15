using AutoCoreX.Models;

namespace AutoCoreX.Services;

// Standart (SAE J2012) OBD-II arıza kodları için Türkçe bilgilendirme sözlüğü.
// Açıklamalar bilgilendirme amaçlıdır; kesin teşhis yerine geçmez.
public static class DtcSozluk
{
    private static readonly Dictionary<string, DtcBilgi> _kodlar = new(StringComparer.OrdinalIgnoreCase);

    private static void K(string kod, string aciklama, string sistem, int ciddiyet, string nedenler, string kontroller) =>
        _kodlar[kod] = new DtcBilgi
        {
            Kod = kod, Aciklama = aciklama, Sistem = sistem, Ciddiyet = ciddiyet,
            Nedenler = nedenler.Split('|'), Kontroller = kontroller.Split('|')
        };

    static DtcSozluk()
    {
        const string Yakit = "Yakıt sistemi", Hava = "Hava / emme sistemi", Sogutma = "Soğutma sistemi", Ateslem = "Ateşleme / yanma",
            Turbo = "Turbo / aşırı besleme", Emisyon = "Emisyon kontrol", Kizdirma = "Kızdırma (ön ısıtma) sistemi", Sensor = "Motor sensörleri",
            Elektrik = "Elektrik / şarj", Ecu = "Motor kontrol ünitesi (ECU)", Iletisim = "Araç ağı (CAN) iletişimi", Yag = "Yağlama sistemi";

        K("P0016", "Krank – eksantrik mili konum uyumsuzluğu (Bank 1, Sensör A)", Sensor, 3,
          "Triger kayışı/zinciri atlaması veya gerginlik sorunu|Krank veya eksantrik sensörü arızası|Kablo/soket problemi",
          "Triger kayışı ayarı ve gergisi|Krank ve eksantrik sensörleri|Sensör soketleri ve kabloları");
        K("P0045", "Turbo basınç kontrol selenoidi devresi / açık devre", Turbo, 2,
          "Selenoid (vakum valfi) arızası|Kablo kopukluğu veya soket oksidasyonu|Vakum hortumu çatlağı",
          "Turbo kontrol selenoidi direnci|Vakum hortumları|Soket ve kablolar");
        K("P0046", "Turbo basınç kontrol selenoidi devresi menzil/performans", Turbo, 2,
          "Selenoid valfi takılması|Vakum kaçağı|Turbo geometri (VGT) mekanizması sıkışması",
          "Vakum hatları|Turbo aktüatör hareketi|Selenoid valfi");
        K("P0087", "Yakıt rayı / sistem basıncı çok düşük", Yakit, 3,
          "Tıkalı yakıt filtresi|Yüksek basınç pompası zayıflığı|Yakıt basınç regülatörü arızası|Enjektör geri dönüş kaçağı|Yakıt seviyesinin çok düşük olması",
          "Yakıt filtresi|Düşük basınç besleme hattı|Ray basınç sensörü verisi|Enjektör geri dönüş testi");
        K("P0088", "Yakıt rayı / sistem basıncı çok yüksek", Yakit, 3,
          "Basınç regülatörü takılı kalması|Ray basınç sensörü hatalı ölçüm|Geri dönüş hattı tıkanıklığı",
          "Basınç regülatörü|Ray basınç sensörü|Geri dönüş hattı");
        K("P0089", "Yakıt basınç regülatörü 1 performans", Yakit, 2,
          "Regülatör valfi arızası|Kirli yakıt|Elektrik bağlantı sorunu",
          "Regülatör valfi|Yakıt filtresi ve yakıt kalitesi|Soket bağlantısı");
        K("P0093", "Yakıt sisteminde kaçak tespit edildi (büyük kaçak)", Yakit, 3,
          "Yüksek basınç hattında kaçak|Enjektör contası kaçağı|Ray veya pompa bağlantı kaçağı",
          "Motor üstü ve yakıt hatlarında ıslaklık/koku|Enjektör contaları|Bağlantı rakorları");
        K("P0100", "Hava akış ölçer (MAF) devresi", Hava, 2,
          "MAF sensörü arızası|Kablo/soket problemi|Hava filtresi sonrası kaçak",
          "MAF soketi ve kabloları|Hava filtresi kutusu bağlantısı|MAF sensörü");
        K("P0101", "Hava akış ölçer (MAF) menzil/performans", Hava, 2,
          "Kirli/arızalı MAF sensörü|Emme hattında hava kaçağı|Tıkalı hava filtresi|EGR valfinin açık kalması",
          "Hava filtresi|Emme hortumları ve kelepçeler|EGR valfi|MAF sensörü değerleri");
        K("P0102", "Hava akış ölçer (MAF) devresi düşük giriş", Hava, 2,
          "MAF sensörü arızası|Soket gevşekliği|Besleme voltajı kesik",
          "MAF soketi|Sensör beslemesi ve şasesi");
        K("P0103", "Hava akış ölçer (MAF) devresi yüksek giriş", Hava, 2,
          "MAF sensörü arızası|Sinyal hattında kısa devre",
          "MAF sensörü|Kablo demeti");
        K("P0105", "Manifold mutlak basınç (MAP) devresi", Hava, 2,
          "MAP sensörü arızası|Kablo/soket problemi",
          "MAP sensörü|Sensör soketi");
        K("P0106", "Manifold mutlak basınç (MAP) menzil/performans", Hava, 2,
          "MAP sensörü arızası|Vakum/hava kaçağı|Turbo hortumunda çatlak",
          "MAP sensörü değeri|Turbo ve intercooler hortumları");
        K("P0110", "Emme havası sıcaklık sensörü devresi", Sensor, 1,
          "Sensör arızası|Kablo/soket problemi",
          "Emme havası sıcaklık sensörü|Soket bağlantısı");
        K("P0112", "Emme havası sıcaklık sensörü düşük giriş", Sensor, 1,
          "Sensör içi kısa devre|Kablo şaseye kaçak", "Sensör direnci|Kablo demeti");
        K("P0113", "Emme havası sıcaklık sensörü yüksek giriş", Sensor, 1,
          "Sensör bağlantısı kopuk|Kablo açık devre", "Soket takılı mı|Kablo sürekliliği");
        K("P0115", "Motor soğutma suyu sıcaklık sensörü devresi", Sogutma, 2,
          "Sensör arızası|Kablo/soket problemi", "Soğutma suyu sıcaklık sensörü|Soket");
        K("P0116", "Motor soğutma suyu sıcaklık sensörü menzil/performans", Sogutma, 2,
          "Termostat açık kalması|Sensör hatalı ölçüm|Düşük antifriz seviyesi",
          "Antifriz seviyesi|Termostat|Sensör değerinin gerçek sıcaklıkla karşılaştırılması");
        K("P0117", "Motor soğutma suyu sıcaklık sensörü düşük giriş", Sogutma, 2,
          "Sensör kısa devre|Kablo şaseye kaçak", "Sensör direnci|Kablo demeti");
        K("P0118", "Motor soğutma suyu sıcaklık sensörü yüksek giriş", Sogutma, 2,
          "Sensör soketi çıkmış|Kablo açık devre", "Soket bağlantısı|Kablo sürekliliği");
        K("P0128", "Soğutma suyu termostat altında (motor çalışma sıcaklığına ulaşamıyor)", Sogutma, 1,
          "Termostatın açık kalması|Sıcaklık sensörü hatası|Düşük antifriz",
          "Termostat|Antifriz seviyesi|Isınma süresi");
        K("P0171", "Karışım çok fakir (Bank 1)", Yakit, 2,
          "Emme manifoldunda hava kaçağı|Kirli MAF sensörü|Düşük yakıt basıncı|Tıkalı enjektör",
          "Vakum hortumları|MAF sensörü|Yakıt basıncı");
        K("P0172", "Karışım çok zengin (Bank 1)", Yakit, 2,
          "Kaçıran enjektör|Hatalı MAF/MAP ölçümü|Tıkalı hava filtresi",
          "Enjektörler|Hava filtresi|MAF/MAP sensörleri");
        K("P0190", "Yakıt rayı basınç sensörü devresi", Yakit, 3,
          "Ray basınç sensörü arızası|Kablo/soket problemi|Sensör beslemesi (5V) sorunu",
          "Ray basınç sensörü soketi|Kablo demeti|Sensör besleme voltajı");
        K("P0191", "Yakıt rayı basınç sensörü menzil/performans", Yakit, 3,
          "Sensör hatalı ölçüm|Gerçek basınç sorunu (pompa/regülatör)",
          "Ray basıncı canlı verisi|Pompa ve regülatör");
        K("P0201", "Enjektör devresi / açık devre – 1. silindir", Yakit, 3,
          "Enjektör bobini arızası|Kablo kopukluğu|ECU sürücü devresi", "1. silindir enjektörü|Enjektör kabloları ve soketi");
        K("P0202", "Enjektör devresi / açık devre – 2. silindir", Yakit, 3,
          "Enjektör bobini arızası|Kablo kopukluğu|ECU sürücü devresi", "2. silindir enjektörü|Enjektör kabloları ve soketi");
        K("P0203", "Enjektör devresi / açık devre – 3. silindir", Yakit, 3,
          "Enjektör bobini arızası|Kablo kopukluğu|ECU sürücü devresi", "3. silindir enjektörü|Enjektör kabloları ve soketi");
        K("P0204", "Enjektör devresi / açık devre – 4. silindir", Yakit, 3,
          "Enjektör bobini arızası|Kablo kopukluğu|ECU sürücü devresi", "4. silindir enjektörü|Enjektör kabloları ve soketi");
        K("P0217", "Motor aşırı ısınma durumu", Sogutma, 3,
          "Düşük antifriz / kaçak|Fan çalışmaması|Termostat kapalı kalması|Su pompası arızası|Silindir kapak contası",
          "Antifriz seviyesi ve kaçak|Fan çalışması|Termostat ve su pompası");
        K("P0234", "Turbo aşırı basınç (overboost)", Turbo, 3,
          "Turbo geometri mekanizması sıkışması|Basınç kontrol valfi arızası|MAP sensörü hatası",
          "Turbo aktüatörü|Kontrol selenoidi|Vakum hatları|MAP sensörü");
        K("P0236", "Turbo basınç sensörü A menzil/performans", Turbo, 2,
          "Sensör arızası|Hortum/kaçak|Kirli sensör", "Basınç sensörü|Turbo hortumları");
        K("P0237", "Turbo basınç sensörü A devresi düşük", Turbo, 2,
          "Sensör arızası|Kablo kısa devre", "Sensör ve soket|Kablo demeti");
        K("P0238", "Turbo basınç sensörü A devresi yüksek", Turbo, 2,
          "Sensör arızası|Kablo açık devre", "Sensör ve soket|Kablo demeti");
        K("P0299", "Turbo düşük basınç (underboost)", Turbo, 2,
          "Turbo/intercooler hortumunda kaçak|Turbo geometri sıkışması (karbon)|Vakum kaçağı|Turbo arızası",
          "Turbo hortumları ve kelepçeler|Intercooler|Turbo aktüatör hareketi|Vakum hatları");
        K("P0300", "Rastgele / çoklu silindir tekleme tespit edildi", Ateslem, 3,
          "Enjektör sorunu|Düşük kompresyon|Hava kaçağı|Yakıt kalitesi",
          "Enjektörler|Kompresyon testi|Emme kaçakları");
        K("P0301", "1. silindirde tekleme tespit edildi", Ateslem, 3, "Enjektör|Kompresyon|Kızdırma bujisi (soğukta)", "1. silindir enjektörü|Kompresyon");
        K("P0302", "2. silindirde tekleme tespit edildi", Ateslem, 3, "Enjektör|Kompresyon|Kızdırma bujisi (soğukta)", "2. silindir enjektörü|Kompresyon");
        K("P0303", "3. silindirde tekleme tespit edildi", Ateslem, 3, "Enjektör|Kompresyon|Kızdırma bujisi (soğukta)", "3. silindir enjektörü|Kompresyon");
        K("P0304", "4. silindirde tekleme tespit edildi", Ateslem, 3, "Enjektör|Kompresyon|Kızdırma bujisi (soğukta)", "4. silindir enjektörü|Kompresyon");
        K("P0335", "Krank mili konum sensörü A devresi", Sensor, 3,
          "Krank sensörü arızası|Sensör ile volan arası kirlenme|Kablo/soket", "Krank sensörü|Soket ve kablo|Volan dişlisi");
        K("P0340", "Eksantrik mili konum sensörü A devresi (Bank 1)", Sensor, 3,
          "Eksantrik sensörü arızası|Kablo/soket|Triger ayarı", "Eksantrik sensörü|Soket ve kablo");
        K("P0380", "Kızdırma bujisi / ısıtıcı devresi A", Kizdirma, 2,
          "Kızdırma bujisi yanması|Kızdırma rölesi/modülü arızası|Kablo kopukluğu",
          "Kızdırma bujileri direnç testi|Kızdırma rölesi|Kablo bağlantıları");
        K("P0381", "Kızdırma bujisi / ısıtıcı gösterge devresi", Kizdirma, 1,
          "Gösterge lambası devresi|Gösterge paneli bağlantısı", "Gösterge lambası devresi");
        K("P0400", "EGR (egzoz gazı geri dönüşü) akış arızası", Emisyon, 2,
          "EGR valfinde karbon birikmesi|EGR valfi arızası|Tıkalı EGR kanalları", "EGR valfi|EGR soğutucu ve kanallar");
        K("P0401", "EGR akışı yetersiz", Emisyon, 2,
          "EGR valfinde karbon birikmesi|EGR kanalı tıkanıklığı|EGR kontrol valfi arızası",
          "EGR valfi temizliği/hareketi|EGR soğutucusu|Vakum ve kontrol hattı");
        K("P0402", "EGR akışı aşırı", Emisyon, 2,
          "EGR valfinin açık kalması (karbon)|Kontrol valfi arızası", "EGR valfi kapanıyor mu|Kontrol valfi");
        K("P0403", "EGR kontrol devresi", Emisyon, 2,
          "EGR valfi motoru/bobini arızası|Kablo/soket", "EGR soketi ve kablosu|EGR valfi");
        K("P0420", "Katalitik konvertör verimi eşik altında (Bank 1)", Emisyon, 1,
          "Katalizör yaşlanması|Oksijen sensörü hatası|Egzoz kaçağı", "Katalizör|Oksijen sensörleri|Egzoz hattı");
        K("P0470", "Egzoz basınç sensörü", Emisyon, 2,
          "Sensör arızası|Sensör hortumu tıkalı/çatlak", "Egzoz basınç sensörü|Sensör hortumu");
        K("P0480", "Soğutma fanı 1 kontrol devresi", Sogutma, 2,
          "Fan rölesi arızası|Fan motoru|Sigorta / kablo", "Fan rölesi ve sigortası|Fan motoru");
        K("P0500", "Araç hız sensörü A", Sensor, 2,
          "Hız sensörü arızası|ABS teker sensörü verisi|Kablo", "Hız sensörü|ABS sensörleri|Kablo bağlantıları");
        K("P0520", "Motor yağ basınç sensörü / müşür devresi", Yag, 2,
          "Yağ müşürü arızası|Kablo/soket", "Yağ müşürü|Soket ve kablo");
        K("P0524", "Motor yağ basıncı çok düşük", Yag, 3,
          "Düşük yağ seviyesi|Yağ pompası arızası|Tıkalı yağ süzgeci|Yanlış viskozitede yağ",
          "Motoru durdurun ve yağ seviyesine bakın|Mekanik yağ basıncı testi");
        K("P0540", "Emme havası ısıtıcısı A devresi", Kizdirma, 1,
          "Isıtıcı elemanı arızası|Röle/kablo", "Isıtıcı elemanı|Röle ve kablo");
        K("P0560", "Sistem voltajı", Elektrik, 2,
          "Zayıf akü|Şarj dinamosu sorunu|Kutup başı oksidasyonu", "Akü testi|Şarj voltajı|Kutup başları ve şase");
        K("P0562", "Sistem voltajı düşük", Elektrik, 2,
          "Zayıf/boşalmış akü|Şarj dinamosu arızası|Gevşek akü bağlantısı", "Akü testi|Şarj voltajı (çalışırken ~13,8–14,5 V)|Bağlantılar");
        K("P0563", "Sistem voltajı yüksek", Elektrik, 2,
          "Konjektör (voltaj regülatörü) arızası", "Şarj voltajı|Dinamo regülatörü");
        K("P0571", "Fren müşürü A devresi", Elektrik, 1,
          "Fren müşürü ayarı/arızası|Kablo", "Fren lambaları yanıyor mu|Fren müşürü");
        K("P0603", "Kontrol modülü iç hafıza (KAM) hatası", Ecu, 2,
          "Akü bağlantısının kesilmesi sonrası|ECU iç arızası|Besleme sorunu", "Akü ve ECU besleme/şase|Kod silinip tekrar geliyor mu");
        K("P0606", "Kontrol modülü işlemci hatası", Ecu, 3,
          "ECU iç arızası|Besleme/şase sorunu|Yazılım hatası", "ECU besleme ve şaseleri|Yetkili servis teşhisi");
        K("P0670", "Kızdırma bujisi kontrol modülü devresi", Kizdirma, 2,
          "Kızdırma modülü arızası|Kablo/soket", "Kızdırma modülü|Kablo bağlantıları");
        K("P0671", "Kızdırma bujisi devresi – 1. silindir", Kizdirma, 1, "1. kızdırma bujisi arızası|Kablo", "1. kızdırma bujisi direnci");
        K("P0672", "Kızdırma bujisi devresi – 2. silindir", Kizdirma, 1, "2. kızdırma bujisi arızası|Kablo", "2. kızdırma bujisi direnci");
        K("P0673", "Kızdırma bujisi devresi – 3. silindir", Kizdirma, 1, "3. kızdırma bujisi arızası|Kablo", "3. kızdırma bujisi direnci");
        K("P0674", "Kızdırma bujisi devresi – 4. silindir", Kizdirma, 1, "4. kızdırma bujisi arızası|Kablo", "4. kızdırma bujisi direnci");
        K("P0700", "Şanzıman kontrol sistemi (arıza lambası talebi)", "Şanzıman", 2,
          "Şanzıman kontrol ünitesinde kayıtlı arıza", "Şanzıman ünitesi kodlarının okunması");
        K("P2002", "Dizel partikül filtresi (DPF) verimi eşik altında (Bank 1)", Emisyon, 2,
          "DPF hasarı/çatlak|DPF fark basınç sensörü hatası|Sensör hortumu sorunu", "DPF fark basınç sensörü ve hortumları|DPF durumu");
        K("P2263", "Turbo / aşırı besleme sistemi performansı", Turbo, 2,
          "Turbo hortum kaçağı|Turbo geometri sıkışması|Basınç kontrol valfi", "Turbo hortumları|Turbo aktüatörü|Kontrol valfi");
        K("P242F", "DPF tıkanıklığı – kül birikimi", Emisyon, 2,
          "DPF ömrünü doldurmuş|Uygun olmayan motor yağı (yüksek kül)", "DPF kül seviyesi|DPF değişimi/temizliği");
        K("P2452", "DPF fark basınç sensörü A devresi", Emisyon, 2,
          "Sensör arızası|Kablo/soket", "DPF basınç sensörü|Soket");
        K("P2453", "DPF fark basınç sensörü A menzil/performans", Emisyon, 2,
          "Sensör hortumları tıkalı/ters takılı|Sensör arızası", "Sensör hortumları|Sensör değeri");
        K("P2463", "DPF tıkanıklığı – is (kurum) birikimi", Emisyon, 2,
          "Sık kısa mesafe kullanım (rejenerasyon tamamlanamıyor)|EGR/enjektör kaynaklı fazla is|Katkı (Eolys) sıvısı bitmiş olabilir",
          "Uzun yol sürüşü / servis rejenerasyonu|Katkı sıvısı seviyesi (PSA araçlarda)|EGR ve enjektörler");
        K("U0001", "Yüksek hızlı CAN iletişim hattı", Iletisim, 3,
          "CAN hattında kopukluk/kısa devre|Arızalı bir kontrol ünitesi", "CAN hattı kabloları|Kontrol ünitelerinin beslemesi");
        K("U0100", "Motor kontrol ünitesi (ECM/PCM) ile iletişim kaybı", Iletisim, 3,
          "ECU beslemesi yok|CAN hattı sorunu|ECU arızası", "ECU sigortaları ve şasesi|CAN hattı");
        K("U0121", "ABS kontrol modülü ile iletişim kaybı", Iletisim, 3,
          "ABS modülü beslemesi|CAN hattı|ABS modül arızası", "ABS sigortası|Soket ve CAN hattı");
        K("U0155", "Gösterge paneli (cluster) ile iletişim kaybı", Iletisim, 2,
          "Gösterge paneli beslemesi|CAN hattı", "Gösterge paneli sigortası|Soket");
    }

    public static int Sayi => _kodlar.Count;

    public static bool GecerliMi(string? kod) =>
        !string.IsNullOrWhiteSpace(kod) && System.Text.RegularExpressions.Regex.IsMatch(kod.Trim(), "^[PCBU][0-3][0-9A-F]{3}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public static DtcBilgi Bul(string kod)
    {
        kod = (kod ?? "").Trim().ToUpperInvariant();
        if (_kodlar.TryGetValue(kod, out var b)) return b;

        // Sözlükte yoksa: koddan çıkarılabilen genel bilgi
        var bilgi = new DtcBilgi { Kod = kod, Sozlukte = false, Ciddiyet = 2 };
        if (!GecerliMi(kod)) { bilgi.Aciklama = "Geçersiz kod biçimi"; bilgi.Sistem = "—"; return bilgi; }

        var alan = kod[0] switch { 'P' => "Motor / şanzıman (Powertrain)", 'C' => "Şasi (fren, direksiyon, süspansiyon)", 'B' => "Gövde (klima, hava yastığı, konfor)", _ => "Araç ağı iletişimi" };
        var tip = kod[1] switch { '0' => "SAE standart (genel) kod", '1' => "Üreticiye özel kod", '2' => kod[0] == 'P' ? "SAE standart (genel) kod" : "Üreticiye özel kod", _ => "Ortak / ayrılmış kod" };
        var altSistem = kod[0] == 'P' ? kod[2] switch
        {
            '1' or '2' => "Yakıt ve hava ölçümü",
            '3' => "Ateşleme / tekleme",
            '4' => "Yardımcı emisyon kontrolü",
            '5' => "Hız, rölanti kontrolü",
            '6' => "Bilgisayar ve çıkış devreleri",
            '7' or '8' or '9' => "Şanzıman",
            _ => "Yakıt/hava ölçümü ve ek emisyon"
        } : alan;

        bilgi.Sistem = altSistem;
        bilgi.Aciklama = $"{alan} · {tip}. Bu kod uygulama sözlüğünde yok.";
        bilgi.Nedenler = new[] { "Kodun anlamı araca/üreticiye göre değişebilir" };
        bilgi.Kontroller = new[] { "Kodu araç markasına özel kaynaklardan veya serviste doğrulatın" };
        return bilgi;
    }
}
