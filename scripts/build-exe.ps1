param(
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$python = Join-Path $root ".venv\Scripts\python.exe"
if (-not (Test-Path $python)) { throw "Run install.bat before building the executable." }
& $python -m PyInstaller --version 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Install PyInstaller in the build environment before building." }

$work = Join-Path $env:TEMP ("1bullet-pyinstaller-" + [Guid]::NewGuid().ToString("N"))
try {
    & $python -m PyInstaller --noconfirm --clean --distpath $Output --workpath $work --specpath $root `
        (Join-Path $root "1bullet.spec")
    if ($LASTEXITCODE -ne 0) { throw "PyInstaller failed." }
    $exe = Join-Path $Output "1bullet.exe"
    if (-not (Test-Path $exe)) { throw "PyInstaller did not create 1bullet.exe." }
} finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
