using Microsoft.JSInterop;

namespace AutoCoreX.Services;

// Ayarlar telefonda (Preferences) saklanır
public class Ayarlar
{
    public bool Ses { get => Preferences.Default.Get("ses", true); set => Preferences.Default.Set("ses", value); }
    public bool Titresim { get => Preferences.Default.Get("titresim", true); set => Preferences.Default.Set("titresim", value); }
    public bool AcilisAnimasyonu { get => Preferences.Default.Get("acilis", true); set => Preferences.Default.Set("acilis", value); }
    public string? SonObdAdres { get => Preferences.Default.Get<string?>("obdAdres", null); set => Preferences.Default.Set("obdAdres", value); }
}

// Ses + titreşim efektleri. Sesler dosya değil, Web Audio ile anında üretilir (APK hafif kalır).
public class Efekt
{
    private readonly IJSRuntime _js;
    private readonly Ayarlar _ayar;
    public Efekt(IJSRuntime js, Ayarlar ayar) { _js = js; _ayar = ayar; }

    public async Task Ses(string ad, int titresimMs = 0)
    {
        if (titresimMs > 0) Titret(titresimMs);
        if (!_ayar.Ses) return;
        try { await _js.InvokeVoidAsync("acx.ses", ad); } catch { }
    }

    public void Titret(int ms = 30)
    {
        if (!_ayar.Titresim) return;
        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms)); } catch { }
    }

    public async Task Esitle()
    {
        try { await _js.InvokeVoidAsync("acx.ayarla", _ayar.Ses, _ayar.Titresim); } catch { }
    }
}

public class Cihaz
{
    private readonly Dictionary<string, string> _onbellek = new();

    // Galeriden fotoğraf seç → küçült → uygulamanın özel klasörüne kaydet
    public async Task<string?> FotoSecAsync()
    {
        try
        {
            var secilen = await MediaPicker.Default.PickPhotoAsync();
            if (secilen == null) return null;
            var klasor = Path.Combine(FileSystem.AppDataDirectory, "gorseller");
            Directory.CreateDirectory(klasor);
            var gecici = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}.tmp");
            await using (var kaynak = await secilen.OpenReadAsync())
            await using (var yaz = File.Create(gecici))
                await kaynak.CopyToAsync(yaz);

            var hedef = Path.Combine(klasor, $"{Guid.NewGuid():N}.jpg");
            if (!Kucult(gecici, hedef, 1100)) File.Copy(gecici, hedef, true);
            try { File.Delete(gecici); } catch { }
            return hedef;
        }
        catch { return null; }
    }

    private static bool Kucult(string kaynak, string hedef, int maks)
    {
#if ANDROID
        try
        {
            var sinir = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
            Android.Graphics.BitmapFactory.DecodeFile(kaynak, sinir);
            if (sinir.OutWidth <= 0) return false;
            var oran = 1;
            while (sinir.OutWidth / oran > maks * 2 || sinir.OutHeight / oran > maks * 2) oran *= 2;
            using var bmp = Android.Graphics.BitmapFactory.DecodeFile(kaynak, new Android.Graphics.BitmapFactory.Options { InSampleSize = oran });
            if (bmp == null) return false;
            var k = Math.Min(1.0, (double)maks / Math.Max(bmp.Width, bmp.Height));
            using var son = k < 1 ? Android.Graphics.Bitmap.CreateScaledBitmap(bmp, (int)(bmp.Width * k), (int)(bmp.Height * k), true) : null;
            using var fs = File.Create(hedef);
            (son ?? bmp).Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg!, 82, fs);
            return true;
        }
        catch { return false; }
#else
        return false;
#endif
    }

    public async Task<string?> GorselAsync(string? yol)
    {
        if (string.IsNullOrEmpty(yol)) return null;
        if (_onbellek.TryGetValue(yol, out var v)) return v;
        if (!File.Exists(yol)) return null;
        v = "data:image/jpeg;base64," + Convert.ToBase64String(await File.ReadAllBytesAsync(yol));
        _onbellek[yol] = v;
        return v;
    }

    public static void DosyaSil(string? yol) { try { if (yol != null && File.Exists(yol)) File.Delete(yol); } catch { } }
}

// Android 12+ için Bluetooth bağlantı izni
public class BluetoothIzni : Permissions.BasePlatformPermission
{
#if ANDROID
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        OperatingSystem.IsAndroidVersionAtLeast(31)
            ? new[] { ("android.permission.BLUETOOTH_CONNECT", true) }
            : new[] { ("android.permission.BLUETOOTH", false), ("android.permission.BLUETOOTH_ADMIN", false) };
#endif
}

// Android 13+ için bildirim izni
public class BildirimIzni : Permissions.BasePlatformPermission
{
#if ANDROID
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        OperatingSystem.IsAndroidVersionAtLeast(33)
            ? new[] { ("android.permission.POST_NOTIFICATIONS", true) }
            : Array.Empty<(string, bool)>();
#endif
}
