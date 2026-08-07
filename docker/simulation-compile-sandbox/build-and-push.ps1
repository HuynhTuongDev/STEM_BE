<#
.SYNOPSIS
  Build, tag va push image stem-arduino-cli-sandbox len Docker Hub.

.DESCRIPTION
  Chi nguoi co quyen push len Docker Hub repo (mac dinh: trieu77) can chay
  script nay. Team member khac chi can pull-and-verify.ps1, KHONG can build
  (build local mat nhieu phut vi phai tai toolchain ESP32 ~300MB+).

  Script KHONG xu ly dang nhap/mat khau Docker Hub -- ban phai tu chay
  `docker login` truoc (mot lan, tren may minh), theo dung nguyen tac khong
  luu credential trong repo/script.

.PARAMETER DockerHubUser
  Docker Hub username. Mac dinh: trieu77.

.PARAMETER ImageName
  Ten image tren Docker Hub. Mac dinh: stem-arduino-cli-sandbox.

.PARAMETER Tag
  Tag version. Mac dinh: v1-esp32-2.0.17 -- doi tag nay khi core/Dockerfile
  thay doi, KHONG ghi de tag cu (giu lich su cac ban da push).

.PARAMETER SkipPush
  Chi build + tag, khong push. Dung de test truoc khi push that.

.EXAMPLE
  docker login
  ./build-and-push.ps1

.EXAMPLE
  # Build + tag thu, chua push
  ./build-and-push.ps1 -SkipPush

.EXAMPLE
  # Push ban version moi
  ./build-and-push.ps1 -Tag "v2-esp32-2.0.17"
#>

param(
    [string]$DockerHubUser = "trieu77",
    [string]$ImageName = "stem-arduino-cli-sandbox",
    [string]$Tag = "v1-esp32-2.0.17",
    [switch]$SkipPush
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$dockerfilePath = Join-Path $PSScriptRoot "Dockerfile"
$localTag = "stem-arduino-cli-sandbox:latest"
$remoteTag = "${DockerHubUser}/${ImageName}:${Tag}"

if (-not (Test-Path $dockerfilePath)) {
    throw "Khong tim thay Dockerfile o: $dockerfilePath"
}

Write-Host "=== Build: $localTag ===" -ForegroundColor Cyan
Write-Host "(Dockerfile: $dockerfilePath, context: $repoRoot)"
docker build -t $localTag -f $dockerfilePath $repoRoot
if ($LASTEXITCODE -ne 0) { throw "docker build that bai." }

Write-Host ""
Write-Host "=== Verify core version truoc khi tag/push ===" -ForegroundColor Cyan
$coreList = docker run --rm --network none $localTag core list 2>&1
Write-Host $coreList
if ($coreList -notmatch "esp32:esp32\s+2\.0\.17") {
    throw "Image vua build KHONG co esp32:esp32 2.0.17 -- kiem tra lai Dockerfile truoc khi push (xem VIRTUAL_LAB_PLAN.md muc 8.13 ve ly do bat buoc pin 2.0.17)."
}
Write-Host "OK: esp32:esp32 dung 2.0.17." -ForegroundColor Green

Write-Host ""
Write-Host "=== Tag: $localTag -> $remoteTag ===" -ForegroundColor Cyan
docker tag $localTag $remoteTag
if ($LASTEXITCODE -ne 0) { throw "docker tag that bai." }

if ($SkipPush) {
    Write-Host ""
    Write-Host "-SkipPush duoc bat -- da build + tag xong, KHONG push." -ForegroundColor Yellow
    Write-Host "Image san sang o local voi tag: $remoteTag"
    exit 0
}

Write-Host ""
Write-Host "=== Push: $remoteTag ===" -ForegroundColor Cyan
Write-Host "(Can 'docker login' truoc neu chua dang nhap Docker Hub voi tai khoan $DockerHubUser)" -ForegroundColor Yellow
docker push $remoteTag
if ($LASTEXITCODE -ne 0) {
    throw "docker push that bai -- ban da chay 'docker login' voi tai khoan $DockerHubUser chua?"
}

Write-Host ""
Write-Host "=== Xong. Team co the pull bang: docker pull $remoteTag ===" -ForegroundColor Green
Write-Host "(hoac chay pull-and-verify.ps1 trong cung thu muc nay)"
