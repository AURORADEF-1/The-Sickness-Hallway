<#
Audit by default. Optional installation uses WinGet's normal prompts.
Run in native Windows PowerShell; no machine execution-policy or PATH edits.
#>
[CmdletBinding()]
param([switch]$InstallTools, [switch]$WithOptionalTools)
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Run this helper on the Windows development PC.' }
$editorVersion = '6000.3.25f1'
$editorExe = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
$hubExe = Join-Path $env:ProgramFiles 'Unity Hub\Unity Hub.exe'
$wingetCommand = Get-Command winget -ErrorAction SilentlyContinue

function Ensure-Package([string]$Id, [bool]$Present) {
    if ($Present) { Write-Host "Present: $Id"; return }
    if (-not $InstallTools) { Write-Host "Missing/not detected: $Id"; return }
    if (-not $wingetCommand) { throw 'WinGet is unavailable. Install/update Microsoft App Installer or use official download pages.' }
    & winget show --id $Id --exact --source winget
    if ($LASTEXITCODE -ne 0) { throw "Could not verify package $Id." }
    & winget install --id $Id --exact --source winget --interactive --no-upgrade
    if ($LASTEXITCODE -ne 0) { throw "Installation of $Id did not complete successfully. Exit code: $LASTEXITCODE" }
}

Ensure-Package 'Unity.UnityHub' (Test-Path $hubExe)
Ensure-Package 'Git.Git' ([bool](Get-Command git -ErrorAction SilentlyContinue))
if ($WithOptionalTools) {
    Ensure-Package 'OpenJS.NodeJS.LTS' ([bool](Get-Command node -ErrorAction SilentlyContinue))
    Ensure-Package 'Python.Python.3.14' ([bool](Get-Command py -ErrorAction SilentlyContinue))
    Ensure-Package 'GitHub.cli' ([bool](Get-Command gh -ErrorAction SilentlyContinue))
}
if (Test-Path $editorExe) {
    Write-Host "Exact Editor detected: $editorExe"
} else {
    Write-Host "Install Unity $editorVersion revision e1dba0a9aba4 through Unity Hub, or locate your custom Editor install."
    Write-Host 'https://unity.com/releases/editor/whats-new/6000.3.25f1'
}
if (Get-Command git -ErrorAction SilentlyContinue) {
    & git --version
    & git lfs version
    if ($LASTEXITCODE -ne 0) { Write-Warning 'Git LFS is missing. Install from https://git-lfs.com before cloning binary assets.' }
}
$vswhereExe = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path $vswhereExe) {
    & $vswhereExe -latest -products '*' -requires Microsoft.VisualStudio.Workload.ManagedGame -property installationPath
} else {
    Write-Host 'Visual Studio installer not detected. Install/modify Visual Studio with Game development with Unity.'
}
Write-Host 'Read WINDOWS_SETUP.md. Sign-in, licensing, exact Editor/module installation and actual Windows playtesting remain separate steps.'
Write-Host 'If tools were installed, open a new terminal before continuing. This script does not alter any existing Unity project.'
