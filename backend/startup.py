from __future__ import annotations

import os
import sys
import winreg

RUN_KEY = r"Software\Microsoft\Windows\CurrentVersion\Run"
VALUE_NAME = "1 Bullet"


def _command() -> str:
    executable = sys.executable if getattr(sys, "frozen", False) else os.path.abspath(sys.argv[0])
    return f'"{executable}"'


def enabled() -> bool:
    if os.name != "nt":
        return False
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, RUN_KEY) as key:
            value, _ = winreg.QueryValueEx(key, VALUE_NAME)
        return os.path.normcase(str(value)) == os.path.normcase(_command())
    except OSError:
        return False


def set_enabled(value: bool) -> bool:
    if os.name != "nt":
        return False
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, RUN_KEY, 0, winreg.KEY_SET_VALUE) as key:
        if value:
            winreg.SetValueEx(key, VALUE_NAME, 0, winreg.REG_SZ, _command())
        else:
            try:
                winreg.DeleteValue(key, VALUE_NAME)
            except FileNotFoundError:
                pass
    return enabled() == value
