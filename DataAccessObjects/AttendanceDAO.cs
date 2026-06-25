using BusinessObjects;

namespace DataAccessObjects;

public class AttendanceDAO
{
    public static List<Attendance> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Attendances.ToList();
    }

    public static Attendance? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Attendances.FirstOrDefault(x => x.AttendanceId == id);
    }

    public static void Save(Attendance entity)
    {
        using var context = new LanguageCenterContext();
        context.Attendances.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Attendance entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Attendances.Find(entity.AttendanceId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Attendances.Find(id);
        if (existing == null) return;
        context.Attendances.Remove(existing);
        context.SaveChanges();
    }
}
