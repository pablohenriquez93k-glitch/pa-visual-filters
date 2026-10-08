# Visual Filters

Client mod for **Planetary Annihilation: TITANS** (build 124683). Cosmetic and accessibility only.

Forum: https://github.com/pablohenriquez93k-glitch/pa-visual-filters/discussions/1

## What it does
Adds image filters to the 3D view, applied right after the game's tone map:

- **Color blindness correction** for protanopia (red-blind), deuteranopia (green-blind) and tritanopia (blue-blind), with a strength setting (25–100 %). It uses the "daltonize" method: it simulates the deficiency and moves the lost color information to the channels you can still see.
- **Saturation** (0–200 %), **contrast** (75–150 %), **brightness** (−20 to +20 %).
- **Sharpness** and **vignette** (off, light, medium, strong).

Everything is in the **VISUAL FILTERS** tab of Settings, with a reset button and three presets (Quick color blindness, High contrast, Cinematic) that fill the options for you. A preset resets every option it does not set to its default; Quick color blindness keeps your correction type, or picks Deuteranopia if correction is off. With the default values the mod does nothing and the game is unchanged.

## Install
Easiest: install **Visual Filters** from the in-game **Community Mods** index. Manual install: close the game and copy the contents of the downloaded package into `%LOCALAPPDATA%\Uber Entertainment\Planetary Annihilation\client_mods\com.pa.pabloandclaude.visualfilters\` so that `modinfo.json` sits directly inside that folder.

1. Open the game and enable **Visual Filters** in the Mods manager (client mod).
2. Settings → **VISUAL FILTERS**, pick your options and press **Save**.
3. **Restart the game.** The filters apply on the next launch.

To remove it, disable it in the Mods manager (or close the game and delete that folder).

## Limits
- **Changes need a game restart.** The engine compiles this image pass once per launch; the mod builds it from your settings when the game starts. Tested: reloading the view, remounting files and changing graphics options do not recompile it.
- **HDR must be on** (Settings → GRAPHICS → HDR). With HDR off the game skips this image pass and the filters do nothing. The tab shows a warning in that case.
- The user interface (menus, HUD) is not filtered. Planets, units and effects in the 3D view are.

## Compatibility
The mod replaces the game shader `shaders/post_hdr_compose.fs` in memory, only when a filter is on. Other mods that replace the same file lose to it while a filter is on. Mods that change other post-process files (for example `post.json`) keep working as long as they still use `post_hdr_compose.fs` for the final pass. Re-check after game patches.

## Translations
The tab is translated into 23 locales. **The translations are automatic and may contain errors.**

## Credits
- Color blindness correction: daltonize method by Onur Fidaner, Poliang Lin and Nevran Ozguven (2005).
- The shader is the game's own `post_hdr_compose.fs` (Uber Entertainment) with the filters added after the tone map.
- Mod by **Pablo & Claude**.

## License
MIT (see `LICENSE`) for the mod's own code. The original game shader code belongs to its owners.
