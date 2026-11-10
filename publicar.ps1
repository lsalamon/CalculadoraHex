<#
.SYNOPSIS
  Gera o executável único da CalculadoraHex em .\publish\.

.DESCRIPTION
  Por padrão produz um .exe autocontido (não precisa do .NET instalado), win-x64.
  Use -FrameworkDependent para um .exe pequeno que exige o .NET 10 Desktop Runtime.

.EXAMPLE
  .\publicar.ps1
  .\publicar.ps1 -FrameworkDependent
  .\publicar.ps1 -Runtime win-arm64
#>
param(
    [switch]$FrameworkDependent,
    [string]$Runtime = "win-x64",
    [string]$Output = (Join-Path $PSScriptRoot "publish")
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "src\CalculadoraHex.App\CalculadoraHex.App.csproj"

$args = @(
    "publish", $project,
    "-c", "Release",
    "-r", $Runtime,
    "-o", $Output,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:DebugType=none",
    "--nologo"
)
$args += if ($FrameworkDependent) { "--self-contained", "false" } else { "--self-contained", "true" }

Write-Host "dotnet $($args -join ' ')" -ForegroundColor DarkGray
& dotnet @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Join-Path $Output "CalculadoraHex.exe"
$mb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "`nPronto: $exe ($mb MB)" -ForegroundColor Green
