# Instala o mod: copia o pak para Content\Paks\~mods e (opcional) define o idioma do jogo.
#   .\install.ps1                 -> so copia o pak (o jogador escolhe Portugues no menu do jogo)
#   .\install.ps1 -ForcarIdioma   -> tambem grava Culture=pt-BR no GameUserSettings.ini
#                                    (o menu continua mostrando "English" selecionado)
#   .\install.ps1 -Desinstalar    -> remove o pak e volta o idioma para ingles
param([switch]$ForcarIdioma, [switch]$Desinstalar)
. "$PSScriptRoot\common.ps1"

$mods = Join-Path (Find-GameDir) 'P1348\Content\Paks\~mods'
$target = Join-Path $mods "$PakName.pak"
$ini = Join-Path $env:LOCALAPPDATA 'P1348\Saved\Config\Windows\GameUserSettings.ini'

function Set-Culture([string]$c) {
    if (-not (Test-Path $ini)) { Write-Host 'GameUserSettings.ini nao existe ainda (abra o jogo uma vez).'; return }
    if (-not (Test-Path "$ini.bak-ptbr")) { Copy-Item $ini "$ini.bak-ptbr" }
    $text = [IO.File]::ReadAllText($ini)
    if ($text -match '(?m)^Culture=.*$') { $text = $text -replace '(?m)^Culture=.*$', "Culture=$c" }
    elseif ($text -match '(?m)^\[Internationalization\]') { $text = $text -replace '(?m)^\[Internationalization\]\r?\n', "[Internationalization]`r`nCulture=$c`r`n" }
    else { $text = "[Internationalization]`r`nCulture=$c`r`n`r`n" + $text }
    [IO.File]::WriteAllText($ini, $text)
    Write-Host "Idioma do jogo definido para $c"
}

if ($Desinstalar) {
    if (Test-Path $target) { Remove-Item $target; Write-Host "Removido: $target" }
    Set-Culture 'en'
    return
}

$src = Join-Path $Root "build\$PakName.pak"
if (-not (Test-Path $src)) { $src = Join-Path $PSScriptRoot "$PakName.pak" }
if (-not (Test-Path $src)) { throw "Pak nao encontrado. Rode scripts\build.ps1 primeiro." }
New-Item -ItemType Directory -Force $mods | Out-Null
Copy-Item $src $target -Force
Write-Host "Instalado: $target"
if ($ForcarIdioma) { Set-Culture $Culture } else { Write-Host 'Escolha "Portugues (Brasil)" em Opcoes > Idioma dentro do jogo.' }
