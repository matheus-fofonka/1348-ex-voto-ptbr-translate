# Configuracao e funcoes compartilhadas pelos scripts do mod.
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent

# Codigo de cultura usado pelo mod (pasta dos .locres e lista do .locmeta)
$Culture = 'pt-BR'
$PakName = 'zzz_PTBR_P'

$Repak = Join-Path $Root 'tools\repak\repak.exe'
$IcuDll = Join-Path $Root 'tools\icu\bin64\icudt64.dll'

# Arquivos de texto do jogo: nome -> caminho do .locres em ingles e do .locmeta dentro do pak
$Targets = [ordered]@{
    Game = @{ Dir = 'P1348/Content/Localization/Game' }
    TRX  = @{ Dir = 'P1348/Plugins/TRX/Content/Localization/TRX' }
}

if (-not ('PakReader' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'PakTool.cs'), (Join-Path $PSScriptRoot 'LocresTool.cs'), (Join-Path $PSScriptRoot 'IcuTool.cs') -ReferencedAssemblies System.Core
}

function Find-GameDir {
    if ($env:GAME_1348_DIR -and (Test-Path $env:GAME_1348_DIR)) { return $env:GAME_1348_DIR }
    $steam = $null
    try { $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction Stop).SteamPath } catch {}
    $libs = @()
    if ($steam) {
        $libs += $steam
        $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            $libs += Select-String -Path $vdf -Pattern '"path"\s+"([^"]+)"' | ForEach-Object { $_.Matches[0].Groups[1].Value -replace '\\\\', '\' }
        }
    }
    foreach ($l in $libs) {
        $d = Join-Path $l 'steamapps\common\1348'
        if (Test-Path (Join-Path $d 'P1348\Content\Paks')) { return $d }
    }
    throw 'Pasta do jogo 1348 Ex Voto nao encontrada. Defina a variavel de ambiente GAME_1348_DIR.'
}

function Get-MainPak { Join-Path (Find-GameDir) 'P1348\Content\Paks\pakchunk0-Windows.pak' }

function Read-Csv([string]$name) {
    $p = Join-Path $Root "translation\$name.csv"
    if (-not (Test-Path $p)) { return @() }
    return @(Import-Csv $p -Encoding UTF8)
}

function Write-Csv([string]$name, $rows) {
    $p = Join-Path $Root "translation\$name.csv"
    $rows | Select-Object id, namespace, key, original, traducao, status | Export-Csv $p -NoTypeInformation -Encoding UTF8
}
