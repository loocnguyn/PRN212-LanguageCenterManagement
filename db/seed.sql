-- ============================================================
--  LANGUAGE CENTER MANAGEMENT SYSTEM - DEMO SEED
--  Run schema.sql first, then this file. Once, on a fresh database.
--
--  This is a SCRIPT, not a pile of data. Every row exists to make one screen
--  worth opening. If you delete a row, some feature loses its demo.
--
--  THE STORY
--    Spring 2026 has finished  -> a completed class with full marks and a
--                                 transcript, plus one student who dropped out
--                                 and got refunded.
--    Summer 2026 is running    -> two ongoing classes; sessions are NOT seeded,
--                                 the app generates them on startup. This is
--                                 where attendance, grade entry, the edit lock
--                                 and the per-session room change are demoed.
--    Fall 2026 is upcoming     -> one class open for enrollment, and one that
--                                 was cancelled.
--
--  LOGINS - the email IS the login. Password is 123456 for every account.
--  Addresses are numbered by role so they are guessable on stage: whatever you
--  need mid-demo, it is <role><nn>@gmail.com.
--
--    admin@gmail.com       ADMIN     the only administrator
--    staff01@gmail.com     STAFF     Academic Setup - classes, semesters, rooms
--    staff02@gmail.com     STAFF     Finance - invoices, payments, revenue report
--    teacher01@gmail.com   TEACHER   English  - the finished class + an ongoing one
--    teacher02@gmail.com   TEACHER   Japanese - primary on the co-taught class
--    teacher03@gmail.com   TEACHER   Korean   - co-teacher, and the upcoming class
--    teacher04@gmail.com   TEACHER   DEACTIVATED - for the Deactivated Accounts screen
--    student01@gmail.com   STUDENT   Pham Thi Cam   - finished A1, now in B1, all paid
--    student02@gmail.com   STUDENT   Do Quoc Hung   - part-paid, appears on the Debt List
--    student03@gmail.com   STUDENT   Nguyen Mai Ly  - discounted invoice, OVERDUE
--    student04@gmail.com   STUDENT   Tran Minh Khoa - paid out of the wallet
--    student05@gmail.com   STUDENT   Le Thi Hoa     - dropped out, cancelled + refunded
--    student06@gmail.com   STUDENT   Vo Gia Bao     - MUST CHANGE PASSWORD at first login
-- ============================================================

USE LanguageCenterDB;
GO

-- ============================================================
--  1. ACCOUNTS - Users.id 1..27
--
--  One shared BCrypt hash of "123456": a real weakness in production, exactly
--  what you want in a seed. must_change_password is 0 everywhere except Gia Bao,
--  who exists to demo the forced-change screen.
--
--  The first thirteen accounts each play a part in the story above. Students
--  07..20 that follow are the rest of the cohort - no story, just enough bodies
--  in the rooms that the rosters and reports look like a real centre.
-- ============================================================
DECLARE @pw NVARCHAR(256) = '$2a$11$U7t7m.JhCoIZBzC.edRuMeKLroLVwOrI05B.WnVSrJPbQT3qrLPiK';

INSERT INTO Users (email, password_hash, role, is_active, must_change_password) VALUES
('admin@gmail.com',     @pw, 'ADMIN',   1, 0),   -- 1
('staff01@gmail.com',   @pw, 'STAFF',   1, 0),   -- 2
('staff02@gmail.com',   @pw, 'STAFF',   1, 0),   -- 3
('teacher01@gmail.com', @pw, 'TEACHER', 1, 0),   -- 4
('teacher02@gmail.com', @pw, 'TEACHER', 1, 0),   -- 5
('teacher03@gmail.com', @pw, 'TEACHER', 1, 0),   -- 6
('teacher04@gmail.com', @pw, 'TEACHER', 0, 0),   -- 7  deactivated account
('student01@gmail.com', @pw, 'STUDENT', 1, 0),   -- 8
('student02@gmail.com', @pw, 'STUDENT', 1, 0),   -- 9
('student03@gmail.com', @pw, 'STUDENT', 1, 0),   -- 10
('student04@gmail.com', @pw, 'STUDENT', 1, 0),   -- 11
('student05@gmail.com', @pw, 'STUDENT', 1, 0),   -- 12
('student06@gmail.com', @pw, 'STUDENT', 1, 1);   -- 13 forced password change
GO

INSERT INTO Admins (user_id, full_name, phone, email) VALUES
(1, N'System Admin', '0900000000', 'admin@gmail.com');
GO

-- Only "Finance" unlocks the finance menus; every other department falls through
-- to the academic group - see MainWindow.ApplyStaffDepartmentVisibility.
INSERT INTO Departments (name) VALUES
(N'Academic Setup'),   -- 1
(N'Finance');          -- 2
GO

INSERT INTO Staff (user_id, full_name, date_of_birth, gender, phone, email, department) VALUES
(2, N'Nguyen Van An',  '1992-03-14', 'Male',   '0901000001', 'staff01@gmail.com',   N'Academic Setup'),  -- 1
(3, N'Tran Thi Kim',   '1990-08-22', 'Female', '0901000002', 'staff02@gmail.com',   N'Finance');         -- 2
GO

INSERT INTO Teachers (user_id, full_name, date_of_birth, gender, phone, email, specialization, degree, status) VALUES
(4, N'Tran Thi Binh',  '1988-02-14', 'Female', '0902000001', 'teacher01@gmail.com', N'English',  N'Master',   'ACTIVE'),   -- 1
(5, N'Le Minh Khoa',   '1985-09-30', 'Male',   '0902000002', 'teacher02@gmail.com', N'Japanese', N'Bachelor', 'ACTIVE'),   -- 2
(6, N'Kim Ji Woo',     '1991-06-08', 'Female', '0902000003', 'teacher03@gmail.com', N'Korean',   N'Master',   'ACTIVE'),   -- 3
(7, N'Tran Van Loc',   '1987-01-23', 'Male',   '0902000004', 'teacher04@gmail.com', N'German',   N'Bachelor', 'RESIGNED'); -- 4
GO

INSERT INTO Students (user_id, full_name, date_of_birth, gender, phone, email, address, balance, status) VALUES
(8,  N'Pham Thi Cam',   '2003-05-12', 'Female', '0903000001', 'student01@gmail.com', N'12 Le Loi, Q1',        0, 'ACTIVE'),  -- 1
(9,  N'Do Quoc Hung',   '2002-08-20', 'Male',   '0903000002', 'student02@gmail.com', N'45 Nguyen Trai, Q5',   0, 'ACTIVE'),  -- 2
(10, N'Nguyen Mai Ly',  '2004-01-30', 'Female', '0903000003', 'student03@gmail.com', N'7 Tran Hung Dao, Q1',  0, 'ACTIVE'),  -- 3
(11, N'Tran Minh Khoa', '2003-11-02', 'Male',   '0903000004', 'student04@gmail.com', N'89 Cach Mang, Q3',     0, 'ACTIVE'),  -- 4
(12, N'Le Thi Hoa',     '2002-04-18', 'Female', '0903000005', 'student05@gmail.com', N'23 Hai Ba Trung, Q1',  0, 'ACTIVE'),  -- 5
(13, N'Vo Gia Bao',     '2005-07-09', 'Male',   '0903000006', 'student06@gmail.com', N'56 Vo Van Tan, Q3',    0, 'ACTIVE');  -- 6
GO

-- The rest of the cohort - students 7..20, users 14..27. These carry no special
-- story; they are here so the class rosters, the grade entry grid, the attendance
-- sheets and the revenue report have a believable amount of data on screen.
INSERT INTO Users (email, password_hash, role, is_active, must_change_password)
SELECT CONCAT('student', RIGHT(CONCAT('0', n), 2), '@gmail.com'),
       '$2a$11$U7t7m.JhCoIZBzC.edRuMeKLroLVwOrI05B.WnVSrJPbQT3qrLPiK', 'STUDENT', 1, 0
FROM  (VALUES (7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20)) AS v(n);
GO

INSERT INTO Students (user_id, full_name, date_of_birth, gender, phone, email, address, balance, status) VALUES
(14, N'Nguyen Thi Ngoc Anh', '2003-03-11', 'Female', '0903000007', 'student07@gmail.com', N'12 Le Loi, Q1',          0, 'ACTIVE'),  -- 7
(15, N'Tran Quang Huy',      '2002-07-19', 'Male',   '0903000008', 'student08@gmail.com', N'45 Nguyen Trai, Q5',     0, 'ACTIVE'),  -- 8
(16, N'Le Thi Thu Ha',       '2004-11-05', 'Female', '0903000009', 'student09@gmail.com', N'7 Tran Hung Dao, Q1',    0, 'ACTIVE'),  -- 9
(17, N'Pham Van Dat',        '2001-05-28', 'Male',   '0903000010', 'student10@gmail.com', N'89 Cach Mang, Q3',       0, 'ACTIVE'),  -- 10
(18, N'Hoang Thi Lan Anh',   '2003-09-14', 'Female', '0903000011', 'student11@gmail.com', N'23 Hai Ba Trung, Q1',    0, 'ACTIVE'),  -- 11
(19, N'Vu Minh Quan',        '2002-01-30', 'Male',   '0903000012', 'student12@gmail.com', N'56 Vo Van Tan, Q3',      0, 'ACTIVE'),  -- 12
(20, N'Dinh Thi Kim Chi',    '2004-06-22', 'Female', '0903000013', 'student13@gmail.com', N'11 Pasteur, Q1',         0, 'ACTIVE'),  -- 13
(21, N'Ngo Thanh Tung',      '2000-12-03', 'Male',   '0903000014', 'student14@gmail.com', N'78 Dien Bien Phu, BT',   0, 'ACTIVE'),  -- 14
(22, N'Bui Thi Hong Nhung',  '2003-04-17', 'Female', '0903000015', 'student15@gmail.com', N'34 Nam Ky, Q3',          0, 'ACTIVE'),  -- 15
(23, N'Duong Gia Han',       '2005-02-09', 'Female', '0903000016', 'student16@gmail.com', N'90 Ly Tu Trong, Q1',     0, 'ACTIVE'),  -- 16
(24, N'Truong Minh Khang',   '2002-08-26', 'Male',   '0903000017', 'student17@gmail.com', N'15 Suong Nguyet, Q1',    0, 'ACTIVE'),  -- 17
(25, N'Phan Thi Yen Nhi',    '2004-10-12', 'Female', '0903000018', 'student18@gmail.com', N'62 Vo Thi Sau, Q3',      0, 'ACTIVE'),  -- 18
(26, N'Lam Quoc Thang',      '2001-03-07', 'Male',   '0903000019', 'student19@gmail.com', N'28 Nguyen Hue, Q1',      0, 'ACTIVE'),  -- 19
(27, N'Cao Thi Thanh Truc',  '2003-12-19', 'Female', '0903000020', 'student20@gmail.com', N'41 Ton Duc Thang, Q1',   0, 'ACTIVE');  -- 20
GO

-- ============================================================
--  2. CATALOGUE - three languages, each with a real level ladder
--
--  Courses pick a language then one of THAT language's levels, which is why
--  levels are per-language rather than one flat list. Inserted beginner-first,
--  so level_id doubles as the teaching order the dropdowns read in.
-- ============================================================
INSERT INTO Languages (name) VALUES
(N'English'),    -- 1
(N'Japanese'),   -- 2
(N'Korean');     -- 3
GO

INSERT INTO Levels (language_id, name) VALUES
(1, N'A1'), (1, N'A2'), (1, N'B1'), (1, N'B2'),   -- 1..4  CEFR
(2, N'N5'), (2, N'N4'),                            -- 5,6   JLPT, N5 is the beginner end
(3, N'TOPIK 1'), (3, N'TOPIK 2');                  -- 7,8
GO

INSERT INTO Courses (code, name, language_id, level_id, duration_sessions, tuition_fee, description) VALUES
('ENG-A1', N'English Beginner A1',     1, 1, 24, 3500000, N'Nhap mon giao tiep hang ngay'),      -- 1
('ENG-B1', N'English Intermediate B1', 1, 3, 32, 5000000, N'Nghe noi trung cap, luyen phan xa'), -- 2
('JPN-N5', N'Japanese N5',             2, 5, 30, 4500000, N'Hiragana, Katakana va ngu phap N5'), -- 3
('KOR-T1', N'Korean TOPIK 1',          3, 7, 24, 3900000, N'So cap, chuan bi TOPIK I'),          -- 4
('ENG-B2', N'English Upper-Int. B2',   1, 4, 32, 5800000, N'Hoc thuat va thuyet trinh');         -- 5
GO

-- TEMPLATE only. Nothing points at these rows - they are COPIED into a class's
-- own ClassGradeComponents when the class is created, and the copy is frozen.
-- Editing these changes what FUTURE classes inherit, never an existing class.
-- Every course needs one: ClassService refuses to open a class without it.
INSERT INTO GradeTypes (course_id, name, weight_percent, description)
SELECT c.course_id, v.name, v.weight_percent, v.description
FROM   Courses c
CROSS JOIN (VALUES
    (N'Attendance', CAST(10 AS DECIMAL(5,2)), N'Attendance and participation'),
    (N'Midterm',    CAST(30 AS DECIMAL(5,2)), N'Midterm exam'),
    (N'Final',      CAST(60 AS DECIMAL(5,2)), N'Final exam')
) AS v(name, weight_percent, description);
GO

INSERT INTO Classrooms (name, capacity, location, is_active) VALUES
('Room 101', 20, 'Floor 1', 1),   -- 1
('Room 201', 18, 'Floor 2', 1),   -- 2
('Room 301', 15, 'Floor 3', 1),   -- 3
('Room 401', 30, 'Floor 4', 0);   -- 4  under renovation - demoes the inactive filter
GO

-- FAP-style daily slots, editable from the Slot Time Setting screen.
INSERT INTO Slots (slot_no, start_time, end_time) VALUES
(1, '07:00', '09:15'),
(2, '09:30', '11:45'),
(3, '12:30', '14:45'),
(4, '15:00', '17:15'),
(5, '17:30', '19:45'),
(6, '20:00', '22:15');
GO

-- ============================================================
--  3. SEMESTERS - one finished, one running, one upcoming
--
--  There is no is_active flag: the current semester is whichever one contains
--  today, so these three must never overlap. setup_end_date closes the setup
--  phase; the learning phase runs from the day after it to end_date, and that
--  is what session generation lays sessions out in.
-- ============================================================
INSERT INTO Semesters (name, start_date, setup_end_date, end_date) VALUES
(N'Spring 2026', '2026-01-05', '2026-01-18', '2026-05-29'),   -- 1  finished
(N'Summer 2026', '2026-06-01', '2026-06-14', '2026-08-28'),   -- 2  RUNNING (contains today)
(N'Fall 2026',   '2026-09-07', '2026-09-20', '2026-12-25');   -- 3  upcoming
GO

-- ============================================================
--  4. CLASSES - one of every status
--
--  snap_* is the frozen copy of the course at creation time. Written literally
--  because the seed does not run through ClassService. Change a course's fee
--  afterwards and these classes keep the price their students were charged.
--
--  Status is DERIVED from the dates, never stored; is_cancelled covers the one
--  state dates cannot express.
-- ============================================================
INSERT INTO Classes
    (semester_id, course_id, classroom_id, name, max_students, start_date, end_date, is_cancelled,
     snap_course_code, snap_course_name, snap_language, snap_level, snap_duration_sessions, snap_tuition_fee)
VALUES
-- 1  COMPLETED - full marks, attendance history, printable transcript
(1, 1, 1, 'A1-SP26', 20, '2026-01-19', '2026-05-29', 0,
 'ENG-A1', N'English Beginner A1',     N'English',  'A1',      24, 3500000),
-- 2  ONGOING - try editing it: start date and room are locked
(2, 2, 2, 'B1-SU26', 18, '2026-06-15', '2026-08-28', 0,
 'ENG-B1', N'English Intermediate B1', N'English',  'B1',      32, 5000000),
-- 3  ONGOING and co-taught - two teachers, one flagged primary
(2, 3, 3, 'N5-SU26', 15, '2026-06-15', '2026-08-28', 0,
 'JPN-N5', N'Japanese N5',             N'Japanese', 'N5',      30, 4500000),
-- 4  UPCOMING - still fully editable, still open for enrollment
(3, 4, 1, 'K1-FA26', 20, '2026-09-21', '2026-12-25', 0,
 'KOR-T1', N'Korean TOPIK 1',          N'Korean',   'TOPIK 1', 24, 3900000),
-- 5  CANCELLED - kept, not deleted, so the finance history stays auditable
(3, 5, 2, 'B2-FA26', 15, '2026-09-21', '2026-12-25', 1,
 'ENG-B2', N'English Upper-Int. B2',   N'English',  'B2',      32, 5800000);
GO

-- Exactly one primary teacher per class (a filtered unique index enforces it).
-- Class 3 is co-taught, to exercise the multi-teacher path.
INSERT INTO ClassTeachers (class_id, teacher_id, is_primary) VALUES
(1, 1, 1),
(2, 1, 1),
(3, 2, 1), (3, 3, 0),
(4, 3, 1),
(5, 1, 1);
GO

-- day_of_week: 1=Mon .. 7=Sun. Times match the Slots above so the weekly grid lines up.
INSERT INTO ClassSchedules (class_id, day_of_week, start_time, end_time) VALUES
(1, 1, '07:00', '09:15'), (1, 3, '07:00', '09:15'),   -- Mon + Wed, slot 1
(2, 2, '17:30', '19:45'), (2, 4, '17:30', '19:45'),   -- Tue + Thu, slot 5
(3, 1, '09:30', '11:45'), (3, 5, '09:30', '11:45'),   -- Mon + Fri, slot 2
(4, 6, '15:00', '17:15'),                             -- Sat, slot 4
(5, 2, '12:30', '14:45');                             -- Tue, slot 3
GO

-- The frozen per-class copy of the grading structure, copied from the course
-- template above. component_id: class 1 -> 1,2,3 / class 2 -> 4,5,6 / ... / class 5 -> 13,14,15
INSERT INTO ClassGradeComponents (class_id, name, weight_percent, description, sort_order)
SELECT c.class_id, v.name, v.weight_percent, v.description, v.sort_order
FROM   Classes c
CROSS JOIN (VALUES
    (N'Attendance', CAST(10 AS DECIMAL(5,2)), N'Attendance and participation', 1),
    (N'Midterm',    CAST(30 AS DECIMAL(5,2)), N'Midterm exam',                 2),
    (N'Final',      CAST(60 AS DECIMAL(5,2)), N'Final exam',                   3)
) AS v(name, weight_percent, description, sort_order);
GO

-- ============================================================
--  5. DISCOUNTS - one percentage, one fixed, one expired
-- ============================================================
INSERT INTO TuitionDiscounts
    (code, name, discount_type, discount_value, start_date, end_date, is_active, note, payment_deadline_days, condition_type)
VALUES
('EARLY5',   N'Early payment 5%',      'PERCENT', 5,      NULL,         NULL,         1,
 N'Giam 5% neu thanh toan du trong 7 ngay ke tu ngay dang ky', 7, 'EARLY_PAYMENT'),          -- 1
('REWARD500K', N'Scholarship 500,000', 'FIXED',   500000, NULL,         NULL,         1,
 N'Ho tro co dinh cho hoc vien xuat sac', NULL, 'NONE'),                                      -- 2
('OLDPROMO', N'Expired 2025 promo',    'PERCENT', 12,     '2025-01-01', '2025-03-31', 0,
 N'Da ket thuc - giu lai de kiem thu bo loc', NULL, 'NONE');                                  -- 3
GO

-- ============================================================
--  6. ENROLLMENTS - 30 of them, ACTIVE / COMPLETED / DROPPED
-- ============================================================
INSERT INTO Enrollments (student_id, class_id, enrolled_date, status) VALUES
(1, 1, '2026-01-08', 'COMPLETED'),   -- 1  Cam  finished A1
(2, 1, '2026-01-08', 'COMPLETED'),   -- 2  Hung finished A1
(5, 1, '2026-01-09', 'DROPPED'),     -- 3  Hoa  dropped out -> refunded
(1, 2, '2026-06-02', 'ACTIVE'),      -- 4  Cam  now in B1
(3, 2, '2026-06-03', 'ACTIVE'),      -- 5  Ly   in B1, discounted + overdue
(2, 3, '2026-06-02', 'ACTIVE'),      -- 6  Hung in N5, part-paid
(4, 3, '2026-06-04', 'ACTIVE'),      -- 7  Khoa in N5, pays from wallet
(6, 4, '2026-07-20', 'ACTIVE');      -- 8  Bao  in the upcoming class
GO

-- The rest of the cohort - enrollment_id 9..30. Several students appear in more
-- than one class, which is what makes the per-student history screens worth
-- opening, and what fills the class rosters up to a believable size:
--   A1-SP26  10 of 20   B1-SU26  8 of 18   N5-SU26  7 of 15   K1-FA26  5 of 20
INSERT INTO Enrollments (student_id, class_id, enrolled_date, status) VALUES
-- class 1, A1-SP26 (finished) - these get full marks and a full attendance sheet
( 7, 1, '2026-01-08', 'COMPLETED'), ( 8, 1, '2026-01-08', 'COMPLETED'),
( 9, 1, '2026-01-09', 'COMPLETED'), (10, 1, '2026-01-09', 'COMPLETED'),
(11, 1, '2026-01-10', 'COMPLETED'), (12, 1, '2026-01-10', 'COMPLETED'),
(13, 1, '2026-01-12', 'COMPLETED'),
-- class 2, B1-SU26 (running) - attendance + midterm only
( 7, 2, '2026-06-02', 'ACTIVE'), ( 8, 2, '2026-06-03', 'ACTIVE'),
(14, 2, '2026-06-03', 'ACTIVE'), (15, 2, '2026-06-04', 'ACTIVE'),
(16, 2, '2026-06-05', 'ACTIVE'), (17, 2, '2026-06-08', 'ACTIVE'),
-- class 3, N5-SU26 (running, co-taught)
( 9, 3, '2026-06-02', 'ACTIVE'), (10, 3, '2026-06-03', 'ACTIVE'),
(18, 3, '2026-06-04', 'ACTIVE'), (19, 3, '2026-06-05', 'ACTIVE'),
(20, 3, '2026-06-09', 'ACTIVE'),
-- class 4, K1-FA26 (upcoming) - enrolled early, nothing has happened yet
(11, 4, '2026-07-21', 'ACTIVE'), (12, 4, '2026-07-22', 'ACTIVE'),
(13, 4, '2026-07-23', 'ACTIVE'), (20, 4, '2026-07-24', 'ACTIVE');
GO

-- ============================================================
--  7. INVOICES - one per enrollment; the first eight cover every payment state
--
--  The amount comes from the CLASS's frozen tuition, not the course's current
--  price. Statuses are reconciled against Payments in section 8.
-- ============================================================
INSERT INTO Invoices
    (student_id, enrollment_id, original_amount, discount_id, discount_amount, amount,
     status, due_date, discount_status, discount_deadline, note)
VALUES
(1, 1, 3500000, NULL, 0,      3500000, 'UNPAID', '2026-02-07', 'NONE',   NULL, NULL),          -- 1 -> PAID
(2, 2, 3500000, NULL, 0,      3500000, 'UNPAID', '2026-02-07', 'NONE',   NULL, NULL),          -- 2 -> PAID
(5, 3, 3500000, NULL, 0,      3500000, 'UNPAID', '2026-02-08', 'NONE',   NULL, NULL),          -- 3 -> CANCELLED
(1, 4, 5000000, NULL, 0,      5000000, 'UNPAID', '2026-07-02', 'NONE',   NULL, NULL),          -- 4 -> PAID
(3, 5, 5000000, 1,    250000, 4750000, 'UNPAID', '2026-07-03', 'ACTIVE', '2026-07-10',
 N'Giam 5% thanh toan som'),                                                                    -- 5 -> OVERDUE, unpaid
(2, 6, 4500000, NULL, 0,      4500000, 'UNPAID', '2026-08-02', 'NONE',   NULL, NULL),          -- 6 -> PARTIAL
(4, 7, 4500000, NULL, 0,      4500000, 'UNPAID', '2026-08-04', 'NONE',   NULL, NULL),          -- 7 -> PAID from wallet
(6, 8, 3900000, NULL, 0,      3900000, 'UNPAID', '2026-09-30', 'NONE',   NULL, NULL);          -- 8 -> not due yet
GO

-- Invoices 9..30 for the rest of the cohort. Generated rather than typed out:
-- the amount is the CLASS's frozen tuition and the due date is 30 days after the
-- student enrolled, which is the same rule the app applies.
INSERT INTO Invoices
    (student_id, enrollment_id, original_amount, discount_id, discount_amount, amount,
     status, due_date, discount_status, note)
SELECT e.student_id, e.enrollment_id, c.snap_tuition_fee, NULL, 0, c.snap_tuition_fee,
       'UNPAID', DATEADD(DAY, 30, e.enrolled_date), 'NONE', NULL
FROM   Enrollments e
JOIN   Classes c ON c.class_id = e.class_id
WHERE  e.enrollment_id >= 9
ORDER  BY e.enrollment_id;
GO

-- Two of them get the fixed 500,000 scholarship, so the discount columns are not
-- uniformly empty and the Revenue Report has something to net off.
UPDATE Invoices
SET    discount_id = 2, discount_amount = 500000, amount = original_amount - 500000,
       discount_status = 'ACTIVE', note = N'Ho tro hoc bong 500,000'
WHERE  invoice_id IN (16, 22);
GO

-- ============================================================
--  8. PAYMENTS - collected by the Finance staff member (staff_id 2)
-- ============================================================
INSERT INTO Payments (invoice_id, staff_id, amount_paid, payment_method, paid_at, receipt_code, note) VALUES
(1, 2, 3500000, 'Cash',     '2026-01-12', 'RCP-2026-0001', NULL),
(2, 2, 3500000, 'Transfer', '2026-01-13', 'RCP-2026-0002', NULL),
(4, 2, 5000000, 'Transfer', '2026-06-08', 'RCP-2026-0003', NULL),
(6, 2, 2000000, 'Cash',     '2026-06-20', 'RCP-2026-0004', N'Dot 1'),
(7, 2, 4500000, 'Wallet',   '2026-07-05', 'RCP-2026-0005', N'Tru tu vi hoc vien');
GO

-- The rest of the cohort settles up, with three deliberate exceptions so the Debt
-- List is not empty: invoices 25, 27 and 29 are never paid.
INSERT INTO Payments (invoice_id, staff_id, amount_paid, payment_method, paid_at, receipt_code, note)
SELECT i.invoice_id,
       CASE WHEN i.invoice_id % 2 = 0 THEN 2 ELSE 1 END,        -- mostly Finance, some Academic
       i.amount,
       CASE i.invoice_id % 3 WHEN 0 THEN 'Cash' WHEN 1 THEN 'Transfer' ELSE 'Card' END,
       DATEADD(DAY, 3, CAST(i.due_date AS DATETIME2)),
       CONCAT('RCP-2026-', RIGHT(CONCAT('000', i.invoice_id + 100), 4)),
       NULL
FROM   Invoices i
WHERE  i.invoice_id >= 9
  AND  i.invoice_id NOT IN (25, 27, 29);
GO

-- Reconcile invoice status against what was actually collected, so the Debt List
-- and the invoice screens agree with the Payments table.
UPDATE i
SET    i.status = CASE WHEN p.paid >= i.amount THEN 'PAID'
                       WHEN p.paid > 0         THEN 'PARTIAL'
                       ELSE 'UNPAID' END
FROM   Invoices i
JOIN  (SELECT invoice_id, SUM(amount_paid) AS paid FROM Payments GROUP BY invoice_id) p
       ON p.invoice_id = i.invoice_id;
GO

-- The dropped enrollment: invoice cancelled, money returned to the wallet.
UPDATE Invoices
SET    status = 'CANCELLED', note = N'Hoc vien rut khoi lop - da hoan tien vao vi'
WHERE  invoice_id = 3;
GO

-- ============================================================
--  9. WALLET - a top-up spent on tuition, a refund, and two that never landed
--
--  The ledger comes first, then Students.balance is recomputed from it, so the
--  two can never disagree. provider_order_id is set only on ZaloPay top-ups.
-- ============================================================
INSERT INTO WalletTransactions (student_id, amount, transaction_type, provider_order_id, description, status, created_at) VALUES
-- Khoa tops up, then spends it on invoice 7 - the two sides of the same transaction
(4, 5000000, 'TOP_UP',  '260704_000001', N'Nap tien qua ZaloPay',           'COMPLETED', '2026-07-04'),
(4, 4500000, 'PAYMENT', NULL,            N'Thanh toan hoc phi N5-SU26',     'COMPLETED', '2026-07-05'),
-- Hoa dropped out and was refunded
(5, 3500000, 'REFUND',  NULL,            N'Hoan hoc phi lop A1-SP26',       'COMPLETED', '2026-02-15'),
-- Not everything is green: one waiting, one failed. Neither counts to the balance.
(1, 1000000, 'TOP_UP',  '260726_000002', N'Dang cho thanh toan',            'PENDING',   '2026-07-26'),
(2,  800000, 'TOP_UP',  '260722_000003', N'Giao dich that bai',             'FAILED',    '2026-07-22');
GO

UPDATE s
SET    s.balance = w.net
FROM   Students s
JOIN  (SELECT student_id,
              SUM(CASE WHEN transaction_type = 'PAYMENT' THEN -amount ELSE amount END) AS net
       FROM   WalletTransactions
       WHERE  status = 'COMPLETED'          -- money that has not arrived is not spendable
       GROUP  BY student_id) w ON w.student_id = s.student_id;
GO

-- ============================================================
--  10. SESSIONS - the FINISHED class only (class 1)
--
--  Deliberately NOT seeded for the running semester: the app generates those on
--  startup (App.xaml.cs -> EnsureSessionsForSemester), and GenerateSessionsForClass
--  skips any class that already has sessions. Seeding them here would permanently
--  disable that feature for the classes you want to demo it on.
--
--  The dates mirror SessionService exactly: start the day after the semester's
--  setup phase ends, step a week at a time, order chronologically across the
--  weekly slots, then stop at the course's session count.
-- ============================================================
;WITH nums AS (
    SELECT TOP (40) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i
    FROM   sys.all_objects
),
planned AS (
    SELECT cs.class_id, cs.schedule_id, c.snap_duration_sessions, s.end_date AS semester_end,
           DATEADD(WEEK, n.i,
               DATEADD(DAY,
                   -- days from the first teaching day to this slot's weekday.
                   -- 1900-01-01 was a Monday, so DATEDIFF % 7 gives 0=Mon..6=Sun.
                   (((cs.day_of_week - 1)
                     - (DATEDIFF(DAY, '19000101', DATEADD(DAY, 1, s.setup_end_date)) % 7) + 7) % 7),
                   DATEADD(DAY, 1, s.setup_end_date))
           ) AS session_date
    FROM   ClassSchedules cs
    JOIN   Classes   c ON c.class_id    = cs.class_id
    JOIN   Semesters s ON s.semester_id = c.semester_id
    CROSS JOIN nums n
    WHERE  cs.class_id = 1
),
capped AS (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY class_id ORDER BY session_date, schedule_id) AS rn
    FROM   planned
    WHERE  session_date <= semester_end
)
INSERT INTO Sessions (class_id, schedule_id, session_date, topic, status)
SELECT class_id, schedule_id, session_date, CONCAT(N'Buoi ', rn), 'COMPLETED'
FROM   capped
WHERE  rn <= snap_duration_sessions;
GO

-- Attendance for every session of that class. Roughly 1 in 12 absent and 1 in 15
-- late, derived from the ids so a fresh database always gives the same history.
INSERT INTO Attendances (session_id, student_id, status, recorded_at)
SELECT s.session_id, e.student_id,
       CASE WHEN ABS(CHECKSUM(s.session_id * 7  + e.student_id * 3)) % 12 = 0 THEN 'ABSENT'
            WHEN ABS(CHECKSUM(s.session_id * 13 + e.student_id * 5)) % 15 = 0 THEN 'LATE'
            ELSE 'PRESENT' END,
       CAST(s.session_date AS DATETIME2)
FROM   Sessions s
JOIN   Enrollments e ON e.class_id = s.class_id
WHERE  e.status <> 'DROPPED';           -- the student who left was not marked after leaving
GO

INSERT INTO TeacherAttendances (session_id, teacher_id, status)
SELECT s.session_id, ct.teacher_id,
       CASE WHEN ABS(CHECKSUM(s.session_id * 11 + ct.teacher_id)) % 20 = 0
            THEN 'SUBSTITUTE' ELSE 'PRESENT' END
FROM   Sessions s
JOIN   ClassTeachers ct ON ct.class_id = s.class_id;
GO

-- ============================================================
--  11. GRADES
--
--  Two states on purpose:
--    the finished class is fully marked  -> a complete weighted average
--    the running classes are part marked -> "chua du dau diem" on screen
--  Scores are derived from the ids, so they are believable and reproducible.
-- ============================================================

-- Finished class: all three components for both students who completed it.
INSERT INTO Grades (enrollment_id, component_id, score, max_score, graded_at)
SELECT e.enrollment_id, cgc.component_id,
       6.5 + (ABS(CHECKSUM(e.enrollment_id * 31 + cgc.component_id * 17)) % 35) / 10.0,
       10, '2026-05-25'
FROM   Enrollments e
JOIN   ClassGradeComponents cgc ON cgc.class_id = e.class_id
WHERE  e.class_id = 1 AND e.status = 'COMPLETED';
GO

-- Running classes: attendance and midterm are in, finals have not been sat.
-- This is what a class mid-flight actually looks like in the grade screens.
INSERT INTO Grades (enrollment_id, component_id, score, max_score, graded_at)
SELECT e.enrollment_id, cgc.component_id,
       6.0 + (ABS(CHECKSUM(e.enrollment_id * 23 + cgc.component_id * 11)) % 40) / 10.0,
       10, '2026-07-15'
FROM   Enrollments e
JOIN   ClassGradeComponents cgc ON cgc.class_id = e.class_id
WHERE  e.class_id IN (2, 3)
  AND  e.status = 'ACTIVE'
  AND  cgc.name IN (N'Attendance', N'Midterm');
GO

PRINT '==> Demo seed inserted. Sign in as admin@gmail.com / 123456';
GO
