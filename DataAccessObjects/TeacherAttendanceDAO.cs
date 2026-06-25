using BusinessObjects;

namespace DataAccessObjects;

public class TeacherAttendanceDAO
{
    public static List<TeacherAttendance> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.TeacherAttendances.ToList();
    }

    public static TeacherAttendance? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.TeacherAttendances.FirstOrDefault(x => x.TeacherAttendanceId == id);
    }

    public static void Save(TeacherAttendance entity)
    {
        using var context = new LanguageCenterContext();
        context.TeacherAttendances.Add(entity);
        context.SaveChanges();
    }

    public static void Update(TeacherAttendance entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.TeacherAttendances.Find(entity.TeacherAttendanceId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.TeacherAttendances.Find(id);
        if (existing == null) return;
        context.TeacherAttendances.Remove(existing);
        context.SaveChanges();
    }
}
