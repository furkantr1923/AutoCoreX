using System.Globalization;
using AutoCoreX.Models;

namespace AutoCoreX.Services;

// Yakıt, bakım ve sağlık hesapları. Tahmin yok: veri eksikse bunu açıkça söyler.
public static class Hesap
{
    public static readonly BakimTuru[] BakimTurleri =
    {
        new("Motor yağı", "yag", 10000, 12),
        new("Yağ filtresi", "filtre", 10000, 12),
        new("Hava filtresi", "filtre", 20000, 24),
        new("Polen filtresi", "filtre", 15000, 12),
        new("Yakıt filtresi", "yakit", 30000, 24),
        new("Fren balatası", "fren", 30000, null),
        new("Fren hidroliği", "fren", null, 24),
        new("Antifriz", "isi", null, 36),
        new("Triger kayışı", "motor", 120000, 60),
        new("Debriyaj", "motor", null, null),
        new("Lastikler", "lastik", 40000, 48),
        new("Akü", "aku", null, 48),
        new("Diğer", "anahtar", null, null),
    };

    public static BakimTuru Tur(string ad) => BakimTurleri.FirstOrDefault(t => t.Ad == ad) ?? BakimTurleri[^1];

    // ---------- Yakıt (depo-full yöntemi) ----------

    public static YakitOzeti YakitOzeti(List<YakitKaydi> kayitlar)
    {
        var o = new YakitOzeti
        {
            ToplamHarcama = kayitlar.Sum(k => k.Toplam),
            ToplamLitre = kayitlar.Sum(k => k.Litre),
            BuAy = kayitlar.Where(k => k.Tarih.Year == DateTime.Today.Year && k.Tarih.Month == DateTime.Today.Month).Sum(k => k.Toplam),
            BuYil = kayitlar.Where(k => k.Tarih.Year == DateTime.Today.Year).Sum(k => k.Toplam),
        };

        var tr = new CultureInfo("tr-TR");
        for (var i = 5; i >= 0; i--)
        {
            var ay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-i);
            o.Aylik.Add(new AylikTutar(ay.ToString("MMM", tr), kayitlar.Where(k => k.Tarih.Year == ay.Year && k.Tarih.Month == ay.Month).Sum(k => k.Toplam)));
        }

        var sirali = kayitlar.OrderBy(k => k.Km).ThenBy(k => k.Tarih).ToList();
        var fullIndeks = sirali.Select((k, i) => (k, i)).Where(x => x.k.DepoDolu).Select(x => x.i).ToList();
        if (fullIndeks.Count < 2)
        {
            o.Not = "Ortalama tüketim için en az 2 adet \"depo full\" kaydı gerekli.";
            return o;
        }

        double litre = 0, tutar = 0; int km = 0;
        for (var n = 1; n < fullIndeks.Count; n++)
        {
            int bas = fullIndeks[n - 1], son = fullIndeks[n];
            var fark = sirali[son].Km - sirali[bas].Km;
            if (fark <= 0) continue;
            for (var j = bas + 1; j <= son; j++) { litre += sirali[j].Litre; tutar += sirali[j].Toplam; }
            km += fark;
        }
        if (km <= 0) { o.Not = "Kilometre değerleri artan sırada olmadığı için tüketim hesaplanamadı."; return o; }
        o.HesaplananKm = km;
        o.OrtTuketim = litre / km * 100;
        o.Maliyet100 = tutar / km * 100;
        return o;
    }

    // ---------- Bakım durumu ----------

    public static List<BakimDurumSatiri> BakimDurumlari(List<BakimKaydi> kayitlar, Arac? arac)
    {
        var liste = new List<BakimDurumSatiri>();
        foreach (var grup in kayitlar.GroupBy(k => k.Tur))
        {
            var son = grup.OrderByDescending(k => k.Tarih).ThenByDescending(k => k.Km).First();
            int? kalanKm = son.SonrakiKm.HasValue && arac != null ? son.SonrakiKm.Value - arac.Km : null;
            int? kalanGun = son.SonrakiTarih.HasValue ? (int)(son.SonrakiTarih.Value.Date - DateTime.Today).TotalDays : null;

            BakimDurumu d;
            if (kalanKm == null && kalanGun == null) d = BakimDurumu.Planyok;
            else if (kalanKm < 0 || kalanGun < 0) d = BakimDurumu.Gecikti;
            else if (kalanKm <= 1000 || kalanGun <= 30) d = BakimDurumu.Yaklasiyor;
            else d = BakimDurumu.Iyi;

            var parca = new List<string>();
            if (kalanKm != null) parca.Add(kalanKm < 0 ? $"{S.Km(-kalanKm.Value)} geçti" : $"{S.Km(kalanKm.Value)} kaldı");
            if (kalanGun != null) parca.Add(kalanGun < 0 ? $"{-kalanGun} gün geçti" : $"{kalanGun} gün kaldı");
            liste.Add(new BakimDurumSatiri(grup.Key, son, d, kalanKm, kalanGun, parca.Count == 0 ? "Sonraki bakım planlanmadı" : string.Join(" · ", parca)));
        }
        return liste.OrderBy(x => x.Durum).ThenBy(x => x.KalanGun ?? int.MaxValue).ToList();
    }

    // ---------- Araç sağlık skoru ----------

    public static SaglikSonucu Saglik(List<BakimDurumSatiri> bakimlar, TaramaKaydi? sonTarama, ObdServisi obd)
    {
        var s = new SaglikSonucu();
        var veriVar = false;
        var puan = 100;

        if (sonTarama != null)
        {
            veriVar = true;
            var etiket = sonTarama.Kaynak == "Demo" ? " (demo tarama)" : "";
            foreach (var kod in sonTarama.KodListesi)
            {
                var b = DtcSozluk.Bul(kod);
                var eksi = b.Ciddiyet switch { 3 => 25, 2 => 12, _ => 5 };
                puan -= eksi;
                s.Etkenler.Add(($"Arıza kodu {kod}{etiket}", -eksi));
            }
            if (sonTarama.KodListesi.Count == 0) s.Etkenler.Add(($"Son taramada arıza kodu yok{etiket}", 0));
        }

        foreach (var b in bakimlar)
        {
            veriVar = true;
            if (b.Durum == BakimDurumu.Gecikti) { puan -= 15; s.Etkenler.Add(($"{b.Tur} gecikti", -15)); }
            else if (b.Durum == BakimDurumu.Yaklasiyor) { puan -= 5; s.Etkenler.Add(($"{b.Tur} yaklaşıyor", -5)); }
        }

        if (obd.Canli)
        {
            veriVar = true;
            var etiket = obd.Durum == ObdDurum.Demo ? " (demo)" : "";
            var sicaklik = obd.Deger("05");
            if (sicaklik > 105) { puan -= 15; s.Etkenler.Add(($"Motor sıcaklığı yüksek{etiket}", -15)); }
            var volt = obd.Voltaj();
            if (volt is > 0 and < 11.8) { puan -= 10; s.Etkenler.Add(($"Akü voltajı düşük{etiket}", -10)); }
        }

        s.Skor = veriVar ? Math.Clamp(puan, 0, 100) : null;
        return s;
    }
}
