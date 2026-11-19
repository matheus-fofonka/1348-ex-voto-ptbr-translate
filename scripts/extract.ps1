# Extrai os textos em ingles do jogo e cria/atualiza os CSVs de traducao,
# preservando as traducoes ja feitas (casadas por namespace + chave).
. "$PSScriptRoot\common.ps1"

$pak = Get-MainPak
New-Item -ItemType Directory -Force (Join-Path $Root 'source\en') | Out-Null

foreach ($name in $Targets.Keys) {
    $dir = $Targets[$name].Dir
    $locres = Join-Path $Root "source\en\$name.locres"
    $locmeta = Join-Path $Root "source\$name.locmeta"
    cmd /c "`"$Repak`" get `"$pak`" $dir/en/$name.locres > `"$locres`""
    if ($LASTEXITCODE) { throw "Falha ao extrair $name.locres" }
    cmd /c "`"$Repak`" get `"$pak`" $dir/$name.locmeta > `"$locmeta`""
    if ($LASTEXITCODE) { throw "Falha ao extrair $name.locmeta" }

    $old = @{}
    foreach ($r in (Read-Csv $name)) { $old["$($r.namespace)|$($r.key)"] = $r }

    $rows = New-Object System.Collections.Generic.List[object]
    $i = 0; $changed = 0; $new = 0
    foreach ($e in [LocresFile]::Load($locres).Entries) {
        $i++
        $prev = $old["$($e.Namespace)|$($e.Key)"]
        $tr = ''; $st = 'pendente'
        if ($prev) {
            $tr = $prev.traducao; $st = $prev.status
            if ($prev.original -ne $e.Text -and $st -ne 'pendente') { $st = 'revisar'; $changed++ }
        } else { $new++ }
        $rows.Add([pscustomobject]@{ id = $i; namespace = $e.Namespace; key = $e.Key; original = $e.Text; traducao = $tr; status = $st })
    }
    Write-Csv $name $rows
    Write-Host "$name : $($rows.Count) linhas ($new novas, $changed com original alterado -> status 'revisar')"
}
