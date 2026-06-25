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
        var existing = context.ClassSchedules.Find(entity.ScheduleId)
            ?? throw new Exception("ClassSchedule not found.");
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.ClassSchedules.Find(id)
            ?? throw new Exception("ClassSchedule not found.");
        context.ClassSchedules.Remove(existing);
        context.SaveChanges();
    }
}
