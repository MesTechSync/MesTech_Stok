namespace MesTech.Application.Interfaces;

/// <summary>
/// WPF code-behind ve Converter sınıfları için izole DI erişim köprüsü.
/// SADECE App.xaml.cs'de kayıtlı — başka sınıflarda kullanımı yasak.
/// ENT-DROP-IMP-SPRINT-D — DEV 1 Task D-01
/// </summary>
public interface IServiceLocatorBridge
{
    /// <summary>Kayıtlı servisi al; bulunamazsa InvalidOperationException fırlatır.</summary>
    T GetRequiredService<T>() where T : notnull;

    /// <summary>Kayıtlı servisi al; bulunamazsa null döner.</summary>
    T? GetService<T>();
}
