# 目标服务器WinRM配置脚本
# 此脚本需要在目标服务器上以管理员身份运行

param(
    [switch]$EnableHTTPS = $false,
    [string]$TrustedHosts = "*"
)

Write-Host "正在配置WinRM服务..." -ForegroundColor Green

try {
    # 启用PowerShell远程管理
    Write-Host "启用PowerShell远程管理..." -ForegroundColor Yellow
    Enable-PSRemoting -Force -SkipNetworkProfileCheck

    # 配置WinRM
    Write-Host "配置WinRM服务..." -ForegroundColor Yellow
    winrm quickconfig -q

    # 设置基本配置
    winrm set winrm/config/service '@{AllowUnencrypted="true"}'
    winrm set winrm/config/service/auth '@{Basic="true"}'
    winrm set winrm/config/client '@{AllowUnencrypted="true"}'
    winrm set winrm/config/client '@{TrustedHosts="' + $TrustedHosts + '"}'

    # 配置防火墙规则
    Write-Host "配置防火墙规则..." -ForegroundColor Yellow
    
    # 检查并创建WinRM-HTTP规则
    $httpRule = Get-NetFirewallRule -DisplayName "WinRM-HTTP" -ErrorAction SilentlyContinue
    if (-not $httpRule) {
        New-NetFirewallRule -DisplayName "WinRM-HTTP" -Direction Inbound -Protocol TCP -LocalPort 5985 -Action Allow
        Write-Host "已创建WinRM HTTP防火墙规则 (端口 5985)" -ForegroundColor Green
    } else {
        Write-Host "WinRM HTTP防火墙规则已存在" -ForegroundColor Green
    }

    # 如果启用HTTPS，配置相应的规则
    if ($EnableHTTPS) {
        $httpsRule = Get-NetFirewallRule -DisplayName "WinRM-HTTPS" -ErrorAction SilentlyContinue
        if (-not $httpsRule) {
            New-NetFirewallRule -DisplayName "WinRM-HTTPS" -Direction Inbound -Protocol TCP -LocalPort 5986 -Action Allow
            Write-Host "已创建WinRM HTTPS防火墙规则 (端口 5986)" -ForegroundColor Green
        } else {
            Write-Host "WinRM HTTPS防火墙规则已存在" -ForegroundColor Green
        }

        # 配置HTTPS监听器
        Write-Host "配置HTTPS监听器..." -ForegroundColor Yellow
        $cert = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*$env:COMPUTERNAME*"} | Select-Object -First 1
        if ($cert) {
            winrm create winrm/config/Listener?Address=*+Transport=HTTPS "@{Hostname=`"$env:COMPUTERNAME`"; CertificateThumbprint=`"$($cert.Thumbprint)`"}"
            Write-Host "HTTPS监听器配置完成" -ForegroundColor Green
        } else {
            Write-Host "警告: 未找到合适的SSL证书，请手动配置HTTPS监听器" -ForegroundColor Red
        }
    }

    # 设置执行策略
    Write-Host "设置PowerShell执行策略..." -ForegroundColor Yellow
    Set-ExecutionPolicy RemoteSigned -Force

    # 确保IIS管理模块可用
    Write-Host "检查IIS管理模块..." -ForegroundColor Yellow
    $iisFeature = Get-WindowsFeature -Name IIS-ManagementConsole -ErrorAction SilentlyContinue
    if ($iisFeature -and $iisFeature.InstallState -eq "Installed") {
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        if (Get-Module WebAdministration) {
            Write-Host "IIS管理模块可用" -ForegroundColor Green
        } else {
            Write-Host "警告: 无法加载IIS管理模块" -ForegroundColor Red
        }
    } else {
        Write-Host "警告: IIS管理控制台未安装" -ForegroundColor Red
    }

    # 启动并设置WinRM服务自动启动
    Write-Host "启动WinRM服务..." -ForegroundColor Yellow
    Start-Service -Name WinRM
    Set-Service -Name WinRM -StartupType Automatic

    # 测试配置
    Write-Host "测试WinRM配置..." -ForegroundColor Yellow
    $listeners = winrm enumerate winrm/config/listener
    Write-Host "当前WinRM监听器:" -ForegroundColor Cyan
    Write-Host $listeners

    Write-Host "`n配置完成!" -ForegroundColor Green
    Write-Host "WinRM服务已启用并配置完成。" -ForegroundColor Green
    Write-Host "可以从远程计算机连接到此服务器进行IIS管理。" -ForegroundColor Green
    
    if ($EnableHTTPS) {
        Write-Host "HTTPS连接端口: 5986" -ForegroundColor Cyan
    }
    Write-Host "HTTP连接端口: 5985" -ForegroundColor Cyan

} catch {
    Write-Host "配置过程中发生错误: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "请检查管理员权限并重试" -ForegroundColor Red
    exit 1
}

Write-Host "`n使用说明:" -ForegroundColor Cyan
Write-Host "1. 确保网络防火墙允许WinRM端口 (5985/5986)" -ForegroundColor White
Write-Host "2. 在客户端配置正确的用户凭据" -ForegroundColor White
Write-Host "3. 确保用户账户有管理IIS的权限" -ForegroundColor White