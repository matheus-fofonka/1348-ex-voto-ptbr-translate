# Valida as traducoes: placeholders {0}/{Nome}, tags <...>, quebras de linha e espacos nas pontas.
# Mostra tambem o progresso por arquivo.
. "$PSScriptRoot\common.ps1"

$erros = 0
foreach ($name in $Targets.Keys) {
    $rows = Read-Csv $name
    foreach ($r in $rows) {
        if ($r.traducao -eq '') { continue }
        $o = $r.original; $t = $r.traducao
        $a = ([regex]::Matches($o, '\{[^}]*\}|<[^>]*>') | ForEach-Object Value | Sort-Object) -join ' '
        $b = ([regex]::Matches($t, '\{[^}]*\}|<[^>]*>') | ForEach-Object Value | Sort-Object) -join ' '
        if ($a -ne $b) { Write-Host "[$name #$($r.id)] marcadores diferentes: '$a' -> '$b'"; $erros++ }
        if (([regex]::Matches($o, "`n")).Count -ne ([regex]::Matches($t, "`n")).Count) { Write-Host "[$name #$($r.id)] numero de quebras de linha diferente"; $erros++ }
        if ((($o -match '^\s') -ne ($t -match '^\s')) -or (($o -match '\s$') -ne ($t -match '\s$'))) { Write-Host "[$name #$($r.id)] espacos nas pontas diferentes"; $erros++ }
    }
    $done = @($rows | Where-Object traducao -ne '').Count
    $byStatus = ($rows | Group-Object status | ForEach-Object { "$($_.Name)=$($_.Count)" }) -join ', '
    Write-Host ("{0}: {1}/{2} traduzidas ({3:P0}) [{4}]" -f $name, $done, $rows.Count, ($done / [math]::Max(1, $rows.Count)), $byStatus)
}
if ($erros) { Write-Host "$erros problema(s) encontrado(s)"; exit 1 } else { Write-Host 'Nenhum problema encontrado.' }
