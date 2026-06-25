using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class ClassScheduleDAO
{
    public static List<ClassSchedule> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.ClassSchedules.ToList();
    }

    public static ClassSchedule? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.ClassSchedules.FirstOrDefault(x => x.ScheduleId == id);
    }

    public static void Save(ClassSchedule entity)
    {
        using var context = new LanguageCenterContext();
        context.ClassSchedules.Add(entity);
        context.SaveChanges();
    }

    public static void Update(ClassSchedule entity)
    {
        using var context = new LanguageCenterContext();
        context.ClassSchedules.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.ClassSchedules.Find(id);
        if (entity != null)
        {
            context.ClassSchedules.Remove(entity);
            context.SaveChanges();
        }
    }
}

