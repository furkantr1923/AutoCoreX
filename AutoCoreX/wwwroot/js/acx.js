// AutoCore X — ses sentezi (Web Audio), dokunma dalgası ve küçük yardımcılar
window.acx = (function () {
    let ctx = null, sesAcik = true;

    function c() {
        if (!ctx) {
            const AC = window.AudioContext || window.webkitAudioContext;
            if (!AC) return null;
            ctx = new AC();
        }
        if (ctx.state === 'suspended') ctx.resume();
        return ctx;
    }

    // tek ton: frekans, süre, dalga, ses, gecikme, bitiş frekansı
    function ton(f, sure, tip, ses, gecikme, f2) {
        const a = c(); if (!a) return;
        const t = a.currentTime + (gecikme || 0);
        const o = a.createOscillator(), g = a.createGain();
        o.type = tip || 'sine';
        o.frequency.setValueAtTime(f, t);
        if (f2) o.frequency.exponentialRampToValueAtTime(f2, t + sure);
        g.gain.setValueAtTime(0.0001, t);
        g.gain.exponentialRampToValueAtTime(ses || 0.12, t + 0.012);
        g.gain.exponentialRampToValueAtTime(0.0001, t + sure);
        o.connect(g).connect(a.destination);
        o.start(t); o.stop(t + sure + 0.05);
    }

    // filtreli gürültü (hışırtı / türbin)
    function gurultu(sure, ses, gecikme, f1, f2) {
        const a = c(); if (!a) return;
        const t = a.currentTime + (gecikme || 0);
        const n = Math.floor(a.sampleRate * sure);
        const b = a.createBuffer(1, n, a.sampleRate), d = b.getChannelData(0);
        for (let i = 0; i < n; i++) d[i] = Math.random() * 2 - 1;
        const kaynak = a.createBufferSource(); kaynak.buffer = b;
        const f = a.createBiquadFilter(); f.type = 'bandpass'; f.Q.value = 1.4;
        f.frequency.setValueAtTime(f1, t); f.frequency.exponentialRampToValueAtTime(f2, t + sure);
        const g = a.createGain();
        g.gain.setValueAtTime(0.0001, t);
        g.gain.exponentialRampToValueAtTime(ses, t + sure * 0.3);
        g.gain.exponentialRampToValueAtTime(0.0001, t + sure);
        kaynak.connect(f).connect(g).connect(a.destination);
        kaynak.start(t); kaynak.stop(t + sure);
    }

    const sesler = {
        tik: () => ton(1800, 0.035, 'square', 0.025),
        gecis: () => gurultu(0.22, 0.05, 0, 600, 2600),
        onay: () => { ton(880, 0.09, 'sine', 0.1); ton(1320, 0.16, 'sine', 0.1, 0.08); },
        sil: () => { ton(420, 0.12, 'triangle', 0.1); ton(260, 0.18, 'triangle', 0.1, 0.09); },
        uyari: () => { ton(740, 0.12, 'square', 0.05); ton(740, 0.12, 'square', 0.05, 0.18); },
        hata: () => ton(220, 0.35, 'sawtooth', 0.06, 0, 120),
        baglan: () => { ton(523, 0.08, 'sine', 0.09); ton(784, 0.08, 'sine', 0.09, 0.08); ton(1046, 0.22, 'sine', 0.1, 0.16); },
        kopma: () => { ton(1046, 0.08, 'sine', 0.08); ton(523, 0.25, 'sine', 0.08, 0.08); },
        tarama: () => { for (let i = 0; i < 6; i++) ton(1200 + i * 120, 0.05, 'sine', 0.04, i * 0.22); },
        baslat: () => { ton(60, 0.9, 'sawtooth', 0.07, 0, 140); gurultu(1.1, 0.06, 0.05, 300, 4200); ton(880, 0.1, 'sine', 0.08, 0.9); ton(1760, 0.25, 'sine', 0.08, 1.0); },
        kayit: () => { ton(660, 0.07, 'sine', 0.09); ton(990, 0.12, 'sine', 0.09, 0.07); },
        kontak: () => {
            // marş: türbin yükselişi + gösterge kontrol bipleri
            ton(45, 1.6, 'sawtooth', 0.09, 0, 95);
            ton(90, 1.6, 'square', 0.025, 0.1, 160);
            gurultu(1.7, 0.07, 0, 200, 5200);
            [0, 1, 2].forEach(i => ton(1500, 0.07, 'sine', 0.07, 1.25 + i * 0.13));
            ton(2000, 0.35, 'sine', 0.08, 1.7);
        }
    };

    // Butonlara dokununca dalga efekti + tık sesi
    document.addEventListener('pointerdown', e => {
        const hedef = e.target.closest('button, a, .dokun, label.anahtar');
        c();
        if (!hedef || hedef.disabled) return;
        if (sesAcik && !hedef.classList.contains('sessiz')) sesler.tik();
        const r = hedef.getBoundingClientRect();
        const d = document.createElement('span');
        const boy = Math.max(r.width, r.height) * 2;
        d.className = 'dalga';
        d.style.width = d.style.height = boy + 'px';
        d.style.left = (e.clientX - r.left - boy / 2) + 'px';
        d.style.top = (e.clientY - r.top - boy / 2) + 'px';
        hedef.appendChild(d);
        setTimeout(() => d.remove(), 650);
    }, { passive: true });

    return {
        ses: ad => { if (sesAcik && sesler[ad]) sesler[ad](); },
        ayarla: (s) => { sesAcik = !!s; },
        yukari: () => { const s = document.querySelector('.sahne'); if (s) s.scrollTo({ top: 0, behavior: 'smooth' }); }
    };
})();
