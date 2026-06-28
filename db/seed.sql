-- ============================================================
--  LANGUAGE CENTER MANAGEMENT SYSTEM — SEED DATA
--  Run schema.sql first before running this file
--
--  Default passwords (plain text, no hashing):
--    admin01   / 123456
--    staff01   / 123456
--    teacher01 / 123456
--    teacher02 / 123456
--    student01 / 123456
--    student02 / 123456
--    student03 / 123456
-- ============================================================

USE LanguageCenterDB;
GO

INSERT INTO Users (username, password_hash, role) VALUES
('admin01',   '123456', 'ADMIN'),
('staff01',   '123456', 'STAFF'),
('teacher01', '123456', 'TEACHER'),
('teacher02', '123456', 'TEACHER'),
('student01', '123456', 'STUDENT'),
('student02', '123456', 'STUDENT'),
('student03', '123456', 'STUDENT');
GO

INSERT INTO Admins (user_id, full_name, phone, email) VALUES
(1, N'System Admin', '0900000000', 'admin@center.edu.vn');
GO

INSERT INTO Staff (user_id, full_name, phone, email, department) VALUES
(2, N'Nguyen Van A', '0901000001', 'a@center.edu.vn', N'Academic Affairs');
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

INSERT INTO Classrooms (name, capacity, location) VALUES
('Room 101', 25, 'Floor 1'),
('Room 201', 20, 'Floor 2'),
('Room 301', 15, 'Floor 3');
GO

INSERT INTO Semesters (name, start_date, end_date, is_active) VALUES
(N'Summer 2025',   '2025-06-01', '2025-08-31', 0),
(N'Fall 2025',     '2025-09-01', '2025-12-31', 1),
(N'Spring 2026',   '2026-01-01', '2026-05-31', 0);
GO

INSERT INTO Classes (semester_id, course_id, teacher_id, classroom_id, name, max_students, start_date, end_date, status) VALUES
(1, 1, 1, 1, 'A1-K01', 20, '2025-06-01', '2025-09-30', 'ONGOING'),
(2, 2, 1, 2, 'B1-K01', 18, '2025-06-15', '2025-12-15', 'ONGOING'),
(2, 3, 2, 3, 'N5-K01', 15, '2025-07-01', '2025-11-30', 'UPCOMING');
GO

-- day_of_week: 1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat, 7=Sun
INSERT INTO ClassSchedules (class_id, day_of_week, start_time, end_time) VALUES
(1, 1, '08:00', '10:00'),
(1, 3, '08:00', '10:00'),
(2, 2, '14:00', '16:00'),
(2, 4, '14:00', '16:00'),
(3, 6, '09:00', '11:30');
GO

INSERT INTO GradeTypes (name, weight_percent, description) VALUES
('Attendance', 10, 'Attendance and participation score'),
('Midterm',    30, 'Midterm exam score'),
('Final',      60, 'Final exam score');
GO

INSERT INTO Enrollments (student_id, class_id, enrolled_date, status) VALUES
(1, 1, '2025-05-28', 'ACTIVE'),
(2, 1, '2025-05-28', 'ACTIVE'),
(3, 1, '2025-05-29', 'ACTIVE'),
(1, 3, '2025-06-25', 'ACTIVE');
GO

INSERT INTO Invoices (student_id, enrollment_id, amount, status, due_date) VALUES
(1, 1, 3500000, 'UNPAID', '2025-06-01'),
(2, 2, 3500000, 'UNPAID', '2025-06-01'),
(3, 3, 3500000, 'UNPAID', '2025-06-01'),
(1, 4, 4500000, 'UNPAID', '2025-07-01');
GO

INSERT INTO Payments (invoice_id, staff_id, amount_paid, payment_method, receipt_code) VALUES
(1, 1, 3500000, 'Transfer', 'RCP-2025-0001'),
(2, 1, 2000000, 'Cash',     'RCP-2025-0002');
GO

UPDATE Invoices SET status = 'PAID'    WHERE invoice_id = 1;
UPDATE Invoices SET status = 'PARTIAL' WHERE invoice_id = 2;
GO

INSERT INTO Sessions (class_id, schedule_id, session_date, topic, status) VALUES
(1, 1, '2025-06-02', 'Introduction - Alphabet',     'COMPLETED'),
(1, 2, '2025-06-04', 'Numbers - Basic Greetings',   'COMPLETED');
GO

INSERT INTO Attendances (session_id, student_id, status) VALUES
(1, 1, 'PRESENT'), (1, 2, 'PRESENT'), (1, 3, 'ABSENT'),
(2, 1, 'PRESENT'), (2, 2, 'LATE'),    (2, 3, 'PRESENT');
GO

INSERT INTO TeacherAttendances (session_id, teacher_id, status) VALUES
(1, 1, 'PRESENT'),
(2, 1, 'PRESENT');
GO

INSERT INTO Grades (enrollment_id, grade_type_id, score, max_score) VALUES
(1, 1, 9.0, 10),
(1, 2, 7.5, 10),
(2, 1, 8.0, 10);
GO

PRINT '==> Seed data inserted successfully.';
GO
