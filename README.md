# Back to the Future — Tradução PT-BR

<img src="src/cover.png" width="220" align="right" alt="xSnoopx">

**Tradução completa do jogo _Back to the Future_ (LMG) para Português do Brasil —
criada e traduzida por [xSnoopx](https://github.com/xsnoopx).**

Instalador para Windows · jogo original gratuito em **[playbacktothefuture.com](https://playbacktothefuture.com)**

[![Baixar](https://img.shields.io/badge/Baixar%20o%20instalador-v1.0.0-ff8a00?style=for-the-badge)](../../releases/latest)
[![Jogo original](https://img.shields.io/badge/Jogo%20original-playbacktothefuture.com-1a2470?style=for-the-badge)](https://playbacktothefuture.com)

---

## O que é

Um instalador que deixa o **Back to the Future do LMG** inteiro em português:
título, menus, pausa, opções, controles, HUD, circuitos do tempo, missões,
legendas de cutscene, modo livre, falas de rua, mapa, créditos e o fim de missão.

- **199 textos de interface** + **230 textos de história** traduzidos
- Nada além de texto é alterado: **nenhum asset, áudio, vídeo, modelo ou ajuste de engine**
- Backup automático e **desinstalador** (volta ao inglês original em um clique)
- Não inclui o jogo — o instalador usa a **sua** cópia oficial

> ⚠️ **Precisa do jogo original.** Baixe grátis em
> **[playbacktothefuture.com](https://playbacktothefuture.com)** (build
> `BackToTheFuture-Windows-7284fa5`). O patch de fã não distribuí nem redistribui
> o jogo, e não tem vínculo com o LMG nem com os detentores de _Back to the Future_.

## Como instalar

1. Baixe e descompacte o jogo oficial ([playbacktothefuture.com](https://playbacktothefuture.com)).
2. Baixe o instalador na aba **[Releases](../../releases/latest)** (ou em `dist/`).
3. Abra `ParadoxDrive-ptBR-Setup-1.0.0.exe`.
   O Windows pode avisar que o arquivo não tem assinatura digital: **Mais informações → Executar assim mesmo**.
4. Confira se a pasta detectada é a do jogo (senão, use **Procurar...**) e clique em **Instalar tradução**.
5. Abra o jogo pelo `ParadoxDrive.exe`.

Para voltar ao inglês: abra o instalador de novo e clique em **Restaurar original**
(o `ParadoxDrive-Win64-Shipping.exe.bak` e os `*.json.en.bak` são restaurados).

## O que foi traduzido

| parte | conteúdo |
|---|---|
| Interface | menus, pausa, opções, controles, HUD, circuitos do tempo, créditos, fim de missão |
| História | 75 textos de missão, 51 linhas de cutscene, 33 cartões do modo livre |
| Mundo | 38 falas de rua, 18 rótulos do mapa, 3 avisos de era, 12 nomes de rua |

Ajustes de estilo:

- **Acentos** são usados em todo o texto. A exceção são os **displays de LED**
  (circuitos do tempo, MPH, marcha): a fonte `Led.ttf` não tem glifos acentuados,
  então datas e unidades ficam em ASCII maiúsculo — `05 NOV 1955  06:15 PM`.
- `You` (legenda do mapa) e `MAP` (título do mapa) ficaram em inglês porque nesta
  build a tela de mapa lê os dois como **chave fixa**; traduzidos, o mapa não abre.
- Nomes de falantes (`DOC`, `MARTY`, `ELLIE`, `WALT`) e o aviso de fã seguem em
  inglês: são usados como id em várias telas.
- `%d`, `%s` e os atalhos entre colchetes (`[T]`, `[ESC]`) foram preservados.

## Como funciona (técnico)

- `src/data/*.json` — os 7 arquivos de dados do jogo, com as mesmas chaves dos
  originais; são copiados para `ParadoxDrive/Content/Data/` (os originais ficam
  guardados como `*.json.en.bak`).
- `src/patch.bin` — diff binário (168 trechos, ~15 KB) entre o executável original
  e o traduzido. O instalador **valida o SHA-256** do executável antes de aplicar e
  **valida o resultado** depois; se não bater, nada é escrito.
- `src/Program.cs` — o instalador (WinForms, compilado com `csc`, .NET Framework
  4.x, sem dependências). Interface com a capa do xSnoopx, detecção automática da
  pasta do jogo, log e desinstalador.
- `src/build_payload.py` — regenera o `patch.bin` a partir do exe original e do
  traduzido.

Recompilar o instalador:

```bat
csc -nologo -target:winexe -platform:anycpu -optimize+ -langversion:5 ^
    -win32icon:app.ico -out:..\dist\ParadoxDrive-ptBR-Setup-1.0.0.exe ^
    -r:System.Windows.Forms.dll -r:System.Drawing.dll ^
    -resource:cover.png,cover.png -resource:patch.bin,patch.bin ^
    -resource:data/missions.json,data.missions.json ^
    -resource:data/cutscenes.json,data.cutscenes.json ^
    -resource:data/freeplay.json,data.freeplay.json ^
    -resource:data/voices.json,data.voices.json ^
    -resource:data/map.json,data.map.json ^
    -resource:data/eras.json,data.eras.json ^
    -resource:data/roads.json,data.roads.json Program.cs
```

Teste sem interface (usado nos testes de instalação/desinstalação):

```bat
ParadoxDrive-ptBR-Setup-1.0.0.exe -silent -dir="C:\...\BackToTheFuture-Windows-7284fa5" -log=log.txt
ParadoxDrive-ptBR-Setup-1.0.0.exe -silent -uninstall -dir="C:\...\BackToTheFuture-Windows-7284fa5"
```

## Créditos

- **Jogo:** _Back to the Future: The Game_ — **LMG** (Live Madden Gaming) ·
  [playbacktothefuture.com](https://playbacktothefuture.com) ·
  [YouTube](https://www.youtube.com/@k1nglmg) · [Instagram](https://www.instagram.com/lmg2kool/)
- **Tradução, capa do instalador e instalador:** **xSnoopx**
- _Back to the Future_ é marca dos seus detentores. Isto é um projeto de fã,
  gratuito e sem fins comerciais.

## English

Full **Brazilian Portuguese** localization of LMG's _Back to the Future_ fan game.
The installer patches **text only** (199 UI strings + 230 story strings), keeps
backups and can restore the original English build. The game itself is **not**
included — download it free at [playbacktothefuture.com](https://playbacktothefuture.com).
Made by [xSnoopx](https://github.com/xsnoopx); not affiliated with LMG or the
rights holders of _Back to the Future_.
