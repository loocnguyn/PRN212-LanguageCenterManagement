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
    balance       DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (balance >= 0),
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

-- Staff departments. access_group decides which menu group a department's
-- staff can reach: 'ACADEMIC' (students/classes/enrollment) or 'FINANCE'
-- (invoices/payments/reports/discounts). Managed via the Departments screen.
CREATE TABLE Departments (
    department_id INT           IDENTITY(1,1) PRIMARY KEY,
    name          NVARCHAR(100) NOT NULL UNIQUE,
    access_group  NVARCHAR(20)  NOT NULL DEFAULT 'ACADEMIC'
                  CHECK (access_group IN ('ACADEMIC', 'FINANCE'))
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

CREATE TABLE Semesters (
    semester_id INT           IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(100) NOT NULL UNIQUE,
    start_date  DATE          NOT NULL,
    end_date    DATE          NOT NULL,
    is_active   BIT           NOT NULL DEFAULT 1,
    setup_end_date DATE          NULL,
    CONSTRAINT chk_semester_dates CHECK (end_date > start_date)
);
GO

CREATE TABLE Classes (
    class_id     INT           IDENTITY(1,1) PRIMARY KEY,
    semester_id  INT           NOT NULL REFERENCES Semesters(semester_id),
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

-- Configurable daily time slots (periods). Admins adjust these via Slot Time Setting;
-- a class schedule picks a day + slot and copies the slot's times into ClassSchedules.
CREATE TABLE Slots (
    slot_id    INT  IDENTITY(1,1) PRIMARY KEY,
    slot_no    INT  NOT NULL UNIQUE,
    start_time TIME NOT NULL,
    end_time   TIME NOT NULL,
    CONSTRAINT chk_slot_time CHECK (end_time > start_time)
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
    course_id      INT           NOT NULL REFERENCES Courses(course_id),
    name           NVARCHAR(100) NOT NULL,
    weight_percent DECIMAL(5,2)  NOT NULL CHECK (weight_percent BETWEEN 0 AND 100),
    description    NVARCHAR(255) NULL,
    CONSTRAINT uq_grade_type_course_name UNIQUE (course_id, name)
);
GO

CREATE INDEX idx_grade_types_course ON GradeTypes(course_id);
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

CREATE TABLE TuitionDiscounts (
    discount_id           INT           IDENTITY(1,1) PRIMARY KEY,
    code                  NVARCHAR(50)  NOT NULL UNIQUE,
    name                  NVARCHAR(150) NOT NULL,
    discount_type         NVARCHAR(20)  NOT NULL
                                        CHECK (discount_type IN ('PERCENT', 'FIXED')),
    discount_value        DECIMAL(18,2) NOT NULL CHECK (discount_value > 0),
    start_date            DATE          NULL,
    end_date              DATE          NULL,
    is_active             BIT           NOT NULL DEFAULT 1,
    note                  NVARCHAR(255) NULL,
    created_at            DATETIME2     NOT NULL DEFAULT GETDATE(),
    payment_deadline_days INT           NULL,
    condition_type        NVARCHAR(30)  NOT NULL DEFAULT 'NONE'
                                        CHECK (condition_type IN ('NONE', 'EARLY_PAYMENT')),
    CONSTRAINT chk_tuition_discount_date
        CHECK (end_date IS NULL OR start_date IS NULL OR end_date >= start_date),
    CONSTRAINT chk_tuition_discount_percent
        CHECK (discount_type <> 'PERCENT' OR discount_value <= 100),
    CONSTRAINT chk_tuition_discount_deadline
        CHECK (payment_deadline_days IS NULL OR payment_deadline_days > 0)
);
GO

-- Scholarships granted for high performance in a course. Each row links to the
-- generated discount voucher; the unique constraint stops a student being rewarded
-- twice for the same course in the same semester. Managed via the Scholarship screen.
CREATE TABLE StudentRewards (
    reward_id     INT          IDENTITY(1,1) PRIMARY KEY,
    student_id    INT          NOT NULL REFERENCES Students(student_id),
    semester_id   INT          NOT NULL REFERENCES Semesters(semester_id),
    course_id     INT          NOT NULL REFERENCES Courses(course_id),
    average_score DECIMAL(4,2) NOT NULL,
    discount_id   INT          NOT NULL REFERENCES TuitionDiscounts(discount_id),
    awarded_at    DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT uq_student_reward UNIQUE (student_id, semester_id, course_id)
);
GO

CREATE TABLE Invoices (
    invoice_id        INT           IDENTITY(1,1) PRIMARY KEY,
    student_id        INT           NOT NULL REFERENCES Students(student_id),
    enrollment_id     INT           NULL     REFERENCES Enrollments(enrollment_id),
    original_amount   DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (original_amount >= 0),
    discount_id       INT           NULL     REFERENCES TuitionDiscounts(discount_id),
    discount_amount   DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (discount_amount >= 0),
    amount            DECIMAL(18,2) NOT NULL CHECK (amount >= 0),
    status            NVARCHAR(20)  NOT NULL DEFAULT 'UNPAID'
                                     CHECK (status IN ('UNPAID', 'PARTIAL', 'PAID', 'CANCELLED')),
    due_date          DATE          NULL,
    discount_deadline DATE          NULL,
    discount_status   NVARCHAR(20)  NOT NULL DEFAULT 'NONE'
                                     CHECK (discount_status IN ('NONE', 'ACTIVE', 'LOCKED', 'EXPIRED')),
    created_at        DATETIME2     NOT NULL DEFAULT GETDATE(),
    note              NVARCHAR(255) NULL,
    CONSTRAINT chk_invoice_discount_not_over_original
        CHECK (discount_amount <= original_amount)
);
GO

CREATE TABLE Payments (
    payment_id     INT           IDENTITY(1,1) PRIMARY KEY,
    invoice_id     INT           NOT NULL REFERENCES Invoices(invoice_id),
    staff_id       INT           NULL     REFERENCES Staff(staff_id),
    amount_paid    DECIMAL(18,2) NOT NULL CHECK (amount_paid > 0),
    payment_method NVARCHAR(50)  NOT NULL DEFAULT 'Cash'
                                 CHECK (payment_method IN ('Cash', 'Transfer', 'Card', 'Wallet')),
    paid_at        DATETIME2     NOT NULL DEFAULT GETDATE(),
    receipt_code   NVARCHAR(50)  NULL UNIQUE,
    note           NVARCHAR(255) NULL
);
GO

CREATE TABLE WalletTransactions (
    transaction_id   INT           IDENTITY(1,1) PRIMARY KEY,
    student_id       INT           NOT NULL REFERENCES Students(student_id),
    amount            DECIMAL(18,2) NOT NULL CHECK (amount > 0),
    transaction_type NVARCHAR(20)  NOT NULL
                                   CHECK (transaction_type IN ('TOP_UP', 'PAYMENT', 'REFUND')),
    provider_order_id NVARCHAR(100) NULL UNIQUE,
    description      NVARCHAR(255) NULL,
    status           NVARCHAR(20)  NOT NULL DEFAULT 'PENDING'
                                   CHECK (status IN ('PENDING', 'COMPLETED', 'FAILED')),
    created_at       DATETIME2     NOT NULL DEFAULT GETDATE()
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
CREATE INDEX idx_classes_semester   ON Classes(semester_id);
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
CREATE INDEX idx_discount_active    ON TuitionDiscounts(is_active, start_date, end_date);
CREATE INDEX idx_invoice_student    ON Invoices(student_id, status);
CREATE INDEX idx_invoice_enrollment ON Invoices(enrollment_id);
CREATE INDEX idx_invoice_discount   ON Invoices(discount_id);
CREATE INDEX idx_payment_invoice    ON Payments(invoice_id);
CREATE INDEX idx_wallet_student     ON WalletTransactions(student_id);
CREATE INDEX idx_wallet_status      ON WalletTransactions(status);
GO

PRINT '==> LanguageCenterDB schema created successfully. Now run seed.sql.';
GO
