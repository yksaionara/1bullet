# -*- mode: python ; coding: utf-8 -*-
from pathlib import Path

root = Path(SPECPATH)
datas = [
    (str(root / "backend"), "backend"),
    (str(root / "assets"), "assets"),
    (str(root / "VERSION"), "."),
    (str(root / "runtime.json"), "."),
    (str(root / "cli.py"), "."),
]

hiddenimports = [
    "app", "agents", "discord_presence", "encounter_log", "history", "inventory",
    "instalock_worker", "live_match", "match_meta", "offline_launch", "party_detector",
    "pick_advisor", "riot_client", "scout_commands", "scoutlog", "session_tracker",
    "startup", "sync", "valapi", "vconstants", "ws_server",
]

a = Analysis([str(root / "run.py")], pathex=[str(root), str(root / "backend")],
             binaries=[], datas=datas, hiddenimports=hiddenimports, hookspath=[],
             hooksconfig={}, runtime_hooks=[], excludes=[], noarchive=False)
pyz = PYZ(a.pure)
exe = EXE(pyz, a.scripts, a.binaries, a.zipfiles, a.datas, [], name="1bullet",
          debug=False, bootloader_ignore_signals=False, strip=False, upx=False,
          console=False, icon=str(root / "assets" / "1bullet.ico"))
