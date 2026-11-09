# CalculadoraHex — Projeto e Plano de Execução

Calculadora desktop em .NET com dois painéis sincronizados (Hexadecimal e Decimal), variáveis nomeadas, aparência de calculadora antiga (display verde, dígitos de 7 segmentos) e histórico gravado automaticamente.

---

## 1. Decisões técnicas

| Item | Decisão | Motivo |
|---|---|---|
| Plataforma | .NET 10 (LTS), `net10.0-windows` | SDK 10.0.403 já instalado na máquina |
| Framework GUI | **WPF** | Estilização livre (fonte 7 segmentos, cores, layout em colunas), DataBinding/MVVM, menus nativos |
| Padrão | MVVM (CommunityToolkit.Mvvm) | Mantém a lógica testável fora da tela |
| Tipo numérico | `System.Numerics.BigInteger` | Sem overflow; produtos hex grandes (ex.: `a*4321`) funcionam sem limite de 64 bits |
| Fonte do display | **DSEG7 Classic** (licença SIL OFL) embarcada como recurso; fallback para `Consolas` | Reproduz o visual de dígitos de calculadora |
| Persistência | JSON (`System.Text.Json`) em `%LOCALAPPDATA%\CalculadoraHex\sessoes\` | Simples, legível, fácil de carregar e versionar |
| Testes | xUnit | Parser/avaliador e persistência com cobertura automatizada |

### Estrutura da solução

```
CalculadoraHex.sln
├─ src/CalculadoraHex.Core/       biblioteca sem UI
│   ├─ Parsing/      Tokenizer, Parser (expressões com variáveis)
│   ├─ Evaluation/   Evaluator, VariableStore
│   ├─ Formatting/   NumberFormatter (hex/dec), Base enum
│   └─ Persistence/  Session, SessionStore (auto-save/load)
├─ src/CalculadoraHex.App/        WPF
│   ├─ Views/        MainWindow, CalculatorPane (UserControl)
│   ├─ ViewModels/   MainViewModel, PaneViewModel
│   ├─ Resources/    Fontes, temas (cores), estilos
│   └─ App.xaml
└─ tests/CalculadoraHex.Core.Tests/
```

---

## 2. Modelo de domínio

**Princípio central:** existe **um único histórico** de operações, independente da base. Cada painel é apenas uma *visão* desse histórico, formatada na sua base. Isso garante:

- O que é digitado em um painel aparece no outro automaticamente.
- Ocultar e reexibir um painel não perde nada: ele apenas re-renderiza o histórico.

```
Entry
  ├─ Id, Timestamp
  ├─ InputBase        (Hex | Dec)   base em que o usuário digitou
  ├─ RawText          "a = a * 4321"  texto original
  ├─ AssignedVariable "a" ou null
  ├─ Value            BigInteger (80082100)
  └─ Error            mensagem, se a linha falhou

Session
  ├─ Nome, CriadaEm, AtualizadaEm
  ├─ Entries[]
  └─ Variables  (snapshot: nome -> valor) — reconstruído ao carregar
```

### Gramática aceita (fase inicial)

```
linha      := [ident '='] expr
expr       := termo (('+'|'-') termo)*
termo      := fator (('*'|'/') fator)*
fator      := numero | ident | '(' expr ')' | '-' fator
numero     := dígitos válidos na base do painel (hex: 0-9 A-F; dec: 0-9)
ident      := letra (letra|dígito|'_')*
```

**Ambiguidade hex × identificador:** no painel hexa, `a`, `b`, `cafe` são dígitos válidos *e* nomes possíveis. Regra: um token que é **variável definida** vale como variável; caso contrário é tratado como número hex. Para forçar um dos sentidos:
- Prefixo `0x` força número (`0xA`); sufixo `h` também, desde que comece por dígito, no estilo assembler (`1Fh`, `0Ah`).
- Prefixo `$` força variável (`$a`).

A UI destaca o token em cor diferente conforme a interpretação, para evitar surpresas.

Divisão é inteira (como em calculadoras de programador); divisão por zero gera `Error` na linha, sem travar.

### Exemplo do enunciado, validado

| Entrada (painel Hexa) | Hexa mostra | Decimal mostra |
|---|---|---|
| `a=1234` | `a = 1234` | `a = 4660` |
| `a=a*4321` | `a = 4C5F4B4` | `a = 80082100` |

(0x1234 = 4660; 0x4321 = 17185; 4660 × 17185 = 80 082 100 = 0x4C5F4B4.)

---

## 3. Interface

### Layout

```
┌─────────────────────────────────────────────────────────────┐
│ Arquivo  Exibir  Variáveis  Operações  Ajuda                │
├──────────────────────────────┬──────────────────────────────┤
│  HEXADECIMAL                 │  DECIMAL                     │
│ ┌──────────────────────────┐ │ ┌──────────────────────────┐ │
│ │ a = 1234                 │ │ │ a = 4660                 │ │
│ │ a = a * 4321             │ │ │ a = a * 4321             │ │
│ │     -> 4C5F4B4           │ │ │     -> 80082100          │ │
│ │                          │ │ │                          │ │
│ └──────────────────────────┘ │ └──────────────────────────┘ │
│ ┌──────────────────────────┐ │ ┌──────────────────────────┐ │
│ │ > _                      │ │ │ > _                      │ │
│ └──────────────────────────┘ │ └──────────────────────────┘ │
├──────────────────────────────┴──────────────────────────────┤
│ Variáveis: a=4C5F4B4 (80082100)        Sessão: 2026-11-09   │
└─────────────────────────────────────────────────────────────┘
```

- Dois `CalculatorPane` lado a lado em um `Grid` com `GridSplitter`.
- Cada painel: display (histórico, rolável) + linha de entrada. A linha de entrada interpreta dígitos **na base do painel**.
- `Exibir > Painel Hexadecimal` / `Painel Decimal` (checáveis) colapsam a coluna; ao reexibir, o histórico já está lá porque é compartilhado.
- Barra de status com variáveis definidas nas duas bases.

### Aparência "calculadora antiga"

- Fundo do display: verde LCD/VFD (`#1F3D1F` fundo, `#9AFF9A` dígitos, `#2E5A2E` segmentos "apagados" ao fundo, como nas telas de 7 segmentos).
- Fonte DSEG7 para números; para letras de variáveis e operadores, `DSEG14` (mesma família, suporta A-Z) ou fallback monoespaçado.
- Bordas e moldura em cinza-chumbo, botões com relevo discreto.
- Tema definido em `ResourceDictionary` para permitir trocar (ex.: âmbar) no futuro.

### Menus (organizados por afinidade; crescem com a app)

| Menu | Itens iniciais | Reservado para o futuro |
|---|---|---|
| **Arquivo** | Nova sessão, Abrir sessão…, Salvar como…, Exportar histórico (txt), Sair | Sessões recentes |
| **Exibir** | ☑ Painel Hexadecimal, ☑ Painel Decimal, Limpar display, Tema | Painel Binário/Octal |
| **Variáveis** | Listar, Limpar todas, Remover… | Importar/exportar |
| **Operações** | Referência rápida das 4 operações (já disponíveis na linha de entrada) | Bitwise (AND/OR/XOR/NOT), Shifts, Módulo, Potência, largura de palavra (8/16/32/64 bits) |
| **Ajuda** | Sintaxe, Sobre | — |

---

## 4. Gravação automática e carregamento

- Cada linha avaliada é anexada à sessão em memória e o arquivo JSON é regravado (debounce de ~300 ms para não escrever a cada tecla).
- Arquivo: `%LOCALAPPDATA%\CalculadoraHex\sessoes\<yyyy-MM-dd_HHmmss>.json`; a sessão atual também é apontada em `ultima.json`.
- Ao iniciar, a app reabre a última sessão (configurável). `Arquivo > Abrir sessão…` lista as sessões gravadas.
- Ao carregar, o `Evaluator` **re-executa** as entradas em ordem para reconstruir as variáveis, garantindo consistência mesmo que o formato evolua.
- Gravação atômica (escreve em `.tmp` e renomeia) para não corromper em caso de queda.

---

## 5. Fases de desenvolvimento

### Fase 0 — Fundação (≈ ½ dia) — ✅ concluída em 2026-11-09
- Criar solução, 3 projetos, `Directory.Build.props`, `.gitignore`, `git init`.
- Pipeline mínimo: `dotnet build` + `dotnet test` verdes.
- **Entrega:** esqueleto compila e roda janela vazia.

### Fase 1 — Núcleo de cálculo (≈ 1 dia) — ✅ concluída em 2026-11-09 (51 testes)
- `Tokenizer`, `Parser`, `Evaluator` com as 4 operações, parênteses, unário `-`.
- `VariableStore` e atribuição `nome = expr`.
- `NumberFormatter` hex/dec; regra de desambiguação hex × variável.
- Testes xUnit cobrindo o exemplo do enunciado, erros de sintaxe e divisão por zero.
- **Entrega:** biblioteca Core testada, sem UI.

### Fase 2 — Interface básica (≈ 1 dia)
- `MainWindow` com menu superior e dois `CalculatorPane`.
- Tema verde, fonte DSEG embarcada, display rolável + linha de entrada.
- Digitar em qualquer painel avalia na base do painel e atualiza **ambos**.
- **Entrega:** calculadora usável com as 4 operações nos dois lados.

### Fase 3 — Variáveis e sincronismo (≈ ½ dia)
- Barra de status com variáveis nas duas bases.
- Menu *Variáveis* (listar, limpar, remover).
- Realce visual do token ambíguo (número vs variável) no painel hexa.
- **Entrega:** cenário `a=1234` / `a=a*4321` funciona exatamente como especificado.

### Fase 4 — Ocultar/exibir painéis (≈ ¼ dia)
- Itens checáveis em *Exibir*; colapso de coluna com `GridSplitter`.
- Painel reexibido mostra histórico completo e rola para o fim.
- Estado de visibilidade salvo nas preferências do usuário.
- **Entrega:** alternância sem perda de dados.

### Fase 5 — Persistência automática (≈ 1 dia)
- `SessionStore`: auto-save com debounce e gravação atômica.
- Reabertura da última sessão; *Arquivo > Nova / Abrir / Salvar como / Exportar*.
- Re-execução das entradas ao carregar; testes de ida e volta (salvar → carregar → mesmo estado).
- **Entrega:** fechar e reabrir a app mantém tudo.

### Fase 6 — Polimento e empacotamento (≈ ½ dia)
- Atalhos de teclado (Ctrl+N, Ctrl+O, Ctrl+1/Ctrl+2 alternam painéis, Esc limpa entrada).
- Diálogo *Sobre*, ajuda de sintaxe, mensagens de erro no estilo do display.
- `dotnet publish` single-file (win-x64), README com instruções.
- **Entrega:** executável distribuível.

### Fases futuras (fora do escopo inicial, já previstas na arquitetura)
- Operações bitwise, shifts, módulo, potência (menu *Operações*).
- Largura de palavra configurável (8/16/32/64 bits) com complemento de dois.
- Painéis adicionais (Binário, Octal): basta adicionar um `PaneViewModel` com outra base.
- Temas alternativos (âmbar, azul VFD).

---

## 6. Riscos e mitigações

| Risco | Mitigação |
|---|---|
| Ambiguidade `a` = dígito hex ou variável | Regra "variável definida vence" + prefixos `0x`/`$` + realce na UI |
| Fonte 7 segmentos sem letras | DSEG14 para texto; fallback monoespaçado automático |
| Corrupção do JSON em queda de energia | Gravação atômica via arquivo temporário + rename |
| Evolução do formato de sessão | Campo `Version` no JSON; carregamento re-executa entradas em vez de confiar em valores salvos |
