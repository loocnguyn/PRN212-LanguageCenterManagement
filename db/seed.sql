-- ============================================================
--  LANGUAGE CENTER MANAGEMENT SYSTEM — SEED DATA
--  Run schema.sql first before running this file
--
--  Default passwords (BCrypt hashed, plain text: 123456 for all accounts)
-- ============================================================

USE LanguageCenterDB;
GO

INSERT INTO Users (username, password_hash, role) VALUES
('admin01',   '$2a$11$JnjyHIawMJ2m7mDk6C2UieiFo95NNTNnaTj.YbpNzGr250T4t7coW', 'ADMIN'),
('staff01',   '$2a$11$U7t7m.JhCoIZBzC.edRuMeKLroLVwOrI05B.WnVSrJPbQT3qrLPiK', 'STAFF'),
('teacher01', '$2a$11$lE49WxTCUi.IgfnEIYeVM.rGKjtWCptubi0UWfDfjrdyb74w2SB8S', 'TEACHER'),
('teacher02', '$2a$11$Ch64HDBEOI5LIwsNXDmHmuaIXsOTGlkD/4KebX8YCIcgBwGHbVxqG', 'TEACHER'),
('student01', '$2a$11$2Dh7pFYpvcXzZtGHqeUMROp16rAfcSwD2JiEDO81ybOheXiJcLV/S', 'STUDENT'),
('student02', '$2a$11$dfaFHqam40lcpx73YB/YCOJKIlBH/1ggqWliBYOL726EbfZZVpZ4C', 'STUDENT'),
('student03', '$2a$11$k9B8Pc/dafh16V0nvJ94ueFE3WA.HuldMSSGG3loLD.Akx2hbqWa.', 'STUDENT'),
('staff02',   '$2a$11$U7t7m.JhCoIZBzC.edRuMeKLroLVwOrI05B.WnVSrJPbQT3qrLPiK', 'STAFF');
GO

INSERT INTO Admins (user_id, full_name, phone, email) VALUES
(1, N'System Admin', '0900000000', 'admin@center.edu.vn');
GO

-- Department names must match the values used in Staff.department below; access_group
-- drives menu visibility (see MainWindow.ApplyStaffDepartmentVisibility).
INSERT INTO Departments (name, access_group) VALUES
(N'Academic Setup', 'ACADEMIC'),
(N'Finance',        'FINANCE');
GO

-- staff01 = Academic Setup (class/semester setup), staff02 = Finance (tuition collection)
INSERT INTO Staff (user_id, full_name, phone, email, department) VALUES
(2, N'Nguyen Van A', '0901000001', 'a@center.edu.vn', N'Academic Setup'),
(8, N'Tran Thi Kim', '0901000002', 'kim@center.edu.vn', N'Finance');
GO

INSERT INTO Teachers (user_id, full_name, phone, email, specialization, degree) VALUES
(3, N'Tran Thi Binh', '0902000001', 'binh@center.edu.vn', 'English',  'Master'),
(4, N'Le Minh Khoa',  '0902000002', 'khoa@center.edu.vn', 'Japanese', 'Bachelor');
GO

INSERT INTO Students (user_id, full_name, date_of_birth, gender, phone, email) VALUES
(5, N'Pham Thi Cam',  '2003-05-12', 'Female', '0903000001', 'cam@mail.com'),
(6, N'Do Quoc Hung',  '2002-08-20', 'Male',   '0903000002', 'hung@mail.com'),
(7, N'Nguyen Mai Ly', '2004-01-30', 'Female', '0903000003', 'ly@mail.com');
GO

-- ============================================================
--  CATALOGUE — languages and their levels
--
--  Courses pick a language, then one of THAT language's levels, which is why
--  levels are seeded per language rather than as one flat list. Each ladder
--  uses the real framework for the language:
--    English, German  — CEFR      (A1 → C2)
--    Japanese         — JLPT      (N5 → N1, note N5 is the BEGINNER end)
--    Chinese          — HSK       (1 → 6)
--    Korean           — TOPIK
--  sort_order is beginner-first so course dropdowns read in teaching order.
-- ============================================================
INSERT INTO Languages (name) VALUES
(N'English'),    -- 1
(N'German'),     -- 2
(N'Japanese'),   -- 3
(N'Chinese'),    -- 4
(N'Korean');     -- 5
GO

INSERT INTO Levels (language_id, name, sort_order) VALUES
-- English (CEFR) — level_id 1..6
(1, N'A1', 1), (1, N'A2', 2), (1, N'B1', 3), (1, N'B2', 4), (1, N'C1', 5), (1, N'C2', 6),
-- German (CEFR) — level_id 7..12
(2, N'A1', 1), (2, N'A2', 2), (2, N'B1', 3), (2, N'B2', 4), (2, N'C1', 5), (2, N'C2', 6),
-- Japanese (JLPT), easiest first — level_id 13..17
(3, N'N5', 1), (3, N'N4', 2), (3, N'N3', 3), (3, N'N2', 4), (3, N'N1', 5),
-- Chinese (HSK) — level_id 18..23
(4, N'HSK 1', 1), (4, N'HSK 2', 2), (4, N'HSK 3', 3),
(4, N'HSK 4', 4), (4, N'HSK 5', 5), (4, N'HSK 6', 6),
-- Korean (TOPIK) — level_id 24..29
(5, N'TOPIK 1', 1), (5, N'TOPIK 2', 2), (5, N'TOPIK 3', 3),
(5, N'TOPIK 4', 4), (5, N'TOPIK 5', 5), (5, N'TOPIK 6', 6);
GO

-- level_id values follow the Levels insert above. Kept explicit rather than
-- looked up so the ids stay predictable for the Classes seed further down.
INSERT INTO Courses (code, name, language_id, level_id, duration_sessions, tuition_fee) VALUES
('ENG-A1',  N'English Beginner A1',      1,  1, 40, 3500000),   -- course 1
('ENG-B1',  N'English Intermediate B1',  1,  3, 60, 5000000),   -- course 2
('JPN-N5',  N'Japanese N5',              3, 13, 50, 4500000),   -- course 3
('ENG-B2',  N'English Upper-Int. B2',    1,  4, 60, 5800000),   -- course 4
('GER-A1',  N'German Beginner A1',       2,  7, 45, 4200000),   -- course 5
('GER-B1',  N'German Intermediate B1',   2,  9, 60, 5600000),   -- course 6
('JPN-N4',  N'Japanese N4',              3, 14, 55, 5200000),   -- course 7
('CHN-HSK1',N'Chinese HSK 1',            4, 18, 40, 3800000),   -- course 8
('CHN-HSK3',N'Chinese HSK 3',            4, 20, 55, 5100000),   -- course 9
('KOR-T1',  N'Korean TOPIK 1',           5, 24, 40, 3900000);   -- course 10
GO

INSERT INTO TuitionDiscounts
    (code, name, discount_type, discount_value, start_date, end_date, is_active, note, payment_deadline_days, condition_type)
VALUES
    ('EARLY5', N'Early payment discount 5%', 'PERCENT', 5, NULL, NULL, 1,
     N'Giảm 5% nếu học viên thanh toán đủ trong 7 ngày từ ngày đăng ký', 7, 'EARLY_PAYMENT'),
    ('GROUP8', N'Group registration discount 8%', 'PERCENT', 8, NULL, NULL, 1,
     N'Giảm 8% cho học viên đăng ký theo nhóm', NULL, 'NONE'),
    ('SCHOLAR500K', N'Scholarship discount 500,000', 'FIXED', 500000, NULL, NULL, 1,
     N'Giảm cố định 500,000 cho học viên được hỗ trợ', NULL, 'NONE');
GO

INSERT INTO Classrooms (name, capacity, location) VALUES
('Room 101', 25, 'Floor 1'),
('Room 201', 20, 'Floor 2'),
('Room 301', 15, 'Floor 3');
GO

-- FAP-style daily slots. These are editable via the Slot Time Setting screen.
INSERT INTO Slots (slot_no, start_time, end_time) VALUES
(1, '07:00', '09:15'),
(2, '09:30', '11:45'),
(3, '12:30', '14:45'),
(4, '15:00', '17:15'),
(5, '17:30', '19:45'),
(6, '20:00', '22:15');
GO

-- setup_end_date = start_date + 2 weeks; the learning phase runs setup_end_date+1 .. end_date.
-- There is no is_active column — the current semester is whichever one contains today, so
-- "Fall 2025" is dated around today to land straight in the LEARNING phase for testing
-- schedule generation without manually patching the DB.
-- These three ranges must stay non-overlapping, otherwise "the current semester" is ambiguous.
INSERT INTO Semesters (name, start_date, setup_end_date, end_date) VALUES
(N'Summer 2025',   '2025-06-01', '2025-06-15', '2025-08-31'),
(N'Fall 2025',     '2026-06-01', '2026-06-15', '2026-08-31'),
(N'Spring 2026',   '2026-09-01', '2026-09-15', '2027-01-31');
GO

-- All classes sit in the active semester (Fall 2025) so schedules/enrollments are testable immediately.
-- snap_* mirror the Courses rows above at "creation time". In the app these are
-- filled by ClassService from the course; here they are written out literally so
-- the seed still demonstrates the freeze: edit a course's tuition_fee afterwards
-- and these classes (and their invoices) keep the original price.
INSERT INTO Classes
    (semester_id, course_id, classroom_id, name, max_students, start_date, end_date, status,
     snap_course_code, snap_course_name, snap_language, snap_level, snap_duration_sessions, snap_tuition_fee)
VALUES
(2, 1, 1, 'A1-K01', 20, '2026-06-01', '2026-08-31', 'ONGOING',
 'ENG-A1', N'English Beginner A1',     N'English',  'A1', 40, 3500000),
(2, 2, 2, 'B1-K01', 18, '2026-06-01', '2026-08-31', 'ONGOING',
 'ENG-B1', N'English Intermediate B1', N'English',  'B1', 60, 5000000),
(2, 3, 3, 'N5-K01', 15, '2026-06-01', '2026-08-31', 'UPCOMING',
 'JPN-N5', N'Japanese N5',             N'Japanese', 'N5', 50, 4500000);
GO

-- Class 1 is co-taught to exercise the multi-teacher path; the other two have a
-- single teacher who is therefore also the primary one.
INSERT INTO ClassTeachers (class_id, teacher_id, is_primary) VALUES
(1, 1, 1),
(1, 2, 0),
(2, 1, 1),
(3, 2, 1);
GO

-- day_of_week: 1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat, 7=Sun
-- Times match the seeded Slots (above) so sessions line up in the weekly grid.
INSERT INTO ClassSchedules (class_id, day_of_week, start_time, end_time) VALUES
(1, 1, '07:00', '09:15'),   -- Slot 1, Mon
(1, 3, '07:00', '09:15'),   -- Slot 1, Wed
(2, 2, '12:30', '14:45'),   -- Slot 3, Tue
(2, 4, '12:30', '14:45'),   -- Slot 3, Thu
(3, 6, '15:00', '17:15');   -- Slot 4, Sat
GO

-- TEMPLATE only (course_id order matches Courses insert above: 1=ENG-A1, 2=ENG-B1,
-- 3=JPN-N5). Nothing points at these rows — they are copied into a class's own
-- ClassGradeComponents when the class is created. Editing them later changes what
-- FUTURE classes inherit, never an existing class.
-- EVERY course needs one: ClassService refuses to open a class for a course with
-- no grading template, since the class would have nothing to snapshot and its
-- grades would have nowhere to attach.
INSERT INTO GradeTypes (course_id, name, weight_percent, description)
SELECT c.course_id, v.name, v.weight_percent, v.description
FROM   Courses c
CROSS JOIN (VALUES
    (N'Attendance', CAST(10 AS DECIMAL(5,2)), N'Attendance and participation score'),
    (N'Midterm',    CAST(30 AS DECIMAL(5,2)), N'Midterm exam score'),
    (N'Final',      CAST(60 AS DECIMAL(5,2)), N'Final exam score')
) AS v(name, weight_percent, description);
GO

INSERT INTO Enrollments (student_id, class_id, enrolled_date, status) VALUES
(1, 1, '2026-05-28', 'ACTIVE'),
(2, 1, '2026-05-28', 'ACTIVE'),
(3, 1, '2026-05-29', 'ACTIVE'),
(1, 3, '2026-05-30', 'ACTIVE');
GO

INSERT INTO Invoices
    (student_id, enrollment_id, original_amount, discount_amount, amount, status, due_date, discount_status)
VALUES
(1, 1, 3500000, 0, 3500000, 'UNPAID', '2025-06-01', 'NONE'),
(2, 2, 3500000, 0, 3500000, 'UNPAID', '2025-06-01', 'NONE'),
(3, 3, 3500000, 0, 3500000, 'UNPAID', '2025-06-01', 'NONE'),
(1, 4, 4500000, 0, 4500000, 'UNPAID', '2025-07-01', 'NONE');
GO

INSERT INTO Payments (invoice_id, staff_id, amount_paid, payment_method, receipt_code) VALUES
(1, 1, 3500000, 'Transfer', 'RCP-2025-0001'),
(2, 1, 2000000, 'Cash',     'RCP-2025-0002');
GO

UPDATE Invoices SET status = 'PAID'    WHERE invoice_id = 1;
UPDATE Invoices SET status = 'PARTIAL' WHERE invoice_id = 2;
GO

-- Sessions are NOT seeded manually: the app auto-generates them from ClassSchedules
-- the first time it runs while the active semester is in the LEARNING phase
-- (see App.xaml.cs OnStartup / SessionService.EnsureSessionsForSemester).
-- Seeding them here would permanently block auto-generation for these classes
-- (GenerateSessionsForClass skips a class once it already has any sessions).

-- The frozen per-class copy of the grading structure. In the app ClassService
-- writes these from the course template; here they are literal so the seed works
-- without running the app. component_id order: class 1 -> 1,2,3; class 2 -> 4,5,6;
-- class 3 -> 7,8,9.
INSERT INTO ClassGradeComponents (class_id, name, weight_percent, description, sort_order) VALUES
(1, 'Attendance', 10, 'Attendance and participation score', 1),
(1, 'Midterm',    30, 'Midterm exam score',                 2),
(1, 'Final',      60, 'Final exam score',                   3),
(2, 'Attendance', 10, 'Attendance and participation score', 1),
(2, 'Midterm',    30, 'Midterm exam score',                 2),
(2, 'Final',      60, 'Final exam score',                   3),
(3, 'Attendance', 10, 'Attendance and participation score', 1),
(3, 'Midterm',    30, 'Midterm exam score',                 2),
(3, 'Final',      60, 'Final exam score',                   3);
GO

-- Enrollments 1 and 2 are both in class 1, so these reference class 1's
-- components (1=Attendance, 2=Midterm).
INSERT INTO Grades (enrollment_id, component_id, score, max_score) VALUES
(1, 1, 9.0, 10),
(1, 2, 7.5, 10),
(2, 1, 8.0, 10);
GO

PRINT '==> Seed data inserted successfully.';
GO
