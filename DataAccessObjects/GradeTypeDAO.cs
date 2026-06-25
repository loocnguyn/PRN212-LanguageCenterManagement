using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class GradeTypeDAO
{
    public static List<GradeType> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.GradeTypes.ToList();
    }

    public static GradeType? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.GradeTypes.FirstOrDefault(x => x.GradeTypeId == id);
    }

    public static void Save(GradeType entity)
    {
        using var context = new LanguageCenterContext();
        context.GradeTypes.Add(entity);
        context.SaveChanges();
    }

    public static void Update(GradeType entity)
    {
        using var context = new LanguageCenterContext();
        context.GradeTypes.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.GradeTypes.Find(id);
        if (entity != null)
        {
            context.GradeTypes.Remove(entity);
            context.SaveChanges();
        }
    }
}

