using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// Windows Forms bağımlılığını kaldırmak için minimal Keys enum'u içeri alıyoruz
using Keys = MesTechStok.Core.Integrations.Barcode.Models.VirtualKeys;
using MesTechStok.Core.Integrations.Barcode.Models;

namespace MesTechStok.Core.Integrations.Barcode
{
    /// <summary>
    /// HID barkod tarayıcıları için gerçek hardware listener.
    /// Windows API kullanarak USB HID cihazlarını dinler.
    /// </summary>
    public class HidBarcodeListener : IDisposable
    {
        private readonly object _lockObject = new object();
        private bool _isListening = false;
        private bool _disposed = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private readonly List<string> _barcodeBuffer = new List<string>();
        private DateTime _lastInputTime = DateTime.MinValue;
        private readonly TimeSpan _barcodeTimeout = TimeSpan.FromMilliseconds(100);

        // Windows API için P/Invoke tanımlamaları
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static HidBarcodeListener? _instance;

        public event EventHandler<string>? BarcodeReceived;
        public event EventHandler<Exception>? Error;

        public bool IsListening => _isListening;

        public HidBarcodeListener()
        {
            _instance = this;
        }

        /// <summary>
        /// HID dinleyicisini başlatır
        /// </summary>
        public async Task<bool> StartListeningAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[HID] StartListeningAsync called");

                if (_isListening)
                {
                    System.Diagnostics.Debug.WriteLine("[HID] Already listening, returning true");
                    return true;
                }

                _cancellationTokenSource = new CancellationTokenSource();

                // Keyboard hook'u kur
                System.Diagnostics.Debug.WriteLine("[HID] Setting up keyboard hook...");
                _hookID = SetHook(_proc);

                _isListening = true;

                System.Diagnostics.Debug.WriteLine($"[HID] Hook ID: {_hookID}, IsListening: {_isListening}");

                // Barkod buffer'ını işleyen task'ı başlat
                _listenerTask = Task.Run(ProcessBarcodeBuffer, _cancellationTokenSource.Token);

                var success = _hookID != IntPtr.Zero;
                System.Diagnostics.Debug.WriteLine($"[HID] ✅ StartListeningAsync result: {success}");

                return success;
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
                return false;
            }
        }

        /// <summary>
        /// HID dinleyicisini durdurur
        /// </summary>
        public async Task<bool> StopListeningAsync()
        {
            try
            {
                if (!_isListening) return true;

                _isListening = false;
                _cancellationTokenSource?.Cancel();

                // Hook'u kaldır
                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                    _hookID = IntPtr.Zero;
                }

                if (_listenerTask != null)
                {
                    await _listenerTask;
                    _listenerTask = null;
                }

                return true;
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
                return false;
            }
        }

        /// <summary>
        /// Keyboard hook kurulumu
        /// </summary>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule?.ModuleName), 0);
            }
        }

        /// <summary>
        /// Keyboard callback - her tuşa basıldığında çağrılır
        /// </summary>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && _instance != null)
                {
                    var vkCode = Marshal.ReadInt32(lParam);
                    var key = (Keys)vkCode;

                    _instance.ProcessKeyInput(key);
                }
            }
            catch (Exception ex)
            {
                _instance?.Error?.Invoke(_instance, ex);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Tuş girişini işler ve barkod buffer'ına ekler
        /// </summary>
        private void ProcessKeyInput(Keys key)
        {
            lock (_lockObject)
            {
                var now = DateTime.Now;

                // DEBUG: Tuş girişi logla
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[HID] Key received: {key} ({(int)key})");
                }
                catch { }

                // Eğer son girişten çok zaman geçtiyse, buffer'ı temizle
                if (now - _lastInputTime > _barcodeTimeout)
                {
                    if (_barcodeBuffer.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HID] Buffer timeout, cleared {_barcodeBuffer.Count} chars");
                    }
                    _barcodeBuffer.Clear();
                }

                _lastInputTime = now;

                // Enter tuşu barkod sonunu işaret eder
                if (key == Keys.Enter)
                {
                    if (_barcodeBuffer.Count > 0)
                    {
                        var barcode = string.Join("", _barcodeBuffer);
                        _barcodeBuffer.Clear();

                        System.Diagnostics.Debug.WriteLine($"[HID] Barcode completed: '{barcode}' (Length: {barcode.Length})");

                        // Barkod minimum uzunluk kontrolü
                        if (barcode.Length >= 3)
                        {
                            System.Diagnostics.Debug.WriteLine($"[HID] ✅ Firing BarcodeReceived event for: {barcode}");
                            Task.Run(() => BarcodeReceived?.Invoke(this, barcode));
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[HID] ❌ Barcode too short, ignoring: {barcode}");
                        }
                    }
                    return;
                }

                // Geçerli karakter ise buffer'a ekle
                var character = GetCharFromKey(key);
                if (!string.IsNullOrEmpty(character))
                {
                    _barcodeBuffer.Add(character);
                    System.Diagnostics.Debug.WriteLine($"[HID] Added char: '{character}', Buffer size: {_barcodeBuffer.Count}");
                }
            }
        }

        /// <summary>
        /// Barkod buffer'ını sürekli işleyen method
        /// </summary>
        private async Task ProcessBarcodeBuffer()
        {
            while (!_cancellationTokenSource?.Token.IsCancellationRequested == true)
            {
                try
                {
                    await Task.Delay(50, _cancellationTokenSource.Token);

                    lock (_lockObject)
                    {
                        var now = DateTime.Now;

                        // Timeout kontrolü
                        if (_barcodeBuffer.Count > 0 && now - _lastInputTime > _barcodeTimeout)
                        {
                            // Eksik barkod - temizle
                            _barcodeBuffer.Clear();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, ex);
                }
            }
        }

        /// <summary>
        /// Virtual key code'dan karakter çevirir
        /// </summary>
        private string GetCharFromKey(Keys key)
        {
            // Sayılar
            if (key >= Keys.D0 && key <= Keys.D9)
                return ((int)(key - Keys.D0)).ToString();

            // NumPad sayılar
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return ((int)(key - Keys.NumPad0)).ToString();

            // Harfler
            if (key >= Keys.A && key <= Keys.Z)
                return key.ToString();

            // Özel karakterler
            return key switch
            {
                Keys.Space => " ",
                Keys.OemMinus => "-",
                Keys.OemPlus => "+",
                Keys.OemPeriod => ".",
                Keys.Oemcomma => ",",
                Keys.OemQuestion => "/",
                Keys.OemSemicolon => ";",
                Keys.OemQuotes => "'",
                Keys.OemOpenBrackets => "[",
                Keys.OemCloseBrackets => "]",
                Keys.OemPipe => "\\",
                _ => string.Empty
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _ = StopListeningAsync();
                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }
    }
}
