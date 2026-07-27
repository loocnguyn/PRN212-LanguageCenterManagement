using BusinessObjects;

namespace DataAccessObjects;

// TeacherAttendanceDAO — EF data access for TeacherAttendance (CRUD + queries).

public class TeacherAttendanceDAO
{
    /// <summary>
    /// The teachers who have a recorded attendance against any session of this class —
    /// i.e. the ones who actually stood in front of it. Taking such a teacher off the
    /// class would leave that history pointing at nobody.
    /// </summary>
    public static List<int> GetTeacherIdsWithAttendance(int classId)
    {
        using var context = new LanguageCenterContext();
        return context.TeacherAttendances
            .Where(ta => ta.Session.ClassId == classId)
            .Select(ta => ta.TeacherId)
            .Distinct()
            .ToList();
    }

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
