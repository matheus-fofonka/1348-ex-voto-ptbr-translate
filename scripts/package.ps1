# Gera o zip de distribuicao (GitHub Releases / Nexus) a partir de build\zzz_PTBR_P.pak.
#   .\package.ps1 -Versao 0.9.0
param([Parameter(Mandatory)][string]$Versao)
. "$PSScriptRoot\common.ps1"

$pak = Join-Path $Root "build\$PakName.pak"
if (-not (Test-Path $pak)) { throw 'Pak nao encontrado. Rode scripts\build.ps1 primeiro.' }

$name = "1348-ExVoto-PTBR-v$Versao"
$stage = Join-Path $Root "build\dist\$name"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null

Copy-Item $pak $stage
Copy-Item (Join-Path $Root 'dist\instalar.ps1'), (Join-Path $Root 'dist\Instalar.bat'), (Join-Path $Root 'dist\Desinstalar.bat') $stage
Copy-Item (Join-Path $Root 'tools\icu\LICENSE') (Join-Path $stage 'LICENCA-ICU.txt')
$utf8 = New-Object Text.UTF8Encoding $true
$leia = [IO.File]::ReadAllText((Join-Path $Root 'dist\LEIA-ME.txt'), [Text.Encoding]::UTF8).Replace('{VERSAO}', $Versao)
[IO.File]::WriteAllText((Join-Path $stage 'LEIA-ME.txt'), $leia, $utf8)

$zip = Join-Path $Root "build\dist\$name.zip"
if (Test-Path $zip) { Remove-Item $zip }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip, 'Optimal', $false)
Write-Host "Gerado: $zip ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)"
