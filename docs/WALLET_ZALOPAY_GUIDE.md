# Wallet + ZaloPay Integration Guide (for Duy)

> Branch: `feature/vnpay-test` (name is stale — this branch contains the working ZaloPay implementation, not VNPay. Rename or merge into `develop` when ready.)
> Status: Backend + UI done, build passes, verified working end-to-end against ZaloPay's real sandbox API.

---

## 1. Why ZaloPay (and not MoMo or VNPay)

- **MoMo** was the original choice. Its public demo credentials (`MOMO` / `F8BBA842ECF85` / ...) stopped reliably working, and getting a real merchant account requires MoMo's M4B business registration (needs business/tax documents — not accessible for a student project).
- **VNPay** was tried next. VNPay does not offer a stable public demo credential pair like MoMo did — every developer must register individually at `sandbox.vnpayment.vn/devreg` (lightweight form, no business docs, but requires waiting hours to days for an email with real credentials). This didn't fit the timeline.
- **ZaloPay** has a public demo credential pair (`app_id=2553`, `key1`, `key2`) that is still active and was verified live — a real `CreateOrderAsync` call against `sb-openapi.zalopay.vn` returned `return_code=1` ("success") with a valid `order_url`. This is why the project switched to ZaloPay as the only payment provider.

All MoMo and VNPay code has been deleted from the project. ZaloPay is the only payment method.

---

## 2. Overall flow

```
Top-up:
Student -> StudentInvoiceWindow -> "Top Up Wallet" -> TopUpWalletWindow
         -> enter amount -> ZaloPay sandbox order created
         -> browser opens order_url -> app polls status every 3s
         -> success -> credited to Student.Balance

Pay tuition:
Student -> StudentInvoiceWindow -> select invoice -> "Pay Tuition From Wallet"
         -> deducts Balance + creates Payment(method=Wallet) + Invoice=PAID (1 DB transaction)
```

Key point: a WPF desktop app has **no public server** to receive ZaloPay's callback, so the app **polls** ZaloPay's Query API every 3 seconds after opening the order page in the browser. No backend server is needed.

---

## 3. What's already implemented

### Database (`db/schema.sql`)
- `Students.balance DECIMAL(18,2) DEFAULT 0` (check >= 0)
- Table `WalletTransactions` (id, student_id, amount, transaction_type: TOP_UP/PAYMENT/REFUND, `provider_order_id`, status: PENDING/COMPLETED/FAILED, created_at)
  - Note: this column used to be called `momo_order_id`; it was renamed to `provider_order_id` since it's no longer MoMo-specific.
- `'Wallet'` added to the `Payments.payment_method` check constraint

**You must re-run `schema.sql`** to get the renamed column (or write your own ALTER script if you don't want to drop the DB).

### Model (`BusinessObjects/`)
- `Student.cs` — `Balance` property + `WalletTransactions` collection
- `WalletTransaction.cs` — `ProviderOrderId` (renamed from `MomoOrderId`)

### Data layer (`DataAccessObjects/`)
- `LanguageCenterContext.cs` — `DbSet<WalletTransaction>` + Fluent config
- `WalletTransactionDAO.cs` — atomic operations (`IsolationLevel.Serializable` transactions):
  - `GetBalance(studentId)`
  - `CreatePendingTopUp(studentId, amount, providerOrderId)`
  - `CompleteTopUp(providerOrderId)` — credits balance + marks COMPLETED (idempotent — calling twice does not double-credit)
  - `FailTopUp(providerOrderId)`
  - `PayInvoiceFromWallet(studentId, invoiceId)`

### Repository (`Repositories/`)
- `IWalletRepository` / `WalletRepository`

### Service (`Services/`)
- `IZaloPayService` / `ZaloPayService.cs` — creates ZaloPay orders and queries status:
  - `CreateOrderAsync(appTransId, amount, description)` -> returns `order_url` to open in browser
  - `QueryOrderStatusAsync(appTransId)` -> used during polling
  - MAC signing: `HMAC-SHA256(key1, "app_id|app_trans_id|app_user|amount|app_time|embed_data|item")`
- `IWalletService` / `WalletService.cs` — business logic:
  - `StartTopUpAsync(studentId, amount)` -> `(orderId, payUrl)`. `orderId` is formatted `yyMMdd_<studentId><HHmmssfff>` — **this exact format is required by ZaloPay's `app_trans_id`** (must start with `yyMMdd_`)
  - `ConfirmTopUpAsync(orderId)` -> used in the polling loop; verifies the amount ZaloPay confirms matches the pending record before crediting (anti-tampering)
  - `PayInvoiceFromWallet`, `GetBalance`, `GetHistory`
- `appsettings.json` — `ZaloPay` config section (currently using ZaloPay's public demo credentials)

### UI (`WpfApp/`)
- `TopUpWalletWindow` — enter amount, opens browser to `order_url`, polls for completion
- `WalletHistoryWindow` — transaction history DataGrid
- `StudentInvoiceWindow` — auto-resolves student from the logged-in user, shows wallet balance, 3 buttons: "Top Up Wallet" / "Wallet History" / "Pay Tuition From Wallet"

All UI text and error messages are in English (translated from the original Vietnamese-language draft).

---

## 4. `appsettings.json`

```json
"ZaloPay": {
  "AppId": "2553",
  "Key1": "PcY4iZIKFCIdgZvA6ueMcMHHUbRLYjPL",
  "Key2": "kLtgPl8HHhfvMuDHPwKfgfsY4Ydm9eIz",
  "CreateEndpoint": "https://sb-openapi.zalopay.vn/v2/create",
  "QueryEndpoint": "https://sb-openapi.zalopay.vn/v2/query"
}
```

These are ZaloPay's **public demo credentials**, verified working. You can keep using them, or register your own sandbox account at `https://sbmc.zalopay.vn/` for a dedicated `app_id`/keys if you want isolation from other developers testing with the same shared account.

---

## 5. How to test end-to-end

### Step 1 — Re-run the DB scripts
```
schema.sql
seed.sql
```
(needed for the `momo_order_id` -> `provider_order_id` column rename)

### Step 2 — Install the ZaloPay Sandbox app on your phone
This is a **separate test app**, not the real ZaloPay app. Download instructions: `https://docs.zalopay.vn/v1/start/`

### Step 3 — Register a test account in the app
1. Open the app, register with any valid-format phone number (does not need to be real)
2. OTP code for registration: **`111111`**
3. Set a 6-digit password (avoid all-repeating digits like `111111`)

### Step 4 — Link a test bank card (to top up test funds)
Use one of these test ATM card numbers (all belong to test cardholder `NGUYEN VAN A`, issue date `10/18`):
```
9704540000000062
9704540000000070
9704540000000088
9704540000000096
9704541000000094
9704541000000078
```
OTP for linking the card: **`111111`**

For a Visa/Mastercard test instead:
- Card number: `4111 1111 1111 1111`
- Name: `NGUYEN VAN A`, expiry `01/25`, CVV `123`

### Step 5 — Run the app and test
1. `dotnet run` (or F5 in Visual Studio)
2. Log in as `cam@mail.com` / `123456`
3. Menu -> My Info -> My Invoices
4. Click **"Top Up Wallet"**, enter an amount (e.g. `50000`), submit
5. A browser window opens to the ZaloPay order page
6. Open the ZaloPay Sandbox app on your phone -> "Scan QR" -> scan the code on the order page
7. Confirm the payment in the app
8. Switch back to the WPF app — it polls every 3 seconds and will show "Top-up successful!" once ZaloPay confirms, then update the balance automatically

### Step 6 — Test paying an invoice from the wallet
Once the balance is topped up, select an unpaid invoice in `StudentInvoiceWindow` and click **"Pay Tuition From Wallet"**.

---

## 6. Known issues / things to watch

- **One phone number = one ZaloPay sandbox account.** If registration fails partway, you may need a different phone number to retry.
- **The branch is still named `feature/vnpay-test`** even though it now contains the ZaloPay implementation (VNPay was tried and abandoned). Consider renaming before merging into `develop`, or just merge and let the name stay as historical context.
- **`ZaloPayService`'s `app_user` is hardcoded to `"student"`** — fine for a demo, but if you want per-student tracking on ZaloPay's side, pass the actual student ID/name instead.
- No automated tests exist for the payment flow; testing so far has been manual (see Step 5 above) plus a scratch console script used during development to verify `CreateOrderAsync`/`WalletService.StartTopUpAsync` against the real sandbox API.

---

## 7. Next steps

1. Merge `feature/vnpay-test` into `develop` once you've verified the flow works on your machine
2. Consider registering your own ZaloPay sandbox app (`https://sbmc.zalopay.vn/`) instead of the shared public demo credentials, for isolation
3. Optional: add a "Refund" flow (schema already supports `transaction_type = 'REFUND'`, just needs a service method)
