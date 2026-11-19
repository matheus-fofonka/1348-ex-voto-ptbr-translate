# 1348 Ex Voto — Tradução em Português do Brasil (não oficial)

Mod de tradução feito por fãs para **1348 Ex Voto** (Sedleo). Adiciona o idioma **pt-BR** ao jogo.
Não é afiliado ao estúdio nem à distribuidora.

> Status: em andamento — veja [PROGRESSO.md](PROGRESSO.md).

## Instalar (jogador)
1. Baixe `zzz_PTBR_P.pak` em [Releases](../../releases) (ou no Nexus Mods).
2. Copie para `...\steamapps\common\1348\P1348\Content\Paks\~mods\` (crie a pasta `~mods` se não existir).
3. Escolha *Português* no menu de idiomas do jogo, ou em
   `%LOCALAPPDATA%\P1348\Saved\Config\Windows\GameUserSettings.ini` defina:
   ```ini
   [Internationalization]
   Culture=pt-BR
   ```
Para desinstalar, apague o `.pak` e volte `Culture=en`.

## Gerar o mod (desenvolvedor)
Requisitos: Windows PowerShell 5.1+ e o jogo instalado (Steam).

Ferramentas em `tools\` (não versionadas):
- `tools\repak\repak.exe` — [trumank/repak](https://github.com/trumank/repak/releases) v0.2.3 (baixa sozinho a DLL do Oodle)
- `tools\icu\bin64\icudt64.dll` — [ICU 64.1 Win64](https://github.com/unicode-org/icu/releases/tag/release-64-1) (`icu4c-64_1-Win64-MSVC2017.zip`)

```powershell
.\scripts\extract.ps1   # extrai os textos do jogo e atualiza translation\*.csv
.\scripts\check.ps1     # valida marcadores e mostra o progresso
.\scripts\build.ps1     # gera build\zzz_PTBR_P.pak
.\scripts\install.ps1   # instala no jogo e ativa pt-BR (-Desinstalar para remover)
```

## Contribuir com a tradução
Os textos ficam em `translation\Game.csv` e `translation\TRX.csv` (colunas `original`, `traducao`, `status`).
Siga o [glossário](glossario.md). Status: `pendente`, `traduzido`, `revisado`, `revisar` (original mudou numa atualização).

## Créditos e licenças
- Jogo e textos originais © Sedleo. Este repositório contém apenas a tradução e ferramentas.
- Dados ICU © Unicode, Inc. — [Unicode License](https://www.unicode.org/copyright.html).
