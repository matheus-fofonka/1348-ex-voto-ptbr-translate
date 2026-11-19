# Instalador do mod de traducao PT-BR para 1348 Ex Voto (versao para jogadores).
#   instalar.ps1               -> copia zzz_PTBR_P.pak para P1348\Content\Paks\~mods
#   instalar.ps1 -Desinstalar  -> remove o pak e volta o idioma do jogo para ingles
param([switch]$Desinstalar)
$ErrorActionPreference = 'Stop'
$PakName = 'zzz_PTBR_P.pak'

function Test-GameDir([string]$d) { $d -and (Test-Path (Join-Path $d 'P1348\Content\Paks')) }

function Find-GameDir {
    if (Test-GameDir $env:GAME_1348_DIR) { return $env:GAME_1348_DIR }
    $libs = @()
    try { $libs += (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction Stop).SteamPath } catch {}
    foreach ($s in @($libs)) {
        $vdf = Join-Path $s 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            $libs += Select-String -Path $vdf -Pattern '"path"\s+"([^"]+)"' | ForEach-Object { $_.Matches[0].Groups[1].Value -replace '\\\\', '\' }
        }
    }
    foreach ($l in $libs) {
        $d = Join-Path $l 'steamapps\common\1348'
        if (Test-GameDir $d) { return $d }
    }
    # Nao achou pela Steam: pede para o jogador escolher a pasta.
    Add-Type -AssemblyName System.Windows.Forms
    $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
    $dlg.Description = 'Selecione a pasta do jogo 1348 Ex Voto (a que contem a pasta P1348)'
    if ($dlg.ShowDialog() -eq 'OK') {
        $d = $dlg.SelectedPath
        if (Test-GameDir $d) { return $d }
        if (Test-GameDir (Split-Path $d -Parent)) { return (Split-Path $d -Parent) }
    }
    throw 'Pasta do jogo nao encontrada. Copie o arquivo zzz_PTBR_P.pak manualmente (veja o LEIA-ME).'
}

try {
    $game = Find-GameDir
    $mods = Join-Path $game 'P1348\Content\Paks\~mods'
    $target = Join-Path $mods $PakName
    Write-Host "Jogo encontrado em: $game"

    if ($Desinstalar) {
        if (Test-Path $target) { Remove-Item $target -Force; Write-Host 'Traducao removida.' }
        else { Write-Host 'A traducao nao estava instalada.' }
        $ini = Join-Path $env:LOCALAPPDATA 'P1348\Saved\Config\Windows\GameUserSettings.ini'
        if (Test-Path $ini) {
            $text = [IO.File]::ReadAllText($ini)
            if ($text -match '(?m)^Culture=pt-BR\s*$') {
                [IO.File]::WriteAllText($ini, ($text -replace '(?m)^Culture=pt-BR', 'Culture=en'))
                Write-Host 'Idioma do jogo voltou para ingles.'
            }
        }
    } else {
        $src = Join-Path $PSScriptRoot $PakName
        if (-not (Test-Path $src)) { throw "Arquivo $PakName nao encontrado ao lado do instalador. Extraia o zip inteiro antes de instalar." }
        New-Item -ItemType Directory -Force $mods | Out-Null
        Copy-Item $src $target -Force
        Write-Host "Instalado em: $target"
        Write-Host ''
        Write-Host 'Abra o jogo e escolha "Portugues" no menu de idiomas (Opcoes).'
    }
} catch {
    Write-Host ''
    Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
