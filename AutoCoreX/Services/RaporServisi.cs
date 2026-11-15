using AutoCoreX.Models;

namespace AutoCoreX.Services;

public class RaporVerisi
{
    public Arac Arac { get; set; } = new();
    public SaglikSonucu Saglik { get; set; } = new();
    public TaramaKaydi? SonTarama { get; set; }
    public List<BakimKaydi> Bakimlar { get; set; } = new();
    public List<BakimDurumSatiri> Durumlar { get; set; } = new();
    public YakitOzeti Yakit { get; set; } = new();
    public string ObdDurumu { get; set; } = "";
    public string Kaynak { get; set; } = "";
    public List<(string Ad, string Deger)> Sensorler { get; set; } = new();
}

// Android'in yerleşik PdfDocument API'si ile PDF üretir (ek paket yok)
public class RaporServisi
{
    public async Task<string?> PdfOlusturAsync(RaporVerisi v)
    {
#if ANDROID
        var yol = Path.Combine(FileSystem.CacheDirectory, $"AutoCoreX_Saglik_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        await Task.Run(() => Yaz(v, yol));
        return yol;
#else
        return await Task.FromResult<string?>(null);
#endif
    }

    public async Task PaylasAsync(string yol) =>
        await Share.Default.RequestAsync(new ShareFileRequest { Title = "Araç sağlık raporu", File = new ShareFile(yol, "application/pdf") });

#if ANDROID
    private const int G = 595, Y = 842, Kenar = 40;

    private sealed class Kalem : IDisposable
    {
        private readonly Android.Graphics.Pdf.PdfDocument _doc = new();
        private Android.Graphics.Pdf.PdfDocument.Page? _sayfa;
        private int _no;
        public Android.Graphics.Canvas C = null!;
        public float Yy;

        public Kalem() => YeniSayfa();

        public void YeniSayfa()
        {
            if (_sayfa != null) { Alt(); _doc.FinishPage(_sayfa); }
            _no++;
            var bilgi = new Android.Graphics.Pdf.PdfDocument.PageInfo.Builder(G, Y, _no).Create();
            _sayfa = _doc.StartPage(bilgi);
            C = _sayfa!.Canvas!;
            Yy = Kenar;
        }

        private void Alt()
        {
            var p = Boya("#7C8A96", 8);
            C.DrawText("AutoCore X · Bilgilendirme amaçlıdır, kesin teşhis yerine geçmez. · Sayfa " + _no, Kenar, Y - 20, p);
        }

        public void Gerekirse(float yukseklik) { if (Yy + yukseklik > Y - 50) YeniSayfa(); }

        public static Android.Graphics.Paint Boya(string renk, float boyut, bool kalin = false)
        {
            var p = new Android.Graphics.Paint { AntiAlias = true, TextSize = boyut, Color = Android.Graphics.Color.ParseColor(renk) };
            if (kalin) p.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
            return p;
        }

        public void Metin(string metin, float boyut = 10, string renk = "#1B2430", bool kalin = false, float girinti = 0)
        {
            var p = Boya(renk, boyut, kalin);
            var genislik = G - Kenar * 2 - girinti;
            foreach (var satir in Sar(metin, p, genislik))
            {
                Gerekirse(boyut + 5);
                Yy += boyut + 3;
                C.DrawText(satir, Kenar + girinti, Yy, p);
            }
            Yy += 2;
        }

        public void Baslik(string metin)
        {
            Gerekirse(40);
            Yy += 14;
            var serit = Boya("#00A8C0", 1);
            C.DrawRect(Kenar, Yy + 2, Kenar + 4, Yy + 18, serit);
            var p = Boya("#0B1016", 13, true);
            C.DrawText(metin.ToUpperInvariant(), Kenar + 12, Yy + 15, p);
            Yy += 24;
        }

        public void Satir(string sol, string sag, string? renk = null)
        {
            Gerekirse(16);
            Yy += 14;
            C.DrawText(sol, Kenar + 8, Yy, Boya("#4A5663", 10));
            var p = Boya(renk ?? "#0B1016", 10, true);
            var w = p.MeasureText(sag);
            C.DrawText(sag, G - Kenar - w, Yy, p);
            var cizgi = Boya("#E3E8EC", 1);
            C.DrawRect(Kenar + 8, Yy + 4, G - Kenar, Yy + 4.6f, cizgi);
        }

        private static IEnumerable<string> Sar(string metin, Android.Graphics.Paint p, float genislik)
        {
            foreach (var paragraf in metin.Split('\n'))
            {
                var satir = "";
                foreach (var kelime in paragraf.Split(' '))
                {
                    var aday = satir.Length == 0 ? kelime : satir + " " + kelime;
                    if (p.MeasureText(aday) > genislik && satir.Length > 0) { yield return satir; satir = kelime; }
                    else satir = aday;
                }
                yield return satir;
            }
        }

        public void Kaydet(string yol)
        {
            if (_sayfa != null) { Alt(); _doc.FinishPage(_sayfa); _sayfa = null; }
            using var fs = File.Create(yol);
            _doc.WriteTo(fs);
        }

        public void Dispose() => _doc.Close();
    }

    private static void Yaz(RaporVerisi v, string yol)
    {
        using var k = new Kalem();

        // Üst bant
        k.C.DrawRect(0, 0, G, 92, Kalem.Boya("#05070A", 1));
        k.C.DrawRect(0, 90, G, 92, Kalem.Boya("#00E5FF", 1));
        k.C.DrawText("AUTOCORE X", Kenar, 42, Kalem.Boya("#00E5FF", 22, true));
        k.C.DrawText("Araç Sağlık Raporu", Kenar, 64, Kalem.Boya("#E6F1F5", 12));
        k.C.DrawText(DateTime.Now.ToString("dd.MM.yyyy HH:mm"), G - Kenar - 90, 42, Kalem.Boya("#7C8A96", 10));
        k.Yy = 100;

        k.Baslik("Araç");
        k.Satir("Araç", v.Arac.TamAd);
        k.Satir("Model yılı / motor", v.Arac.Ozet);
        k.Satir("Kilometre", S.Km(v.Arac.Km));
        if (!string.IsNullOrWhiteSpace(v.Arac.Plaka)) k.Satir("Plaka", v.Arac.Plaka!);

        k.Baslik("Sağlık durumu");
        k.Satir("Sağlık skoru", v.Saglik.Skor.HasValue ? $"{v.Saglik.Skor}/100 · {v.Saglik.Seviye}" : "Veri yetersiz",
            v.Saglik.Skor switch { null => "#7C8A96", >= 85 => "#118A4E", >= 65 => "#B7791F", _ => "#C5283D" });
        foreach (var e in v.Saglik.Etkenler) k.Satir("  • " + e.Metin, e.Puan == 0 ? "—" : e.Puan.ToString());

        k.Baslik("OBD-II bağlantısı");
        k.Satir("Bağlantı durumu", v.ObdDurumu);
        k.Satir("Veri kaynağı", v.Kaynak, v.Kaynak.StartsWith("DEMO") ? "#B7791F" : null);
        if (v.Sensorler.Count == 0) k.Metin("Anlık sensör verisi yok (adaptör bağlı değil).", 9, "#7C8A96");
        foreach (var s in v.Sensorler) k.Satir(s.Ad, s.Deger);

        k.Baslik("Arıza kodları");
        if (v.SonTarama == null) k.Metin("Henüz arıza taraması yapılmadı.", 9, "#7C8A96");
        else
        {
            k.Satir("Son tarama", $"{v.SonTarama.Zaman:dd.MM.yyyy HH:mm} · {(v.SonTarama.Kaynak == "Demo" ? "DEMO (gerçek değil)" : "Gerçek araç")}",
                v.SonTarama.Kaynak == "Demo" ? "#B7791F" : null);
            if (v.SonTarama.MilAcik.HasValue) k.Satir("Motor arıza lambası (MIL)", v.SonTarama.MilAcik.Value ? "AÇIK" : "Kapalı");
            if (v.SonTarama.KodListesi.Count == 0) k.Metin("Kayıtlı arıza kodu bulunamadı.", 10, "#118A4E");
            foreach (var kod in v.SonTarama.KodListesi)
            {
                var b = DtcSozluk.Bul(kod);
                k.Metin($"{b.Kod} — {b.Aciklama}", 10, "#0B1016", true, 8);
                k.Metin($"Sistem: {b.Sistem} · Ciddiyet: {b.CiddiyetAdi}", 9, "#4A5663", false, 8);
                k.Metin("Olası nedenler: " + string.Join(", ", b.Nedenler), 9, "#4A5663", false, 8);
            }
        }

        k.Baslik("Yaklaşan bakımlar");
        if (v.Durumlar.Count == 0) k.Metin("Bakım planı yok.", 9, "#7C8A96");
        foreach (var d in v.Durumlar)
            k.Satir(d.Tur, d.Metin, d.Durum switch { BakimDurumu.Gecikti => "#C5283D", BakimDurumu.Yaklasiyor => "#B7791F", _ => null });

        k.Baslik("Bakım geçmişi (son 12)");
        if (v.Bakimlar.Count == 0) k.Metin("Bakım kaydı yok.", 9, "#7C8A96");
        foreach (var b in v.Bakimlar.Take(12))
            k.Satir($"{b.Tarih:dd.MM.yyyy} · {b.Tur}", $"{S.Km(b.Km)} · {S.Para(b.Maliyet)}");

        k.Baslik("Yakıt");
        k.Satir("Ortalama tüketim", v.Yakit.OrtTuketim.HasValue ? $"{v.Yakit.OrtTuketim:0.0} L/100 km" : "Hesaplanamadı");
        k.Satir("100 km maliyeti", v.Yakit.Maliyet100.HasValue ? S.Para(v.Yakit.Maliyet100.Value) : "Hesaplanamadı");
        k.Satir("Bu yıl yakıt gideri", S.Para(v.Yakit.BuYil));
        k.Satir("Toplam yakıt gideri", S.Para(v.Yakit.ToplamHarcama));
        if (v.Yakit.Not != null) k.Metin(v.Yakit.Not, 9, "#7C8A96");

        if (!string.IsNullOrWhiteSpace(v.Arac.Notlar))
        {
            k.Baslik("Notlar");
            k.Metin(v.Arac.Notlar!);
        }

        k.Baslik("Yasal bilgilendirme");
        k.Metin("Bu rapor bilgilendirme amaçlıdır ve kesin teşhis yerine geçmez. DEMO etiketli veriler simülasyondur, gerçek araç verisi değildir. Arıza durumunda yetkili/uzman servise başvurun.", 9, "#4A5663");

        k.Kaydet(yol);
    }
#endif
}

// Uygulama açıldığında geciken/yaklaşan bakımları Android bildirimiyle hatırlatır
public static class Bildirim
{
    public static async Task BakimHatirlat(List<BakimDurumSatiri> durumlar, string aracAdi)
    {
#if ANDROID
        var onemli = durumlar.Where(d => d.Durum is BakimDurumu.Gecikti or BakimDurumu.Yaklasiyor).ToList();
        if (onemli.Count == 0) return;
        var anahtar = DateTime.Today.ToString("yyyyMMdd");
        if (Preferences.Default.Get("sonBildirim", "") == anahtar) return;   // günde bir kez
        if (await Permissions.CheckStatusAsync<BildirimIzni>() != PermissionStatus.Granted) return;

        try
        {
            var ctx = Android.App.Application.Context;
            if (ctx.GetSystemService(Android.Content.Context.NotificationService) is not Android.App.NotificationManager nm) return;
            var baslik = $"{aracAdi}: {onemli.Count} bakım dikkat istiyor";
            var metin = string.Join(" · ", onemli.Take(3).Select(d => $"{d.Tur} ({d.Metin})"));

            var acilis = ctx.PackageManager?.GetLaunchIntentForPackage(ctx.PackageName!);
            var bekleyen = acilis == null ? null : Android.App.PendingIntent.GetActivity(ctx, 0, acilis, Android.App.PendingIntentFlags.Immutable);

            Android.App.Notification bildirim;
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                nm.CreateNotificationChannel(new Android.App.NotificationChannel("bakim", "Bakım hatırlatmaları", Android.App.NotificationImportance.Default));
                bildirim = new Android.App.Notification.Builder(ctx, "bakim")
                    .SetContentTitle(baslik).SetContentText(metin).SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                    .SetStyle(new Android.App.Notification.BigTextStyle().BigText(metin)).SetContentIntent(bekleyen).SetAutoCancel(true).Build();
            }
            else
            {
#pragma warning disable CA1422
                bildirim = new Android.App.Notification.Builder(ctx)
                    .SetContentTitle(baslik).SetContentText(metin).SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                    .SetContentIntent(bekleyen).SetAutoCancel(true).Build();
#pragma warning restore CA1422
            }
            nm.Notify(1001, bildirim);
            Preferences.Default.Set("sonBildirim", anahtar);
        }
        catch { }
#else
        await Task.CompletedTask;
#endif
    }
}
