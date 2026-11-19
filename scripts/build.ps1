# Gera build\zzz_PTBR_P.pak a partir dos CSVs de traducao.
# Conteudo do pak:
#  - .locres em PT-BR (linhas sem traducao ficam em ingles) e .locmeta com a cultura nova
#  - dados ICU oficiais 64.1 (mesma versao do jogo) + portugues, necessarios para o
#    Unreal aceitar a cultura pt-BR (o jogo so traz ICU de de/en/es/fr/it/ja/ko/zh)
. "$PSScriptRoot\common.ps1"

$pak = Get-MainPak
$stage = Join-Path $Root "build\$PakName"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }

foreach ($name in $Targets.Keys) {
    $dir = $Targets[$name].Dir
    $src = Join-Path $Root "source\en\$name.locres"
    if (-not (Test-Path $src)) { throw "Rode scripts\extract.ps1 primeiro ($src nao existe)" }
    $tr = @{}
    foreach ($r in (Read-Csv $name)) { if ($r.traducao -ne '') { $tr["$($r.namespace)|$($r.key)"] = $r.traducao } }

    $loc = [LocresFile]::Load($src)
    $n = 0
    foreach ($e in $loc.Entries) {
        $t = $tr["$($e.Namespace)|$($e.Key)"]
        if ($t) { $e.Text = $t -replace "`r?`n", "`r`n"; $n++ }
    }
    $outDir = Join-Path $stage ($dir + '/' + $Culture)
    New-Item -ItemType Directory -Force $outDir | Out-Null
    $loc.Save((Join-Path $outDir "$name.locres"))
    [LocmetaTool]::AddCulture((Join-Path $Root "source\$name.locmeta"), (Join-Path $stage "$dir/$name.locmeta"), $Culture)
    Write-Host "$name : $n de $($loc.Entries.Count) linhas traduzidas"
}

# ICU: substitui todos os arquivos ICU do jogo pelos oficiais e adiciona portugues
$icu = [IcuTool]::ReadCommonData($IcuDll)
$pre = 'Engine/Content/Internationalization/'
$gameIcu = (New-Object PakReader $pak).Entries | Where-Object { $_.Path.StartsWith($pre) } | ForEach-Object { $_.Path.Substring($pre.Length) }
$folders = $gameIcu | ForEach-Object { $_.Substring(0, $_.LastIndexOf('/') + 1) } | Sort-Object -Unique
$files = @($gameIcu) + @($folders | ForEach-Object { foreach ($l in 'pt', 'pt_BR', 'pt_PT') { "$_$l.res" } } | Where-Object { $icu.ContainsKey($_) })
foreach ($f in $files) {
    $out = Join-Path $stage ($pre + $f)
    New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
    [IO.File]::WriteAllBytes($out, $icu[$f])
}
Write-Host "ICU: $($files.Count) arquivos"

$outPak = Join-Path $Root "build\$PakName.pak"
if (Test-Path $outPak) { Remove-Item $outPak }
& $Repak pack --version V11 --mount-point '../../../' --quiet $stage
if ($LASTEXITCODE) { throw 'repak falhou' }
Write-Host "Pak gerado: $outPak ($([math]::Round((Get-Item $outPak).Length / 1MB, 1)) MB)"
