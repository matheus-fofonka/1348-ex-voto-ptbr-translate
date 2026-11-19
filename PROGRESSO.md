# Progresso — Tradução PT-BR de 1348 Ex Voto

> Arquivo de checkpoint. Uma sessão nova deve ler este arquivo primeiro e continuar de "Próximo passo".

## Estado atual
- **Fase:** 4 — primeira tradução completa (Game 2126/2126, TRX 18/18; 130 linhas de dev mantidas em inglês, status `ignorar`).
  Primeiro commit feito (identidade git configurada só neste repositório).
- **Publicação:** código no GitHub (main). Zip v0.9.0 gerado por `scripts\package.ps1` com instalador
  (`dist\instalar.ps1` + `Instalar.bat`/`Desinstalar.bat`); textos prontos em `dist\RELEASE.md` e `dist\NEXUS.md`.
  Upload da Release e do Nexus é feito pelo usuário.
- **Próximo passo:** revisão do usuário jogando (marcar linhas como `revisado` ou corrigir `traducao` no CSV);
  depois `build.ps1` + `package.ps1 -Versao X` para uma nova versão.
- Pontos a revisar no contexto do jogo: "Bugigangas" (Trinkets), "Ícone de ritmo" (Tempo icon), tratamento "tu"
  nos diálogos, gênero de falas onde inimigos chamam Aeta de "garoto" (mantido como no original, ela usa armadura).

## Como funciona (resumo técnico)
- Jogo em UE5, pak v11, Oodle. `source\en\*.locres` extraídos com repak (`scripts\extract.ps1`).
- O mod adiciona a cultura `pt-BR`: `.locres` em `.../Localization/<alvo>/pt-BR/` + `.locmeta` com pt-BR na lista.
- O jogo não traz dados ICU de português; o pak substitui os 1100 arquivos ICU do jogo pelos oficiais
  do ICU 64.1 (mesma versão; o `pool.res` do jogo é incompatível com arquivos avulsos) e adiciona pt, pt_BR, pt_PT.
- O menu de idiomas do jogo já tem a opção "Portuguese", que grava `Culture=pt-BR` — o mesmo código do mod.
- `scripts\install.ps1` copia o pak para `Content\Paks\~mods\` (não mexe no .ini; `-ForcarIdioma` grava pt-BR).

## Fluxo de cada lote
1. Pegar o próximo intervalo de ~170 ids `pendente`.
2. Escrever `translation\lotes\lote-NN.json` (`{"Game": {"id": "texto"}}`), em UTF-8.
3. `scripts\apply.ps1 translation\lotes\lote-NN.json` → `scripts\check.ps1`.
4. Linhas só numéricas, notas de desenvolvedor ou "Cutscene Manager"/debug: copiar o original com status `ignorar`.
5. Adicionar a linha do lote na tabela abaixo; atualizar `glossario.md`; commit `Lote NN: Game ids X–Y`.
6. A cada ~5 lotes: `scripts\build.ps1` + `scripts\install.ps1` e testar no jogo.
- Atenção: no PowerShell 5.1, ler arquivos .md/.csv sempre com `-Encoding UTF8` (senão os acentos estragam).

## Teste da prova de conceito (2026-11-18) — concluída
- [x] Abre sem crash; menus traduzidos; acentos corretos; opção "Português (Brasil)" aparece e troca de idioma funciona.
- [x] Escolhendo Português no menu, o jogo grava `Culture=pt-BR` e reabre em PT.
- Ao reabrir, o seletor mostra "English" mesmo com o texto em PT — acontece igual com Français, é comportamento do jogo.

## Lotes
| Lote | Arquivo | Ids | Observação | Status |
|------|---------|-----|------------|--------|
| 00 | Game | 2035–2126 | menus (54 linhas) | traduzido |
| 01 | Game | 1–170 | 10 linhas ignoradas (números/notas de dev) | traduzido |
| 02 | Game | 171–340 | linhas "CUTSCENE"/"Cutscene Manager" ignoradas | traduzido |
| 03 | Game | 341–510 | rótulos de debug ignorados | traduzido |
| 04 | Game | 511–680 | 580 ("I am Really Hungry!", teste) ignorada | traduzido |
| 05 | Game | 681–850 | 749/755 (frases de teste de dev) ignoradas; build+install feitos | traduzido |
| 06 | Game | 851–1020 | Level/Demo, LookX/Y, Lorem ipsum ignorados | traduzido |
| 07 | Game | 1021–1190 | rótulos de debug (PLAYER TAUNTING, Options Name…) ignorados | traduzido |
| 08 | Game | 1191–1360 | "Showcase:", SKILL NAME, TEST ignorados | traduzido |
| 09 | Game | 1361–1530 | nomes de capítulos (key_level1–9); 1423 (lorem) ignorada | traduzido |
| 10 | Game | 1531–1700 | WAITING FOR REPLY / Waiting Before Talking ignorados; build+install feitos | traduzido |
| 11 | Game | 1701–1880 | — | traduzido |
| 12 | Game + TRX | 1881–2111 + TRX 1–18 | placeholders/testes de dev ignorados; build+install feitos | traduzido |

## Decisões
- 2026-11-18: adicionar idioma novo pt-BR (não substituir); Claude traduz, usuário revisa; trabalho em lotes com checkpoint; instalador no futuro.
- Diálogos arcaicos ("thou") → "tu" com conjugação de 2ª pessoa; nomes italianos e latim mantidos (flagellanti, briganti, cavaliere).
