# Student import — sample files

Open **Staff → Student Management → Import list**, pick one of these, and the
preview appears before anything is written to the database.

Columns, in this order. Row 1 is a header and is skipped.

| # | Column | Required | Notes |
|---|---|---|---|
| 1 | FullName | **yes** | |
| 2 | DateOfBirth | no | `dd/MM/yyyy` |
| 3 | Gender | no | `Male` or `Female` |
| 4 | Phone | no | 10 digits starting with `0` |
| 5 | Email | **yes** | becomes the student's login, must be unique |
| 6 | Address | no | wrap in `"` if it contains a comma |

## The files

| File | What it demonstrates |
|---|---|
| `1-clean.csv` | 8 rows, all valid — imports fully, 8 accounts created |
| `2-every-error.csv` | one valid row and one row per rejection reason |
| `3-vietnamese.csv` | Vietnamese diacritics and quoted addresses containing commas |
| `4-optional-columns.csv` | short rows — only FullName and Email are actually required |
| `5-excel-format.xlsx` | the same import from Excel, including one bad row |

## What `2-every-error.csv` covers

Each row names its own problem in the Address column, and the app repeats it in
the **Status** column of the preview:

- no email at all — it is the login, so it cannot be blank
- an email that is not a valid address
- an email that repeats an earlier row **in the same file**
- a student whose name **and** date of birth already exist in the system
- an email that **already has an account** (`student01@gmail.com`, from the seed)
- a date of birth that is not `dd/MM/yyyy`
- a phone that is not 10 digits starting with `0`
- a gender that is neither `Male` nor `Female`
- a date of birth in the future
- a missing full name

Only the first row is valid, so importing this file creates exactly one account
and skips ten. Nothing is guessed at and nothing is half-written.

## After importing

Every imported account signs in with **its email** and the starting password
shown at the bottom of the import window (`123456` by default), and is then
required to choose a new password before reaching the app.

Re-running the same file a second time is safe: every row now fails the
"already has an account" check, so nothing is duplicated.
