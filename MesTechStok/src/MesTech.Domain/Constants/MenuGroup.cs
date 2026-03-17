namespace MesTech.Domain.Constants;

/// <summary>
/// Menu grubu kayit tipi.
/// EMR: ENT-M4-MENU-v2
/// </summary>
public record MenuGroup(
    int Id,
    string Name,
    string FontAwesomeIcon,
    string MaterialIcon,
    string[] Items);
