<#
    一键构建脚本。
    需要 .NET SDK 8.0 或更高版本（https://dotnet.microsoft.com/download）。

    用法：
        .\build.ps1              正常构建
        .\build.ps1 -Clean       先清理再构建
#>

[CmdletBinding()]
param(
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'StarsectorConfigTool.csproj'
$dist = Join-Path $root 'dist'

Write-Host ''
Write-Host '=== Starsector 基础配置工具 - 构建 ===' -ForegroundColor Cyan

# 检查 dotnet
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host '找不到 dotnet 命令。请先安装 .NET SDK：' -ForegroundColor Red
    Write-Host '  https://dotnet.microsoft.com/download' -ForegroundColor Yellow
    exit 1
}

$ver = & dotnet --version
Write-Host "dotnet 版本: $ver"

if ($Clean) {
    Write-Host '清理 bin / obj / dist ...'
    foreach ($d in @('bin', 'obj', 'dist')) {
        $p = Join-Path $root $d
        if (Test-Path $p) { Remove-Item $p -Recurse -Force }
    }
}

Write-Host '正在编译 ...'
& dotnet build $proj -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host '编译失败。' -ForegroundColor Red
    exit $LASTEXITCODE
}

$exe = Join-Path $root 'bin\Release\net48\StarsectorConfigTool.exe'
if (-not (Test-Path $exe)) {
    Write-Host '编译完成但没找到输出文件：' -ForegroundColor Red
    Write-Host "  $exe" -ForegroundColor Yellow
    exit 1
}

# 复制到 dist，附带说明文件
if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }
Copy-Item $exe $dist -Force

# .exe.config 要跟着一起发：
# 它负责告诉 WinForms 开启高 DPI 支持，缺了它在不同缩放比例的
# 显示器之间拖动窗口时控件不会重新缩放。
$cfg = "$exe.config"
if (Test-Path $cfg) { Copy-Item $cfg $dist -Force }

foreach ($f in @('README.md', 'LICENSE')) {
    $src = Join-Path $root $f
    if (Test-Path $src) { Copy-Item $src $dist -Force }
}

$size = [math]::Round((Get-Item (Join-Path $dist 'StarsectorConfigTool.exe')).Length / 1KB, 1)
Write-Host ''
Write-Host "构建成功：dist\StarsectorConfigTool.exe  ($size KB)" -ForegroundColor Green
Write-Host '  exe 可以单独运行；连同一个 .exe.config 一起发能获得更好的多显示器缩放支持。'
Write-Host ''
