using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class SessionDAO
{
    public static List<Session> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Sessions.ToList();
    }

    public static Session? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Sessions.FirstOrDefault(x => x.SessionId == id);
    }

    public static void Save(Session entity)
    {
        using var context = new LanguageCenterContext();
        context.Sessions.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Session entity)
    {
        using var context = new LanguageCenterContext();
        context.Sessions.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Sessions.Find(id);
        if (entity != null)
        {
            context.Sessions.Remove(entity);
            context.SaveChanges();
        }
    }
}

