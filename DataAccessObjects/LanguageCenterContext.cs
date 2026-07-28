using System;
using System.Collections.Generic;
using System.IO;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccessObjects;

// ============================================================
//  LanguageCenterContext — the single EF Core DbContext for the
//  whole app. Every DAO news one up, does its work, disposes it.
//  CONTENTS:
//    1. Constructors        — parameterless (runtime) + options (tests)
//    2. DbSet<T> properties — one table per entity, alphabetical
//    3. OnConfiguring       — reads the connection string from appsettings.json
//    4. GetConnectionString — config lookup helper
//    5. OnModelCreating     — Fluent API: keys, indexes, column names,
//                             relationships — one Entity<T>(...) block each
//    6. OnModelCreatingPartial — hook for generated partial (if any)
// ============================================================
public partial class LanguageCenterContext : DbContext
{
    // ---- 1. Constructors ---------------------------------------
    public LanguageCenterContext()
    {
    }

    public LanguageCenterContext(DbContextOptions<LanguageCenterContext> options)
        : base(options)
    {
    }

    // ---- 2. DbSet<T> properties (one per table) ----------------
    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassTeacher> ClassTeachers { get; set; }

    public virtual DbSet<ClassGradeComponent> ClassGradeComponents { get; set; }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }

    public virtual DbSet<Classroom> Classrooms { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<GradeType> GradeTypes { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Slot> Slots { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherAttendance> TeacherAttendances { get; set; }

    public virtual DbSet<TuitionDiscount> TuitionDiscounts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    public virtual DbSet<StudentAward> StudentAwards { get; set; }

    // ---- 3. OnConfiguring (which database to talk to) ----------
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(GetConnectionString());

    // ---- 4. Connection-string lookup ---------------------------
    private static string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        return config["ConnectionStrings:DefaultConnectionString"]!;
    }

    // ---- 5. OnModelCreating (entity -> table mapping) ----------
    // Each modelBuilder.Entity<T>(...) block below configures one table:
    // primary key, unique/index constraints, and the entity-property to
    // snake_case column-name mapping. Grouped loosely by domain.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admins__43AA414174102C06");

            entity.HasIndex(e => e.UserId, "UQ__Admins__B9BE370E03615341").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_admins_user");

            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Admin)
                .HasForeignKey<Admin>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Admins__user_id__29572725");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__20D6A9680CFC29C4");

            entity.HasIndex(e => e.SessionId, "idx_attend_session");

            entity.HasIndex(e => e.StudentId, "idx_attend_student");

            entity.HasIndex(e => new { e.SessionId, e.StudentId }, "uq_attendance").IsUnique();

            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("recorded_at");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PRESENT")
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Session).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attendanc__sessi__5FB337D6");

            entity.HasOne(d => d.Student).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attendanc__stude__60A75C0F");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__FDF47986771E1F8D");

            entity.HasIndex(e => e.ClassroomId, "idx_classes_classroom");

            entity.HasIndex(e => e.CourseId, "idx_classes_course");

            entity.HasIndex(e => e.SemesterId, "idx_classes_semester");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.ClassroomId).HasColumnName("classroom_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.MaxStudents)
                .HasDefaultValue(30)
                .HasColumnName("max_students");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.IsCancelled)
                .HasDefaultValue(false)
                .HasColumnName("is_cancelled");

            // Derived from the dates in memory — see Class.Status.
            entity.Ignore(e => e.Status);
            entity.Ignore(e => e.IsOpenForEnrollment);

            // Frozen copy of the course — written once by ClassService.Create.
            entity.Property(e => e.SnapCourseCode)
                .HasMaxLength(20)
                .HasColumnName("snap_course_code");
            entity.Property(e => e.SnapCourseName)
                .HasMaxLength(150)
                .HasColumnName("snap_course_name");
            entity.Property(e => e.SnapLanguage)
                .HasMaxLength(50)
                .HasColumnName("snap_language");
            entity.Property(e => e.SnapLevel)
                .HasMaxLength(50)
                .HasColumnName("snap_level");
            entity.Property(e => e.SnapDurationSessions).HasColumnName("snap_duration_sessions");
            entity.Property(e => e.SnapTuitionFee)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("snap_tuition_fee");

            // Computed from ClassTeachers in memory — not columns.
            entity.Ignore(e => e.PrimaryTeacher);
            entity.Ignore(e => e.Teachers);
            entity.Ignore(e => e.TeacherNames);

            entity.HasOne(d => d.Semester).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Classes__semeste__37A5467C");

            entity.HasOne(d => d.Classroom).WithMany(p => p.Classes)
                .HasForeignKey(d => d.ClassroomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Classes__classro__3A81B327");

            entity.HasOne(d => d.Course).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Classes__course___38996AB5");

        });

        modelBuilder.Entity<ClassTeacher>(entity =>
        {
            entity.HasKey(e => new { e.ClassId, e.TeacherId }).HasName("pk_class_teachers");

            entity.ToTable("ClassTeachers");

            entity.HasIndex(e => e.TeacherId, "idx_class_teachers_teacher");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassTeachers)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Teacher).WithMany(p => p.ClassTeachers)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ClassGradeComponent>(entity =>
        {
            entity.HasKey(e => e.ComponentId);

            entity.ToTable("ClassGradeComponents");

            entity.HasIndex(e => e.ClassId, "idx_class_components_class");

            entity.HasIndex(e => new { e.ClassId, e.Name }, "uq_class_component_name").IsUnique();

            entity.Property(e => e.ComponentId).HasColumnName("component_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.WeightPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("weight_percent");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");

            entity.HasOne(d => d.Class).WithMany(p => p.GradeComponents)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(e => e.LanguageId);

            entity.ToTable("Languages");

            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.HasKey(e => e.LevelId);

            entity.ToTable("Levels");

            entity.HasIndex(e => e.LanguageId, "idx_levels_language");

            entity.HasIndex(e => new { e.LanguageId, e.Name }, "uq_level_language_name").IsUnique();

            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");

            entity.HasOne(d => d.Language).WithMany(p => p.Levels)
                .HasForeignKey(d => d.LanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ClassSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__ClassSch__C46A8A6FAEE60C2A");

            entity.HasIndex(e => new { e.ClassId, e.DayOfWeek, e.StartTime, e.EndTime }, "idx_schedule_conflict");

            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.StartTime).HasColumnName("start_time");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSchedules)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__ClassSche__class__412EB0B6");
        });

        modelBuilder.Entity<Slot>(entity =>
        {
            entity.HasKey(e => e.SlotId);
            entity.ToTable("Slots");
            entity.HasIndex(e => e.SlotNo).IsUnique();
            entity.Ignore(e => e.Display);
            entity.Property(e => e.SlotId).HasColumnName("slot_id");
            entity.Property(e => e.SlotNo).HasColumnName("slot_no");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId);
            entity.ToTable("Departments");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
        });

        modelBuilder.Entity<Classroom>(entity =>
        {
            entity.HasKey(e => e.ClassroomId).HasName("PK__Classroo__448E90B831D65B4A");

            entity.HasIndex(e => e.Name, "UQ__Classroo__72E12F1BA8BA2489").IsUnique();

            entity.Property(e => e.ClassroomId).HasColumnName("classroom_id");
            entity.Property(e => e.Capacity)
                .HasDefaultValue(30)
                .HasColumnName("capacity");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Location)
                .HasMaxLength(100)
                .HasColumnName("location");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Courses__8F1EF7AE79D7A60B");

            entity.HasIndex(e => e.Code, "UQ__Courses__357D4CF90FE285FB").IsUnique();

            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationSessions).HasColumnName("duration_sessions");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");

            // Null-safe display helpers over the navigations — not columns.
            entity.Ignore(e => e.LanguageName);
            entity.Ignore(e => e.LevelName);

            entity.HasOne(d => d.Language).WithMany(p => p.Courses)
                .HasForeignKey(d => d.LanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Level).WithMany(p => p.Courses)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.Property(e => e.TuitionFee)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tuition_fee");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Enrollme__6D24AA7AF66B1007");

            entity.HasIndex(e => new { e.ClassId, e.Status }, "idx_enrollment_class");

            entity.HasIndex(e => new { e.StudentId, e.Status }, "idx_enrollment_student");

            entity.HasIndex(e => new { e.StudentId, e.ClassId }, "uq_enrollment").IsUnique();

            entity.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.EnrolledDate)
                .HasDefaultValueSql("(CONVERT([date],getdate()))")
                .HasColumnName("enrolled_date");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Class).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__class__47DBAE45");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__stude__46E78A0C");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradeId).HasName("PK__Grades__3A8F732C60073A96");

            entity.HasIndex(e => new { e.EnrollmentId, e.ComponentId }, "uq_grade").IsUnique();

            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
            entity.Property(e => e.ComponentId).HasColumnName("component_id");
            entity.Property(e => e.GradedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("graded_at");
            entity.Property(e => e.MaxScore)
                .HasDefaultValue(10m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("max_score");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.Score)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("score");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Grades)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Grades__enrollme__5165187F");

            // Grades attach to the CLASS's frozen component, never the course template.
            entity.HasOne(d => d.Component).WithMany(p => p.Grades)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<GradeType>(entity =>
        {
            entity.HasKey(e => e.GradeTypeId).HasName("PK__GradeTyp__31F4E60DAE71DD85");

            entity.HasIndex(e => e.CourseId, "idx_grade_types_course");

            entity.HasIndex(e => new { e.CourseId, e.Name }, "uq_grade_type_course_name").IsUnique();

            entity.Property(e => e.GradeTypeId).HasColumnName("grade_type_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.WeightPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("weight_percent");

            entity.HasOne(d => d.Course).WithMany(p => p.GradeTypes)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GradeType__cours__GradeTypeCourse");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__F58DFD49E83980ED");

            entity.HasIndex(e => e.EnrollmentId, "idx_invoice_enrollment");

            entity.HasIndex(e => new { e.StudentId, e.Status }, "idx_invoice_student");

            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.DiscountDeadline).HasColumnName("discount_deadline");
            entity.Property(e => e.DiscountId).HasColumnName("discount_id");
            entity.Property(e => e.DiscountStatus)
                .HasMaxLength(20)
                .HasDefaultValue("NONE")
                .HasColumnName("discount_status");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.OriginalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("original_amount");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("UNPAID")
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.EnrollmentId)
                .HasConstraintName("FK__Invoices__enroll__6E01572D");

            entity.HasOne(d => d.Discount).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.DiscountId)
                .HasConstraintName("FK_Invoices_TuitionDiscounts");

            entity.HasOne(d => d.Student).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invoices__studen__6D0D32F4");
        });

        modelBuilder.Entity<TuitionDiscount>(entity =>
        {
            entity.HasKey(e => e.DiscountId);

            entity.HasIndex(e => e.Code).IsUnique();

            entity.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate }, "idx_discount_active");

            entity.Property(e => e.DiscountId).HasColumnName("discount_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("discount_value");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.PaymentDeadlineDays).HasColumnName("payment_deadline_days");
            entity.Property(e => e.ConditionType)
                .HasMaxLength(30)
                .HasDefaultValue("NONE")
                .HasColumnName("condition_type");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__ED1FC9EA760D6510");

            entity.HasIndex(e => e.ReceiptCode, "UQ__Payments__5E03528E0ACCB334").IsUnique();

            entity.HasIndex(e => e.InvoiceId, "idx_payment_invoice");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.AmountPaid)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount_paid");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValue("Cash")
                .HasColumnName("payment_method");
            entity.Property(e => e.ReceiptCode)
                .HasMaxLength(50)
                .HasColumnName("receipt_code");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__invoic__74AE54BC");

            entity.HasOne(d => d.Staff).WithMany(p => p.Payments)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__Payments__staff___75A278F5");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__Sessions__69B13FDC62A37B01");

            entity.HasIndex(e => new { e.ClassId, e.SessionDate }, "idx_session_class_date");

            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.SessionDate).HasColumnName("session_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("SCHEDULED")
                .HasColumnName("status");
            entity.Property(e => e.Topic)
                .HasMaxLength(200)
                .HasColumnName("topic");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.RoomChangeNote)
                .HasMaxLength(255)
                .HasColumnName("room_change_note");
            entity.Ignore(e => e.EffectiveRoomName);
            entity.Ignore(e => e.HasRoomOverride);

            entity.HasOne(d => d.Class).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sessions__class___59063A47");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("FK__Sessions__schedu__59FA5E80");

            entity.HasOne(d => d.Room).WithMany()
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__1963DD9C38F5AC6F");

            entity.HasIndex(e => e.UserId, "UQ__Staff__B9BE370E1657D607").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_staff_user");

            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasColumnName("department");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Staff)
                .HasForeignKey<Staff>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Staff__user_id__24927208");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__2A33069A0205CD1C");

            entity.HasIndex(e => e.UserId, "UQ__Students__B9BE370EE7829AC5").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_students_user");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("balance");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Students__user_i__173876EA");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.TeacherId).HasName("PK__Teachers__03AE777EB212A258");

            entity.HasIndex(e => e.UserId, "UQ__Teachers__B9BE370E81470FCD").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_teachers_user");

            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Degree)
                .HasMaxLength(100)
                .HasColumnName("degree");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Specialization)
                .HasMaxLength(100)
                .HasColumnName("specialization");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Teacher)
                .HasForeignKey<Teacher>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Teachers__user_i__1DE57479");
        });

        modelBuilder.Entity<TeacherAttendance>(entity =>
        {
            entity.HasKey(e => e.TeacherAttendanceId).HasName("PK__TeacherA__EEAE1048981E6DBE");

            entity.HasIndex(e => new { e.SessionId, e.TeacherId }, "uq_teacher_attendance").IsUnique();

            entity.Property(e => e.TeacherAttendanceId).HasColumnName("teacher_attendance_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PRESENT")
                .HasColumnName("status");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");

            entity.HasOne(d => d.Session).WithMany(p => p.TeacherAttendances)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TeacherAt__sessi__6754599E");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherAttendances)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TeacherAt__teach__68487DD7");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3213E83F64D5B981");

            entity.HasIndex(e => e.Email, "UQ__Users__Email").IsUnique();

            entity.HasIndex(e => e.Role, "idx_users_role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MustChangePassword)
                .HasDefaultValue(false)
                .HasColumnName("must_change_password");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(256)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("PK__Semester__DF0A8A91");

            entity.HasIndex(e => e.Name, "UQ__Semester__Name").IsUnique();

            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.SetupEndDate).HasColumnName("setup_end_date");

            // Activeness is derived from the dates, not stored — see Semester.IsActive.
            // The legacy is_active column is left in place for older databases but is
            // no longer read or written (it has a DEFAULT, so inserts are unaffected).
            entity.Ignore(e => e.IsActive);
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__WalletTr__55F68FC0");

            // Filtered, matching schema.sql: unique per ZaloPay order, but the many
            // rows with no order id at all (spends, refunds) are not duplicates.
            entity.HasIndex(e => e.ProviderOrderId, "UQ__WalletTr__ProviderOrderId")
                .IsUnique()
                .HasFilter("[provider_order_id] IS NOT NULL");

            entity.HasIndex(e => e.StudentId, "idx_wallet_student");

            entity.HasIndex(e => e.Status, "idx_wallet_status");

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .HasColumnName("transaction_type");
            entity.Property(e => e.ProviderOrderId)
                .HasMaxLength(100)
                .HasColumnName("provider_order_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Student).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WalletTra__stude__WalletStudent");
        });

        modelBuilder.Entity<StudentAward>(entity =>
        {
            entity.HasKey(e => e.AwardId).HasName("PK__StudentAwards__AwardId");

            // The reason this table exists. Awarding is not idempotent the way
            // reading a ranking is: press twice and a student is paid twice, so
            // the second press is refused here rather than by a screen.
            entity.HasIndex(e => new { e.StudentId, e.SemesterId }, "uq_award_per_student_semester")
                .IsUnique();

            // uq_award_per_student_semester leads on student_id, so it cannot serve
            // "everyone awarded this term" — which is what the ranking screen asks.
            entity.HasIndex(e => e.SemesterId, "idx_awards_semester");

            entity.Property(e => e.AwardId).HasColumnName("award_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.AverageScore)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("average_score");
            entity.Property(e => e.Threshold)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("threshold");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.AwardedBy).HasColumnName("awarded_by");
            entity.Property(e => e.AwardedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("awarded_at");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");

            // Computed for display only — nothing behind them in the table.
            entity.Ignore(e => e.StudentName);
            entity.Ignore(e => e.SemesterName);
            entity.Ignore(e => e.AmountDisplay);

            entity.HasOne(d => d.Student).WithMany()
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK__StudentAw__stude__AwardStudent");

            entity.HasOne(d => d.Semester).WithMany()
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK__StudentAw__semes__AwardSemester");

            entity.HasOne(d => d.Transaction).WithMany()
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK__StudentAw__trans__AwardTransaction");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
