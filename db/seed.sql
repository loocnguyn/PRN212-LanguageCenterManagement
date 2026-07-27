-- ============================================================
--  LANGUAGE CENTER MANAGEMENT SYSTEM - SEED DATA
--  Run schema.sql first, then this file. One file, one run.
--
--  Sign-in is by EMAIL: the address on each Users row IS the login, and it
--  is repeated as the contact email of the matching profile row. Password
--  for every account is 123456 (BCrypt hashed below).
--
--  must_change_password stays 0 throughout. These are demo logins - being
--  marched through a password change before you can look at anything would
--  make the seed useless for testing. Accounts created by an Admin or by the
--  student import get the flag set instead.
--
--  Every table uses IDENTITY, so the ids in the comments are simply the
--  order the rows are inserted in. Run this file once, on a database
--  freshly created by schema.sql.
--
--  What it creates:
--    - 50 accounts: 1 admin, 6 staff, 10 teachers, 33 students
--    - 2 departments (Academic Setup, Finance), 10 classrooms
--    - 5 languages with their level ladders, 16 courses, 8 discounts
--    - 7 semesters, 19 classes spanning completed / ongoing / upcoming / cancelled
--    - 47 enrollments with invoices, payments and grades
--    - Sessions + attendance for PAST classes only (see section 12)
--    - Wallet transactions
-- ============================================================

USE LanguageCenterDB;
GO

INSERT INTO Users (email, password_hash, role) VALUES
('admin@center.edu.vn',  '$2a$11$JnjyHIawMJ2m7mDk6C2UieiFo95NNTNnaTj.YbpNzGr250T4t7coW', 'ADMIN'),
('a@center.edu.vn',      '$2a$11$U7t7m.JhCoIZBzC.edRuMeKLroLVwOrI05B.WnVSrJPbQT3qrLPiK', 'STAFF'),
('binh@center.edu.vn',   '$2a$11$lE49WxTCUi.IgfnEIYeVM.rGKjtWCptubi0UWfDfjrdyb74w2SB8S', 'TEACHER'),
('khoa@center.edu.vn',   '$2a$11$Ch64HDBEOI5LIwsNXDmHmuaIXsOTGlkD/4KebX8YCIcgBwGHbVxqG', 'TEACHER'),
('cam@mail.com',         '$2a$11$2Dh7pFYpvcXzZtGHqeUMROp16rAfcSwD2JiEDO81ybOheXiJcLV/S', 'STUDENT'),
('hung@mail.com',        '$2a$11$dfaFHqam40lcpx73YB/YCOJKIlBH/1ggqWliBYOL726EbfZZVpZ4C', 'STUDENT'),
('ly@mail.com',          '$2a$11$k9B8Pc/dafh16V0nvJ94ueFE3WA.HuldMSSGG3loLD.Akx2hbqWa.', 'STUDENT'),
('kim@center.edu.vn',    '$2a$11$U7t7m.JhCoIZBzC.edRuMeKLroLVwOrI05B.WnVSrJPbQT3qrLPiK', 'STAFF');
GO

INSERT INTO Admins (user_id, full_name, phone, email) VALUES
(1, N'System Admin', '0900000000', 'admin@center.edu.vn');
GO

-- Department names must match the values used in Staff.department below, and are
-- matched in code to decide menu access — see MainWindow.ApplyStaffDepartmentVisibility.
INSERT INTO Departments (name) VALUES
(N'Academic Setup'),
(N'Finance');
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
--  Levels list by level_id, so each ladder is inserted beginner-first and course
--  dropdowns read in teaching order.
-- ============================================================
INSERT INTO Languages (name) VALUES
(N'English'),    -- 1
(N'German'),     -- 2
(N'Japanese'),   -- 3
(N'Chinese'),    -- 4
(N'Korean');     -- 5
GO

-- Inserted beginner-first: level_id doubles as the display order.
INSERT INTO Levels (language_id, name) VALUES
-- English (CEFR) — level_id 1..6
(1, N'A1'), (1, N'A2'), (1, N'B1'), (1, N'B2'), (1, N'C1'), (1, N'C2'),
-- German (CEFR) — level_id 7..12
(2, N'A1'), (2, N'A2'), (2, N'B1'), (2, N'B2'), (2, N'C1'), (2, N'C2'),
-- Japanese (JLPT), easiest first — level_id 13..17
(3, N'N5'), (3, N'N4'), (3, N'N3'), (3, N'N2'), (3, N'N1'),
-- Chinese (HSK) — level_id 18..23
(4, N'HSK 1'), (4, N'HSK 2'), (4, N'HSK 3'), (4, N'HSK 4'), (4, N'HSK 5'), (4, N'HSK 6'),
-- Korean (TOPIK) — level_id 24..29
(5, N'TOPIK 1'), (5, N'TOPIK 2'), (5, N'TOPIK 3'),
(5, N'TOPIK 4'), (5, N'TOPIK 5'), (5, N'TOPIK 6');
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
-- No status column: these classes read as ONGOING purely because their dates
-- straddle today. Nothing has to be kept in sync as time passes.
INSERT INTO Classes
    (semester_id, course_id, classroom_id, name, max_students, start_date, end_date,
     snap_course_code, snap_course_name, snap_language, snap_level, snap_duration_sessions, snap_tuition_fee)
VALUES
(2, 1, 1, 'A1-K01', 20, '2026-06-08', '2026-08-31',
 'ENG-A1', N'English Beginner A1',     N'English',  'A1', 40, 3500000),
(2, 2, 2, 'B1-K01', 18, '2026-06-08', '2026-08-31',
 'ENG-B1', N'English Intermediate B1', N'English',  'B1', 60, 5000000),
(2, 3, 3, 'N5-K01', 15, '2026-06-08', '2026-08-31',
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


-- ============================================================
--  FROM HERE ON: the larger demo data set.
--  Everything above is the minimum the app needs to run; everything
--  below exists so the reports, filters and history screens have
--  something real to show.
-- ============================================================

-- ============================================================
--  1. ACCOUNTS  — Users.id 9 .. 50
--
--  All share one BCrypt hash of "123456". Reusing a hash means these
--  rows are byte-identical in the password column, which would be a
--  real weakness in production but is exactly what you want in a seed:
--  every demo account opens with the same password.
-- ============================================================
DECLARE @pw NVARCHAR(256) = '$2a$11$U7t7m.JhCoIZBzC.edRuMeKLroLVwOrI05B.WnVSrJPbQT3qrLPiK';

-- The email IS the login. Each address below repeats as the contact email of the
-- matching profile row further down — Users.email is the credential, the profile
-- copy is contact detail, and the seed keeps the two identical.
INSERT INTO Users (email, password_hash, role, is_active) VALUES
-- Staff — id 9..12. There is deliberately no second ADMIN account: the system
-- has exactly one administrator, admin@center.edu.vn, created at the top of this file.
('nam@center.edu.vn',       @pw, 'STAFF',   1),
('hanh@center.edu.vn',      @pw, 'STAFF',   1),
('toan@center.edu.vn',      @pw, 'STAFF',   1),
('lan@center.edu.vn',       @pw, 'STAFF',   1),
-- Teachers — id 13..20
('trang@center.edu.vn',     @pw, 'TEACHER', 1),
('duc@center.edu.vn',       @pw, 'TEACHER', 1),
('yuki@center.edu.vn',      @pw, 'TEACHER', 1),
('bao@center.edu.vn',       @pw, 'TEACHER', 1),
('mai@center.edu.vn',       @pw, 'TEACHER', 1),
('klaus@center.edu.vn',     @pw, 'TEACHER', 1),
('loc@center.edu.vn',       @pw, 'TEACHER', 0),   -- deactivated, for the Deactivated Accounts screen
('jiwoo@center.edu.vn',     @pw, 'TEACHER', 1),
-- Students — id 21..50
('ngocanh@mail.com',   @pw, 'STUDENT', 1), ('quanghuy@mail.com',  @pw, 'STUDENT', 1),
('thuha@mail.com',     @pw, 'STUDENT', 1), ('vandat@mail.com',    @pw, 'STUDENT', 1),
('lananh@mail.com',    @pw, 'STUDENT', 1), ('minhquan@mail.com',  @pw, 'STUDENT', 1),
('kimchi@mail.com',    @pw, 'STUDENT', 1), ('thanhtung@mail.com', @pw, 'STUDENT', 1),
('hongnhung@mail.com', @pw, 'STUDENT', 1), ('giahan@mail.com',    @pw, 'STUDENT', 1),
('minhkhang@mail.com', @pw, 'STUDENT', 1), ('yennhi@mail.com',    @pw, 'STUDENT', 1),
('quocthang@mail.com', @pw, 'STUDENT', 1), ('thanhtruc@mail.com', @pw, 'STUDENT', 1),
('vanphuc@mail.com',   @pw, 'STUDENT', 1), ('bichngoc@mail.com',  @pw, 'STUDENT', 1),
('minhtriet@mail.com', @pw, 'STUDENT', 1), ('kieutrang@mail.com', @pw, 'STUDENT', 1),
('quangvinh@mail.com', @pw, 'STUDENT', 1), ('myduyen@mail.com',   @pw, 'STUDENT', 1),
('giabao@mail.com',    @pw, 'STUDENT', 1), ('tuyetmai@mail.com',  @pw, 'STUDENT', 1),
('vanhieu@mail.com',   @pw, 'STUDENT', 1), ('kimngan@mail.com',   @pw, 'STUDENT', 1),
('hoanglong@mail.com', @pw, 'STUDENT', 1), ('thuyduong@mail.com', @pw, 'STUDENT', 0),  -- deactivated
('trungkien@mail.com', @pw, 'STUDENT', 1), ('hongvan@mail.com',   @pw, 'STUDENT', 1),
('anhkiet@mail.com',   @pw, 'STUDENT', 1), ('ngocdiep@mail.com',  @pw, 'STUDENT', 1);
GO

-- ============================================================
--  2. ORGANISATION
-- ============================================================

-- No further departments. The centre has exactly the two created above —
-- Academic Setup and Finance — and every staff member below belongs to one of
-- them. Only "Finance" unlocks the finance menus; Academic Setup falls through
-- to the academic group (MainWindow.ApplyStaffDepartmentVisibility).

-- Staff — staff_id 3..6, users 10..12 then 9
INSERT INTO Staff (user_id, full_name, date_of_birth, gender, phone, email, department) VALUES
(10, N'Vo Thi Hanh',    '1993-04-18', 'Female', '0901000003', 'hanh@center.edu.vn',  N'Finance'),
(11, N'Dang Quoc Toan', '1990-11-02', 'Male',   '0901000004', 'toan@center.edu.vn',  N'Academic Setup'),
(12, N'Bui Ngoc Lan',   '1995-07-25', 'Female', '0901000005', 'lan@center.edu.vn',   N'Academic Setup'),
-- staff06 (user 9) is inserted last on purpose: staff_id 3..5 keep the values the
-- Payments section further down collects money under.
(9,  N'Le Hoang Nam',   '1989-03-12', 'Male',   '0900000001', 'nam@center.edu.vn',   N'Finance');
GO

-- Teachers — teacher_id 3..10, users 13..20.
-- Specialisations line up with the five languages in the catalogue so every
-- course has someone who could plausibly teach it.
INSERT INTO Teachers (user_id, full_name, date_of_birth, gender, phone, email, specialization, degree, status) VALUES
(13, N'Nguyen Thu Trang', '1988-02-14', 'Female', '0902000003', 'trang@center.edu.vn',  N'English',  N'Master',   'ACTIVE'),
(14, N'Hoang Minh Duc',   '1985-09-30', 'Male',   '0902000004', 'duc@center.edu.vn',    N'English',  N'PhD',      'ACTIVE'),
(15, N'Sato Yuki',        '1991-06-08', 'Female', '0902000005', 'yuki@center.edu.vn',   N'Japanese', N'Master',   'ACTIVE'),
(16, N'Pham Gia Bao',     '1992-12-21', 'Male',   '0902000006', 'bao@center.edu.vn',    N'Chinese',  N'Bachelor', 'ACTIVE'),
(17, N'Ly Thi Mai',       '1994-03-05', 'Female', '0902000007', 'mai@center.edu.vn',    N'Chinese',  N'Master',   'ACTIVE'),
(18, N'Klaus Bergmann',   '1983-08-17', 'Male',   '0902000008', 'klaus@center.edu.vn',  N'German',   N'Master',   'ACTIVE'),
(19, N'Tran Van Loc',     '1987-01-23', 'Male',   '0902000009', 'loc@center.edu.vn',    N'German',   N'Bachelor', 'RESIGNED'),
(20, N'Kim Ji Woo',       '1990-10-11', 'Female', '0902000010', 'jiwoo@center.edu.vn',  N'Korean',   N'Master',   'ON_LEAVE');
GO

-- Classrooms — id 4..10
INSERT INTO Classrooms (name, capacity, location, is_active) VALUES
('Room 102',   25, 'Floor 1', 1),   -- 4
('Room 103',   30, 'Floor 1', 1),   -- 5
('Room 202',   20, 'Floor 2', 1),   -- 6
('Room 203',   18, 'Floor 2', 1),   -- 7
('Room 302',   16, 'Floor 3', 1),   -- 8
('Lab A',      12, 'Floor 4', 1),   -- 9
('Room 401',   35, 'Floor 4', 0);   -- 10 — under renovation
GO

-- ============================================================
--  3. STUDENTS — student_id 4 .. 33, users 21 .. 50
--
--  balance starts at 0 for everyone and is recomputed from the wallet ledger
--  at the end of section 13. Typing balances in by hand here would let them
--  drift out of step with WalletTransactions, which is exactly the kind of
--  inconsistency the wallet screens would then display.
-- ============================================================
INSERT INTO Students (user_id, full_name, date_of_birth, gender, phone, email, address, balance, status) VALUES
(21, N'Nguyen Thi Ngoc Anh', '2003-03-11', 'Female', '0903000004', 'ngocanh@mail.com',  N'12 Le Loi, Q1', 0, 'ACTIVE'),
(22, N'Tran Quang Huy',      '2002-07-19', 'Male',   '0903000005', 'quanghuy@mail.com', N'45 Nguyen Trai, Q5', 0, 'ACTIVE'),
(23, N'Le Thi Thu Ha',       '2004-11-05', 'Female', '0903000006', 'thuha@mail.com',    N'7 Tran Hung Dao, Q1', 0, 'ACTIVE'),
(24, N'Pham Van Dat',        '2001-05-28', 'Male',   '0903000007', 'vandat@mail.com',   N'89 Cach Mang, Q3', 0, 'ACTIVE'),
(25, N'Hoang Thi Lan Anh',   '2003-09-14', 'Female', '0903000008', 'lananh@mail.com',   N'23 Hai Ba Trung, Q1', 0, 'ACTIVE'),
(26, N'Vu Minh Quan',        '2002-01-30', 'Male',   '0903000009', 'minhquan@mail.com', N'56 Vo Van Tan, Q3', 0, 'ACTIVE'),
(27, N'Dinh Thi Kim Chi',    '2004-06-22', 'Female', '0903000010', 'kimchi@mail.com',   N'11 Pasteur, Q1', 0, 'ACTIVE'),
(28, N'Ngo Thanh Tung',      '2000-12-03', 'Male',   '0903000011', 'thanhtung@mail.com',N'78 Dien Bien Phu, BT', 0, 'ACTIVE'),
(29, N'Bui Thi Hong Nhung',  '2003-04-17', 'Female', '0903000012', 'hongnhung@mail.com',N'34 Nam Ky, Q3', 0, 'ACTIVE'),
(30, N'Duong Gia Han',       '2005-02-09', 'Female', '0903000013', 'giahan@mail.com',   N'90 Ly Tu Trong, Q1', 0, 'ACTIVE'),
(31, N'Truong Minh Khang',   '2002-08-26', 'Male',   '0903000014', 'minhkhang@mail.com',N'15 Suong Nguyet, Q1', 0, 'ACTIVE'),
(32, N'Phan Thi Yen Nhi',    '2004-10-12', 'Female', '0903000015', 'yennhi@mail.com',   N'62 Vo Thi Sau, Q3', 0, 'ACTIVE'),
(33, N'Lam Quoc Thang',      '2001-03-07', 'Male',   '0903000016', 'quocthang@mail.com',N'28 Nguyen Hue, Q1', 0, 'ACTIVE'),
(34, N'Cao Thi Thanh Truc',  '2003-12-19', 'Female', '0903000017', 'thanhtruc@mail.com',N'41 Ton Duc Thang, Q1', 0, 'ACTIVE'),
(35, N'Ho Van Phuc',         '2002-05-04', 'Male',   '0903000018', 'vanphuc@mail.com',  N'73 Bach Dang, BT', 0, 'ACTIVE'),
(36, N'Nguyen Thi Bich Ngoc','2004-01-15', 'Female', '0903000019', 'bichngoc@mail.com', N'19 Ham Nghi, Q1', 0, 'ACTIVE'),
(37, N'Dao Minh Triet',      '2000-09-23', 'Male',   '0903000020', 'minhtriet@mail.com',N'52 Ly Thuong Kiet,Q10',0,       'ACTIVE'),
(38, N'Vo Thi Kieu Trang',   '2003-07-08', 'Female', '0903000021', 'kieutrang@mail.com',N'86 Su Van Hanh, Q10', 0, 'ACTIVE'),
(39, N'Ta Quang Vinh',       '2001-11-27', 'Male',   '0903000022', 'quangvinh@mail.com',N'37 3/2, Q10', 0, 'ACTIVE'),
(40, N'Luong Thi My Duyen',  '2004-04-02', 'Female', '0903000023', 'myduyen@mail.com',  N'64 Nguyen Dinh, PN', 0, 'ACTIVE'),
(41, N'Chu Gia Bao',         '2002-02-16', 'Male',   '0903000024', 'giabao@mail.com',   N'21 Phan Xich Long,PN', 0, 'ACTIVE'),
(42, N'Nguyen Thi Tuyet Mai','2003-08-31', 'Female', '0903000025', 'tuyetmai@mail.com', N'95 Hoang Van Thu, TB', 0, 'ACTIVE'),
(43, N'Trinh Van Hieu',      '2000-06-13', 'Male',   '0903000026', 'vanhieu@mail.com',  N'48 Cong Hoa, TB', 0, 'ACTIVE'),
(44, N'Mai Thi Kim Ngan',    '2005-01-25', 'Female', '0903000027', 'kimngan@mail.com',  N'12 Truong Chinh, TB', 0, 'ACTIVE'),
(45, N'Do Hoang Long',       '2002-10-07', 'Male',   '0903000028', 'hoanglong@mail.com',N'70 Au Co, TP', 0, 'ACTIVE'),
(46, N'Nguyen Thi Thuy Duong','2003-05-20','Female', '0903000029', 'thuyduong@mail.com',N'33 Lac Long Quan, T11',0,       'GRADUATED'),
(47, N'Pham Trung Kien',     '2001-09-11', 'Male',   '0903000030', 'trungkien@mail.com',N'59 Hoa Binh, T11', 0, 'GRADUATED'),
(48, N'Le Thi Hong Van',     '2004-03-29', 'Female', '0903000031', 'hongvan@mail.com',  N'81 Dong Den, TB', 0, 'SUSPENDED'),
(49, N'Nguyen Anh Kiet',     '2002-12-06', 'Male',   '0903000032', 'anhkiet@mail.com',  N'26 Quang Trung, GV', 0, 'ACTIVE'),
(50, N'Ha Thi Ngoc Diep',    '2003-06-18', 'Female', '0903000033', 'ngocdiep@mail.com', N'44 Le Duc Tho, GV', 0, 'ACTIVE');
GO

-- ============================================================
--  4. COURSES — course_id 11 .. 16
--
--  level_id values come from the Levels insert further up this file:
--    English  1=A1 2=A2 3=B1 4=B2 5=C1 6=C2
--    German   7=A1 8=A2 9=B1
--    Japanese 13=N5 14=N4 15=N3
--    Chinese  18=HSK1 19=HSK2 20=HSK3
--    Korean   24=TOPIK1 25=TOPIK2
-- ============================================================
INSERT INTO Courses (code, name, language_id, level_id, duration_sessions, tuition_fee, description) VALUES
('ENG-A2',   N'English Elementary A2',  1,  2, 45, 4000000, N'Tiếp nối A1, tập trung giao tiếp hằng ngày'),          -- 11
('ENG-C1',   N'English Advanced C1',    1,  5, 70, 7200000, N'Luyện học thuật và thuyết trình nâng cao'),            -- 12
('JPN-N3',   N'Japanese N3',            3, 15, 60, 6000000, N'Ngữ pháp trung cấp, luyện đọc hiểu JLPT N3'),          -- 13
('CHN-HSK2', N'Chinese HSK 2',          4, 19, 45, 4300000, N'Mở rộng vốn từ và mẫu câu cơ bản'),                    -- 14
('KOR-T2',   N'Korean TOPIK 2',         5, 25, 45, 4400000, N'Hoàn thiện sơ cấp, chuẩn bị TOPIK I'),                 -- 15
('GER-A2',   N'German Elementary A2',   2,  8, 50, 4800000, N'Ngữ pháp cách và giao tiếp thông dụng');               -- 16
GO

-- Every course needs a grading template — ClassService refuses to open a class
-- for a course without one. Courses 1..10 were covered above; these are for 11..16.
INSERT INTO GradeTypes (course_id, name, weight_percent, description)
SELECT c.course_id, v.name, v.weight_percent, v.description
FROM   Courses c
CROSS JOIN (VALUES
    (N'Attendance', CAST(10 AS DECIMAL(5,2)), N'Attendance and participation score'),
    (N'Midterm',    CAST(30 AS DECIMAL(5,2)), N'Midterm exam score'),
    (N'Final',      CAST(60 AS DECIMAL(5,2)), N'Final exam score')
) AS v(name, weight_percent, description)
WHERE  c.course_id BETWEEN 11 AND 16;
GO

-- ============================================================
--  5. TUITION DISCOUNTS — discount_id 4 .. 8
-- ============================================================
INSERT INTO TuitionDiscounts
    (code, name, discount_type, discount_value, start_date, end_date, is_active, note, payment_deadline_days, condition_type)
VALUES
('LOYAL10',   N'Returning student 10%',   'PERCENT', 10,  NULL,         NULL,         1,
 N'Giảm 10% cho học viên đã học ít nhất một khoá', NULL, 'NONE'),                                      -- 4
('SIBLING7',  N'Sibling discount 7%',     'PERCENT', 7,   NULL,         NULL,         1,
 N'Giảm 7% khi có anh chị em cùng theo học', NULL, 'NONE'),                                            -- 5
('SUMMER15',  N'Summer promotion 15%',    'PERCENT', 15,  '2026-05-01', '2026-07-31', 1,
 N'Khuyến mãi hè, áp dụng trong thời gian giới hạn', 10, 'EARLY_PAYMENT'),                             -- 6
('REWARD1M',  N'Scholarship 1,000,000',   'FIXED',   1000000, NULL,     NULL,         1,
 N'Voucher học bổng cấp cho học viên xuất sắc', NULL, 'NONE'),                                         -- 7
('OLDPROMO',  N'Expired 2025 promotion',  'PERCENT', 12,  '2025-01-01', '2025-03-31', 0,
 N'Chương trình đã kết thúc, giữ lại để kiểm thử bộ lọc', NULL, 'NONE');                               -- 8
GO

-- ============================================================
--  6. SEMESTERS — semester_id 4 .. 7
--
--  These fill the gaps AROUND the three semesters created above without overlapping
--  any of them — non-overlap is what lets the app derive "the current
--  semester" from today's date instead of storing a flag.
--
--    Spring 2025   2025-01-06 .. 2025-05-25   (id 4)  ← past
--    [Summer 2025  2025-06-01 .. 2025-08-31]  existing
--    Autumn 2025   2025-09-08 .. 2025-12-28   (id 5)  ← past
--    Winter 2026   2026-01-05 .. 2026-05-24   (id 6)  ← past
--    [Fall 2025    2026-06-01 .. 2026-08-31]  existing, CURRENT
--    [Spring 2026  2026-09-01 .. 2027-01-31]  existing, upcoming
--    Summer 2027   2027-02-08 .. 2027-05-30   (id 7)  ← upcoming
-- ============================================================
INSERT INTO Semesters (name, start_date, setup_end_date, end_date) VALUES
(N'Spring 2025', '2025-01-06', '2025-01-19', '2025-05-25'),   -- 4
(N'Autumn 2025', '2025-09-08', '2025-09-21', '2025-12-28'),   -- 5
(N'Winter 2026', '2026-01-05', '2026-01-18', '2026-05-24'),   -- 6
(N'Summer 2027', '2027-02-08', '2027-02-21', '2027-05-30');   -- 7
GO

-- ============================================================
--  7. CLASSES — class_id 4 .. 19
--
--  snap_* is the frozen copy of the course at creation time. Written out
--  literally here because the seed does not run through ClassService.
--  There is no status column: a class reads as UPCOMING / ONGOING /
--  COMPLETED purely from its dates, and is_cancelled covers the one state
--  dates cannot express.
--
--  Class start dates begin the day after their semester's setup ends, which
--  is where session generation starts from.
-- ============================================================
INSERT INTO Classes
    (semester_id, course_id, classroom_id, name, max_students, start_date, end_date, is_cancelled,
     snap_course_code, snap_course_name, snap_language, snap_level, snap_duration_sessions, snap_tuition_fee)
VALUES
-- ---- Spring 2025 (sem 4) — completed ----
(4,  1, 1, 'A1-SP25-01', 20, '2025-01-20', '2025-05-25', 0,
 'ENG-A1',   N'English Beginner A1',     N'English',  'A1',     40, 3500000),   -- 4
(4,  3, 3, 'N5-SP25-01', 15, '2025-01-20', '2025-05-25', 0,
 'JPN-N5',   N'Japanese N5',             N'Japanese', 'N5',     50, 4500000),   -- 5

-- ---- Autumn 2025 (sem 5) — completed ----
(5,  2, 2, 'B1-AU25-01', 18, '2025-09-22', '2025-12-28', 0,
 'ENG-B1',   N'English Intermediate B1', N'English',  'B1',     60, 5000000),   -- 6
(5,  8, 4, 'HSK1-AU25-01',22,'2025-09-22', '2025-12-28', 0,
 'CHN-HSK1', N'Chinese HSK 1',           N'Chinese',  'HSK 1',  40, 3800000),   -- 7
(5,  5, 5, 'GA1-AU25-01',24, '2025-09-22', '2025-12-28', 0,
 'GER-A1',   N'German Beginner A1',      N'German',   'A1',     45, 4200000),   -- 8

-- ---- Winter 2026 (sem 6) — completed ----
(6, 11, 1, 'A2-WI26-01', 20, '2026-01-19', '2026-05-24', 0,
 'ENG-A2',   N'English Elementary A2',   N'English',  'A2',     45, 4000000),   -- 9
(6, 10, 6, 'KT1-WI26-01',18, '2026-01-19', '2026-05-24', 0,
 'KOR-T1',   N'Korean TOPIK 1',          N'Korean',   'TOPIK 1',40, 3900000),   -- 10
(6,  7, 3, 'N4-WI26-01', 15, '2026-01-19', '2026-05-24', 0,
 'JPN-N4',   N'Japanese N4',             N'Japanese', 'N4',     55, 5200000),   -- 11

-- ---- Fall 2025 (sem 2) — CURRENT, sessions auto-generate ----
(2,  4, 2, 'B2-FA25-01', 18, '2026-06-16', '2026-08-31', 0,
 'ENG-B2',   N'English Upper-Int. B2',   N'English',  'B2',     60, 5800000),   -- 12
(2,  9, 4, 'HSK3-FA25-01',20,'2026-06-16', '2026-08-31', 0,
 'CHN-HSK3', N'Chinese HSK 3',           N'Chinese',  'HSK 3',  55, 5100000),   -- 13
(2,  6, 5, 'GB1-FA25-01',20, '2026-06-16', '2026-08-31', 0,
 'GER-B1',   N'German Intermediate B1',  N'German',   'B1',     60, 5600000),   -- 14
(2, 10, 6, 'KT1-FA25-01',18, '2026-06-16', '2026-08-31', 0,
 'KOR-T1',   N'Korean TOPIK 1',          N'Korean',   'TOPIK 1',40, 3900000),   -- 15

-- ---- Spring 2026 (sem 3) — upcoming ----
(3,  1, 1, 'A1-SP26-01', 25, '2026-09-16', '2027-01-31', 0,
 'ENG-A1',   N'English Beginner A1',     N'English',  'A1',     40, 3500000),   -- 16
(3, 13, 3, 'N3-SP26-01', 15, '2026-09-16', '2027-01-31', 0,
 'JPN-N3',   N'Japanese N3',             N'Japanese', 'N3',     60, 6000000),   -- 17
(3, 12, 7, 'C1-SP26-01', 12, '2026-09-16', '2027-01-31', 1,
 'ENG-C1',   N'English Advanced C1',     N'English',  'C1',     70, 7200000),   -- 18 — CANCELLED

-- ---- Summer 2027 (sem 7) — upcoming ----
(7, 14, 4, 'HSK2-SU27-01',20,'2027-02-22', '2027-05-30', 0,
 'CHN-HSK2', N'Chinese HSK 2',           N'Chinese',  'HSK 2',  45, 4300000);   -- 19
GO

-- Teacher assignments. Exactly one primary per class (enforced by a filtered
-- unique index); classes 6 and 12 are co-taught to exercise that path.
INSERT INTO ClassTeachers (class_id, teacher_id, is_primary) VALUES
(4,  3, 1),
(5,  4, 1),
(6,  3, 1), (6, 5, 0),
(7,  6, 1),
(8,  8, 1),
(9,  4, 1),
(10, 10, 1),
(11, 5, 1),
(12, 3, 1), (12, 4, 0),
(13, 7, 1),
(14, 8, 1),
(15, 10, 1),
(16, 4, 1),
(17, 5, 1),
(18, 3, 1),
(19, 7, 1);
GO

-- Weekly patterns. Times match the seeded Slots so the timetable grid lines up.
-- day_of_week: 1=Mon .. 7=Sun
INSERT INTO ClassSchedules (class_id, day_of_week, start_time, end_time) VALUES
(4,  1, '09:30', '11:45'), (4,  4, '09:30', '11:45'),
(5,  2, '17:30', '19:45'), (5,  5, '17:30', '19:45'),
(6,  1, '12:30', '14:45'), (6,  3, '12:30', '14:45'), (6, 5, '12:30', '14:45'),
(7,  2, '07:00', '09:15'), (7,  4, '07:00', '09:15'),
(8,  6, '09:30', '11:45'),
(9,  1, '15:00', '17:15'), (9,  3, '15:00', '17:15'),
(10, 2, '20:00', '22:15'), (10, 4, '20:00', '22:15'),
(11, 6, '07:00', '09:15'), (11, 7, '07:00', '09:15'),
(12, 2, '09:30', '11:45'), (12, 4, '09:30', '11:45'),
(13, 1, '17:30', '19:45'), (13, 3, '17:30', '19:45'),
(14, 6, '12:30', '14:45'),
(15, 5, '20:00', '22:15'),
(16, 1, '07:00', '09:15'), (16, 3, '07:00', '09:15'),
(17, 2, '15:00', '17:15'), (17, 4, '15:00', '17:15'),
(18, 6, '15:00', '17:15'),
(19, 1, '09:30', '11:45'), (19, 3, '09:30', '11:45');
GO

-- The class's frozen grading structure, copied from its course template.
-- Generated rather than typed out — 16 classes x 3 components.
INSERT INTO ClassGradeComponents (class_id, name, weight_percent, description, sort_order)
SELECT c.class_id, v.name, v.weight_percent, v.description, v.sort_order
FROM   Classes c
CROSS JOIN (VALUES
    (N'Attendance', CAST(10 AS DECIMAL(5,2)), N'Attendance and participation score', 1),
    (N'Midterm',    CAST(30 AS DECIMAL(5,2)), N'Midterm exam score',                 2),
    (N'Final',      CAST(60 AS DECIMAL(5,2)), N'Final exam score',                   3)
) AS v(name, weight_percent, description, sort_order)
WHERE  c.class_id BETWEEN 4 AND 19;
GO

-- ============================================================
--  8. ENROLLMENTS — enrollment_id 5 .. 47
--
--  Past classes are COMPLETED, the current semester's are ACTIVE, and a
--  couple are DROPPED so the dropped-row styling has something to show.
--  Several students appear in more than one class across semesters, which
--  is what makes the per-student history screens worth opening.
-- ============================================================
INSERT INTO Enrollments (student_id, class_id, enrolled_date, status) VALUES
-- class 4 — ENG-A1, Spring 2025 (completed)
(4,  4, '2025-01-08', 'COMPLETED'),   -- 5
(5,  4, '2025-01-08', 'COMPLETED'),   -- 6
(6,  4, '2025-01-09', 'COMPLETED'),   -- 7
(7,  4, '2025-01-09', 'COMPLETED'),   -- 8
(8,  4, '2025-01-10', 'DROPPED'),     -- 9
-- class 5 — JPN-N5, Spring 2025 (completed)
(9,  5, '2025-01-08', 'COMPLETED'),   -- 10
(10, 5, '2025-01-10', 'COMPLETED'),   -- 11
(11, 5, '2025-01-12', 'COMPLETED'),   -- 12
-- class 6 — ENG-B1, Autumn 2025 (completed)
(4,  6, '2025-09-10', 'COMPLETED'),   -- 13
(12, 6, '2025-09-10', 'COMPLETED'),   -- 14
(13, 6, '2025-09-11', 'COMPLETED'),   -- 15
(14, 6, '2025-09-12', 'COMPLETED'),   -- 16
-- class 7 — CHN-HSK1, Autumn 2025 (completed)
(15, 7, '2025-09-10', 'COMPLETED'),   -- 17
(16, 7, '2025-09-11', 'COMPLETED'),   -- 18
(17, 7, '2025-09-13', 'COMPLETED'),   -- 19
-- class 8 — GER-A1, Autumn 2025 (completed)
(18, 8, '2025-09-12', 'COMPLETED'),   -- 20
(19, 8, '2025-09-14', 'COMPLETED'),   -- 21
-- class 9 — ENG-A2, Winter 2026 (completed)
(5,  9, '2026-01-07', 'COMPLETED'),   -- 22
(20, 9, '2026-01-07', 'COMPLETED'),   -- 23
(21, 9, '2026-01-08', 'COMPLETED'),   -- 24
(22, 9, '2026-01-09', 'DROPPED'),     -- 25
-- class 10 — KOR-T1, Winter 2026 (completed)
(23, 10, '2026-01-07', 'COMPLETED'),  -- 26
(24, 10, '2026-01-08', 'COMPLETED'),  -- 27
-- class 11 — JPN-N4, Winter 2026 (completed)
(9,  11, '2026-01-07', 'COMPLETED'),  -- 28
(25, 11, '2026-01-09', 'COMPLETED'),  -- 29
-- class 12 — ENG-B2, Fall 2025 (current)
(4,  12, '2026-06-02', 'ACTIVE'),     -- 30
(6,  12, '2026-06-02', 'ACTIVE'),     -- 31
(26, 12, '2026-06-03', 'ACTIVE'),     -- 32
(27, 12, '2026-06-03', 'ACTIVE'),     -- 33
(28, 12, '2026-06-05', 'ACTIVE'),     -- 34
-- class 13 — CHN-HSK3, Fall 2025 (current)
(15, 13, '2026-06-02', 'ACTIVE'),     -- 35
(29, 13, '2026-06-04', 'ACTIVE'),     -- 36
-- class 14 — GER-B1, Fall 2025 (current)
(18, 14, '2026-06-02', 'ACTIVE'),     -- 37
(30, 14, '2026-06-06', 'ACTIVE'),     -- 38
-- class 15 — KOR-T1, Fall 2025 (current)
(23, 15, '2026-06-03', 'ACTIVE'),     -- 39
(31, 15, '2026-06-03', 'ACTIVE'),     -- 40
(32, 15, '2026-06-07', 'ACTIVE'),     -- 41
-- class 16 — ENG-A1, Spring 2026 (upcoming)
(33, 16, '2026-08-20', 'ACTIVE'),     -- 42
(7,  16, '2026-08-21', 'ACTIVE'),     -- 43
(8,  16, '2026-08-22', 'ACTIVE'),     -- 44
-- class 17 — JPN-N3, Spring 2026 (upcoming)
(25, 17, '2026-08-21', 'ACTIVE'),     -- 45
-- class 19 — CHN-HSK2, Summer 2027 (upcoming)
(16, 19, '2027-01-15', 'ACTIVE'),     -- 46
(17, 19, '2027-01-16', 'ACTIVE');     -- 47
GO

-- ============================================================
--  9. INVOICES — invoice_id 5 .. 47
--
--  Inserted in enrollment order, so invoice_id N belongs to enrollment_id N.
--  Amount comes from the CLASS's frozen tuition, not the course's current
--  price — that is the whole point of the snapshot.
--
--  Status is set here and then reconciled against Payments in section 11.
-- ============================================================
INSERT INTO Invoices
    (student_id, enrollment_id, original_amount, discount_id, discount_amount, amount, status, due_date, discount_status, note)
SELECT
    e.student_id,
    e.enrollment_id,
    c.snap_tuition_fee,
    NULL,
    0,
    c.snap_tuition_fee,
    'UNPAID',                                    -- reconciled below
    DATEADD(DAY, 30, e.enrolled_date),
    'NONE',
    NULL
FROM   Enrollments e
JOIN   Classes c ON c.class_id = e.class_id
WHERE  e.enrollment_id BETWEEN 5 AND 47
ORDER BY e.enrollment_id;
GO

-- A few invoices carry a discount, so the discount columns and the
-- "Discount status" filter are not uniformly empty.
UPDATE Invoices
SET    discount_id = 4,                          -- LOYAL10, 10%
       discount_amount = ROUND(original_amount * 0.10, 0),
       amount = original_amount - ROUND(original_amount * 0.10, 0),
       discount_status = 'ACTIVE',
       note = N'Học viên cũ đăng ký lại'
WHERE  invoice_id IN (13, 30, 35, 37);           -- returning students
GO

UPDATE Invoices
SET    discount_id = 6,                          -- SUMMER15, 15%, has a deadline
       discount_amount = ROUND(original_amount * 0.15, 0),
       amount = original_amount - ROUND(original_amount * 0.15, 0),
       discount_status = 'ACTIVE',
       discount_deadline = '2026-07-31',
       note = N'Khuyến mãi hè 2026'
WHERE  invoice_id IN (32, 33, 39, 40);
GO

UPDATE Invoices
SET    discount_id = 7,                          -- REWARD1M, fixed voucher
       discount_amount = 1000000,
       amount = original_amount - 1000000,
       discount_status = 'LOCKED',
       note = N'Voucher học bổng kỳ trước'
WHERE  invoice_id IN (22, 31);
GO

-- The cancelled class has no enrollments, but one student had an invoice
-- raised before the class was called off — cancelled rather than deleted so
-- the finance history stays auditable.
INSERT INTO Invoices
    (student_id, enrollment_id, original_amount, discount_amount, amount, status, due_date, discount_status, note)
VALUES
(6, NULL, 7200000, 0, 7200000, 'CANCELLED', '2026-09-30', 'NONE',
 N'Lớp C1-SP26-01 bị huỷ, hoàn tất hoàn tiền');   -- invoice 48
GO

-- ============================================================
--  10. PAYMENTS — payment_id 3 onwards
--
--  Past semesters are settled in full; the current semester is a mix of
--  paid, partly paid and untouched so the Debt List has real rows.
-- ============================================================

-- Fully settled: every invoice from the three past semesters (5..29),
-- minus the two dropped enrollments (9, 25) which were refunded instead.
INSERT INTO Payments (invoice_id, staff_id, amount_paid, payment_method, paid_at, receipt_code, note)
SELECT
    i.invoice_id,
    CASE WHEN i.invoice_id % 3 = 0 THEN 3 ELSE 2 END,       -- collected by a Finance staff member
    i.amount,
    CASE i.invoice_id % 4 WHEN 0 THEN 'Cash' WHEN 1 THEN 'Transfer'
                          WHEN 2 THEN 'Card' ELSE 'Wallet' END,
    DATEADD(DAY, 5, CAST(i.due_date AS DATETIME2)),
    CONCAT('RCP-EX-', RIGHT(CONCAT('0000', i.invoice_id), 4)),
    NULL
FROM   Invoices i
WHERE  i.invoice_id BETWEEN 5 AND 29
  AND  i.invoice_id NOT IN (9, 25);
GO

-- Current semester: some paid in full, some part-paid.
INSERT INTO Payments (invoice_id, staff_id, amount_paid, payment_method, paid_at, receipt_code, note) VALUES
(30, 2, 5220000, 'Transfer', '2026-06-10', 'RCP-EX-1030', NULL),          -- full (after 10% off)
(31, 2, 4800000, 'Cash',     '2026-06-11', 'RCP-EX-1031', NULL),          -- full (after voucher)
(32, 3, 2000000, 'Card',     '2026-06-12', 'RCP-EX-1032', N'Đợt 1'),      -- partial
(34, 3, 3000000, 'Transfer', '2026-06-14', 'RCP-EX-1034', N'Đợt 1'),      -- partial
(35, 2, 4590000, 'Wallet',   '2026-06-15', 'RCP-EX-1035', NULL),          -- full (after 10% off)
(37, 2, 5040000, 'Transfer', '2026-06-16', 'RCP-EX-1037', NULL),          -- full (after 10% off)
(39, 3, 1500000, 'Cash',     '2026-06-18', 'RCP-EX-1039', N'Đợt 1');      -- partial
GO

-- Reconcile invoice status against what was actually collected, so the Debt
-- List and the invoice screens agree with the Payments table.
UPDATE i
SET    i.status = CASE
                    WHEN p.paid >= i.amount THEN 'PAID'
                    WHEN p.paid > 0         THEN 'PARTIAL'
                    ELSE 'UNPAID'
                  END
FROM   Invoices i
JOIN  (SELECT invoice_id, SUM(amount_paid) AS paid
       FROM   Payments GROUP BY invoice_id) p ON p.invoice_id = i.invoice_id
WHERE  i.invoice_id BETWEEN 5 AND 47;
GO

-- The two dropped enrollments: invoice cancelled, money returned to wallet
-- (the wallet rows are in section 13).
UPDATE Invoices
SET    status = 'CANCELLED', note = N'Học viên đã rút khỏi lớp, hoàn tiền vào ví'
WHERE  invoice_id IN (9, 25);
GO

-- ============================================================
--  11. GRADES — for the completed classes only
--
--  Scores are derived from the row ids rather than hand-typed: deterministic,
--  so re-running on a fresh database gives the same numbers, and spread
--  across 6.0–9.9 so the weighted average has a believable distribution.
-- ============================================================
INSERT INTO Grades (enrollment_id, component_id, score, max_score)
SELECT
    e.enrollment_id,
    cgc.component_id,
    6.0 + (ABS(CHECKSUM(e.enrollment_id * 31 + cgc.component_id * 17)) % 40) / 10.0,
    10
FROM   Enrollments e
JOIN   ClassGradeComponents cgc ON cgc.class_id = e.class_id
WHERE  e.class_id BETWEEN 4 AND 11        -- the three past semesters
  AND  e.status = 'COMPLETED';            -- dropped students were never graded
GO

-- Partial grading in the current semester: attendance and midterm recorded,
-- finals not sat yet. This is what a class mid-flight actually looks like.
INSERT INTO Grades (enrollment_id, component_id, score, max_score)
SELECT
    e.enrollment_id,
    cgc.component_id,
    6.5 + (ABS(CHECKSUM(e.enrollment_id * 23 + cgc.component_id * 11)) % 35) / 10.0,
    10
FROM   Enrollments e
JOIN   ClassGradeComponents cgc ON cgc.class_id = e.class_id
WHERE  e.class_id BETWEEN 12 AND 15
  AND  e.status = 'ACTIVE'
  AND  cgc.name IN (N'Attendance', N'Midterm');
GO

-- ============================================================
--  12. SESSIONS — PAST classes only (class_id 4 .. 11)
--
--  Deliberately NOT seeded for classes in the current semester: the app
--  generates those on startup (App.xaml.cs → EnsureSessionsForSemester),
--  and GenerateSessionsForClass skips any class that already has sessions.
--  Seeding them would permanently disable that path for those classes.
--  EnsureSessionsForSemester only ever touches the ACTIVE semester, so
--  past classes are safe to fill in here.
--
--  The dates mirror SessionService exactly: start from the day after the
--  semester's setup phase ends, step a week at a time, order chronologically
--  across all weekly slots, then stop at the course's session count.
-- ============================================================
;WITH nums AS (
    SELECT TOP (60) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i
    FROM   sys.all_objects
),
planned AS (
    SELECT
        cs.class_id,
        cs.schedule_id,
        c.snap_duration_sessions,
        s.end_date AS semester_end,
        DATEADD(WEEK, n.i,
            DATEADD(DAY,
                -- days from the first teaching day to the slot's weekday.
                -- 1900-01-01 was a Monday, so DATEDIFF % 7 gives 0=Mon..6=Sun,
                -- matching day_of_week - 1.
                (((cs.day_of_week - 1)
                  - (DATEDIFF(DAY, '19000101', DATEADD(DAY, 1, s.setup_end_date)) % 7) + 7) % 7),
                DATEADD(DAY, 1, s.setup_end_date))
        ) AS session_date
    FROM   ClassSchedules cs
    JOIN   Classes   c ON c.class_id    = cs.class_id
    JOIN   Semesters s ON s.semester_id = c.semester_id
    CROSS JOIN nums n
    WHERE  cs.class_id BETWEEN 4 AND 11
),
capped AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY class_id ORDER BY session_date, schedule_id) AS rn
    FROM   planned
    WHERE  session_date <= semester_end
)
INSERT INTO Sessions (class_id, schedule_id, session_date, topic, status)
SELECT class_id, schedule_id, session_date,
       CONCAT(N'Buổi ', rn),
       'COMPLETED'                       -- these semesters are all in the past
FROM   capped
WHERE  rn <= snap_duration_sessions;
GO

-- Student attendance for every generated session. Roughly 1 in 20 absent and
-- 1 in 25 late, derived from the ids so the result is reproducible.
INSERT INTO Attendances (session_id, student_id, status, recorded_at)
SELECT
    s.session_id,
    e.student_id,
    CASE
        WHEN ABS(CHECKSUM(s.session_id * 7  + e.student_id * 3)) % 20 = 0 THEN 'ABSENT'
        WHEN ABS(CHECKSUM(s.session_id * 13 + e.student_id * 5)) % 25 = 0 THEN 'LATE'
        WHEN ABS(CHECKSUM(s.session_id * 29 + e.student_id * 7)) % 40 = 0 THEN 'EXCUSED'
        ELSE 'PRESENT'
    END,
    CAST(s.session_date AS DATETIME2)
FROM   Sessions s
JOIN   Enrollments e ON e.class_id = s.class_id
WHERE  s.class_id BETWEEN 4 AND 11    -- only the sessions seeded just above; the
                                      -- app may already have generated its own
                                      -- for the current semester's classes
  AND  e.status <> 'DROPPED';
GO

-- Teacher attendance for the same sessions.
INSERT INTO TeacherAttendances (session_id, teacher_id, status)
SELECT
    s.session_id,
    ct.teacher_id,
    CASE WHEN ABS(CHECKSUM(s.session_id * 11 + ct.teacher_id)) % 30 = 0
         THEN 'SUBSTITUTE' ELSE 'PRESENT' END
FROM   Sessions s
JOIN   ClassTeachers ct ON ct.class_id = s.class_id
WHERE  s.class_id BETWEEN 4 AND 11;
GO

-- ============================================================
--  13. WALLET
--
--  The ledger comes first, then Students.balance is recomputed from it, so
--  the two can never disagree. provider_order_id is unique and is only set
--  on ZaloPay top-ups — wallet spends and refunds have no provider order.
--
--  Student 15 tops up and then immediately spends it on invoice 35, which
--  section 10 records as a 'Wallet' payment: the two sides of that one
--  transaction are meant to line up.
-- ============================================================
INSERT INTO WalletTransactions (student_id, amount, transaction_type, provider_order_id, description, status, created_at) VALUES
-- Completed top-ups via ZaloPay
(5,  1500000, 'TOP_UP',  '260615_000001', N'Nạp tiền qua ZaloPay',            'COMPLETED', '2026-06-15'),
(7,   500000, 'TOP_UP',  '260616_000002', N'Nạp tiền qua ZaloPay',            'COMPLETED', '2026-06-16'),
(10, 2000000, 'TOP_UP',  '260617_000003', N'Nạp tiền qua ZaloPay',            'COMPLETED', '2026-06-17'),
(14,  750000, 'TOP_UP',  '260618_000004', N'Nạp tiền qua ZaloPay',            'COMPLETED', '2026-06-18'),
(19,  300000, 'TOP_UP',  '260619_000005', N'Nạp tiền qua ZaloPay',            'COMPLETED', '2026-06-19'),
(24, 1200000, 'TOP_UP',  '260620_000006', N'Nạp tiền qua ZaloPay',            'COMPLETED', '2026-06-20'),
-- Topped up then spent on tuition — net zero, matching the Wallet payment
-- recorded against invoice 35 in section 10.
(15, 4590000, 'TOP_UP',  '260614_000007', N'Nạp tiền qua ZaloPay',            'COMPLETED', '2026-06-14'),
(15, 4590000, 'PAYMENT', NULL,            N'Thanh toán học phí HSK3-FA25-01', 'COMPLETED', '2026-06-15'),
-- Refunds for the two dropped enrollments (invoices 9 and 25, cancelled above)
(8,  3500000, 'REFUND',  NULL,            N'Hoàn học phí lớp A1-SP25-01',     'COMPLETED', '2025-02-10'),
(22, 4000000, 'REFUND',  NULL,            N'Hoàn học phí lớp A2-WI26-01',     'COMPLETED', '2026-02-12'),
-- In-flight and failed top-ups, so the wallet history is not uniformly green.
-- Neither counts toward the balance — only COMPLETED rows do.
(4,   800000, 'TOP_UP',  '260722_000008', N'Đang chờ thanh toán',             'PENDING',   '2026-07-22'),
(11,  600000, 'TOP_UP',  '260710_000009', N'Giao dịch thất bại',              'FAILED',    '2026-07-10');
GO

-- Recompute every balance from the ledger. PENDING and FAILED rows are
-- excluded — money that has not actually arrived is not spendable.
UPDATE s
SET    s.balance = w.net
FROM   Students s
JOIN  (SELECT student_id,
              SUM(CASE WHEN transaction_type = 'PAYMENT' THEN -amount ELSE amount END) AS net
       FROM   WalletTransactions
       WHERE  status = 'COMPLETED'
       GROUP  BY student_id) w ON w.student_id = s.student_id;
GO

PRINT '==> Seed data inserted successfully.';
GO
