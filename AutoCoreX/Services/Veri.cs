using SQLite;
using AutoCoreX.Models;

namespace AutoCoreX.Services;

// Tüm kayıtlar telefonda (SQLite). İnternet ve hesap gerekmez.
public class Veri
{
    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _kilit = new(1, 1);

    public event Action? Degisti;
    private void Bildir() => Degisti?.Invoke();

    private async Task<SQLiteAsyncConnection> Db()
    {
        if (_db != null) return _db;
        await _kilit.WaitAsync();
        try
        {
            if (_db == null)
            {
                var b = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "autocorex.db3"));
                await b.CreateTablesAsync(CreateFlags.None, typeof(Arac), typeof(BakimKaydi), typeof(YakitKaydi), typeof(Yolculuk), typeof(TaramaKaydi));
                _db = b;
            }
            return _db;
        }
        finally { _kilit.Release(); }
    }

    public Task HazirlaAsync() => Db();

    // ---------- Araçlar ----------

    public int AktifAracId
    {
        get => Preferences.Default.Get("aktifArac", 0);
        set { Preferences.Default.Set("aktifArac", value); Bildir(); }
    }

    public async Task<List<Arac>> AraclarAsync() => await (await Db()).Table<Arac>().OrderBy(a => a.Id).ToListAsync();

    public async Task<Arac?> AracAsync(int id) => await (await Db()).Table<Arac>().Where(a => a.Id == id).FirstOrDefaultAsync();

    public async Task<Arac?> AktifAracAsync()
    {
        var a = await AracAsync(AktifAracId);
        if (a != null) return a;
        a = (await AraclarAsync()).FirstOrDefault();
        if (a != null) Preferences.Default.Set("aktifArac", a.Id);
        return a;
    }

    public async Task AracKaydetAsync(Arac a)
    {
        var db = await Db();
        if (a.Id == 0)
        {
            await db.InsertAsync(a);
            if (await db.Table<Arac>().CountAsync() == 1 || AktifAracId == 0) Preferences.Default.Set("aktifArac", a.Id);
        }
        else await db.UpdateAsync(a);
        Bildir();
    }

    public async Task AracSilAsync(int id)
    {
        var db = await Db();
        var a = await AracAsync(id);
        if (a?.FotoYolu != null) DosyaSil(a.FotoYolu);
        foreach (var b in await BakimlarAsync(id)) if (b.FotoYolu != null) DosyaSil(b.FotoYolu);
        await db.ExecuteAsync("DELETE FROM BakimKaydi WHERE AracId = ?", id);
        await db.ExecuteAsync("DELETE FROM YakitKaydi WHERE AracId = ?", id);
        await db.ExecuteAsync("DELETE FROM Yolculuk WHERE AracId = ?", id);
        await db.ExecuteAsync("DELETE FROM TaramaKaydi WHERE AracId = ?", id);
        await db.DeleteAsync<Arac>(id);
        SecureStorage.Default.Remove($"sasi_{id}");
        if (AktifAracId == id) Preferences.Default.Set("aktifArac", 0);
        Bildir();
    }

    public async Task KmGuncelleAsync(int aracId, int km)
    {
        var a = await AracAsync(aracId);
        if (a != null && km > a.Km) { a.Km = km; await (await Db()).UpdateAsync(a); }
    }

    // Şasi numarası veritabanına değil, Android Keystore korumalı SecureStorage'a yazılır
    public async Task SasiKaydetAsync(int aracId, string? sasi)
    {
        if (string.IsNullOrWhiteSpace(sasi)) SecureStorage.Default.Remove($"sasi_{aracId}");
        else await SecureStorage.Default.SetAsync($"sasi_{aracId}", sasi.Trim().ToUpperInvariant());
    }

    public async Task<string?> SasiAsync(int aracId)
    {
        try { return await SecureStorage.Default.GetAsync($"sasi_{aracId}"); } catch { return null; }
    }

    // ---------- Bakım ----------

    public async Task<List<BakimKaydi>> BakimlarAsync(int aracId) =>
        await (await Db()).Table<BakimKaydi>().Where(b => b.AracId == aracId).OrderByDescending(b => b.Tarih).ToListAsync();

    public async Task<BakimKaydi?> BakimAsync(int id) => await (await Db()).Table<BakimKaydi>().Where(b => b.Id == id).FirstOrDefaultAsync();

    public async Task BakimKaydetAsync(BakimKaydi b)
    {
        var db = await Db();
        if (b.Id == 0) await db.InsertAsync(b); else await db.UpdateAsync(b);
        await KmGuncelleAsync(b.AracId, b.Km);
        Bildir();
    }

    public async Task BakimSilAsync(int id)
    {
        var b = await BakimAsync(id);
        if (b?.FotoYolu != null) DosyaSil(b.FotoYolu);
        await (await Db()).DeleteAsync<BakimKaydi>(id);
        Bildir();
    }

    // ---------- Yakıt ----------

    public async Task<List<YakitKaydi>> YakitlarAsync(int aracId) =>
        await (await Db()).Table<YakitKaydi>().Where(y => y.AracId == aracId).OrderByDescending(y => y.Tarih).ToListAsync();

    public async Task<YakitKaydi?> YakitAsync(int id) => await (await Db()).Table<YakitKaydi>().Where(y => y.Id == id).FirstOrDefaultAsync();

    public async Task YakitKaydetAsync(YakitKaydi y)
    {
        var db = await Db();
        if (y.Id == 0) await db.InsertAsync(y); else await db.UpdateAsync(y);
        await KmGuncelleAsync(y.AracId, y.Km);
        Bildir();
    }

    public async Task YakitSilAsync(int id) { await (await Db()).DeleteAsync<YakitKaydi>(id); Bildir(); }

    // ---------- Yolculuk ----------

    public async Task<List<Yolculuk>> YolculuklarAsync(int aracId) =>
        await (await Db()).Table<Yolculuk>().Where(y => y.AracId == aracId).OrderByDescending(y => y.Baslangic).ToListAsync();

    public async Task<Yolculuk?> YolculukAsync(int id) => await (await Db()).Table<Yolculuk>().Where(y => y.Id == id).FirstOrDefaultAsync();

    public async Task YolculukKaydetAsync(Yolculuk y) { await (await Db()).InsertAsync(y); Bildir(); }

    public async Task YolculukSilAsync(int id) { await (await Db()).DeleteAsync<Yolculuk>(id); Bildir(); }

    // ---------- Arıza taramaları ----------

    public async Task<List<TaramaKaydi>> TaramalarAsync(int aracId) =>
        await (await Db()).Table<TaramaKaydi>().Where(t => t.AracId == aracId).OrderByDescending(t => t.Zaman).ToListAsync();

    public async Task TaramaKaydetAsync(TaramaKaydi t) { await (await Db()).InsertAsync(t); Bildir(); }

    // ---------- Sıfırlama ----------

    public async Task TumunuSilAsync()
    {
        var db = await Db();
        foreach (var a in await AraclarAsync()) SecureStorage.Default.Remove($"sasi_{a.Id}");
        await db.DeleteAllAsync<BakimKaydi>();
        await db.DeleteAllAsync<YakitKaydi>();
        await db.DeleteAllAsync<Yolculuk>();
        await db.DeleteAllAsync<TaramaKaydi>();
        await db.DeleteAllAsync<Arac>();
        var klasor = Path.Combine(FileSystem.AppDataDirectory, "gorseller");
        try { if (Directory.Exists(klasor)) Directory.Delete(klasor, true); } catch { }
        Preferences.Default.Set("aktifArac", 0);
        Bildir();
    }

    private static void DosyaSil(string yol) { try { if (File.Exists(yol)) File.Delete(yol); } catch { } }
}
