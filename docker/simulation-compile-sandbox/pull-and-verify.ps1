<#
.SYNOPSIS
  Pull image stem-arduino-cli-sandbox tu Docker Hub va verify nhanh.

.DESCRIPTION
  Team member chay script nay thay vi tu build image (build local mat nhieu
  phut vi phai tai toolchain ESP32 ~300MB+ lan dau). Sau khi pull, script tu
  dong tag lai thanh "stem-arduino-cli-sandbox:latest" (dung ten mac dinh
  code dang tim qua SimulationCompile:DockerImage neu khong set trong
  appsettings.json) va verify ben trong image thuc su co esp32:esp32 dung
  version 2.0.17 (KHONG phai 3.3.10 -- xem VIRTUAL_LAB_PLAN.md muc 8.12/8.13
  ve ly do phai pin dung version nay, core moi hon crash khong tat dinh
  trong QEMU).

.PARAMETER DockerHubUser
  Docker Hub username. Mac dinh: trieu77.

.PARAMETER ImageName
  Ten image tren Docker Hub. Mac dinh: stem-arduino-cli-sandbox.

.PARAMETER Tag
  Tag version can pull. Mac dinh: v1-esp32-2.0.17.

.EXAMPLE
  ./pull-and-verify.ps1
#>

param(
    [string]$DockerHubUser = "trieu77",
    [string]$ImageName = "stem-arduino-cli-sandbox",
    [string]$Tag = "v1-esp32-2.0.17"
)

$ErrorActionPreference = "Stop"
$remoteTag = "${DockerHubUser}/${ImageName}:${Tag}"
$localTag = "stem-arduino-cli-sandbox:latest"

Write-Host "=== Kiem tra Docker dang chay ===" -ForegroundColor Cyan
docker info *> $null
if ($LASTEXITCODE -ne 0) {
    throw "Docker chua chay -- mo Docker Desktop truoc khi chay script nay."
}
Write-Host "OK: Docker dang chay." -ForegroundColor Green

Write-Host ""
Write-Host "=== Pull: $remoteTag ===" -ForegroundColor Cyan
docker pull $remoteTag
if ($LASTEXITCODE -ne 0) {
    throw "docker pull that bai -- kiem tra ket noi mang, hoac neu repo la private thi ban da duoc them quyen pull chua?"
}

Write-Host ""
Write-Host "=== Tag lai thanh: $localTag ===" -ForegroundColor Cyan
Write-Host "(De code dang doc SimulationCompile:DockerImage='stem-arduino-cli-sandbox:latest' tu tim thay ngay, khong can sua appsettings.json)"
docker tag $remoteTag $localTag
if ($LASTEXITCODE -ne 0) { throw "docker tag that bai." }

Write-Host ""
Write-Host "=== Verify: esp32:esp32 core version ===" -ForegroundColor Cyan
$coreList = docker run --rm --network none $localTag core list 2>&1
Write-Host $coreList

if ($coreList -match "esp32:esp32\s+2\.0\.17") {
    Write-Host "OK: esp32:esp32 dung version 2.0.17." -ForegroundColor Green
} else {
    Write-Host "CANH BAO: khong tim thay esp32:esp32 2.0.17 trong image -- kiem tra lai tag da pull, dung bao gio dung ban co core 3.3.10 tro len (crash khong tat dinh trong QEMU, xem VIRTUAL_LAB_PLAN.md 8.12/8.13)." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Xong ===" -ForegroundColor Green
Write-Host "appsettings.json cua ban co the giu nguyen SimulationCompile:DockerImage mac dinh"
Write-Host "(tro toi '$localTag' vua tag o tren), hoac tro thang toi '$remoteTag'."
Write-Host "Nho: van can Docker Desktop chay tren may de test full Virtual Lab simulation."
