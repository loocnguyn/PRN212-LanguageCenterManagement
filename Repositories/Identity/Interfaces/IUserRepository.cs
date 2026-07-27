using BusinessObjects;

namespace Repositories;

// IUserRepository — repository contract for User persistence.

public interface IUserRepository
{
    List<User> GetAll();
    User? GetById(int id);

    /// <summary>The account whose login is this address, or null. Case-insensitive.</summary>
    User? GetByEmail(string email);

    /// <summary>Whether the address is already a login; pass the edited row's id to ignore itself.</summary>
    bool IsEmailTaken(string email, int? exceptUserId = null);

    void Save(User user);
    void Update(User user);
    void Delete(int id);
}
