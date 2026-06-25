using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class UserDAO
{
    public static List<User> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Users.ToList();
    }

    public static User? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Users.FirstOrDefault(x => x.Id == id);
    }

    public static User? GetByUsername(string username)
    {
        using var context = new LanguageCenterContext();
        return context.Users.FirstOrDefault(u => u.Username == username);
    }

    public static void Save(User entity)
    {
        using var context = new LanguageCenterContext();
        context.Users.Add(entity);
        context.SaveChanges();
    }

    public static void Update(User entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Users.Find(entity.Id)
            ?? throw new Exception("User not found.");
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Users.Find(id)
            ?? throw new Exception("User not found.");
        context.Users.Remove(existing);
        context.SaveChanges();
    }
}
