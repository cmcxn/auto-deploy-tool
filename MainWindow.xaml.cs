using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Web.Administration;
using System.Data.SqlClient;
using System.ServiceProcess;
using System.Xml.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace AutoDeployTool
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region 绑定属性
        private string _serverAddress = "localhost";
        private string _username;
        private SecureString _password;
        private string _targetDirectory;
        private string _sourceDirectory;
        private bool _includeSubdirectories = true;
        private string _iisSiteName;
        private string _dbConnectionString;
        private SqlExecutionOrder _sqlExecutionOrder = SqlExecutionOrder.Sequential;
        private bool _performFileDeployment = true;
        private bool _performSqlExecution = true;
        private bool _performWindowsServices = true;
        private bool _performIisOperations = true;
        private bool _stopIisBeforeDeploy = true;
        private bool _startIisAfterDeploy = true;
        private bool _backupBeforeDeploy = true;
        private bool _verifyDeployment = true;
        private bool _useRemoteIisManagement = false;
        
        private ObservableCollection<FileReplaceRule> _fileReplaceRules = new ObservableCollection<FileReplaceRule>();
        private ObservableCollection<SqlScript> _sqlScripts = new ObservableCollection<SqlScript>();
        private ObservableCollection<WindowsService> _windowsServices = new ObservableCollection<WindowsService>();
        
        private bool _isDeploying;
        private CancellationTokenSource _cancellationTokenSource;

        public string ServerAddress
        {
            get => _serverAddress;
            set { _serverAddress = value; OnPropertyChanged(); }
        }

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public SecureString Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string TargetDirectory
        {
            get => _targetDirectory;
            set { _targetDirectory = value; OnPropertyChanged(); }
        }

        public string SourceDirectory
        {
            get => _sourceDirectory;
            set { _sourceDirectory = value; OnPropertyChanged(); }
        }

        public bool IncludeSubdirectories
        {
            get => _includeSubdirectories;
            set { _includeSubdirectories = value; OnPropertyChanged(); }
        }

        public string IisSiteName
        {
            get => _iisSiteName;
            set { _iisSiteName = value; OnPropertyChanged(); }
        }

        public string DbConnectionString
        {
            get => _dbConnectionString;
            set { _dbConnectionString = value; OnPropertyChanged(); }
        }

        public SqlExecutionOrder SqlExecutionOrder
        {
            get => _sqlExecutionOrder;
            set { _sqlExecutionOrder = value; OnPropertyChanged(); }
        }

        public bool PerformFileDeployment
        {
            get => _performFileDeployment;
            set { _performFileDeployment = value; OnPropertyChanged(); }
        }

        public bool PerformSqlExecution
        {
            get => _performSqlExecution;
            set { _performSqlExecution = value; OnPropertyChanged(); }
        }

        public bool PerformWindowsServices
        {
            get => _performWindowsServices;
            set { _performWindowsServices = value; OnPropertyChanged(); }
        }

        public bool PerformIisOperations
        {
            get => _performIisOperations;
            set { _performIisOperations = value; OnPropertyChanged(); }
        }

        public bool StopIisBeforeDeploy
        {
            get => _stopIisBeforeDeploy;
            set { _stopIisBeforeDeploy = value; OnPropertyChanged(); }
        }

        public bool StartIisAfterDeploy
        {
            get => _startIisAfterDeploy;
            set { _startIisAfterDeploy = value; OnPropertyChanged(); }
        }

        public bool BackupBeforeDeploy
        {
            get => _backupBeforeDeploy;
            set { _backupBeforeDeploy = value; OnPropertyChanged(); }
        }

        public bool VerifyDeployment
        {
            get => _verifyDeployment;
            set { _verifyDeployment = value; OnPropertyChanged(); }
        }

        public bool UseRemoteIisManagement
        {
            get => _useRemoteIisManagement;
            set { _useRemoteIisManagement = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FileReplaceRule> FileReplaceRules
        {
            get => _fileReplaceRules;
            set { _fileReplaceRules = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SqlScript> SqlScripts
        {
            get => _sqlScripts;
            set { _sqlScripts = value; OnPropertyChanged(); }
        }

        public ObservableCollection<WindowsService> WindowsServices
        {
            get => _windowsServices;
            set { _windowsServices = value; OnPropertyChanged(); }
        }

        public bool IsDeploying
        {
            get => _isDeploying;
            set { _isDeploying = value; OnPropertyChanged(); }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // 添加默认规则
            FileReplaceRules.Add(new FileReplaceRule { SourcePattern = "*.*", TargetPath = "" });
        }

        #region 事件处理
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = (sender as System.Windows.Controls.PasswordBox).SecurePassword;
        }

        private void BrowseTargetDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TargetDirectory = dialog.SelectedPath;
            }
        }

        private void BrowseSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SourceDirectory = dialog.SelectedPath;
            }
        }

        private void AddFileRule_Click(object sender, RoutedEventArgs e)
        {
            FileReplaceRules.Add(new FileReplaceRule());
        }

        private void RemoveFileRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is FileReplaceRule rule)
            {
                FileReplaceRules.Remove(rule);
            }
        }

        private void AddSqlScript_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "SQL脚本文件 (*.sql)|*.sql|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                SqlScripts.Add(new SqlScript { ScriptPath = dialog.FileName });
            }
        }

        private void RemoveSqlScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is SqlScript script)
            {
                SqlScripts.Remove(script);
            }
        }

        private void AddWindowsService_Click(object sender, RoutedEventArgs e)
        {
            WindowsServices.Add(new WindowsService());
        }

        private void RemoveWindowsService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is WindowsService service)
            {
                WindowsServices.Remove(service);
            }
        }

        private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "部署配置文件 (*.deploy)|*.deploy|所有文件 (*.*)|*.*",
                    DefaultExt = ".deploy"
                };

                if (dialog.ShowDialog() == true)
                {
                    var config = new DeploymentConfiguration
                    {
                        ServerAddress = ServerAddress,
                        Username = Username,
                        Password = SecureStringToString(Password),
                        TargetDirectory = TargetDirectory,
                        SourceDirectory = SourceDirectory,
                        IncludeSubdirectories = IncludeSubdirectories,
                        IisSiteName = IisSiteName,
                        DbConnectionString = DbConnectionString,
                        SqlExecutionOrder = SqlExecutionOrder,
                        PerformFileDeployment = PerformFileDeployment,
                        PerformSqlExecution = PerformSqlExecution,
                        PerformWindowsServices = PerformWindowsServices,
                        PerformIisOperations = PerformIisOperations,
                        StopIisBeforeDeploy = StopIisBeforeDeploy,
                        StartIisAfterDeploy = StartIisAfterDeploy,
                        BackupBeforeDeploy = BackupBeforeDeploy,
                        VerifyDeployment = VerifyDeployment,
                        UseRemoteIisManagement = UseRemoteIisManagement,
                        FileReplaceRules = FileReplaceRules?.ToList() ?? new List<FileReplaceRule>(),
                        SqlScripts = SqlScripts?.ToList() ?? new List<SqlScript>(),
                        WindowsServices = WindowsServices?.ToList() ?? new List<WindowsService>()
                    };

                    using (var stream = File.OpenWrite(dialog.FileName))
                    {
                        var serializer = new XmlSerializer(typeof(DeploymentConfiguration));
                        serializer.Serialize(stream, config);
                    }

                    Log("配置已保存成功");
                }
            }
            catch (Exception ex)
            {
                Log($"保存配置失败: {ex.Message}");
            }
        }

        private void LoadConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "部署配置文件 (*.deploy)|*.deploy|所有文件 (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    using (var stream = File.OpenRead(dialog.FileName))
                    {
                        var serializer = new XmlSerializer(typeof(DeploymentConfiguration));
                        var config = (DeploymentConfiguration)serializer.Deserialize(stream);

                        ServerAddress = config.ServerAddress;
                        Username = config.Username;
                        Password = StringToSecureString(config.Password);
                        TargetDirectory = config.TargetDirectory;
                        SourceDirectory = config.SourceDirectory;
                        IncludeSubdirectories = config.IncludeSubdirectories;
                        IisSiteName = config.IisSiteName;
                        DbConnectionString = config.DbConnectionString;
                        SqlExecutionOrder = config.SqlExecutionOrder;
                        PerformFileDeployment = config.PerformFileDeployment;
                        PerformSqlExecution = config.PerformSqlExecution;
                        PerformWindowsServices = config.PerformWindowsServices;
                        PerformIisOperations = config.PerformIisOperations;
                        StopIisBeforeDeploy = config.StopIisBeforeDeploy;
                        StartIisAfterDeploy = config.StartIisAfterDeploy;
                        BackupBeforeDeploy = config.BackupBeforeDeploy;
                        VerifyDeployment = config.VerifyDeployment;
                        UseRemoteIisManagement = config.UseRemoteIisManagement;
                        FileReplaceRules = new ObservableCollection<FileReplaceRule>(config.FileReplaceRules ?? new List<FileReplaceRule>());
                        SqlScripts = new ObservableCollection<SqlScript>(config.SqlScripts ?? new List<SqlScript>());
                        WindowsServices = new ObservableCollection<WindowsService>(config.WindowsServices ?? new List<WindowsService>());
                    }

                    Log("配置已加载成功");
                }
            }
            catch (Exception ex)
            {
                Log($"加载配置失败: {ex.Message}");
            }
        }

        private void ValidateConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(TargetDirectory) || !Directory.Exists(TargetDirectory))
                    errors.Add("目标目录不存在或未设置");

                if (PerformFileDeployment && (string.IsNullOrWhiteSpace(SourceDirectory) || !Directory.Exists(SourceDirectory)))
                    errors.Add("源目录不存在或未设置");

                if (PerformFileDeployment && !FileReplaceRules.Any())
                    errors.Add("请添加至少一个文件替换规则");

                if (PerformSqlExecution && string.IsNullOrWhiteSpace(DbConnectionString))
                    errors.Add("数据库连接字符串未设置");

                if (PerformSqlExecution && !SqlScripts.Any())
                    errors.Add("请添加至少一个SQL脚本");

                if (PerformIisOperations && string.IsNullOrWhiteSpace(IisSiteName))
                    errors.Add("IIS站点名未设置");

                if (UseRemoteIisManagement && !IsLocalServer())
                {
                    if (string.IsNullOrWhiteSpace(Username) || Password == null)
                        errors.Add("远程IIS管理需要用户名和密码");
                }

                if (errors.Any())
                {
                    Log("配置验证失败:");
                    foreach (var error in errors)
                    {
                        Log($"- {error}");
                    }
                }
                else
                {
                    Log("配置验证成功");
                }
            }
            catch (Exception ex)
            {
                Log($"验证配置时出错: {ex.Message}");
            }
        }

        private async void TestRemoteConnection_Click(object sender, RoutedEventArgs e)
        {
            if (IsLocalServer())
            {
                Log("当前配置为本地服务器，无需测试远程连接");
                return;
            }

            if (string.IsNullOrWhiteSpace(Username) || Password == null)
            {
                Log("请先配置用户名和密码");
                return;
            }

            try
            {
                Log("正在测试远程连接...");
                
                var testScript = @"
                    Write-Output 'Connection successful'
                    Get-Date
                ";

                var result = await ExecuteRemotePowerShellAsync(testScript, CancellationToken.None);
                Log($"远程连接测试成功: {result}");
            }
            catch (Exception ex)
            {
                Log($"远程连接测试失败: {ex.Message}");
                Log("请确保:");
                Log("1. 目标服务器WinRM服务已启用");
                Log("2. 用户名和密码正确");
                Log("3. 网络连接正常");
                Log("4. 防火墙允许WinRM连接(端口5985)");
            }
        }

        private async void StartDeployment_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeploying) return;

            try
            {
                ValidateConfiguration_Click(sender, e);
                
                IsDeploying = true;
                _cancellationTokenSource = new CancellationTokenSource();
                DeploymentProgressBar.Value = 0;
                Log("开始部署...");

                var success = await PerformDeployment(_cancellationTokenSource.Token);

                if (success)
                {
                    Log("部署成功完成!");
                }
                else if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Log("部署失败");
                }
                else
                {
                    Log("部署已取消");
                }
            }
            catch (Exception ex)
            {
                Log($"部署过程中发生错误: {ex.Message}");
            }
            finally
            {
                IsDeploying = false;
                DeploymentProgressBar.Value = 100;
            }
        }

        private void CancelDeployment_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeploying && _cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                Log("正在取消部署...");
            }
        }
        #endregion

        #region 部署逻辑
        private async Task<bool> PerformDeployment(CancellationToken cancellationToken)
        {
            try
            {
                // 计算总步骤数
                int totalSteps = 0;
                int currentStep = 0;

                if (PerformIisOperations && StopIisBeforeDeploy) totalSteps++;
                if (BackupBeforeDeploy) totalSteps++;
                if (PerformFileDeployment) totalSteps++;
                if (PerformSqlExecution) totalSteps++;
                if (PerformWindowsServices) totalSteps++;
                if (PerformIisOperations && StartIisAfterDeploy) totalSteps++;
                if (VerifyDeployment) totalSteps++;

                // 1. 停止IIS站点
                if (PerformIisOperations && StopIisBeforeDeploy)
                {
                    currentStep++;
                    UpdateProgress(currentStep, totalSteps, "停止IIS站点...");
                    
                    if (!await StopIisSiteAsync(cancellationToken))
                        return false;
                }

                // 2. 备份目标目录
                if (BackupBeforeDeploy)
                {
                    currentStep++;
                    UpdateProgress(currentStep, totalSteps, "备份目标目录...");
                    
                    if (!await BackupDirectoryAsync(TargetDirectory, cancellationToken))
                        return false;
                }

                // 3. 文件部署
                if (PerformFileDeployment)
                {
                    currentStep++;
                    UpdateProgress(currentStep, totalSteps, "开始文件部署...");
                    
                    if (!await DeployFilesAsync(cancellationToken))
                        return false;
                }

                // 4. 执行SQL脚本
                if (PerformSqlExecution)
                {
                    currentStep++;
                    UpdateProgress(currentStep, totalSteps, "执行SQL脚本...");
                    
                    if (!await ExecuteSqlScriptsAsync(cancellationToken))
                        return false;
                }

                // 5. 处理Windows服务
                if (PerformWindowsServices)
                {
                    currentStep++;
                    UpdateProgress(currentStep, totalSteps, "处理Windows服务...");
                    
                    if (!await ProcessWindowsServicesAsync(cancellationToken))
                        return false;
                }

                // 6. 启动IIS站点
                if (PerformIisOperations && StartIisAfterDeploy)
                {
                    currentStep++;
                    UpdateProgress(currentStep, totalSteps, "启动IIS站点...");
                    
                    if (!await StartIisSiteAsync(cancellationToken))
                        return false;
                }

                // 7. 验证部署
                if (VerifyDeployment)
                {
                    currentStep++;
                    UpdateProgress(currentStep, totalSteps, "验证部署结果...");
                    
                    if (!await VerifyDeploymentAsync(cancellationToken))
                        return false;
                }

                UpdateProgress(100, 100, "部署完成");
                return true;
            }
            catch (OperationCanceledException)
            {
                Log("部署已被用户取消");
                return false;
            }
            catch (Exception ex)
            {
                Log($"部署失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> StopIisSiteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return false;

                Log($"停止IIS站点: {IisSiteName}");
                
                if (UseRemoteIisManagement && !IsLocalServer())
                {
                    // 使用PowerShell远程管理IIS
                    return await StopIisSiteRemoteAsync(cancellationToken);
                }
                else
                {
                    // 使用本地IIS管理API
                    return await StopIisSiteLocalAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Log($"停止IIS站点失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> StopIisSiteLocalAsync(CancellationToken cancellationToken)
        {
            using (var serverManager = new ServerManager())
            {
                var site = serverManager.Sites[IisSiteName];
                if (site == null)
                {
                    Log($"找不到IIS站点: {IisSiteName}");
                    return false;
                }

                if (site.State == ObjectState.Started)
                {
                    site.Stop();
                    serverManager.CommitChanges();
                    await Task.Delay(2000, cancellationToken); // 等待站点停止
                    Log($"IIS站点 {IisSiteName} 已停止");
                }
                else
                {
                    Log($"IIS站点 {IisSiteName} 已经处于停止状态");
                }
            }
            return true;
        }

        private async Task<bool> StopIisSiteRemoteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var script = $@"
                    Import-Module WebAdministration
                    $site = Get-Website -Name '{IisSiteName}' -ErrorAction SilentlyContinue
                    if ($site -eq $null) {{
                        Write-Output 'SITE_NOT_FOUND'
                        exit 1
                    }}
                    if ($site.State -eq 'Started') {{
                        Stop-Website -Name '{IisSiteName}'
                        Start-Sleep -Seconds 2
                        $updatedSite = Get-Website -Name '{IisSiteName}'
                        if ($updatedSite.State -eq 'Stopped') {{
                            Write-Output 'STOPPED'
                        }} else {{
                            Write-Output 'FAILED_TO_STOP'
                            exit 1
                        }}
                    }} else {{
                        Write-Output 'ALREADY_STOPPED'
                    }}
                ";

                var result = await ExecuteRemotePowerShellAsync(script, cancellationToken);
                
                if (result.Contains("SITE_NOT_FOUND"))
                {
                    Log($"找不到IIS站点: {IisSiteName}");
                    return false;
                }
                else if (result.Contains("STOPPED") || result.Contains("ALREADY_STOPPED"))
                {
                    Log($"IIS站点 {IisSiteName} 已停止");
                    return true;
                }
                else
                {
                    Log($"远程停止IIS站点失败: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"远程停止IIS站点时出错: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> StartIisSiteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return false;

                Log($"启动IIS站点: {IisSiteName}");
                
                if (UseRemoteIisManagement && !IsLocalServer())
                {
                    // 使用PowerShell远程管理IIS
                    return await StartIisSiteRemoteAsync(cancellationToken);
                }
                else
                {
                    // 使用本地IIS管理API
                    return await StartIisSiteLocalAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Log($"启动IIS站点失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> StartIisSiteLocalAsync(CancellationToken cancellationToken)
        {
            using (var serverManager = new ServerManager())
            {
                var site = serverManager.Sites[IisSiteName];
                if (site == null)
                {
                    Log($"找不到IIS站点: {IisSiteName}");
                    return false;
                }

                if (site.State == ObjectState.Stopped)
                {
                    site.Start();
                    serverManager.CommitChanges();
                    await Task.Delay(2000, cancellationToken); // 等待站点启动
                    Log($"IIS站点 {IisSiteName} 已启动");
                }
                else
                {
                    Log($"IIS站点 {IisSiteName} 已经处于运行状态");
                }
            }
            return true;
        }

        private async Task<bool> StartIisSiteRemoteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var script = $@"
                    Import-Module WebAdministration
                    $site = Get-Website -Name '{IisSiteName}' -ErrorAction SilentlyContinue
                    if ($site -eq $null) {{
                        Write-Output 'SITE_NOT_FOUND'
                        exit 1
                    }}
                    if ($site.State -eq 'Stopped') {{
                        Start-Website -Name '{IisSiteName}'
                        Start-Sleep -Seconds 2
                        $updatedSite = Get-Website -Name '{IisSiteName}'
                        if ($updatedSite.State -eq 'Started') {{
                            Write-Output 'STARTED'
                        }} else {{
                            Write-Output 'FAILED_TO_START'
                            exit 1
                        }}
                    }} else {{
                        Write-Output 'ALREADY_STARTED'
                    }}
                ";

                var result = await ExecuteRemotePowerShellAsync(script, cancellationToken);
                
                if (result.Contains("SITE_NOT_FOUND"))
                {
                    Log($"找不到IIS站点: {IisSiteName}");
                    return false;
                }
                else if (result.Contains("STARTED") || result.Contains("ALREADY_STARTED"))
                {
                    Log($"IIS站点 {IisSiteName} 已启动");
                    return true;
                }
                else
                {
                    Log($"远程启动IIS站点失败: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"远程启动IIS站点时出错: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> BackupDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return false;
                if (!Directory.Exists(directoryPath))
                {
                    Log($"备份失败: 目录 {directoryPath} 不存在");
                    return false;
                }

                var backupPath = $"{directoryPath}_{DateTime.Now:yyyyMMddHHmmss}";
                Log($"正在备份目录到: {backupPath}");

                await Task.Run(() =>
                {
                    CopyDirectory(directoryPath, backupPath, IncludeSubdirectories);
                }, cancellationToken);

                Log("目录备份完成");
                return true;
            }
            catch (Exception ex)
            {
                Log($"备份目录失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> DeployFilesAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return false;
                if (!Directory.Exists(SourceDirectory))
                {
                    Log($"文件部署失败: 源目录 {SourceDirectory} 不存在");
                    return false;
                }

                if (!Directory.Exists(TargetDirectory))
                {
                    Log($"创建目标目录: {TargetDirectory}");
                    Directory.CreateDirectory(TargetDirectory);
                }

                foreach (var rule in FileReplaceRules)
                {
                    if (cancellationToken.IsCancellationRequested) return false;
                    
                    if (string.IsNullOrWhiteSpace(rule.SourcePattern))
                    {
                        Log("跳过空的文件替换规则");
                        continue;
                    }

                    Log($"应用文件替换规则: {rule.SourcePattern} -> {rule.TargetPath}");
                    
                    var sourceFiles = Directory.GetFiles(SourceDirectory, rule.SourcePattern, 
                        IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                    
                    foreach (var sourceFile in sourceFiles)
                    {
                        if (cancellationToken.IsCancellationRequested) return false;
                        
                        var relativePath = Path.GetRelativePath(SourceDirectory, sourceFile);
                        var targetFile = Path.Combine(TargetDirectory, rule.TargetPath, relativePath);
                        var targetDir = Path.GetDirectoryName(targetFile);

                        if (!Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }

                        // 如果是配置文件，可能需要特殊处理
                        if (Path.GetExtension(sourceFile).Equals(".config", StringComparison.OrdinalIgnoreCase))
                        {
                            await ProcessConfigFileAsync(sourceFile, targetFile, cancellationToken);
                        }
                        else
                        {
                            File.Copy(sourceFile, targetFile, true);
                        }
                        
                        Log($"已复制: {sourceFile} -> {targetFile}");
                    }
                }

                Log("文件部署完成");
                return true;
            }
            catch (Exception ex)
            {
                Log($"文件部署失败: {ex.Message}");
                return false;
            }
        }

        private async Task ProcessConfigFileAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
        {
            // 简单的配置文件处理示例，实际应用中可能需要更复杂的逻辑
            await Task.Run(() =>
            {
                // 如果目标文件存在，可能需要合并配置而不是直接覆盖
                if (File.Exists(targetPath))
                {
                    // 这里可以实现配置文件合并逻辑
                    // 简单处理：备份原有文件，然后复制新文件
                    var backupPath = $"{targetPath}.bak";
                    File.Copy(targetPath, backupPath, true);
                    File.Copy(sourcePath, targetPath, true);
                    Log($"已更新配置文件并创建备份: {backupPath}");
                }
                else
                {
                    File.Copy(sourcePath, targetPath, true);
                }
            }, cancellationToken);
        }

        private async Task<bool> ExecuteSqlScriptsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return false;
                if (SqlScripts == null || !SqlScripts.Any())
                {
                    Log("没有需要执行的SQL脚本");
                    return true;
                }

                Log($"开始执行SQL脚本，共 {SqlScripts.Count} 个");

                if (SqlExecutionOrder == SqlExecutionOrder.Sequential)
                {
                    // 顺序执行
                    foreach (var script in SqlScripts)
                    {
                        if (cancellationToken.IsCancellationRequested) return false;
                        
                        if (!File.Exists(script.ScriptPath))
                        {
                            Log($"SQL脚本不存在: {script.ScriptPath}");
                            if (!script.ContinueOnError) return false;
                            continue;
                        }

                        Log($"执行SQL脚本: {script.ScriptPath}");
                        var success = await ExecuteSqlScriptAsync(script.ScriptPath, cancellationToken);
                        
                        if (!success)
                        {
                            Log($"SQL脚本执行失败: {script.ScriptPath}");
                            if (!script.ContinueOnError) return false;
                        }
                    }
                }
                else
                {
                    // 并行执行
                    var tasks = SqlScripts.Select(async script =>
                    {
                        if (cancellationToken.IsCancellationRequested) return true;
                        
                        if (!File.Exists(script.ScriptPath))
                        {
                            Log($"SQL脚本不存在: {script.ScriptPath}");
                            return script.ContinueOnError;
                        }

                        Log($"执行SQL脚本: {script.ScriptPath}");
                        var success = await ExecuteSqlScriptAsync(script.ScriptPath, cancellationToken);
                        
                        if (!success)
                        {
                            Log($"SQL脚本执行失败: {script.ScriptPath}");
                            return script.ContinueOnError;
                        }
                        return true;
                    });

                    var results = await Task.WhenAll(tasks);
                    if (results.Any(r => !r)) return false;
                }

                Log("所有SQL脚本执行完成");
                return true;
            }
            catch (Exception ex)
            {
                Log($"执行SQL脚本失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ExecuteSqlScriptAsync(string scriptPath, CancellationToken cancellationToken)
        {
            try
            {
                var scriptContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);
                
                using (var connection = new SqlConnection(DbConnectionString))
                {
                    await connection.OpenAsync(cancellationToken);
                    
                    // 分割批处理命令
                    var commands = scriptContent.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var commandText in commands)
                    {
                        if (cancellationToken.IsCancellationRequested) return false;
                        
                        var trimmedCommand = commandText.Trim();
                        if (string.IsNullOrEmpty(trimmedCommand)) continue;
                        
                        using (var command = new SqlCommand(trimmedCommand, connection))
                        {
                            command.CommandTimeout = 300; // 5分钟超时
                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log($"执行SQL脚本时出错: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ProcessWindowsServicesAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return false;
                if (WindowsServices == null || !WindowsServices.Any())
                {
                    Log("没有需要处理的Windows服务");
                    return true;
                }

                foreach (var service in WindowsServices)
                {
                    if (cancellationToken.IsCancellationRequested) return false;
                    
                    if (string.IsNullOrWhiteSpace(service.ServiceName))
                    {
                        Log("跳过服务名为空的服务配置");
                        continue;
                    }

                    Log($"处理Windows服务: {service.ServiceName}");

                    // 停止服务
                    if (service.StopBeforeDeploy)
                    {
                        if (!await StopWindowsServiceAsync(service.ServiceName, cancellationToken))
                        {
                            Log($"停止服务 {service.ServiceName} 失败");
                            return false;
                        }
                    }

                    // 更新服务
                    if (service.UpdateService && !string.IsNullOrWhiteSpace(service.ServicePath))
                    {
                        if (!await UpdateWindowsServiceAsync(service.ServiceName, service.ServicePath, cancellationToken))
                        {
                            Log($"更新服务 {service.ServiceName} 失败");
                            return false;
                        }
                    }

                    // 启动服务
                    if (service.StartAfterDeploy)
                    {
                        if (!await StartWindowsServiceAsync(service.ServiceName, cancellationToken))
                        {
                            Log($"启动服务 {service.ServiceName} 失败");
                            return false;
                        }
                    }
                }

                Log("所有Windows服务处理完成");
                return true;
            }
            catch (Exception ex)
            {
                Log($"处理Windows服务失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> StopWindowsServiceAsync(string serviceName, CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using (var service = new ServiceController(serviceName))
                    {
                        if (service.Status == ServiceControllerStatus.Running || 
                            service.Status == ServiceControllerStatus.StartPending)
                        {
                            Log($"停止服务 {serviceName}...");
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(1));
                            
                            if (service.Status == ServiceControllerStatus.Stopped)
                            {
                                Log($"服务 {serviceName} 已停止");
                                return true;
                            }
                            else
                            {
                                Log($"服务 {serviceName} 未能停止，当前状态: {service.Status}");
                                return false;
                            }
                        }
                        else
                        {
                            Log($"服务 {serviceName} 已经处于停止状态或无法停止，状态: {service.Status}");
                            return true;
                        }
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Log($"停止服务 {serviceName} 时出错: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> StartWindowsServiceAsync(string serviceName, CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using (var service = new ServiceController(serviceName))
                    {
                        if (service.Status == ServiceControllerStatus.Stopped || 
                            service.Status == ServiceControllerStatus.StopPending)
                        {
                            Log($"启动服务 {serviceName}...");
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(1));
                            
                            if (service.Status == ServiceControllerStatus.Running)
                            {
                                Log($"服务 {serviceName} 已启动");
                                return true;
                            }
                            else
                            {
                                Log($"服务 {serviceName} 未能启动，当前状态: {service.Status}");
                                return false;
                            }
                        }
                        else
                        {
                            Log($"服务 {serviceName} 已经处于运行状态，状态: {service.Status}");
                            return true;
                        }
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Log($"启动服务 {serviceName} 时出错: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> UpdateWindowsServiceAsync(string serviceName, string servicePath, CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(servicePath))
                {
                    Log($"服务文件不存在: {servicePath}");
                    return false;
                }

                return await Task.Run(() =>
                {
                    // 使用sc命令更新服务
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "sc.exe",
                            Arguments = $"config {serviceName} binPath= \"{servicePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Log($"服务 {serviceName} 已更新为: {servicePath}");
                        return true;
                    }
                    else
                    {
                        Log($"更新服务 {serviceName} 失败: {output}");
                        return false;
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Log($"更新服务 {serviceName} 时出错: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> VerifyDeploymentAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return false;

                Log("开始验证部署结果...");
                bool allVerified = true;

                // 验证文件部署
                if (PerformFileDeployment)
                {
                    foreach (var rule in FileReplaceRules)
                    {
                        if (cancellationToken.IsCancellationRequested) return false;
                        
                        if (string.IsNullOrWhiteSpace(rule.SourcePattern)) continue;
                        
                        var sourceFiles = Directory.GetFiles(SourceDirectory, rule.SourcePattern, 
                            IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        
                        foreach (var sourceFile in sourceFiles)
                        {
                            var relativePath = Path.GetRelativePath(SourceDirectory, sourceFile);
                            var targetFile = Path.Combine(TargetDirectory, rule.TargetPath, relativePath);
                            
                            if (!File.Exists(targetFile))
                            {
                                Log($"验证失败: 目标文件不存在 - {targetFile}");
                                allVerified = false;
                            }
                            else
                            {
                                // 简单验证：比较文件大小
                                var sourceSize = new FileInfo(sourceFile).Length;
                                var targetSize = new FileInfo(targetFile).Length;
                                
                                if (sourceSize != targetSize)
                                {
                                    Log($"验证失败: 文件大小不匹配 - {targetFile}");
                                    allVerified = false;
                                }
                            }
                        }
                    }
                }

                // 验证IIS站点状态
                if (PerformIisOperations && StartIisAfterDeploy)
                {
                    if (UseRemoteIisManagement && !IsLocalServer())
                    {
                        // 远程验证IIS站点状态
                        var script = $@"
                            Import-Module WebAdministration
                            $site = Get-Website -Name '{IisSiteName}' -ErrorAction SilentlyContinue
                            if ($site -ne $null -and $site.State -eq 'Started') {{
                                Write-Output 'RUNNING'
                            }} else {{
                                Write-Output 'NOT_RUNNING'
                            }}
                        ";
                        
                        var result = await ExecuteRemotePowerShellAsync(script, cancellationToken);
                        if (result.Contains("NOT_RUNNING"))
                        {
                            Log($"验证失败: IIS站点 {IisSiteName} 未启动");
                            allVerified = false;
                        }
                    }
                    else
                    {
                        using (var serverManager = new ServerManager())
                        {
                            var site = serverManager.Sites[IisSiteName];
                            if (site != null && site.State != ObjectState.Started)
                            {
                                Log($"验证失败: IIS站点 {IisSiteName} 未启动");
                                allVerified = false;
                            }
                        }
                    }
                }

                // 验证Windows服务状态
                if (PerformWindowsServices)
                {
                    foreach (var service in WindowsServices)
                    {
                        if (cancellationToken.IsCancellationRequested) return false;
                        if (string.IsNullOrWhiteSpace(service.ServiceName) || !service.StartAfterDeploy) continue;
                        
                        using (var sc = new ServiceController(service.ServiceName))
                        {
                            if (sc.Status != ServiceControllerStatus.Running)
                            {
                                Log($"验证失败: Windows服务 {service.ServiceName} 未运行");
                                allVerified = false;
                            }
                        }
                    }
                }

                if (allVerified)
                {
                    Log("部署验证成功");
                }
                else
                {
                    Log("部署验证发现问题");
                }

                return allVerified;
            }
            catch (Exception ex)
            {
                Log($"验证部署时出错: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 辅助方法
        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                LogTextBox.ScrollToEnd();
            });
        }

        private void UpdateProgress(int currentStep, int totalSteps, string message)
        {
            var percentage = (int)((double)currentStep / totalSteps * 100);
            Dispatcher.Invoke(() =>
            {
                DeploymentProgressBar.Value = percentage;
                Log($"{message} ({percentage}%)");
            });
        }

        private void CopyDirectory(string sourceDir, string destDir, bool copySubDirs)
        {
            // 获取源目录的目录信息
            DirectoryInfo dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"源目录不存在或无法访问: {sourceDir}");
            }

            // 如果目标目录不存在，则创建
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // 获取源目录中的文件并复制到目标目录
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDir, file.Name);
                file.CopyTo(tempPath, true);
            }

            // 如果复制子目录，则复制每个子目录
            if (copySubDirs)
            {
                DirectoryInfo[] subDirs = dir.GetDirectories();
                foreach (DirectoryInfo subDir in subDirs)
                {
                    string tempPath = Path.Combine(destDir, subDir.Name);
                    CopyDirectory(subDir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private string SecureStringToString(SecureString secureString)
        {
            if (secureString == null) return null;
            
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        private SecureString StringToSecureString(string plainString)
        {
            if (string.IsNullOrWhiteSpace(plainString)) return null;
            
            var secureString = new SecureString();
            foreach (char c in plainString)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();
            return secureString;
        }

        #region Remote IIS Management Helper Methods
        private bool IsLocalServer()
        {
            return string.IsNullOrWhiteSpace(ServerAddress) || 
                   ServerAddress.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   ServerAddress.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   ServerAddress.Equals(".", StringComparison.OrdinalIgnoreCase) ||
                   ServerAddress.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> ExecuteRemotePowerShellAsync(string script, CancellationToken cancellationToken)
        {
            return await ExecuteRemotePowerShellWithRetryAsync(script, cancellationToken, maxRetries: 3);
        }

        private async Task<string> ExecuteRemotePowerShellWithRetryAsync(string script, CancellationToken cancellationToken, int maxRetries = 3)
        {
            Exception lastException = null;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        Log($"重试远程PowerShell连接 (第{attempt}次尝试)...");
                        await Task.Delay(2000 * attempt, cancellationToken); // 递增延迟
                    }

                    return await Task.Run(() =>
                    {
                        if (string.IsNullOrWhiteSpace(Username) || Password == null)
                        {
                            throw new InvalidOperationException("远程PowerShell连接需要用户名和密码");
                        }

                        // 创建PowerShell runspace配置
                        var connectionInfo = new WSManConnectionInfo(
                            new Uri($"http://{ServerAddress}:5985/wsman"),
                            "http://schemas.microsoft.com/powershell/Microsoft.PowerShell",
                            new PSCredential(Username, Password)
                        );

                        // 设置连接超时和操作超时
                        connectionInfo.OperationTimeout = TimeSpan.FromMinutes(2);
                        connectionInfo.OpenTimeout = TimeSpan.FromSeconds(30);

                        using (var runspace = RunspaceFactory.CreateRunspace(connectionInfo))
                        {
                            runspace.Open();
                            
                            using (var powershell = PowerShell.Create())
                            {
                                powershell.Runspace = runspace;
                                powershell.AddScript(script);
                                
                                var results = powershell.Invoke();
                                var output = string.Join("\n", results.Select(r => r?.ToString()));
                                
                                if (powershell.HadErrors)
                                {
                                    var errors = string.Join("\n", powershell.Streams.Error.Select(e => e.ToString()));
                                    throw new InvalidOperationException($"PowerShell执行出错: {errors}");
                                }
                                
                                return output;
                            }
                        }
                    }, cancellationToken);
                }
                catch (PSRemotingTransportException ex)
                {
                    lastException = ex;
                    Log($"远程PowerShell连接失败 (尝试 {attempt}/{maxRetries}) - 请检查WinRM服务是否启用: {ex.Message}");
                    if (attempt == maxRetries)
                    {
                        throw new InvalidOperationException($"远程连接失败 (已重试{maxRetries}次): {ex.Message}");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastException = ex;
                    Log($"远程PowerShell认证失败 - 请检查用户名和密码: {ex.Message}");
                    throw new InvalidOperationException($"认证失败: {ex.Message}"); // 认证错误不重试
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Log($"执行远程PowerShell命令失败 (尝试 {attempt}/{maxRetries}): {ex.Message}");
                    if (attempt == maxRetries)
                    {
                        throw;
                    }
                }
            }

            throw lastException ?? new InvalidOperationException("远程PowerShell执行失败");
        }
        #endregion

        #region INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    #region 数据模型
    public class FileReplaceRule : INotifyPropertyChanged
    {
        private string _sourcePattern;
        private string _targetPath;

        public string SourcePattern
        {
            get => _sourcePattern;
            set { _sourcePattern = value; OnPropertyChanged(); }
        }

        public string TargetPath
        {
            get => _targetPath;
            set { _targetPath = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SqlScript : INotifyPropertyChanged
    {
        private string _scriptPath;
        private bool _continueOnError = true;

        public string ScriptPath
        {
            get => _scriptPath;
            set { _scriptPath = value; OnPropertyChanged(); }
        }

        public bool ContinueOnError
        {
            get => _continueOnError;
            set { _continueOnError = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class WindowsService : INotifyPropertyChanged
    {
        private string _serviceName;
        private string _servicePath;
        private bool _stopBeforeDeploy = true;
        private bool _startAfterDeploy = true;
        private bool _updateService = true;

        public string ServiceName
        {
            get => _serviceName;
            set { _serviceName = value; OnPropertyChanged(); }
        }

        public string ServicePath
        {
            get => _servicePath;
            set { _servicePath = value; OnPropertyChanged(); }
        }

        public bool StopBeforeDeploy
        {
            get => _stopBeforeDeploy;
            set { _stopBeforeDeploy = value; OnPropertyChanged(); }
        }

        public bool StartAfterDeploy
        {
            get => _startAfterDeploy;
            set { _startAfterDeploy = value; OnPropertyChanged(); }
        }

        public bool UpdateService
        {
            get => _updateService;
            set { _updateService = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [XmlRoot("DeploymentConfiguration")]
    public class DeploymentConfiguration
    {
        public string ServerAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string TargetDirectory { get; set; }
        public string SourceDirectory { get; set; }
        public bool IncludeSubdirectories { get; set; }
        public string IisSiteName { get; set; }
        public string DbConnectionString { get; set; }
        public SqlExecutionOrder SqlExecutionOrder { get; set; }
        public bool PerformFileDeployment { get; set; }
        public bool PerformSqlExecution { get; set; }
        public bool PerformWindowsServices { get; set; }
        public bool PerformIisOperations { get; set; }
        public bool StopIisBeforeDeploy { get; set; }
        public bool StartIisAfterDeploy { get; set; }
        public bool BackupBeforeDeploy { get; set; }
        public bool VerifyDeployment { get; set; }
        public bool UseRemoteIisManagement { get; set; }
        public List<FileReplaceRule> FileReplaceRules { get; set; } = new List<FileReplaceRule>();
        public List<SqlScript> SqlScripts { get; set; } = new List<SqlScript>();
        public List<WindowsService> WindowsServices { get; set; } = new List<WindowsService>();
    }

    public enum SqlExecutionOrder
    {
        Sequential,
        Parallel
    }
    #endregion

    #region 转换器
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Enum.Parse(targetType, parameter?.ToString()) : System.Windows.Data.Binding.DoNothing;
        }
    }
    #endregion
}
