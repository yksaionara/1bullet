# 1 Bullet release checklist

Use this checklist before creating a public release tag.

## Product checks

- [ ] The public version is clean semantic versioning only (for example `v2.2.6`), with no commit SHA/build metadata shown in the main UI.
- [ ] Normal UI contains no debug counters, raw backend output, placeholder text, or developer-only labels.
- [ ] Diagnostics and logs are available but not visually dominant during normal use.
- [ ] VALORANT closed state is clean and does not generate fake/demo players.
- [ ] Lobby state works.
- [ ] Agent Select works with real players and agents.
- [ ] Live match state works.
- [ ] Hidden/incognito names do not break the UI.
- [ ] Ranks/RR load correctly.
- [ ] Party detection works.
- [ ] Vandal and Phantom skins fail gracefully when unavailable.
- [ ] Profile and match-history views open without errors.
- [ ] Custom theme persists.
- [ ] Custom background persists and can be removed.
- [ ] Start with Windows can be enabled and disabled without duplicates.
- [ ] Update check points only to `yksaionara/1bullet`.
- [ ] Previous installed version detects the new release correctly.

## Match-action checks

- [ ] Dodge is a main-screen action, not buried in Settings.
- [ ] Dodge is one-click/instant and does not show a confirmation or warning dialog.
- [ ] Dodge failure is reported non-blockingly and does not crash the app.
- [ ] Instalock behavior matches the selected settings and does not display stale state.
- [ ] Team-side information is hidden or disabled when it is unavailable.
- [ ] Presence/status controls use clear user-facing labels.

## Repository checks

- [ ] `VERSION` and `runtime.json` match.
- [ ] No secrets, lockfile passwords, authorization headers, local tokens, or personal absolute paths are committed.
- [ ] `LICENSE` and `NOTICE` are present.
- [ ] README download/update instructions match the actual release assets.
- [ ] No functional updater links point to the upstream Valorant-Scout repository.
- [ ] No GitHub Pages/hosted-dashboard dependency is required for normal operation.

## Local preflight

Run from the repository root:

```powershell
./scripts/verify-version.ps1
python -m compileall -q backend run.py cli.py
./.venv/Scripts/python.exe scripts/import_smoke.py
dotnet restore wpf/OneBullet.csproj
dotnet build wpf/OneBullet.csproj -c Release
```

Then perform a real VALORANT smoke test before tagging.

## Tagging

Only create the tag after every required check above passes:

```powershell
git tag v<version>
git push origin v<version>
```

The release workflow is tag-only. It builds and verifies the source package, backend, WPF desktop app, portable package, installer, and SHA-256 checksums before publishing the release.

## Expected public assets

- `1.Bullet.Setup.exe`
- `1bullet-portable-v<version>.zip`
- `1bullet-v<version>.zip`
- `SHA256SUMS.txt`

A raw standalone `1bullet.exe` should not be published because it requires `1bullet-backend.exe` beside it.

## After release

- [ ] Release is not a draft.
- [ ] All expected assets exist and are non-empty.
- [ ] `releases/latest` returns the new tag.
- [ ] Installer launches and installs per-user without requiring elevation.
- [ ] Installed app launches with no console/browser window.
- [ ] Previous version's update prompt opens the new installer asset.
- [ ] Uninstall removes the app and its Start-with-Windows entry.
