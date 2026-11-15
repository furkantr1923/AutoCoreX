using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoCoreX.Services;

public enum ObdDurum { Kapali, Baglaniyor, Bagli, Demo }

// OBD-II yönetimi: gerçek ELM327 bağlantısı veya AÇIKÇA ETİKETLİ demo simülasyonu.
// Yalnızca okuma yapar. ECU'ya yazma, kod silme veya kontrol komutu yoktur.
public class ObdServisi
{
    public ObdDurum Durum { get; private set; } = ObdDurum.Kapali;
    public string? CihazAdi { get; private set; }
    public string? Protokol { get; private set; }
    public string? AdaptorSurumu { get; private set; }
    public string? Hata { get; private set; }
    public bool EcuYanitVerdi { get; private set; }
    public HashSet<string> Desteklenen { get; } = new();
    public DateTime SonGuncelleme { get; private set; }
    public List<string> Gunluk { get; } = new();

    public event Action? Degisti;

    private readonly Dictionary<string, double> _degerler = new();
    private readonly SemaphoreSlim _hat = new(1, 1);
    private IObdBaglanti? _baglanti;
    private CancellationTokenSource? _dongu;
    private char _protokolNo = '0';

    public bool Canli => Durum is ObdDurum.Bagli or ObdDurum.Demo;
    public bool Demo => Durum == ObdDurum.Demo;
    public string KaynakEtiketi => Durum switch { ObdDurum.Demo => "DEMO", ObdDurum.Bagli => "CANLI", ObdDurum.Baglaniyor => "BAĞLANIYOR", _ => "BAĞLANTI YOK" };

    public double? Deger(string pid) { lock (_degerler) return _degerler.TryGetValue(pid, out var v) ? v : null; }
    public double? Voltaj() => Deger("42") ?? Deger("ATRV");
    public bool Destekli(string pid) => Demo || Desteklenen.Contains(pid);

    private void Bildir() => Degisti?.Invoke();

    private void Log(string metin)
    {
        lock (Gunluk)
        {
            Gunluk.Add($"{DateTime.Now:HH:mm:ss}  {metin}");
            if (Gunluk.Count > 150) Gunluk.RemoveAt(0);
        }
    }

    // ================= DEMO =================

    public async Task DemoBaslatAsync()
    {
        await DurdurAsync();
        Durum = ObdDurum.Demo;
        CihazAdi = "Simülasyon";
        Protokol = "Demo (gerçek araç verisi değildir)";
        EcuYanitVerdi = false;
        Desteklenen.Clear();
        foreach (var p in Pidler.Hepsi) Desteklenen.Add(p.Kod);
        Log("DEMO modu başladı. Gösterilen değerler simülasyondur.");
        _dongu = new CancellationTokenSource();
        _ = DemoDongusu(_dongu.Token);
        Bildir();
    }

    private async Task DemoDongusu(CancellationToken ct)
    {
        var r = new Random();
        double rpm = 820, hiz = 0, sicaklik = 34, gaz = 0, hedefRpm = 820, hedefHiz = 0, sure = 0;
        var evre = 0; // 0 rölanti, 1 hızlanma, 2 seyir, 3 yavaşlama
        while (!ct.IsCancellationRequested)
        {
            sure -= 0.25;
            if (sure <= 0)
            {
                evre = (evre + 1) % 4;
                sure = evre switch { 0 => 4 + r.NextDouble() * 4, 1 => 4 + r.NextDouble() * 3, 2 => 6 + r.NextDouble() * 6, _ => 4 };
                switch (evre)
                {
                    case 0: hedefRpm = 820; hedefHiz = 0; gaz = 0; break;
                    case 1: hedefRpm = 2600 + r.Next(900); hedefHiz = 55 + r.Next(45); gaz = 40 + r.Next(25); break;
                    case 2: hedefRpm = 1700 + r.Next(400); gaz = 14 + r.Next(8); break;
                    default: hedefRpm = 1100; hedefHiz = 15; gaz = 0; break;
                }
            }
            rpm += (hedefRpm - rpm) * 0.12 + (r.NextDouble() - 0.5) * 30;
            hiz += (hedefHiz - hiz) * 0.05;
            sicaklik = Math.Min(89 + (r.NextDouble() - 0.5), sicaklik + 0.35);
            var yuk = Math.Clamp(18 + (rpm - 800) / 45 + gaz * 0.4 + (r.NextDouble() - 0.5) * 3, 0, 100);
            var map = 101 + Math.Max(0, rpm - 1400) / 18 * (gaz / 50 + 0.3);

            lock (_degerler)
            {
                _degerler["0C"] = Math.Max(0, rpm);
                _degerler["0D"] = Math.Max(0, Math.Round(hiz));
                _degerler["05"] = Math.Round(sicaklik);
                _degerler["04"] = yuk;
                _degerler["11"] = Math.Clamp(gaz + (r.NextDouble() - 0.5) * 2, 0, 100);
                _degerler["0F"] = 27 + hiz * 0.03 + (r.NextDouble() - 0.5);
                _degerler["0B"] = Math.Min(230, map);
                _degerler["42"] = 14.1 + (r.NextDouble() - 0.5) * 0.12;
            }
            SonGuncelleme = DateTime.Now;
            Bildir();
            try { await Task.Delay(250, ct); } catch { break; }
        }
    }

    // ================= GERÇEK BAĞLANTI =================

    public async Task<bool> BaglanAsync(string adres, string ad)
    {
        await DurdurAsync();
        Durum = ObdDurum.Baglaniyor;
        CihazAdi = ad;
        Hata = null;
        EcuYanitVerdi = false;
        Desteklenen.Clear();
        Bildir();
#if ANDROID
        try
        {
            Log($"Bağlanılıyor: {ad} ({adres})");
            _baglanti = new BluetoothObd();
            await _baglanti.AcAsync(adres);
            Log("Bluetooth bağlantısı kuruldu.");

            AdaptorSurumu = Temiz(await Komut("ATZ", 4000)).LastOrDefault();
            Log($"ATZ → {AdaptorSurumu ?? "yanıt yok"}");
            foreach (var k in new[] { "ATE0", "ATL0", "ATS0", "ATH0", "ATSP0" })
                Log($"{k} → {string.Join(" ", Temiz(await Komut(k, 2000)))}");

            Log("ECU aranıyor (0100)… Bu işlem 10 saniye sürebilir.");
            var yanit = await Komut("0100", 15000);
            var bitler = Veri(yanit, "0100");
            if (bitler == null || bitler.Length < 4)
            {
                Hata = "ECU yanıt vermedi. Kontak açık mı? Adaptör OBD soketine tam oturdu mu?";
                Log($"0100 → {string.Join(" ", Temiz(yanit))}");
                await _baglanti.KapatAsync();
                Durum = ObdDurum.Kapali;
                Bildir();
                return false;
            }
            EcuYanitVerdi = true;
            DestekleriEkle(bitler, 0x00);
            if ((bitler[3] & 1) == 1)
            {
                var b2 = Veri(await Komut("0120", 4000), "0120");
                if (b2 is { Length: >= 4 })
                {
                    DestekleriEkle(b2, 0x20);
                    if ((b2[3] & 1) == 1)
                    {
                        var b3 = Veri(await Komut("0140", 4000), "0140");
                        if (b3 is { Length: >= 4 }) DestekleriEkle(b3, 0x40);
                    }
                }
            }
            Protokol = Temiz(await Komut("ATDP", 2000)).FirstOrDefault() ?? "Bilinmiyor";
            var no = Temiz(await Komut("ATDPN", 2000)).FirstOrDefault() ?? "0";
            _protokolNo = no.Length > 0 ? no[^1] : '0';
            Log($"Protokol: {Protokol}");
            Log($"Desteklenen PID sayısı: {Desteklenen.Count}");

            Durum = ObdDurum.Bagli;
            SonGuncelleme = DateTime.Now;
            _dongu = new CancellationTokenSource();
            _ = OkumaDongusu(_dongu.Token);
            Bildir();
            return true;
        }
        catch (Exception ex)
        {
            Hata = "Bağlanılamadı: " + ex.Message;
            Log(Hata);
            try { if (_baglanti != null) await _baglanti.KapatAsync(); } catch { }
            Durum = ObdDurum.Kapali;
            Bildir();
            return false;
        }
#else
        Hata = "Gerçek OBD bağlantısı yalnızca Android'de desteklenir.";
        Durum = ObdDurum.Kapali;
        Bildir();
        return await Task.FromResult(false);
#endif
    }

    private void DestekleriEkle(byte[] b, int taban)
    {
        for (var i = 0; i < 32; i++)
            if ((b[i / 8] & (0x80 >> (i % 8))) != 0)
                Desteklenen.Add((taban + i + 1).ToString("X2"));
    }

    private async Task OkumaDongusu(CancellationToken ct)
    {
        var tur = 0;
        var sorgu = Pidler.Hepsi.Where(p => Desteklenen.Contains(p.Kod)).ToList();
        var hataSayisi = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                foreach (var p in sorgu)
                {
                    if (ct.IsCancellationRequested) break;
                    var b = Veri(await Komut("01" + p.Kod, 1500), "01" + p.Kod);
                    if (b != null && b.Length >= (p.Kod is "0C" or "42" ? 2 : 1))
                        lock (_degerler) _degerler[p.Kod] = p.Coz(b);
                }
                if (!Desteklenen.Contains("42") && tur % 5 == 0)
                {
                    var v = Temiz(await Komut("ATRV", 1500)).FirstOrDefault()?.TrimEnd('V', 'v');
                    if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var volt))
                        lock (_degerler) _degerler["ATRV"] = volt;
                }
                tur++;
                hataSayisi = 0;
                SonGuncelleme = DateTime.Now;
                Bildir();
            }
            catch (Exception ex)
            {
                if (++hataSayisi >= 3)
                {
                    Hata = "Bağlantı koptu: " + ex.Message;
                    Log(Hata);
                    await DurdurAsync();
                    return;
                }
            }
            try { await Task.Delay(120, ct); } catch { break; }
        }
    }

    // ================= ARIZA KODLARI (Mod 03, salt okunur) =================

    public async Task<(List<string> Kodlar, bool? Mil, string? Hata)> ArizaOkuAsync()
    {
        if (Durum == ObdDurum.Demo)
        {
            await Task.Delay(1800);
            Log("DEMO arıza taraması: örnek kodlar üretildi.");
            return (new List<string> { "P0401", "P2463" }, true, null);
        }
        if (Durum != ObdDurum.Bagli) return (new(), null, "Önce OBD adaptörüne bağlanın veya demo modunu açın.");

        try
        {
            bool? mil = null;
            var durum = Veri(await Komut("0101", 3000), "0101");
            if (durum is { Length: >= 1 }) mil = (durum[0] & 0x80) != 0;

            var ham = await Komut("03", 6000);
            Log($"03 → {string.Join(" ", Temiz(ham))}");
            return (KodlariCoz(ham), mil, null);
        }
        catch (Exception ex) { return (new(), null, "Okuma hatası: " + ex.Message); }
    }

    private List<string> KodlariCoz(string ham)
    {
        var kodlar = new List<string>();
        var satirlar = Temiz(ham).Where(s => !s.Contains("NODATA")).ToList();
        var can = _protokolNo is >= '6' and <= '9' or 'A' or 'B' or 'C';
        var hexler = new List<string>();

        if (can)
        {
            // Çok çerçeveli yanıt: "00A" uzunluk satırı ve "0:" "1:" önekleri atılır
            var birlesik = string.Concat(satirlar.Where(s => s.Length > 3).Select(s => Regex.Replace(s, "^[0-9A-F]:", "")));
            var i = birlesik.IndexOf("43", StringComparison.Ordinal);
            if (i < 0) return kodlar;
            var govde = birlesik[(i + 2)..];
            if (govde.Length >= 2) govde = govde[2..]; // kod sayısı baytı
            hexler.Add(govde);
        }
        else
        {
            foreach (var s in satirlar.Where(s => s.StartsWith("43")))
                hexler.Add(s[2..]);
        }

        foreach (var h in hexler)
            for (var i = 0; i + 4 <= h.Length; i += 4)
            {
                var parca = h.Substring(i, 4);
                if (parca == "0000" || !Regex.IsMatch(parca, "^[0-9A-F]{4}$")) continue;
                var b0 = Convert.ToByte(parca[..2], 16);
                var harf = "PCBU"[b0 >> 6];
                var kod = $"{harf}{(b0 >> 4) & 3}{b0 & 0xF:X}{parca[2..]}";
                if (!kodlar.Contains(kod)) kodlar.Add(kod);
            }
        return kodlar;
    }

    // ================= Ortak =================

    public async Task DurdurAsync()
    {
        _dongu?.Cancel();
        _dongu = null;
        if (_baglanti != null)
        {
            try { await _baglanti.KapatAsync(); } catch { }
            _baglanti = null;
            Log("Bağlantı kapatıldı.");
        }
        else if (Durum == ObdDurum.Demo) Log("DEMO modu kapatıldı.");
        Durum = ObdDurum.Kapali;
        lock (_degerler) _degerler.Clear();
        Bildir();
    }

    private async Task<string> Komut(string komut, int ms)
    {
        if (_baglanti == null) throw new InvalidOperationException("Bağlantı yok.");
        await _hat.WaitAsync();
        try { return await _baglanti.KomutAsync(komut, ms); }
        finally { _hat.Release(); }
    }

    private static List<string> Temiz(string yanit) =>
        yanit.Replace("SEARCHING...", "").Replace(">", "")
             .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
             .Select(s => Regex.Replace(s, "\\s", "").ToUpperInvariant())
             .Where(s => s.Length > 0)
             .ToList();

    // "010C" isteğine gelen "410C1AF8" yanıtından veri baytlarını çıkarır
    private static byte[]? Veri(string yanit, string istek)
    {
        var onek = "4" + istek[1..];
        foreach (var s in Temiz(yanit))
        {
            var satir = Regex.Replace(s, "^[0-9A-F]:", "");
            var i = satir.IndexOf(onek, StringComparison.Ordinal);
            if (i < 0) continue;
            var hex = satir[(i + onek.Length)..];
            if (hex.Length < 2 || !Regex.IsMatch(hex, "^[0-9A-F]+$")) continue;
            var b = new byte[hex.Length / 2];
            for (var k = 0; k < b.Length; k++) b[k] = Convert.ToByte(hex.Substring(k * 2, 2), 16);
            return b;
        }
        return null;
    }
}
