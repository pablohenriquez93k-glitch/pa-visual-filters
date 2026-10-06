// Visual Filters: reads the settings and mounts the filtered post_hdr_compose.fs in memory.
// The engine compiles that shader once per game launch, so changes apply after a restart.
var VisualFilters = (function () {
    var G = 'com.pa.pabloandclaude.visualfilters';
    var DEF = { dalton: 'off', dalton_k: '100', sat: '100', con: '100', bri: '0', vig: 'off', sharp: 'off' };
    var DALTON = { off: 0, protan: 1, deutan: 2, tritan: 3 };
    var VIG = { off: 0, light: 0.25, medium: 0.45, strong: 0.65 };
    var SHARP = { off: 0, light: 0.15, medium: 0.3, strong: 0.5 };
    function num(v, def, min, max) { v = Number(v); return isFinite(v) ? Math.max(min, Math.min(max, v)) : def; }
    function leer() {
        var d = {};
        try { if (api.settings.loadLocalData && !api.settings.data) { api.settings.loadLocalData(); } d = (api.settings.data && api.settings.data[G]) || {}; } catch (e) { d = {}; }
        var c = {};
        Object.keys(DEF).forEach(function (k) { c[k] = (d[k] === undefined || d[k] === null || d[k] === '') ? DEF[k] : String(d[k]); });
        return {
            dalton: DALTON.hasOwnProperty(c.dalton) ? DALTON[c.dalton] : 0,
            daltonK: num(c.dalton_k, 100, 0, 100) / 100,
            sat: num(c.sat, 100, 0, 200) / 100,
            con: num(c.con, 100, 50, 200) / 100,
            bri: num(c.bri, 0, -50, 50) / 100,
            vig: VIG.hasOwnProperty(c.vig) ? VIG[c.vig] : 0,
            sharp: SHARP.hasOwnProperty(c.sharp) ? SHARP[c.sharp] : 0
        };
    }
    function neutro(p) { return !p.dalton && p.sat === 1 && p.con === 1 && p.bri === 0 && !p.vig && !p.sharp; }
    function f(x) { return x.toFixed(4); }
    function firma(p) { return neutro(p) ? 'vanilla' : JSON.stringify(p); }
    function shader(p) {
        var defs = [
            '#define VF_DALTON ' + p.dalton, '#define VF_DALTON_K ' + f(p.daltonK),
            '#define VF_SAT ' + f(p.sat), '#define VF_CON ' + f(p.con), '#define VF_BRI ' + f(p.bri),
            '#define VF_VIG ' + f(p.vig), '#define VF_VIG_ON ' + (p.vig > 0 ? 1 : 0),
            '#define VF_SHARP ' + f(p.sharp), '#define VF_SHARP_ON ' + (p.sharp > 0 ? 1 : 0)
        ];
        return VisualFiltersPlantilla.replace('//VF_PARAMS\n', defs.join('\n') + '\n');
    }
    // Start scene only: the first mount before the first match is the one the engine uses.
    function montar() {
        var p = leer(), s = firma(p);
        try {
            if (!neutro(p)) { api.file.mountMemoryFiles({ '/shaders/post_hdr_compose.fs': shader(p) }); }
            if (!sessionStorage.getItem(G + '.montado')) { sessionStorage.setItem(G + '.montado', s); }
        } catch (e) { console.error('[Visual Filters] mount failed', e); }
        return s;
    }
    function montado() { try { return sessionStorage.getItem(G + '.montado'); } catch (e) { return null; } }
    return { G: G, DEF: DEF, leer: leer, firma: firma, shader: shader, montar: montar, montado: montado, neutro: neutro };
})();
