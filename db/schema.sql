-- ============================================================
--  LANGUAGE CENTER MANAGEMENT SYSTEM — SCHEMA
--  Run this file first, then run seed.sql
-- ============================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'LanguageCenterDB')
BEGIN
    ALTER DATABASE LanguageCenterDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LanguageCenterDB;
END
GO

CREATE DATABASE LanguageCenterDB
    COLLATE Vietnamese_CI_AS;
GO

USE LanguageCenterDB;
GO

-- ============================================================
-- 1. AUTH & USER MANAGEMENT
-- ============================================================

CREATE TABLE Users (
    id            INT           IDENTITY(1,1) PRIMARY KEY,
    username      NVARCHAR(50)  NOT NULL UNIQUE,
    password_hash NVARCHAR(256) NOT NULL,
    role          NVARCHAR(20)  NOT NULL CHECK (role IN ('ADMIN', 'TEACHER', 'STUDENT', 'STAFF')),
    is_active     BIT           NOT NULL DEFAULT 1,
    created_at    DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Students (
    student_id    INT           IDENTITY(1,1) PRIMARY KEY,
    user_id       INT           NOT NULL UNIQUE REFERENCES Users(id),
    full_name     NVARCHAR(100) NOT NULL,
    date_of_birth DATE          NULL,
    gender        NVARCHAR(10)  NULL CHECK (gender IN ('Male', 'Female')),
    phone         NVARCHAR(20)  NULL,
    email         NVARCHAR(100) NULL,
    address       NVARCHAR(255) NULL,
    status        NVARCHAR(20)  NOT NULL DEFAULT 'ACTIVE'
                                CHECK (status IN ('ACTIVE', 'SUSPENDED', 'GRADUATED', 'DROPPED'))
);
GO

CREATE TABLE Teachers (
    teacher_id     INT           IDENTITY(1,1) PRIMARY KEY,
    user_id        INT           NOT NULL UNIQUE REFERENCES Users(id),
    full_name      NVARCHAR(100) NOT NULL,
    date_of_birth  DATE          NULL,
    gender         NVARCHAR(10)  NULL CHECK (gender IN ('Male', 'Female')),
    phone          NVARCHAR(20)  NULL,
    email          NVARCHAR(100) NULL,
    specialization NVARCHAR(100) NULL,
    degree         NVARCHAR(100) NULL,
    status         NVARCHAR(20)  NOT NULL DEFAULT 'ACTIVE'
                                 CHECK (status IN ('ACTIVE', 'ON_LEAVE', 'RESIGNED'))
);
GO

CREATE TABLE Staff (
    staff_id      INT           IDENTITY(1,1) PRIMARY KEY,
    user_id       INT           NOT NULL UNIQUE REFERENCES Users(id),
    full_name     NVARCHAR(100) NOT NULL,
    date_of_birth DATE          NULL,
    gender        NVARCHAR(10)  NULL CHECK (gender IN ('Male', 'Female')),
    phone         NVARCHAR(20)  NULL,
    email         NVARCHAR(100) NULL,
    department    NVARCHAR(100) NULL
);
GO

CREATE TABLE Admins (
    admin_id  INT           IDENTITY(1,1) PRIMARY KEY,
    user_id   INT           NOT NULL UNIQUE REFERENCES Users(id),
    full_name NVARCHAR(100) NOT NULL,
    phone     NVARCHAR(20)  NULL,
    email     NVARCHAR(100) NULL
);
GO

-- ============================================================
-- 2. COURSE & CLASS MANAGEMENT
-- ============================================================

CREATE TABLE Courses (
    course_id         INT           IDENTITY(1,1) PRIMARY KEY,
    code              NVARCHAR(20)  NOT NULL UNIQUE,
    name              NVARCHAR(150) NOT NULL,
    level             NVARCHAR(50)  NULL,
    language          NVARCHAR(50)  NOT NULL DEFAULT 'English',
    duration_sessions INT           NOT NULL DEFAULT 0,
    tuition_fee       DECIMAL(18,2) NOT NULL DEFAULT 0,
    description       NVARCHAR(MAX) NULL,
    is_active         BIT           NOT NULL DEFAULT 1,
    created_at        DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Classrooms (
    classroom_id INT           IDENTITY(1,1) PRIMARY KEY,
    name         NVARCHAR(50)  NOT NULL UNIQUE,
    capacity     INT           NOT NULL DEFAULT 30,
    location     NVARCHAR(100) NULL,
    is_active    BIT           NOT NULL DEFAULT 1
);
GO

CREATE TABLE Classes (
    class_id     INT           IDENTITY(1,1) PRIMARY KEY,
    course_id    INT           NOT NULL REFERENCES Courses(course_id),
    teacher_id   INT           NOT NULL REFERENCES Teachers(teacher_id),
    classroom_id INT           NOT NULL REFERENCES Classrooms(classroom_id),
    name         NVARCHAR(100) NOT NULL,
    max_students INT           NOT NULL DEFAULT 30,
    start_date   DATE          NULL,
    end_date     DATE          NULL,
    status       NVARCHAR(20)  NOT NULL DEFAULT 'UPCOMING'
                               CHECK (status IN ('UPCOMING', 'ONGOING', 'COMPLETED', 'CANCELLED')),
    created_at   DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO

-- day_of_week: 1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat, 7=Sun
CREATE TABLE ClassSchedules (
    schedule_id INT     IDENTITY(1,1) PRIMARY KEY,
    class_id    INT     NOT NULL REFERENCES Classes(class_id) ON DELETE CASCADE,
    day_of_week TINYINT NOT NULL CHECK (day_of_week BETWEEN 1 AND 7),
    start_time  TIME    NOT NULL,
    end_time    TIME    NOT NULL,
    CONSTRAINT chk_time CHECK (end_time > start_time)
);
GO

-- ============================================================
-- 3. ENROLLMENT & ACADEMIC RESULTS
-- ============================================================

CREATE TABLE Enrollments (
    enrollment_id INT           IDENTITY(1,1) PRIMARY KEY,
    student_id    INT           NOT NULL REFERENCES Students(student_id),
    class_id      INT           NOT NULL REFERENCES Classes(class_id),
    enrolled_date DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    status        NVARCHAR(20)  NOT NULL DEFAULT 'ACTIVE'
                                CHECK (status IN ('ACTIVE', 'DEFERRED', 'TRANSFERRED', 'COMPLETED', 'DROPPED')),
    note          NVARCHAR(255) NULL,
    CONSTRAINT uq_enrollment UNIQUE (student_id, class_id)
);
GO

CREATE TABLE GradeTypes (
    grade_type_id  INT           IDENTITY(1,1) PRIMARY KEY,
    name           NVARCHAR(100) NOT NULL,
    weight_percent DECIMAL(5,2)  NOT NULL CHECK (weight_percent BETWEEN 0 AND 100),
    description    NVARCHAR(255) NULL
);
GO

CREATE TABLE Grades (
    grade_id      INT           IDENTITY(1,1) PRIMARY KEY,
    enrollment_id INT           NOT NULL REFERENCES Enrollments(enrollment_id),
    grade_type_id INT           NOT NULL REFERENCES GradeTypes(grade_type_id),
    score         DECIMAL(5,2)  NOT NULL CHECK (score >= 0),
    max_score     DECIMAL(5,2)  NOT NULL DEFAULT 10,
    graded_at     DATETIME2     NOT NULL DEFAULT GETDATE(),
    note          NVARCHAR(255) NULL,
    CONSTRAINT uq_grade UNIQUE (enrollment_id, grade_type_id),
    CONSTRAINT chk_score CHECK (score <= max_score)
);
GO

-- ============================================================
-- 4. SESSIONS & ATTENDANCE
-- ============================================================

CREATE TABLE Sessions (
    session_id   INT           IDENTITY(1,1) PRIMARY KEY,
    class_id     INT           NOT NULL REFERENCES Classes(class_id),
    schedule_id  INT           NULL     REFERENCES ClassSchedules(schedule_id),
    session_date DATE          NOT NULL,
    topic        NVARCHAR(200) NULL,
    status       NVARCHAR(20)  NOT NULL DEFAULT 'SCHEDULED'
                               CHECK (status IN ('SCHEDULED', 'COMPLETED', 'CANCELLED', 'MAKEUP'))
);
GO

CREATE TABLE Attendances (
    attendance_id INT           IDENTITY(1,1) PRIMARY KEY,
    session_id    INT           NOT NULL REFERENCES Sessions(session_id),
    student_id    INT           NOT NULL REFERENCES Students(student_id),
    status        NVARCHAR(20)  NOT NULL DEFAULT 'PRESENT'
                                CHECK (status IN ('PRESENT', 'ABSENT', 'LATE', 'EXCUSED')),
    note          NVARCHAR(255) NULL,
    recorded_at   DATETIME2     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT uq_attendance UNIQUE (session_id, student_id)
);
GO

CREATE TABLE TeacherAttendances (
    teacher_attendance_id INT           IDENTITY(1,1) PRIMARY KEY,
    session_id            INT           NOT NULL REFERENCES Sessions(session_id),
    teacher_id            INT           NOT NULL REFERENCES Teachers(teacher_id),
    status                NVARCHAR(20)  NOT NULL DEFAULT 'PRESENT'
                                        CHECK (status IN ('PRESENT', 'ABSENT', 'SUBSTITUTE')),
    note                  NVARCHAR(255) NULL,
    CONSTRAINT uq_teacher_attendance UNIQUE (session_id, teacher_id)
);
GO

-- ============================================================
-- 5. FINANCIAL MANAGEMENT
-- ============================================================

CREATE TABLE Invoices (
    invoice_id    INT           IDENTITY(1,1) PRIMARY KEY,
    student_id    INT           NOT NULL REFERENCES Students(student_id),
    enrollment_id INT           NULL     REFERENCES Enrollments(enrollment_id),
    amount        DECIMAL(18,2) NOT NULL,
    status        NVARCHAR(20)  NOT NULL DEFAULT 'UNPAID'
                                CHECK (status IN ('UNPAID', 'PARTIAL', 'PAID', 'CANCELLED')),
    due_date      DATE          NULL,
    created_at    DATETIME2     NOT NULL DEFAULT GETDATE(),
    note          NVARCHAR(255) NULL
);
GO

CREATE TABLE Payments (
    payment_id     INT           IDENTITY(1,1) PRIMARY KEY,
    invoice_id     INT           NOT NULL REFERENCES Invoices(invoice_id),
    staff_id       INT           NULL     REFERENCES Staff(staff_id),
    amount_paid    DECIMAL(18,2) NOT NULL CHECK (amount_paid > 0),
    payment_method NVARCHAR(50)  NOT NULL DEFAULT 'Cash'
                                 CHECK (payment_method IN ('Cash', 'Transfer', 'Card')),
    paid_at        DATETIME2     NOT NULL DEFAULT GETDATE(),
    receipt_code   NVARCHAR(50)  NULL UNIQUE,
    note           NVARCHAR(255) NULL
);
GO

-- ============================================================
-- 6. INDEXES
-- ============================================================

CREATE INDEX idx_users_role         ON Users(role);
CREATE INDEX idx_students_user      ON Students(user_id);
CREATE INDEX idx_teachers_user      ON Teachers(user_id);
CREATE INDEX idx_staff_user         ON Staff(user_id);
CREATE INDEX idx_admins_user        ON Admins(user_id);
CREATE INDEX idx_classes_course     ON Classes(course_id);
CREATE INDEX idx_classes_teacher    ON Classes(teacher_id);
CREATE INDEX idx_classes_classroom  ON Classes(classroom_id);
CREATE INDEX idx_classes_status     ON Classes(status);
CREATE INDEX idx_schedule_conflict  ON ClassSchedules(class_id, day_of_week, start_time, end_time);
CREATE INDEX idx_enrollment_student ON Enrollments(student_id, status);
CREATE INDEX idx_enrollment_class   ON Enrollments(class_id, status);
CREATE INDEX idx_session_class_date ON Sessions(class_id, session_date);
CREATE INDEX idx_attend_session     ON Attendances(session_id);
CREATE INDEX idx_attend_student     ON Attendances(student_id);
CREATE INDEX idx_invoice_student    ON Invoices(student_id, status);
CREATE INDEX idx_invoice_enrollment ON Invoices(enrollment_id);
CREATE INDEX idx_payment_invoice    ON Payments(invoice_id);
GO

PRINT '==> LanguageCenterDB schema created successfully. Now run seed.sql.';
GO
