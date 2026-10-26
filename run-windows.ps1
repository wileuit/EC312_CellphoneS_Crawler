# Script tự động kiểm tra môi trường, khôi phục, build và chạy CellphoneS Crawler (EC312 Lab 3 - Điện thoại di động)
param(
    [int]$Target = 500,
    [switch]$Headful,
    [switch]$Screenshots,
    [string]$Category = "",
    [string]$Output = "output/phones",
    [int]$Delay = 1800
)

$ErrorActionPreference = 'Stop'

Write-Host "=== EC312 LAB 03 - CELLPHONES SMARTPHONE CRAWLER (500 SẢN PHẨM) ===" -ForegroundColor Cyan

# 1. Kiểm tra môi trường .NET SDK
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetCmd) {
    if (Test-Path "C:\Program Files\dotnet\dotnet.exe") {
        Write-Host "Tìm thấy .NET tại C:\Program Files\dotnet. Đang nạp vào PATH..." -ForegroundColor Yellow
        $env:PATH = "C:\Program Files\dotnet;" + $env:PATH
    } else {
        throw "Không tìm thấy .NET SDK! Vui lòng cài đặt .NET 8 SDK từ https://dotnet.microsoft.com/"
    }
}

$dotnetVersion = dotnet --version
Write-Host ".NET SDK hiện tại: $dotnetVersion" -ForegroundColor Green

# 2. Khôi phục packages & Build
Write-Host "Đang khôi phục packages và build dự án..." -ForegroundColor Cyan
dotnet build -c Debug
if ($LASTEXITCODE -ne 0) { throw "Lỗi build dự án!" }

# 3. Đảm bảo Playwright Chromium đã được cài đặt
$installer = Join-Path $PSScriptRoot "bin\Debug\net8.0\playwright.ps1"
if (Test-Path $installer) {
    $pwFolder = Join-Path $env:LOCALAPPDATA "ms-playwright"
    $hasChromium = (Test-Path $pwFolder) -and (Get-ChildItem $pwFolder -Filter "chromium-*" -ErrorAction SilentlyContinue)
    if (-not $hasChromium) {
        Write-Host "Đang tải Chromium cho Playwright..." -ForegroundColor Yellow
        powershell -ExecutionPolicy Bypass -File $installer install chromium
    }
}

# 4. Chuẩn bị đối số dòng lệnh
$cliArgs = @("--target", $Target.ToString(), "--delay", $Delay.ToString(), "--output", $Output)
if ($Headful) { $cliArgs += "--headful" }
if ($Screenshots) { $cliArgs += "--screenshots" }
if (-not [string]::IsNullOrWhiteSpace($Category)) { $cliArgs += @("--category", $Category) }

Write-Host "Đang khởi chạy Crawler với mục tiêu: $Target sản phẩm điện thoại..." -ForegroundColor Green
Write-Host "Thư mục đầu ra: $Output" -ForegroundColor Green
dotnet run --no-build -c Debug -- @cliArgs
