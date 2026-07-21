using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

// ============================================================
//  SessionDAO — the dated class meetings (auto-generated from schedules).
//  CONTENTS:
//    1. CRUD       — GetAll/GetById/Save/Update/Delete
//    2. BulkSave   — insert many generated sessions in one call
//    3. Queries    — by class / by classes / with details; CountByClassId
// ============================================================
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
        var existing = context.Sessions.Find(entity.SessionId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Sessions.Find(id);
        if (existing == null) return;
        context.Sessions.Remove(existing);
        context.SaveChanges();
    }

    public static List<Session> GetByClassId(int classId)
    {
        using var context = new LanguageCenterContext();
        return context.Sessions.Where(s => s.ClassId == classId).ToList();
    }

    public static int CountByClassId(int classId)
    {
        using var context = new LanguageCenterContext();
        return context.Sessions.Count(s => s.ClassId == classId);
    }

    public static void BulkSave(List<Session> sessions)
    {
        using var context = new LanguageCenterContext();
        context.Sessions.AddRange(sessions);
        context.SaveChanges();
    }

    public static List<Session> GetByClassIds(List<int> classIds)
    {
        using var context = new LanguageCenterContext();
        return context.Sessions
            .Where(s => classIds.Contains(s.ClassId))
            .Include(s => s.Class)
                .ThenInclude(c => c.Course)
            .Include(s => s.Class)
                .ThenInclude(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
            .Include(s => s.Class)
                .ThenInclude(c => c.Classroom)
            .Include(s => s.Schedule)
            .Include(s => s.Attendances)
            .Include(s => s.TeacherAttendances)
            .OrderBy(s => s.SessionDate)
            .ToList();
    }

    public static List<Session> GetByClassIdWithDetails(int classId)
    {
        using var context = new LanguageCenterContext();
        return context.Sessions
            .Where(s => s.ClassId == classId)
            .Include(s => s.Class)
                .ThenInclude(c => c.Course)
            .Include(s => s.Class)
                .ThenInclude(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
            .Include(s => s.Class)
                .ThenInclude(c => c.Classroom)
            .Include(s => s.Schedule)
            .OrderBy(s => s.SessionDate)
            .ToList();
    }
}
