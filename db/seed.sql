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

INSERT INTO Courses (code, name, level, language, duration_sessions, tuition_fee) VALUES
('ENG-A1', N'English Beginner A1',     'A1', 'English',  40, 3500000),
('ENG-B1', N'English Intermediate B1', 'B1', 'English',  60, 5000000),
('JPN-N5', N'Japanese N5',             'N5', 'Japanese', 50, 4500000);
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

-- setup_end_date = start_date + 2 weeks (setup phase); learning phase runs setup_end_date..end_date
-- Fall 2025 (active) is dated around "today" so it's immediately in the LEARNING phase for testing
-- schedule generation without manually patching the DB.
INSERT INTO Semesters (name, start_date, end_date, setup_end_date, is_active) VALUES
(N'Summer 2025',   '2025-06-01', '2025-08-31', '2025-06-15', 0),
(N'Fall 2025',     '2026-06-01', '2026-08-31', '2026-06-15', 1),
(N'Spring 2026',   '2026-09-01', '2027-01-31', '2026-09-15', 0);
GO

-- All classes sit in the active semester (Fall 2025) so schedules/enrollments are testable immediately.
INSERT INTO Classes (semester_id, course_id, teacher_id, classroom_id, name, max_students, start_date, end_date, status) VALUES
(2, 1, 1, 1, 'A1-K01', 20, '2026-06-01', '2026-08-31', 'ONGOING'),
(2, 2, 1, 2, 'B1-K01', 18, '2026-06-01', '2026-08-31', 'ONGOING'),
(2, 3, 2, 3, 'N5-K01', 15, '2026-06-01', '2026-08-31', 'UPCOMING');
GO

-- day_of_week: 1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat, 7=Sun
-- Times match the fixed FAP-style slots in BusinessObjects/ScheduleSlot.cs so sessions line up in the grid.
INSERT INTO ClassSchedules (class_id, day_of_week, start_time, end_time) VALUES
(1, 1, '07:00', '09:15'),   -- Slot 1, Mon
(1, 3, '07:00', '09:15'),   -- Slot 1, Wed
(2, 2, '12:30', '14:45'),   -- Slot 3, Tue
(2, 4, '12:30', '14:45'),   -- Slot 3, Thu
(3, 6, '15:00', '17:15');   -- Slot 4, Sat
GO

-- Each course gets its own grading structure (course_id order matches Courses insert above:
-- 1=ENG-A1, 2=ENG-B1, 3=JPN-N5). Every course starts with the same default weights;
-- they can be customized per-course later via GradeType Management.
INSERT INTO GradeTypes (course_id, name, weight_percent, description) VALUES
(1, 'Attendance', 10, 'Attendance and participation score'),
(1, 'Midterm',    30, 'Midterm exam score'),
(1, 'Final',      60, 'Final exam score'),
(2, 'Attendance', 10, 'Attendance and participation score'),
(2, 'Midterm',    30, 'Midterm exam score'),
(2, 'Final',      60, 'Final exam score'),
(3, 'Attendance', 10, 'Attendance and participation score'),
(3, 'Midterm',    30, 'Midterm exam score'),
(3, 'Final',      60, 'Final exam score');
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

INSERT INTO Grades (enrollment_id, grade_type_id, score, max_score) VALUES
(1, 1, 9.0, 10),
(1, 2, 7.5, 10),
(2, 1, 8.0, 10);
GO

PRINT '==> Seed data inserted successfully.';
GO
