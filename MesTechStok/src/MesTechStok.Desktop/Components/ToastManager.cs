using System;
using MesTechStok.Desktop;
using MesTechStok.Desktop.Components;

namespace MesTechStok.Desktop
{
    // ... (existing code)
}

public static class ToastManager
{
    public static event EventHandler<ToastEventArgs>? ToastRequested;

    public static void Show(string message, string title = "", string type = "Info")
    {
        ToastRequested?.Invoke(null, new ToastEventArgs(message, title, type));
    }

    public static void ShowError(string message, string title = "Hata") => Show(message, title, "Error");
    public static void ShowSuccess(string message, string title = "Başarılı") => Show(message, title, "Success");
    public static void ShowWarning(string message, string title = "Uyarı") => Show(message, title, "Warning");
    public static void ShowInfo(string message, string title = "Bilgi") => Show(message, title, "Info");
}