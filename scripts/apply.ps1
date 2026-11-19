# Aplica um lote de traducoes (JSON: { "Game": { "<id>": "texto" }, "TRX": { ... } }) aos CSVs.
param([Parameter(Mandatory)][string]$Lote, [string]$Status = 'traduzido')
. "$PSScriptRoot\common.ps1"

$data = [IO.File]::ReadAllText((Resolve-Path $Lote), [Text.Encoding]::UTF8) | ConvertFrom-Json
foreach ($prop in $data.PSObject.Properties) {
    $name = $prop.Name
    $rows = Read-Csv $name
    $byId = @{}; foreach ($r in $rows) { $byId[$r.id] = $r }
    $n = 0
    foreach ($p in $prop.Value.PSObject.Properties) {
        $r = $byId[$p.Name]
        if (-not $r) { throw "$name : id $($p.Name) nao existe" }
        $r.traducao = $p.Value; $r.status = $Status; $n++
    }
    Write-Csv $name $rows
    Write-Host "$name : $n linhas aplicadas"
}
