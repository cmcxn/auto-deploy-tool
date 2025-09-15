# 本地构建和打包脚本
# Build and Package Script for AutoDeployTool

param(
    [string]$Version = "local",
    [switch]$Clean = $false
)

Write-Host "AutoDeployTool 本地构建脚本" -ForegroundColor Green
Write-Host "版本: $Version" -ForegroundColor Yellow

# 清理构建目录
if ($Clean) {
    Write-Host "清理构建目录..." -ForegroundColor Yellow
    if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
    if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
    if (Test-Path "publish") { Remove-Item "publish" -Recurse -Force }
}

try {
    # 恢复依赖项
    Write-Host "恢复 NuGet 包..." -ForegroundColor Yellow
    dotnet restore AutoDeployTool.csproj
    if ($LASTEXITCODE -ne 0) { throw "依赖项恢复失败" }

    # 构建项目
    Write-Host "构建项目..." -ForegroundColor Yellow
    dotnet build AutoDeployTool.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "构建失败" }

    # 发布项目
    Write-Host "发布项目..." -ForegroundColor Yellow
    dotnet publish AutoDeployTool.csproj --configuration Release --no-build --output ./publish
    if ($LASTEXITCODE -ne 0) { throw "发布失败" }

    # 创建发布包
    $zipName = "AutoDeployTool-$Version.zip"
    Write-Host "创建发布包: $zipName" -ForegroundColor Yellow
    
    if (Test-Path $zipName) { Remove-Item $zipName -Force }
    Compress-Archive -Path ./publish/* -DestinationPath $zipName

    Write-Host "构建完成！" -ForegroundColor Green
    Write-Host "发布包位置: $zipName" -ForegroundColor Green
    Write-Host "发布目录: ./publish" -ForegroundColor Green

} catch {
    Write-Host "构建失败: $_" -ForegroundColor Red
    exit 1
}