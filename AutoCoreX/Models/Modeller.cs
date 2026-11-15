using System.Globalization;
using SQLite;

namespace AutoCoreX.Models;

public class Arac
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Marka { get; set; } = "";
    public string Model { get; set; } = "";
    public int Yil { get; set; } = DateTime.Today.Year;
    public string MotorTipi { get; set; } = "";
    public string YakitTipi { get; set; } = "Dizel";
    public string Sanziman { get; set; } = "Manuel";
    public int MotorHacmi { get; set; }          // cc
    public int Km { get; set; }
    public string? Plaka { get; set; }
    public bool SasiKayitli { get; set; }        // şasi no SecureStorage'da şifreli tutulur
    public string? FotoYolu { get; set; }
    public DateTime? SatinAlma { get; set; }
    public string? Notlar { get; set; }
    public DateTime Olusturma { get; set; } = DateTime.Now;

    [Ignore] public string TamAd => $"{Marka} {Model}".Trim();
    [Ignore] public string Ozet => $"{Yil} · {(MotorHacmi > 0 ? (MotorHacmi / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " " : "")}{MotorTipi} · {YakitTipi} · {Sanziman}";
}

public class BakimKaydi
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int AracId { get; set; }
    public string Tur { get; set; } = "Motor yağı";
    public DateTime Tarih { get; set; } = DateTime.Today;
    public int Km { get; set; }
    public string? Aciklama { get; set; }
    public string? Servis { get; set; }
    public double Maliyet { get; set; }
    public string? FotoYolu { get; set; }
    public int? SonrakiKm { get; set; }
    public DateTime? SonrakiTarih { get; set; }
}

public class YakitKaydi
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int AracId { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Today;
    public int Km { get; set; }
    public double Litre { get; set; }
    public double LitreFiyat { get; set; }
    public double Toplam { get; set; }
    public string YakitTuru { get; set; } = "Dizel";
    public string? Istasyon { get; set; }
    public string? Not { get; set; }
    public bool DepoDolu { get; set; } = true;
}

public class Yolculuk
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int AracId { get; set; }
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public double MesafeKm { get; set; }
    public double OrtHiz { get; set; }
    public double MaksHiz { get; set; }
    public string RotaJson { get; set; } = "[]";   // [[enlem,boylam],...]

    [Ignore] public TimeSpan Sure => Bitis - Baslangic;
}

public class TaramaKaydi
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int AracId { get; set; }
    public DateTime Zaman { get; set; } = DateTime.Now;
    public string Kaynak { get; set; } = "Demo";     // "Demo" veya "Gerçek"
    public string Kodlar { get; set; } = "";         // virgülle
    public bool? MilAcik { get; set; }

    [Ignore] public List<string> KodListesi => Kodlar.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

public record BakimTuru(string Ad, string Ikon, int? AralikKm, int? AralikAy);

public enum BakimDurumu { Gecikti, Yaklasiyor, Iyi, Planyok }

public record BakimDurumSatiri(string Tur, BakimKaydi Son, BakimDurumu Durum, int? KalanKm, int? KalanGun, string Metin);

public record AylikTutar(string Etiket, double Tutar);

public class YakitOzeti
{
    public double? OrtTuketim { get; set; }      // L/100 km
    public double? Maliyet100 { get; set; }      // TL/100 km
    public double ToplamHarcama { get; set; }
    public double ToplamLitre { get; set; }
    public double BuAy { get; set; }
    public double BuYil { get; set; }
    public int HesaplananKm { get; set; }
    public string? Not { get; set; }
    public List<AylikTutar> Aylik { get; set; } = new();
}

public class SaglikSonucu
{
    public int? Skor { get; set; }
    public List<(string Metin, int Puan)> Etkenler { get; set; } = new();
    public string Seviye => Skor switch { null => "Veri yetersiz", >= 85 => "İyi", >= 65 => "Dikkat", _ => "Kritik" };
}

public class DtcBilgi
{
    public string Kod { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public string Sistem { get; set; } = "";
    public int Ciddiyet { get; set; }            // 1 düşük, 2 orta, 3 yüksek
    public string[] Nedenler { get; set; } = Array.Empty<string>();
    public string[] Kontroller { get; set; } = Array.Empty<string>();
    public bool Sozlukte { get; set; } = true;

    public string CiddiyetAdi => Ciddiyet switch { 3 => "Yüksek", 2 => "Orta", _ => "Düşük" };
    public string ServisOnerisi => Ciddiyet switch
    {
        3 => "Aracı zorlamadan en kısa sürede servise götürün. Motor uyarı lambası yanıp sönüyorsa sürüşü durdurun.",
        2 => "Yakın zamanda servise kontrol ettirin; arıza büyümeden çözülmesi önerilir.",
        _ => "Acil değil; bir sonraki bakımda kontrol ettirilebilir."
    };
}

public static class S
{
    // SVG / CSS içindeki sayılar her zaman nokta ile yazılmalı (tr-TR virgül kullanır)
    public static string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    public static string Para(double v) => v.ToString("#,0.00", new CultureInfo("tr-TR")) + " ₺";
    public static string Km(int v) => v.ToString("#,0", new CultureInfo("tr-TR")) + " km";
    public static string Sure(TimeSpan t) => t.TotalHours >= 1 ? $"{(int)t.TotalHours} sa {t.Minutes:00} dk" : $"{t.Minutes} dk {t.Seconds:00} sn";
}
