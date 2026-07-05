# Hướng dẫn tính năng Ví + MoMo (cho Duy)

> Nhánh: `feature/wallet-momo` (đã tách từ `develop`)
> Trạng thái: Backend + UI đã xong, build pass. Cần Duy setup MoMo sandbox thật để test end-to-end.

---

## 1. Tổng quan luồng

```
Nạp tiền:
Student → TopUpWalletWindow → nhập tiền → MoMo sandbox (browser)
        → app polling status mỗi 3s → thành công → cộng vào Student.Balance

Đóng học phí:
Student → StudentInvoiceWindow → chọn invoice → "Đóng học phí từ ví"
        → trừ Balance + tạo Payment(method=Wallet) + Invoice = PAID (1 transaction)
```

Điểm mấu chốt: **WPF không nhận được callback từ MoMo** (không có public server), nên ta **polling** MoMo Query API để biết khi nào thanh toán xong. Không cần backend riêng.

---

## 2. Những gì đã làm sẵn

### Database (`db/schema.sql`)
- `Students.balance DECIMAL(18,2) DEFAULT 0` (check >= 0)
- Bảng mới `WalletTransactions` (id, student_id, amount, transaction_type: TOP_UP/PAYMENT/REFUND, momo_order_id, status: PENDING/COMPLETED/FAILED, created_at)
- Thêm `'Wallet'` vào check constraint `Payments.payment_method`
- Index cho student_id + status

⚠️ **Duy phải chạy lại `schema.sql`** để có cột/bảng mới (hoặc tự viết ALTER script nếu không muốn drop DB).

### Model (`BusinessObjects/`)
- `Student.cs` — thêm `Balance` + collection `WalletTransactions`
- `WalletTransaction.cs` (mới)

### Data layer (`DataAccessObjects/`)
- `LanguageCenterContext.cs` — thêm `DbSet<WalletTransaction>` + Fluent config
- `WalletTransactionDAO.cs` (mới) — các thao tác atomic (transaction Serializable):
  - `GetBalance(studentId)`
  - `CreatePendingTopUp(studentId, amount, momoOrderId)`
  - `CompleteTopUp(momoOrderId)` — cộng tiền + đánh dấu COMPLETED (idempotent, gọi lại không cộng 2 lần)
  - `FailTopUp(momoOrderId)`
  - `PayInvoiceFromWallet(studentId, invoiceId)`

### Repository (`Repositories/`)
- `IWalletRepository` / `WalletRepository`

### Service (`Services/`)
- `MoMoService.cs` — gọi MoMo sandbox `/create` và `/query`, ký HMAC-SHA256
- `WalletService.cs` — business logic:
  - `StartTopUpAsync(studentId, amount)` → trả `(orderId, payUrl)`
  - `ConfirmTopUpAsync(orderId)` → dùng trong vòng polling
  - `PayInvoiceFromWallet(studentId, invoiceId)`
  - `GetBalance`, `GetHistory`
- `appsettings.json` — thêm section `MoMo` (đang dùng credentials sandbox công khai của MoMo)

### UI (`WpfApp/`)
- `TopUpWalletWindow` — nạp tiền + polling
- `WalletHistoryWindow` — lịch sử giao dịch
- `StudentInvoiceWindow` — hoàn thiện (auto-resolve student từ user đăng nhập), thêm số dư + 3 nút ví
- `MainWindow.xaml.cs` — truyền `_currentUser` vào StudentInvoiceWindow

---

## 3. Hướng đi kế tiếp để setup MoMo thành công

### Bước 1 — Đăng ký MoMo sandbox
1. Vào https://developers.momo.vn/ → đăng ký tài khoản developer
2. Vào phần Sandbox / Test → lấy bộ credentials:
   - `partnerCode`
   - `accessKey`
   - `secretKey`
3. Tải app **MoMo Test** (bản sandbox) để có tài khoản test + thẻ test để thanh toán

> Hiện tại `appsettings.json` đang dùng bộ credentials demo công khai của MoMo (`MOMO / F8BBA842ECF85 / K951B6PE1waDMi640xX08PD3vg6EkVlz`). Bộ này chạy được với môi trường test chung, nhưng **nên thay bằng credentials riêng của Duy** để ổn định và tránh đụng dữ liệu người khác.

### Bước 2 — Cập nhật `appsettings.json`
```json
"MoMo": {
  "PartnerCode": "<partnerCode của Duy>",
  "AccessKey": "<accessKey>",
  "SecretKey": "<secretKey>",
  "Endpoint": "https://test-payment.momo.vn/v2/gateway/api/create",
  "QueryEndpoint": "https://test-payment.momo.vn/v2/gateway/api/query",
  "RedirectUrl": "https://momo.vn/return",
  "IpnUrl": "https://momo.vn/notify"
}
```
- `RedirectUrl` / `IpnUrl`: với desktop app không có server nên để URL bất kỳ (MoMo vẫn yêu cầu có, nhưng ta dùng polling nên không cần chúng hoạt động thật).

### Bước 3 — Test end-to-end
1. Chạy lại `schema.sql` + `seed.sql`
2. Login bằng `student01 / 123456`
3. Menu → My Invoices → "Nạp tiền vào ví" → nhập số tiền (vd 50000)
4. Browser mở trang MoMo sandbox → thanh toán bằng tài khoản/thẻ test
5. Quay lại app, đợi vài giây → số dư tự cập nhật (polling)
6. Chọn 1 invoice → "Đóng học phí từ ví" → invoice chuyển PAID

### Bước 4 — Các case cần test kỹ
- [ ] Nạp thành công → balance tăng đúng
- [ ] Nạp rồi hủy giữa chừng (không thanh toán) → sau 5 phút timeout, không cộng tiền
- [ ] Đóng học phí khi ví không đủ → báo lỗi "Số dư không đủ"
- [ ] Gọi `ConfirmTopUpAsync` 2 lần cho cùng order → không cộng tiền 2 lần (đã chống, nhưng test lại)
- [ ] Số tiền nạp nhỏ nhất MoMo cho phép (thường 1.000đ)

---

## 4. Lưu ý kỹ thuật

- **Số tiền MoMo là số nguyên (VND)** — `MoMoService` đang cast `(long)amount`. Không dùng số lẻ.
- **orderId phải unique** mỗi lần — đang tạo dạng `TOPUP-{studentId}-{timestamp ms}`, ổn.
- **Polling timeout 5 phút, interval 3s** — chỉnh trong `TopUpWalletWindow.xaml.cs` (`PollInterval`, `PollTimeout`) nếu cần.
- **Bảo mật key**: sau này khi nộp bài / lên production, KHÔNG commit secretKey thật. Cân nhắc `appsettings.Development.json` (gitignore) hoặc user secrets.

---

## 5. Nếu muốn mở rộng thêm (optional)
- **Refund**: khi hủy lớp → cộng lại tiền vào ví (transaction_type = REFUND). DAO đã có sẵn enum, chỉ cần thêm method.
- **Admin cộng tiền tay** (mock, chỉ để dev test nhanh không cần MoMo): thêm 1 nút ẩn trong admin, gọi thẳng `CompleteTopUp` sau khi `CreatePendingTopUp`.
- **Giới hạn số tiền nạp** (min/max) — validate thêm trong `StartTopUpAsync`.

---

*Toàn bộ backend + UI đã sẵn sàng. Việc còn lại của Duy chủ yếu là: đăng ký sandbox, thay credentials, và test thực tế.*
