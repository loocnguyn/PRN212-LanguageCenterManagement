# Language Center Management System

Hệ thống quản lý trung tâm ngoại ngữ — môn PRN212, nhóm SE1919.

## Thành viên

| MSSV | Họ tên | Role | Branch |
|------|--------|------|--------|
| SE203692 | Nguyễn Thành Lộc | Leader | `feature/loc-auth` |
| SE204969 | Vũ Nguyễn Trung Nguyên | Member | `feature/nguyen-student` |
| SE204605 | Chiêm Minh Thức | Member | `feature/thuc-course-class` |
| SE203237 | Bùi Phạm Chí Nhân | Member | `feature/nhan-teacher` |
| SE193918 | Nguyễn Hồng Duy | Member | `feature/duy-tuition-grade` |

## Tech Stack

- **Ngôn ngữ:** C# / WPF (.NET 10)
- **Database:** SQL Server — `LanguageCenterDB`
- **ORM:** Entity Framework Core 10
- **IDE:** Visual Studio 2022

## Cấu trúc Solution (5 projects)

```
LanguageCenter.slnx
├── BusinessObjects/        — Entity classes (EF Core scaffold)
├── DataAccessObjects/      — DbContext + DAO classes
├── Repositories/           — Interfaces + Repository classes
├── Services/               — Interfaces + Service classes
└── WpfApp/                 — WPF UI (Windows)
```

**Project references (một chiều):**
```
WpfApp → Services → Repositories → DataAccessObjects → BusinessObjects
```

## Hướng dẫn cài đặt

### 1. Yêu cầu
- Visual Studio 2022
- SQL Server (bất kỳ edition)
- .NET 10 SDK

### 2. Clone repo

```bash
git clone https://github.com/loocnguyn/PRN212-LanguageCenterManagement.git
cd PRN212-LanguageCenterManagement
git checkout feature/<tên-branch-của-bạn>
```

### 3. Tạo database

Chạy file SQL sau trong **SSMS**:

> Tự chạy script SQL schema để tạo `LanguageCenterDB` với đầy đủ bảng và dữ liệu mẫu.

### 4. Cấu hình connection string

Mở `WpfApp/appsettings.json` và sửa thông tin kết nối:

```json
{
  "ConnectionStrings": {
    "DefaultConnectionString": "Server=<tên-server>;Database=LanguageCenterDB;uid=sa;pwd=<mật-khẩu>;TrustServerCertificate=True;"
  }
}
```

> `Server=.` nếu dùng SQL Server mặc định localhost. Kiểm tra tên server trong SSMS.

### 5. Build & Run

```bash
dotnet build
```

Hoặc mở `LanguageCenter.slnx` trong Visual Studio → **F5**.

## Quy trình làm việc với Git

```
main          ← chỉ merge từ develop khi hoàn chỉnh
  └── develop ← tất cả PR merge vào đây
        ├── feature/loc-auth
        ├── feature/nguyen-student
        ├── feature/nhan-teacher
        ├── feature/thuc-course-class
        └── feature/duy-tuition-grade
```

- **Không push thẳng vào `main` hoặc `develop`**
- Làm việc trên branch của mình → tạo **Pull Request** vào `develop`
- PR cần ít nhất **1 người approve** trước khi merge

## Pattern template (theo đúng thứ tự)

```
XxxDAO.cs
  → IXxxRepository + XxxRepository
    → IXxxService + XxxService
      → XxxWindow.xaml + XxxWindow.xaml.cs
```

Xem [`DataAccessObjects/UserDAO.cs`](DataAccessObjects/UserDAO.cs) làm template mẫu.
