using System;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs; //Wer neue Ordner erstell, muss sie auch hier importieren


namespace ShopBackend.Application.Interfaces
{
    public interface IUserService
    {
        // Task ist ein generischer Typ, der eine asynchrone Operation repräsentiert, die einen Wert zurückgibt.
        // In diesem Fall gibt GetByIdAsync einen Task zurück, der ein User-Objekt enthält, wenn die Operation abgeschlossen ist. Das bedeutet, dass die Methode asynchron ausgeführt wird und der Aufrufer warten kann, bis das Ergebnis verfügbar ist, ohne den Hauptthread zu blockieren.
        
        // Das in der Klammer <> sind die Rückgabewerte, das hinter dem Methodennamen die Abzufragenden Parameter
        Task<User> GetByIdAsync(int id);
        Task<IEnumerable<User>> GetAllAsync(); // man könnte auch List statt IEnumerable verwenden, aber die liste wäre weniger restriktiv,da sie mehr operationen unterstützt, wie z.B. hinzufügen oder entfernen.
        Task<User> CreateAsync(CreateUserDto dto);  // Kapselung der Benutzerdaten statt über {User User} alle Einträge möglich zu machen für die Aktionen Create, Update und ChangePassword in DTOs, damit mir kein User einfach die Rolle Admin setzen kann und damit die Datenstruktur klarer und sicherer ist.
        Task UpdateAsync (int id, UpdateUserDto dto);
        Task ChangePasswordAsync (int id, ChangePasswordDto dto);
        Task DeleteAsync (int id);
        Task<LoginResponseDto> LoginAsync(LoginDto dto);
        
    }
}
