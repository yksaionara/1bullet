# 1 Bullet

[![CI](https://github.com/yksaionara/1bullet/actions/workflows/ci.yml/badge.svg)](https://github.com/yksaionara/1bullet/actions/workflows/ci.yml)
[![Latest release](https://img.shields.io/github/v/release/yksaionara/1bullet)](https://github.com/yksaionara/1bullet/releases/latest)
[![License: GPL-3.0](https://img.shields.io/badge/License-GPL--3.0-blue.svg)](LICENSE)

Made by Saif · developed with assistance from OpenAI ChatGPT and Codex.

1 Bullet is a native Windows VALORANT companion. It reads Riot's local client interfaces on your PC to show live lobby and match information such as ranks, RR, parties, encounters, agents, player cards, and equipped weapon skins when that data is available.

## Download

Use the [latest release](https://github.com/yksaionara/1bullet/releases/latest).

**Recommended:** install with `1.Bullet.Setup.exe`.

The installer includes both parts required by the desktop app:

- `1bullet.exe` — native .NET 8 / WPF desktop UI
- `1bullet-backend.exe` — local Python backend used for Riot/game data

A portable package is also published as `1bullet-portable-v<version>.zip`. Extract the whole ZIP before running `1bullet.exe`; the backend executable must remain next to it.

The raw desktop executable is intentionally not published by itself because it cannot function without the backend.

## Architecture

```
Native WPF desktop UI (C# / .NET 8)
        ↕ HTTP on 127.0.0.1 only
Local Python backend
        ↕ Riot lockfile + local client APIs
VALORANT / Riot Client
```

The desktop UI does not use WebView2, Electron, Chromium, Tauri, or an embedded browser engine.

## Features

- Native Windows desktop UI.
- Live local-client scoreboard and match state.
- Rank, RR, party, encounter, agent, and player-card information when available.
- Equipped Vandal and Phantom skins when Riot exposes loadout data.
- Competitive match history and profile details.
- Custom accent colors and local background images.
- Optional Start with Windows.
- Update checks against this repository only.
- Empty/waiting states instead of production demo players.

## Privacy and network behavior

- Riot lockfile passwords and local authorization headers stay local.
- The backend binds to `127.0.0.1`.
- Custom background images remain on your PC.
- Release/update checks go to `yksaionara/1bullet` on GitHub.
- Riot and asset endpoints may still be contacted when required for live data and artwork.

## Updates

Installed builds check:

`https://api.github.com/repos/yksaionara/1bullet/releases/latest`

When a newer version exists, 1 Bullet offers the installer from this repository's release assets.

Release artifacts include SHA-256 checksums in `SHA256SUMS.txt`.

## Build from source

Requirements:

- Windows x64
- .NET 8 SDK
- CPython 3.12.10
- Inno Setup for installer builds

Backend/source environment:

```powershell
./install.bat
./start.bat
```

Native desktop UI:

```powershell
dotnet restore wpf/OneBullet.csproj
dotnet build wpf/OneBullet.csproj -c Release
```

Release validation:

```powershell
./scripts/verify-version.ps1
python -m compileall -q backend run.py cli.py
./.venv/Scripts/python.exe scripts/import_smoke.py
```

See `docs/RELEASE_CHECKLIST.md` before creating a public tag.

## Troubleshooting

If VALORANT data does not appear:

1. Start Riot Client and VALORANT.
2. Wait until the client finishes loading.
3. Enter the lobby, Agent Select, or a match.
4. If the backend fails, use the app's diagnostics/log controls or inspect `%LOCALAPPDATA%\1Bullet\backend-console.log`.

If a portable build says the backend is missing, make sure both `1bullet.exe` and `1bullet-backend.exe` were extracted into the same folder.

## License and credits

1 Bullet is distributed under the GNU General Public License v3.0. Required upstream licensing and provenance notices are retained in `NOTICE`.

Development assistance: OpenAI ChatGPT and Codex.

1 Bullet is not affiliated with, endorsed by, or sponsored by Riot Games. Features that automate client actions may be subject to Riot's terms and policies.
