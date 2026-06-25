using BusinessObjects;

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
        var existing = context.ClassSchedules.Find(entity.ScheduleId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.ClassSchedules.Find(id);
        if (existing == null) return;
        context.ClassSchedules.Remove(existing);
        context.SaveChanges();
    }
}
