using BusinessObjects;

namespace DataAccessObjects;

public class ClassDAO
{
    public static List<Class> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Classes.ToList();
    }

    public static Class? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Classes.FirstOrDefault(x => x.ClassId == id);
    }

    public static void Save(Class entity)
    {
        using var context = new LanguageCenterContext();
        context.Classes.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Class entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Classes.Find(entity.ClassId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Classes.Find(id);
        if (existing == null) return;
        context.Classes.Remove(existing);
        context.SaveChanges();
    }
}
