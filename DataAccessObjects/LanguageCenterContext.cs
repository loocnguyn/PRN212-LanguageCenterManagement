using System;
using System.Collections.Generic;
using System.IO;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccessObjects;

public partial class LanguageCenterContext : DbContext
{
    public LanguageCenterContext()
    {
    }

    public LanguageCenterContext(DbContextOptions<LanguageCenterContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }

    public virtual DbSet<Classroom> Classrooms { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<GradeType> GradeTypes { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherAttendance> TeacherAttendances { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(GetConnectionString());

    private static string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        return config["ConnectionStrings:DefaultConnectionString"]!;
    }

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

            entity.HasIndex(e => e.Status, "idx_classes_status");

            entity.HasIndex(e => e.TeacherId, "idx_classes_teacher");

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
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("UPCOMING")
                .HasColumnName("status");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");

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

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Classes__teacher__398D8EEE");
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
            entity.Property(e => e.Language)
                .HasMaxLength(50)
                .HasDefaultValue("English")
                .HasColumnName("language");
            entity.Property(e => e.Level)
                .HasMaxLength(50)
                .HasColumnName("level");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
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

            entity.HasIndex(e => new { e.EnrollmentId, e.GradeTypeId }, "uq_grade").IsUnique();

            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
            entity.Property(e => e.GradeTypeId).HasColumnName("grade_type_id");
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

            entity.HasOne(d => d.GradeType).WithMany(p => p.Grades)
                .HasForeignKey(d => d.GradeTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Grades__grade_ty__52593CB8");
        });

        modelBuilder.Entity<GradeType>(entity =>
        {
            entity.HasKey(e => e.GradeTypeId).HasName("PK__GradeTyp__31F4E60DAE71DD85");

            entity.Property(e => e.GradeTypeId).HasColumnName("grade_type_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.WeightPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("weight_percent");
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
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("UNPAID")
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.EnrollmentId)
                .HasConstraintName("FK__Invoices__enroll__6E01572D");

            entity.HasOne(d => d.Student).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invoices__studen__6D0D32F4");
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

            entity.HasOne(d => d.Class).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sessions__class___59063A47");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("FK__Sessions__schedu__59FA5E80");
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

            entity.HasIndex(e => e.Username, "UQ__Users__F3DBC572EC69ECFB").IsUnique();

            entity.HasIndex(e => e.Role, "idx_users_role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(256)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
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
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
