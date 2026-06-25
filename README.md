# Language Center Management System

A desktop application built with C# WPF to digitalize and streamline the daily operations of a foreign language center — replacing manual Excel sheets, paper receipts, and handwritten attendance logs with a centralized, role-based management system.

## What the system can do

**For Admins**
- Manage user accounts (create, edit, lock/unlock accounts for Teachers, Staff, Students)
- Manage courses, classes, and classrooms
- Detect and prevent teacher/room scheduling conflicts
- View revenue reports and payment statistics

**For Staff**
- Manage student profiles and enrollment
- Register students into classes
- Create invoices and record tuition payments (cash, transfer, card)
- Track students with outstanding balances

**For Teachers**
- View personal teaching schedule
- Take attendance per class session
- Enter and edit student grades (attendance, midterm, final)
- View class roster and academic results

**For Students**
- View personal class schedule
- View attendance history
- View grades and academic results
- View invoices and payment status

## Tech Stack

- **Platform:** C# / WPF on .NET 10
- **Database:** Microsoft SQL Server
- **ORM:** Entity Framework Core 10
- **Architecture:** 5-layer — BusinessObjects → DataAccessObjects → Repositories → Services → WpfApp

## Team

| Student ID | Name | Responsibility |
|------------|------|----------------|
| SE203692 | Nguyễn Thành Lộc | Leader · Authentication · Account Management · Revenue Report |
| SE204969 | Vũ Nguyễn Trung Nguyên | Student Management · Enrollment |
| SE204605 | Chiêm Minh Thức | Course · Class · Classroom Management · Schedule Conflict Check |
| SE203237 | Bùi Phạm Chí Nhân | Teacher Management · Attendance · Session |
| SE193918 | Nguyễn Hồng Duy | Tuition Fee · Payment · Grades |
