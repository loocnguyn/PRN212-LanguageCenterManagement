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
        var existing = context.Sessions.Find(entity.SessionId)
            ?? throw new Exception("Session not found.");
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Sessions.Find(id)
            ?? throw new Exception("Session not found.");
        context.Sessions.Remove(existing);
        context.SaveChanges();
    }
}
