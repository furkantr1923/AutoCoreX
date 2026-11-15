using System.Text.Json;
using AutoCoreX.Models;

namespace AutoCoreX.Services;

// GPS yolculuk kaydı. Yalnızca kullanıcı "Başlat" dediğinde ve uygulama açıkken konum alınır.
public class GpsServisi
{
    private readonly Veri _veri;
    public GpsServisi(Veri veri) => _veri = veri;

    public bool Aktif { get; private set; }
    public DateTime Baslangic { get; private set; }
    public double MesafeKm { get; private set; }
    public double AnlikHiz { get; private set; }
    public double MaksHiz { get; private set; }
    public double? Dogruluk { get; private set; }
    public int NoktaSayisi => _noktalar.Count;
    public TimeSpan Sure => Aktif ? DateTime.Now - Baslangic : TimeSpan.Zero;
    public double OrtHiz => Sure.TotalHours > 0.0005 ? MesafeKm / Sure.TotalHours : 0;

    public event Action? Degisti;

    private readonly List<double[]> _noktalar = new();
    private Location? _son;
    private int _aracId;

    public async Task<string?> BaslatAsync(int aracId)
    {
        if (Aktif) return null;
        var izin = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (izin != PermissionStatus.Granted) izin = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (izin != PermissionStatus.Granted) return "Konum izni verilmedi. Yolculuk kaydı için konum izni gerekli.";

        _aracId = aracId;
        _noktalar.Clear();
        _son = null;
        MesafeKm = 0; MaksHiz = 0; AnlikHiz = 0; Dogruluk = null;
        Baslangic = DateTime.Now;
        try
        {
            Geolocation.Default.LocationChanged += KonumGeldi;
            var ok = await Geolocation.Default.StartListeningForegroundAsync(new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(2)));
            if (!ok) { Geolocation.Default.LocationChanged -= KonumGeldi; return "GPS başlatılamadı. Konum servisi açık mı?"; }
        }
        catch (Exception ex)
        {
            Geolocation.Default.LocationChanged -= KonumGeldi;
            return "GPS başlatılamadı: " + ex.Message;
        }
        Aktif = true;
        try { DeviceDisplay.Current.KeepScreenOn = true; } catch { }
        Degisti?.Invoke();
        return null;
    }

    private void KonumGeldi(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var k = e.Location;
        Dogruluk = k.Accuracy;
        if (k.Accuracy is > 40) { Degisti?.Invoke(); return; }   // zayıf sinyal: kaydetme

        if (_son != null)
        {
            var d = Location.CalculateDistance(_son, k, DistanceUnits.Kilometers);
            var sn = (k.Timestamp - _son.Timestamp).TotalSeconds;
            if (d < 0.004) { AnlikHiz = k.Speed.HasValue ? k.Speed.Value * 3.6 : 0; Degisti?.Invoke(); return; } // titreşim filtresi
            if (sn > 0 && d / (sn / 3600) > 250) { Degisti?.Invoke(); return; }  // GPS sıçraması
            MesafeKm += d;
            AnlikHiz = k.Speed.HasValue ? k.Speed.Value * 3.6 : (sn > 0 ? d / (sn / 3600) : 0);
        }
        MaksHiz = Math.Max(MaksHiz, AnlikHiz);
        _son = k;
        _noktalar.Add(new[] { Math.Round(k.Latitude, 6), Math.Round(k.Longitude, 6) });
        Degisti?.Invoke();
    }

    public async Task<Yolculuk?> DurdurAsync(bool kaydet = true)
    {
        if (!Aktif) return null;
        try { Geolocation.Default.StopListeningForeground(); } catch { }
        Geolocation.Default.LocationChanged -= KonumGeldi;
        try { DeviceDisplay.Current.KeepScreenOn = false; } catch { }
        Aktif = false;

        Yolculuk? y = null;
        if (kaydet && _noktalar.Count >= 2 && MesafeKm > 0.02)
        {
            var sure = DateTime.Now - Baslangic;
            // En fazla 600 nokta sakla
            var adim = Math.Max(1, _noktalar.Count / 600);
            var rota = _noktalar.Where((_, i) => i % adim == 0 || i == _noktalar.Count - 1).ToList();
            y = new Yolculuk
            {
                AracId = _aracId,
                Baslangic = Baslangic,
                Bitis = DateTime.Now,
                MesafeKm = Math.Round(MesafeKm, 2),
                MaksHiz = Math.Round(MaksHiz),
                OrtHiz = sure.TotalHours > 0 ? Math.Round(MesafeKm / sure.TotalHours, 1) : 0,
                RotaJson = JsonSerializer.Serialize(rota)
            };
            await _veri.YolculukKaydetAsync(y);
        }
        Degisti?.Invoke();
        return y;
    }

    public static List<double[]> Rota(Yolculuk y)
    {
        try { return JsonSerializer.Deserialize<List<double[]>>(y.RotaJson) ?? new(); } catch { return new(); }
    }
}
