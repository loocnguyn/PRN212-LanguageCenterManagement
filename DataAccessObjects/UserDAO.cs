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
        return context.Users.FirstOrDefault(u => u.Id == id);
    }

    public static User? GetByUsername(string username)
    {
        using var context = new LanguageCenterContext();
        return context.Users.FirstOrDefault(u => u.Username == username);
    }

    public static void Save(User user)
    {
        using var context = new LanguageCenterContext();
        context.Users.Add(user);
        context.SaveChanges();
    }

    public static void Update(User user)
    {
        using var context = new LanguageCenterContext();
        context.Users.Update(user);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var user = context.Users.Find(id);
        if (user != null)
        {
            context.Users.Remove(user);
            context.SaveChanges();
        }
    }
}
