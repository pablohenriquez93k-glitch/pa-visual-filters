// Scene settings: VISUAL FILTERS tab.
(function () {
    var G = VisualFilters.G, DEF = VisualFilters.DEF;
    if (window.VisualFiltersIdiomas) { VisualFiltersIdiomas(); }
    var L = function (t) { return '!LOC:' + t; };
    function pcts(v) { return v.map(function (n) { return n + '%'; }); }
    function signo(v) { return v.map(function (n) { return (Number(n) > 0 ? '+' : '') + n + '%'; }); }
    var nivel = ['off', 'light', 'medium', 'strong'], nivelTxt = ['Off', 'Light', 'Medium', 'Strong'].map(L);
    var sat = ['0', '25', '50', '75', '90', '100', '110', '125', '150', '175', '200'];
    var con = ['75', '85', '90', '100', '110', '120', '135', '150'];
    var bri = ['-20', '-10', '-5', '0', '5', '10', '20'];
    var dk = ['25', '50', '75', '100'];
    var defs = {
        title: L('VISUAL FILTERS'),
        local_only: true,
        settings: {
            dalton: { title: L('Color blindness correction'), type: 'select', options: ['off', 'protan', 'deutan', 'tritan'],
                optionsText: ['Off', 'Protanopia (red-blind)', 'Deuteranopia (green-blind)', 'Tritanopia (blue-blind)'].map(L), default: DEF.dalton },
            dalton_k: { title: L('Correction strength'), type: 'select', options: dk, optionsText: pcts(dk), default: DEF.dalton_k },
            sat: { title: L('Saturation'), type: 'select', options: sat, optionsText: pcts(sat), default: DEF.sat },
            con: { title: L('Contrast'), type: 'select', options: con, optionsText: pcts(con), default: DEF.con },
            bri: { title: L('Brightness'), type: 'select', options: bri, optionsText: signo(bri), default: DEF.bri },
            sharp: { title: L('Sharpness'), type: 'select', options: nivel, optionsText: nivelTxt, default: DEF.sharp },
            vig: { title: L('Vignette'), type: 'select', options: nivel, optionsText: nivelTxt, default: DEF.vig }
        }
    };
    api.settings.definitions[G] = defs;
    // Presets: defaults + these values. Color blindness type is kept (deuteranopia if off, for the quick one).
    var PRESETS = [
        { id: 'dalton', nombre: 'Quick color blindness', v: { dalton_k: '100', sat: '110' } },
        { id: 'contraste', nombre: 'High contrast', v: { con: '120', sat: '125', sharp: 'medium' } },
        { id: 'cine', nombre: 'Cinematic', v: { con: '110', vig: 'medium' } }
    ];
    function opt(k) { return '<div class="option" data-bind="template: { name: \'setting-template\', data: $root.settingsItemMap()[\'' + G + '.' + k + '\'] }"></div>'; }
    function grupo(titulo, claves, extra) {
        return '<div class="form-group"><div class="sub-group-title" data-bind="text: loc(\'!LOC:' + titulo + '\')"></div><div class="sub-group top">' + claves.map(opt).join('') + (extra || '') + '</div></div>';
    }
    function txt(t) { return '<div class="option" style="padding:6px 0;font-style:italic" data-bind="text: loc(\'!LOC:' + t + '\')"></div>'; }
    var html =
        '<div class="option-list visualfilters" style="max-height:100%;overflow-y:auto" data-bind="visible: ($root.settingGroups().indexOf(\'' + G + '\') === $root.activeSettingsGroupIndex())">' +
        txt('Changes apply after restarting Planetary Annihilation. The user interface is not filtered.') +
        '<div class="option vf-hdr" style="padding:6px 0;color:#ffc84a;display:none" data-bind="text: loc(\'!LOC:Filters need HDR on (GRAPHICS tab). With HDR off the game skips this image pass.\')"></div>' +
        '<div class="option vf-pendiente" style="padding:6px 0;color:#ffc84a;display:none" data-bind="text: loc(\'!LOC:Restart the game to apply the saved filters.\')"></div>' +
        grupo('PRESETS', [], '<div class="option" style="display:flex;flex-wrap:nowrap">' + PRESETS.map(function (q) {
            return '<button class="btn vf-preset" data-preset="' + q.id + '" style="margin:0 6px 4px 0;min-width:0;flex:1 1 0;white-space:normal" data-bind="text: loc(\'!LOC:' + q.nombre + '\')"></button>';
        }).join('') + '</div>' + txt('A preset fills the options below; press Save and restart.')) +
        grupo('COLOR BLINDNESS', ['dalton', 'dalton_k']) +
        grupo('IMAGE', ['sat', 'con', 'bri', 'sharp', 'vig']) +
        grupo('RESET', [], '<div class="option"><button class="btn vf-reset" data-bind="text: loc(\'!LOC:Reset to defaults\')"></button></div>' +
            txt('Translations are automatic and may contain errors.')) +
        '</div>';
    if ($('.container_settings').length) { $('.container_settings').append(html); } else { $(function () { $('.container_settings').append(html); }); }
    if (window.model && model.settingDefinitions) { model.settingDefinitions(api.settings.definitions); model.settingDefinitions.valueHasMutated(); }

    // Saved settings differ from the ones mounted at launch -> show the restart notice.
    function aviso() {
        var m = VisualFilters.montado(), s = VisualFilters.firma(VisualFilters.leer());
        $('.vf-pendiente').toggle(!!m && m !== s);
        var hdr = 'ON';
        try { var map = model.settingsItemMap(); hdr = String(map['graphics.hdr'] ? map['graphics.hdr'].value() : api.settings.value('graphics', 'hdr')); } catch (e) {}
        $('.vf-hdr').toggle(hdr === 'OFF');
    }
    function engancharse() {
        var map = window.model && model.settingsItemMap && model.settingsItemMap();
        if (!map || !$('.vf-reset').length || !Object.keys(defs.settings).every(function (k) { return map[G + '.' + k]; })) { return false; }
        $('.vf-reset').off('click').on('click', function () {
            Object.keys(defs.settings).forEach(function (k) { map[G + '.' + k].value(defs.settings[k].default); });
        });
        $('.vf-preset').off('click').on('click', function () {
            var q = PRESETS.filter(function (x) { return x.id === $(this).attr('data-preset'); }, this)[0];
            if (!q) { return; }
            var tipo = map[G + '.dalton'].value();
            if (q.id === 'dalton' && tipo === 'off') { tipo = 'deutan'; }
            Object.keys(defs.settings).forEach(function (k) {
                map[G + '.' + k].value(q.v.hasOwnProperty(k) ? q.v[k] : (k === 'dalton' ? tipo : defs.settings[k].default));
            });
        });
        if (map['graphics.hdr'] && map['graphics.hdr'].value.subscribe) { map['graphics.hdr'].value.subscribe(aviso); }
        aviso(); return true;
    }
    var intentos = 0, timer = setInterval(function () { if (engancharse()) { clearInterval(timer); } else if (++intentos > 100) { clearInterval(timer); console.warn('Visual Filters: settings not found; presets and reset inactive'); } }, 300);
    var s0 = api.settings.save;
    api.settings.save = function () { var r = s0.apply(this, arguments); try { aviso(); } catch (e) {} return r; };
})();
