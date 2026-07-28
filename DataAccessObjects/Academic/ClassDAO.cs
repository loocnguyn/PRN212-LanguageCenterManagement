using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

// ============================================================
//  ClassDAO — course offerings for a semester (a class = a run of a course).
//  CONTENTS:
//    1. CRUD                      — GetAll/GetById/Save/Update/Delete
//    2. GetBySemesterId(+Details) — classes in a semester; Details eager-loads
//                                   Course/Teacher/Classroom navigation props
//    3. UpdateStatus              — set ONGOING/COMPLETED/CANCELLED
// ============================================================
public class ClassDAO
{
    public static List<Class> GetAll()
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Classes
                .Include(c => c.Course)
                .Include(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
                .Include(c => c.Classroom)
                .ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving classes: {ex.Message}", ex);
        }
    }

    public static Class? GetById(int id)
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Classes
                .Include(c => c.Course)
                .Include(c => c.Classroom)
                .Include(c => c.Semester)
                .Include(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
                .Include(c => c.GradeComponents)
                .FirstOrDefault(x => x.ClassId == id);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving class {id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a class by SNAPSHOTTING <paramref name="courseId"/>: the course's pricing,
    /// duration, language/level and grading structure are copied onto the class and frozen.
    /// Class, teacher assignments and grade components are written in one transaction so a
    /// class can never exist without the structure its grades will hang off.
    /// </summary>
    /// <param name="teacherIds">Teachers to assign; the first is treated as primary unless
    /// <paramref name="primaryTeacherId"/> says otherwise.</param>
    public static int CreateWithSnapshot(Class entity, int courseId, IList<int> teacherIds, int? primaryTeacherId)
    {
        if (teacherIds == null || teacherIds.Count == 0)
            throw new ArgumentException("A class needs at least one teacher.");

        using var context = new LanguageCenterContext();
        using var tx = context.Database.BeginTransaction();
        try
        {
            var course = context.Courses
                .Include(c => c.Language)
                .Include(c => c.Level)
                .FirstOrDefault(c => c.CourseId == courseId)
                ?? throw new Exception($"Course {courseId} not found.");

            var template = context.GradeTypes
                .Where(g => g.CourseId == courseId)
                .OrderBy(g => g.GradeTypeId)
                .ToList();

            if (template.Count == 0)
                throw new Exception(
                    $"Course '{course.Name}' has no grading structure yet. " +
                    "Define its grade components before opening a class.");

            // Freeze the course onto the class.
            entity.CourseId = courseId;
            entity.SnapCourseCode = course.Code;
            entity.SnapCourseName = course.Name;
            entity.SnapLanguage = course.Language?.Name ?? "";
            entity.SnapLevel = course.Level?.Name;
            entity.SnapDurationSessions = course.DurationSessions;
            entity.SnapTuitionFee = course.TuitionFee;

            Validate(entity);
            context.Classes.Add(entity);
            context.SaveChanges(); // need the identity for the child rows

            var primary = primaryTeacherId ?? teacherIds[0];
            foreach (var teacherId in teacherIds.Distinct())
            {
                context.ClassTeachers.Add(new ClassTeacher
                {
                    ClassId = entity.ClassId,
                    TeacherId = teacherId,
                    IsPrimary = teacherId == primary
                });
            }

            var order = 1;
            foreach (var t in template)
            {
                context.ClassGradeComponents.Add(new ClassGradeComponent
                {
                    ClassId = entity.ClassId,
                    Name = t.Name,
                    WeightPercent = t.WeightPercent,
                    Description = t.Description,
                    SortOrder = order++
                });
            }

            context.SaveChanges();
            tx.Commit();
            return entity.ClassId;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            throw new Exception($"Error creating class: {ex.Message}", ex);
        }
    }

    /// <summary>Replaces a class's teacher assignments. The snapshot fields are untouched.</summary>
    public static void SetTeachers(int classId, IList<int> teacherIds, int? primaryTeacherId)
    {
        if (teacherIds == null || teacherIds.Count == 0)
            throw new ArgumentException("A class needs at least one teacher.");

        using var context = new LanguageCenterContext();
        using var tx = context.Database.BeginTransaction();
        try
        {
            var existing = context.ClassTeachers.Where(ct => ct.ClassId == classId).ToList();
            context.ClassTeachers.RemoveRange(existing);
            context.SaveChanges(); // clear first so the one-primary index can't trip mid-way

            var primary = primaryTeacherId ?? teacherIds[0];
            foreach (var teacherId in teacherIds.Distinct())
            {
                context.ClassTeachers.Add(new ClassTeacher
                {
                    ClassId = classId,
                    TeacherId = teacherId,
                    IsPrimary = teacherId == primary
                });
            }

            context.SaveChanges();
            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            throw new Exception($"Error updating teachers for class {classId}: {ex.Message}", ex);
        }
    }

    /// <summary>The class's frozen grading structure, in display order.</summary>
    public static List<ClassGradeComponent> GetGradeComponents(int classId)
    {
        using var context = new LanguageCenterContext();
        return context.ClassGradeComponents
            .Where(c => c.ClassId == classId)
            .OrderBy(c => c.SortOrder)
            .ToList();
    }

    public static void Save(Class entity)
    {
        try
        {
            Validate(entity);
            using var context = new LanguageCenterContext();
            context.Classes.Add(entity);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving class: {ex.Message}", ex);
        }
    }

    public static void Update(Class entity)
    {
        try
        {
            Validate(entity);
            using var context = new LanguageCenterContext();
            var existing = context.Classes.Find(entity.ClassId);
            if (existing == null)
                throw new Exception($"Class with ID {entity.ClassId} not found for update.");

            // SetValues would happily copy whatever the caller left in the Snap*
            // fields over the frozen ones. The snapshot is immutable, so restore it
            // (along with CreatedAt) after the bulk copy — editing a class can change
            // its name, room, dates and capacity, never what it was sold as.
            var originalCreatedAt = existing.CreatedAt;
            var snapCode = existing.SnapCourseCode;
            var snapName = existing.SnapCourseName;
            var snapLanguage = existing.SnapLanguage;
            var snapLevel = existing.SnapLevel;
            var snapDuration = existing.SnapDurationSessions;
            var snapFee = existing.SnapTuitionFee;
            var originalCourseId = existing.CourseId;

            context.Entry(existing).CurrentValues.SetValues(entity);

            existing.CreatedAt = originalCreatedAt;
            existing.CourseId = originalCourseId;
            existing.SnapCourseCode = snapCode;
            existing.SnapCourseName = snapName;
            existing.SnapLanguage = snapLanguage;
            existing.SnapLevel = snapLevel;
            existing.SnapDurationSessions = snapDuration;
            existing.SnapTuitionFee = snapFee;

            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating class {entity.ClassId}: {ex.Message}", ex);
        }
    }

    public static void Delete(int id)
    {
        try
        {
            using var context = new LanguageCenterContext();
            var existing = context.Classes.Find(id);
            if (existing == null)
                throw new Exception($"Class with ID {id} not found for delete.");
            context.Classes.Remove(existing);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting class {id}: {ex.Message}", ex);
        }
    }

    public static List<Class> GetBySemesterId(int semesterId)
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Classes
                .Where(c => c.SemesterId == semesterId)
                .Include(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
                .Include(c => c.Classroom)
                .ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving classes for semester {semesterId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Cancels or reinstates a class. UPCOMING / ONGOING / COMPLETED are not settable:
    /// they follow the class's dates (see Class.Status), so cancellation is the only
    /// status an operator actually decides.
    /// </summary>
    public static void SetCancelled(int classId, bool cancelled)
    {
        try
        {
            using var context = new LanguageCenterContext();
            var existing = context.Classes.Find(classId);
            if (existing == null)
                throw new Exception($"Class with ID {classId} not found.");
            existing.IsCancelled = cancelled;
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating class {classId}: {ex.Message}", ex);
        }
    }

    public static List<Class> GetBySemesterIdWithDetails(int semesterId)
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Classes
                .Where(c => c.SemesterId == semesterId)
                .Include(c => c.Course)
                .Include(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
                .Include(c => c.Classroom)
                .Include(c => c.ClassSchedules)
                .ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving classes with details for semester {semesterId}: {ex.Message}", ex);
        }
    }

    public static List<Class> GetClassesForTeacher(int teacherId, int semesterId)
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Classes
                .Where(c => c.SemesterId == semesterId && c.ClassTeachers.Any(ct => ct.TeacherId == teacherId))
                .Include(c => c.Course)
                .Include(c => c.Classroom)
                .Include(c => c.ClassSchedules)
                .Include(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
                .ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving classes for teacher {teacherId} in semester {semesterId}: {ex.Message}", ex);
        }
    }

    public static List<Course> GetCoursesForTeacher(int teacherId, int semesterId)
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Classes
                .Where(c => c.SemesterId == semesterId && c.ClassTeachers.Any(ct => ct.TeacherId == teacherId))
                .Select(c => c.Course)
                .Where(c => c != null)
                .Distinct()
                .OrderBy(c => c!.Name)
                .ToList()!;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving courses for teacher {teacherId} in semester {semesterId}: {ex.Message}", ex);
        }
    }

    private static void Validate(Class entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("Class name is required.");

        if (entity.MaxStudents <= 0)
            throw new ArgumentException("MaxStudents must be greater than 0.");

        // No status check: it is derived from the dates below, not supplied.
        if (entity.StartDate > entity.EndDate)
            throw new ArgumentException("Start date cannot be later than end date.");
    }
}
