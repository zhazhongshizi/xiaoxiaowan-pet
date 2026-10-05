param([string]$Output = (Join-Path $PSScriptRoot '..\..\dist\desktop'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x C# compiler not found.' }
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$Output = (Resolve-Path -LiteralPath $Output).Path
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 `
    "/out:$Output\XiaoXiaoWan.exe" "/win32manifest:$root\app.manifest" "/win32icon:$root\pet.ico" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll `
    "/resource:$root\atlas.png,atlas.png" "/resource:$root\pet.ico,pet.ico" "$root\Pet.cs"
if ($LASTEXITCODE -ne 0) { throw "Compiler failed: $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $root '..\README.md') -Destination "$Output\README.md"
Write-Output "$Output\XiaoXiaoWan.exe"
