using BusinessObjects;
using Microsoft.EntityFrameworkCore;

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
        context.TeacherAttendances.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.TeacherAttendances.Find(id);
        if (entity != null)
        {
            context.TeacherAttendances.Remove(entity);
            context.SaveChanges();
        }
    }
}

