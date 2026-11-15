using System.Diagnostics;
using System.Text;

namespace AutoCoreX.Services;

public record PidTanim(string Kod, string Ad, string Kisa, string Birim, double Min, double Max, int Ondalik, Func<byte[], double> Coz);

// Standart OBD-II Mod 01 PID'leri (SAE J1979)
public static class Pidler
{
    public static readonly PidTanim Rpm = new("0C", "Motor devri", "RPM", "rpm", 0, 6000, 0, b => (b[0] * 256 + b[1]) / 4.0);
    public static readonly PidTanim Hiz = new("0D", "Araç hızı", "HIZ", "km/s", 0, 220, 0, b => b[0]);
    public static readonly PidTanim Sogutma = new("05", "Soğutma suyu sıcaklığı", "MOTOR", "°C", -40, 130, 0, b => b[0] - 40);
    public static readonly PidTanim Yuk = new("04", "Hesaplanan motor yükü", "YÜK", "%", 0, 100, 0, b => b[0] * 100.0 / 255);
    public static readonly PidTanim Gaz = new("11", "Gaz kelebeği konumu", "GAZ", "%", 0, 100, 0, b => b[0] * 100.0 / 255);
    public static readonly PidTanim EmmeSicaklik = new("0F", "Emme havası sıcaklığı", "EMME", "°C", -40, 80, 0, b => b[0] - 40);
    public static readonly PidTanim Manifold = new("0B", "Emme manifoldu basıncı", "MAP", "kPa", 0, 255, 0, b => b[0]);
    public static readonly PidTanim EcuVoltaj = new("42", "Kontrol modülü voltajı", "ECU V", "V", 9, 16, 1, b => (b[0] * 256 + b[1]) / 1000.0);

    public static readonly PidTanim[] Hepsi = { Rpm, Hiz, Sogutma, Yuk, Gaz, EmmeSicaklik, Manifold, EcuVoltaj };
    public static PidTanim? Bul(string kod) => Hepsi.FirstOrDefault(p => p.Kod == kod);
}

public interface IObdBaglanti
{
    bool Acik { get; }
    Task AcAsync(string adres);
    Task<string> KomutAsync(string komut, int zamanAsimiMs);
    Task KapatAsync();
}

public record EslesmisCihaz(string Ad, string Adres);

#if ANDROID
// ELM327 klasik Bluetooth (SPP / RFCOMM) bağlantısı
public class BluetoothObd : IObdBaglanti
{
    private static readonly Java.Util.UUID Spp = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB")!;
    private Android.Bluetooth.BluetoothSocket? _soket;
    private Stream? _giris, _cikis;

    public bool Acik => _soket?.IsConnected == true;

    private static Android.Bluetooth.BluetoothAdapter? Adaptor()
    {
        var yonetici = Android.App.Application.Context.GetSystemService(Android.Content.Context.BluetoothService) as Android.Bluetooth.BluetoothManager;
        return yonetici?.Adapter;
    }

    public static string? Durum()
    {
        var a = Adaptor();
        if (a == null) return "Bu telefonda Bluetooth bulunamadı.";
        if (!a.IsEnabled) return "Bluetooth kapalı. Telefon ayarlarından açın.";
        return null;
    }

    public static List<EslesmisCihaz> Eslesmisler()
    {
        var a = Adaptor();
        if (a?.BondedDevices == null) return new();
        return a.BondedDevices
            .Select(c => new EslesmisCihaz(string.IsNullOrWhiteSpace(c.Name) ? "Adsız cihaz" : c.Name!, c.Address ?? ""))
            .Where(c => c.Adres != "")
            .OrderByDescending(c => c.Ad.Contains("OBD", StringComparison.OrdinalIgnoreCase) || c.Ad.Contains("ELM", StringComparison.OrdinalIgnoreCase) || c.Ad.Contains("V-LINK", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task AcAsync(string adres)
    {
        var a = Adaptor() ?? throw new InvalidOperationException("Bluetooth bulunamadı.");
        var cihaz = a.GetRemoteDevice(adres) ?? throw new InvalidOperationException("Cihaz bulunamadı.");
        try { a.CancelDiscovery(); } catch { }

        try
        {
            _soket = cihaz.CreateRfcommSocketToServiceRecord(Spp);
            await Task.Run(() => _soket!.Connect());
        }
        catch
        {
            // Bazı klon adaptörler güvenli olmayan soketle bağlanır
            try { _soket?.Close(); } catch { }
            _soket = cihaz.CreateInsecureRfcommSocketToServiceRecord(Spp);
            await Task.Run(() => _soket!.Connect());
        }
        _giris = _soket!.InputStream;
        _cikis = _soket.OutputStream;
    }

    public Task<string> KomutAsync(string komut, int zamanAsimiMs) => Task.Run(async () =>
    {
        if (_giris == null || _cikis == null) throw new InvalidOperationException("Bağlantı açık değil.");
        var java = (_giris as Android.Runtime.InputStreamInvoker)?.BaseInputStream;
        var tampon = new byte[512];

        // önceki komuttan kalan veriyi temizle
        while (java != null && java.Available() > 0) _giris.Read(tampon, 0, Math.Min(tampon.Length, java.Available()));

        var veri = Encoding.ASCII.GetBytes(komut + "\r");
        _cikis.Write(veri, 0, veri.Length);
        _cikis.Flush();

        var sb = new StringBuilder();
        var sure = Stopwatch.StartNew();
        while (sure.ElapsedMilliseconds < zamanAsimiMs)
        {
            var mevcut = java?.Available() ?? 1;
            if (mevcut > 0)
            {
                var n = _giris.Read(tampon, 0, Math.Min(tampon.Length, mevcut));
                if (n > 0) sb.Append(Encoding.ASCII.GetString(tampon, 0, n));
                if (sb.ToString().Contains('>')) break;
            }
            else await Task.Delay(15);
        }
        return sb.ToString();
    });

    public Task KapatAsync()
    {
        try { _giris?.Dispose(); } catch { }
        try { _cikis?.Dispose(); } catch { }
        try { _soket?.Close(); } catch { }
        _soket = null; _giris = null; _cikis = null;
        return Task.CompletedTask;
    }
}
#endif
