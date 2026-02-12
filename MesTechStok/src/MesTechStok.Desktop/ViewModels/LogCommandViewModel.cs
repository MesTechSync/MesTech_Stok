using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MesTechStok.Desktop.Services;

namespace MesTechStok.Desktop.ViewModels
{
    /// <summary>
    /// ACİL LOG İYİLEŞTİRME: Log monitoring için command-line interface
    /// </summary>
    public class LogCommandViewModel : BaseViewModel
    {
        private readonly LogAnalysisService _logAnalysisService;
        private readonly ILogger<LogCommandViewModel> _logger;

        private string _commandInput = string.Empty;
        private string _commandOutput = string.Empty;
        private bool _isExecuting = false;

        public LogCommandViewModel()
        {
            var serviceProvider = App.ServiceProvider;
            _logAnalysisService = serviceProvider?.GetService<LogAnalysisService>()
                ?? throw new InvalidOperationException("LogAnalysisService servisi bulunamadı");
            _logger = serviceProvider.GetService<ILogger<LogCommandViewModel>>()
                ?? throw new InvalidOperationException("Logger servisi bulunamadı");

            ExecuteCommand = new RelayCommand(async () => await ExecuteCommandAsync(), () => !IsExecuting);
            ClearCommand = new RelayCommand(() => { CommandOutput = ""; CommandInput = ""; });

            CommandOutput = "MesTech Stok Log Monitoring Console v1.0\n" +
                           "Kullanılabilir komutlar:\n" +
                           "- analyze: Log dosyalarını analiz et\n" +
                           "- fix-encoding: UTF-8 encoding sorunlarını düzelt\n" +
                           "- help: Yardım göster\n" +
                           "- clear: Ekranı temizle\n\n";
        }

        public string CommandInput
        {
            get => _commandInput;
            set => SetProperty(ref _commandInput, value);
        }

        public string CommandOutput
        {
            get => _commandOutput;
            set => SetProperty(ref _commandOutput, value);
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                SetProperty(ref _isExecuting, value);
                ((RelayCommand)ExecuteCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand ExecuteCommand { get; }
        public ICommand ClearCommand { get; }

        private async Task ExecuteCommandAsync()
        {
            if (string.IsNullOrWhiteSpace(CommandInput) || IsExecuting) return;

            IsExecuting = true;
            var command = CommandInput.Trim().ToLowerInvariant();
            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            CommandOutput += $"[{timestamp}] > {CommandInput}\n";
            CommandInput = "";

            try
            {
                switch (command)
                {
                    case "analyze":
                        await ExecuteAnalyzeCommand();
                        break;
                    case "fix-encoding":
                        await ExecuteFixEncodingCommand();
                        break;
                    case "help":
                        ExecuteHelpCommand();
                        break;
                    case "clear":
                        CommandOutput = "";
                        break;
                    case "status":
                        ExecuteStatusCommand();
                        break;
                    default:
                        CommandOutput += $"Bilinmeyen komut: {command}\n'help' yazarak yardım alabilirsiniz.\n\n";
                        break;
                }
            }
            catch (Exception ex)
            {
                CommandOutput += $"HATA: {ex.Message}\n\n";
                _logger.LogError(ex, "Log komut çalıştırma hatası: {Command}", command);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        private async Task ExecuteAnalyzeCommand()
        {
            CommandOutput += "Log dosyaları analiz ediliyor...\n";

            var result = await _logAnalysisService.AnalyzeEncodingIssuesAsync();

            CommandOutput += $"✅ ANALIZ TAMAMLANDI\n";
            CommandOutput += $"📊 {result.TotalFilesAnalyzed} dosya analiz edildi\n";
            CommandOutput += $"🔍 {result.TotalIssues} sorun tespit edildi\n\n";

            if (result.EncodingIssues.Count > 0)
            {
                CommandOutput += "🔤 ENCODING SORUNLARI:\n";
                foreach (var issue in result.EncodingIssues)
                {
                    CommandOutput += $"  - {issue}\n";
                }
                CommandOutput += "\n";
            }

            if (result.PerformanceIssues.Count > 0)
            {
                CommandOutput += "⚡ PERFORMANS SORUNLARI:\n";
                foreach (var issue in result.PerformanceIssues)
                {
                    CommandOutput += $"  - {issue}\n";
                }
                CommandOutput += "\n";
            }

            if (result.SecurityIssues.Count > 0)
            {
                CommandOutput += "🔒 GÜVENLİK SORUNLARI:\n";
                foreach (var issue in result.SecurityIssues)
                {
                    CommandOutput += $"  - {issue}\n";
                }
                CommandOutput += "\n";
            }

            if (!result.HasIssues)
            {
                CommandOutput += "✨ Hiçbir sorun tespit edilmedi!\n\n";
            }
            else
            {
                CommandOutput += "💡 Sorunları düzeltmek için 'fix-encoding' komutunu kullanabilirsiniz.\n\n";
            }
        }

        private async Task ExecuteFixEncodingCommand()
        {
            CommandOutput += "Encoding sorunları düzeltiliyor...\n";

            var success = await _logAnalysisService.FixEncodingIssuesAsync();

            if (success)
            {
                CommandOutput += "✅ Encoding sorunları başarıyla düzeltildi!\n";
                CommandOutput += "📝 Tüm log dosyaları UTF-8 BOM ile yeniden kaydedildi.\n\n";
            }
            else
            {
                CommandOutput += "❌ Encoding düzeltme işlemi başarısız!\n";
                CommandOutput += "📋 Detaylar için application loglarını kontrol edin.\n\n";
            }
        }

        private void ExecuteHelpCommand()
        {
            CommandOutput += "📖 MesTech Stok Log Monitoring - YARDIM\n";
            CommandOutput += "==========================================\n\n";
            CommandOutput += "analyze       - Log dosyalarını analiz et (encoding, performans, güvenlik)\n";
            CommandOutput += "fix-encoding  - UTF-8 encoding sorunlarını otomatik düzelt\n";
            CommandOutput += "status        - Sistem durumu göster\n";
            CommandOutput += "clear         - Konsol ekranını temizle\n";
            CommandOutput += "help          - Bu yardım mesajını göster\n\n";
            CommandOutput += "💡 İPUCU: Komutlar büyük/küçük harf duyarlı değildir.\n\n";
        }

        private void ExecuteStatusCommand()
        {
            CommandOutput += "📊 SİSTEM DURUMU\n";
            CommandOutput += "=================\n";
            CommandOutput += $"🕒 Sistem zamanı: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n";
            CommandOutput += $"💾 Çalışma dizini: {Environment.CurrentDirectory}\n";
            CommandOutput += $"🔗 .NET Version: {Environment.Version}\n";
            CommandOutput += $"💻 İşletim sistemi: {Environment.OSVersion}\n";
            CommandOutput += $"🏠 Kullanıcı: {Environment.UserName}\n\n";
        }
    }

    /// <summary>
    /// Simple RelayCommand implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Func<Task>? _asyncExecute;
        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Func<Task> asyncExecute, Func<bool>? canExecute = null)
        {
            _asyncExecute = asyncExecute ?? throw new ArgumentNullException(nameof(asyncExecute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object? parameter)
        {
            if (_asyncExecute != null)
                await _asyncExecute();
            else
                _execute?.Invoke();
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Base ViewModel with INotifyPropertyChanged
    /// </summary>
    public class BaseViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}
