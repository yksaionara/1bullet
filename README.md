# 1 Bullet

Made by Saif.

1 Bullet is a Windows VALORANT companion that reads the local Riot Client API to show live match and lobby information: player ranks, RR, parties, encounters, agents, player cards, and equipped Vandal and Phantom skins when Riot exposes loadout data.

## Features

- Live local-client scoreboard with rank, party, encounter, agent, and loadout data.
- Empty, waiting state when VALORANT is closed or no live match is available. Production builds never generate demo players or skins.
- Local settings for dashboard accent color, custom PNG/JPG/JPEG/WEBP background, and Windows startup.
- Local-only Riot authentication. Lockfile credentials and authorization headers are never sent to project services.
- Optional Discord Rich Presence and safe-by-default agent-select actions.

## Install and Update

Download the latest installer from the [1bullet releases page](https://github.com/yksaionara/1bullet/releases) — it is built as `1 Bullet Setup.exe` and listed there as `1.Bullet.Setup.exe` (GitHub shows dots instead of spaces). The installed application is `1bullet.exe`.

Source ZIP releases are named `1bullet-v<version>.zip`. `start.bat` checks `yksaionara/1bullet` GitHub Releases before launch and applies a newer ZIP release transactionally. `UPDATE.bat` runs the same updater on demand.

The installed `1bullet.exe` opens a bundled local dashboard (`/dashboard`) with the live scoreboard, Vandal/Phantom skins, theme, background, and Start-with-Windows settings. When a newer release is published to this repository, the app shows an update prompt and downloads `1 Bullet Setup.exe` from these releases.

## Build From Source

```powershell
./install.bat
./start.bat
./scripts/build-release.ps1 -Version (Get-Content VERSION).Trim() -Output ./dist
```

The release scripts require Windows x64 and the pinned Python runtime described in `runtime.json`.

## Troubleshooting

- Start the Riot Client and VALORANT, then wait until the game reaches the menus.
- If the dashboard is empty during agent select, retry after the local client has finished loading.
- Run `install.bat` again to repair the source-runtime environment. Logs are stored in `%LOCALAPPDATA%\1Bullet`.

## License and Attribution

1 Bullet is a modified version of [Valorant Scout](https://github.com/kryotrades/Valorant-Scout) by kryotrades. It is distributed under the GNU General Public License v3.0; see `LICENSE` and `NOTICE`.

1 Bullet is not affiliated with, endorsed by, or sponsored by Riot Games. Client automation may violate Riot's Terms of Service and is used at your own risk.
