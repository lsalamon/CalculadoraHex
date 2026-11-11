# CalculadoraHex

Calculadora desktop para Windows com dois painéis sincronizados, **Hexadecimal** à esquerda e **Decimal** à direita. O que você digita em um lado aparece traduzido no outro, variáveis valem nos dois, e tudo é gravado automaticamente.

![CalculadoraHex](docs/tela-fase2.png)

## Uso rápido

Digite uma expressão na linha `>` de qualquer painel e pressione Enter. Os dígitos são lidos na base do painel onde você está.

| Você digita (painel hexa) | Painel hexa mostra | Painel decimal mostra |
|---|---|---|
| `a=1234` | `a = 1234` | `a = 4660` |
| `a=a*4321` | `a = 4C5F4B4` | `a = 80082100` |
| `(FF+1)/2` | `(0xFF + 1) / 2 = 80` | `(255 + 1) / 2 = 128` |

- Operadores: `+ - * /` com precedência usual, parênteses e `-` unário. Divisão inteira. Inteiros de precisão arbitrária (sem overflow).
- Variáveis: `nome = expressão`. Nomes começam por letra ou `_` e não diferenciam maiúsculas.
- No painel hexa, `a`, `b`, `cafe` são dígitos **e** nomes possíveis. Regra: se a variável existe, é variável; senão, é número. O display sublinha o token quando a regra foi aplicada. Para forçar: `0xA` / `1Fh` é sempre número, `$a` é sempre variável.
- Ctrl+1 e Ctrl+2 ocultam ou exibem cada painel; ao voltar, o histórico completo está lá.
- Comentários: uma linha que começa por `#` não é calculada. O texto entra literalmente, com o `#`, nos dois painéis (ex.: `#soma de valores`). É gravado na sessão, exportado e pode ser apagado como qualquer linha.
- Menu **Editar**: Ctrl+D apaga a última linha do histórico e recalcula as variáveis; Ctrl+T insere uma linha tracejada separadora nos dois painéis (também gravada e exportada).
- Menu **Variáveis**: Ctrl+P imprime todas as variáveis no display, uma por linha e em ordem alfabética, entre linhas separadoras (sem repetir a anterior se já houver uma). Essas linhas guardam o valor daquele momento e são gravadas na sessão.
- F1 abre a referência de sintaxe e atalhos.

## Sessões

Cada operação é gravada em `%LOCALAPPDATA%\CalculadoraHex\sessoes\<data_hora>.json`. Ao abrir a app, a última sessão volta com histórico e variáveis. O menu **Arquivo** permite iniciar outra sessão, abrir uma antiga, salvar com outro nome e exportar o histórico em texto com as duas bases lado a lado.

### Importar histórico

**Arquivo > Importar histórico** (Ctrl+I) acrescenta operações à sessão atual, sem apagar nada. Aceita:

- uma sessão gravada (`.json`), cujas linhas são reavaliadas na ordem e na base originais;
- o texto gerado por **Exportar histórico** (`.txt`): cada linha traz `[hex]` ou `[dec]` e, depois de `;`, uma anotação com a expressão e o resultado nas duas bases. Importa de volta sem perguntar a base. Linhas que deram erro saem só como anotação e não são reproduzidas;
- um script de texto (`.txt`) com uma expressão por linha. Prefixos `[hex]`, `[dec]`, `hex:` ou `dec:` fixam a base da linha; sem prefixo vale a base escolhida no diálogo. Linhas iniciadas por `#` viram comentários no histórico; as iniciadas por `;` ou `//` são ignoradas, e o que vem depois de `;` no fim de uma linha é só anotação; uma linha só de traços (`---`) vira separador; `[var] nome = valor` recria uma variável impressa.

```text
# script.txt
a = 1234
[dec] b = a * 2
hex: c = b + FF
```

## Compilar e executar

Requer o [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
```

```bash
dotnet run --project src/CalculadoraHex.App
```

```bash
dotnet test
```

## Gerar o executável único

```bash
.\publicar.ps1
```

Gera `publish\CalculadoraHex.exe` autocontido (não precisa do .NET instalado). Com `-FrameworkDependent` o arquivo fica pequeno, mas exige o .NET 10 Desktop Runtime na máquina.

## Estrutura

```
src/CalculadoraHex.Core/   núcleo sem UI: parser, avaliador, variáveis, formatação, persistência
src/CalculadoraHex.App/    WPF (MVVM): janela, painéis, tema verde, diálogos, auto-save
tests/                     xUnit para o Core
PLANO.md                   projeto, decisões e fases de desenvolvimento
```

## Licença

MIT.
