-- ============================================================
--  001 - StudentAwards
--
--  Run this against a database that was created BEFORE the awards table
--  existed. A database built fresh from schema.sql already has everything
--  here and does not need it.
--
--  Safe to run twice: every step checks for itself first.
-- ============================================================
USE LanguageCenterDB;
GO

-- 1. Let the wallet ledger say that money arrived as a reward.
--    The CHECK has to be dropped and rebuilt; there is no ALTER for it.
IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE parent_object_id = OBJECT_ID('WalletTransactions')
             AND definition LIKE '%transaction_type%'
             AND definition NOT LIKE '%REWARD%')
BEGIN
    DECLARE @constraint NVARCHAR(200) = (
        SELECT TOP 1 name FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('WalletTransactions')
          AND definition LIKE '%transaction_type%');

    EXEC('ALTER TABLE WalletTransactions DROP CONSTRAINT ' + @constraint);

    ALTER TABLE WalletTransactions ADD CONSTRAINT chk_wallet_transaction_type
        CHECK (transaction_type IN ('TOP_UP', 'PAYMENT', 'REFUND', 'REWARD'));

    PRINT '==> WalletTransactions.transaction_type now accepts REWARD.';
END
GO

-- 2. The awards table itself. See schema.sql for why it is separate from the
--    wallet ledger and why the UNIQUE is the point of it.
IF OBJECT_ID('StudentAwards', 'U') IS NULL
BEGIN
    CREATE TABLE StudentAwards (
        award_id       INT           IDENTITY(1,1) PRIMARY KEY,
        student_id     INT           NOT NULL REFERENCES Students(student_id),
        semester_id    INT           NOT NULL REFERENCES Semesters(semester_id),
        amount         DECIMAL(18,2) NOT NULL CHECK (amount > 0),
        average_score  DECIMAL(4,2)  NULL,
        threshold      DECIMAL(4,2)  NULL,
        transaction_id INT           NOT NULL REFERENCES WalletTransactions(transaction_id),
        awarded_by     INT           NULL REFERENCES Users(id),
        awarded_at     DATETIME2     NOT NULL DEFAULT GETDATE(),
        note           NVARCHAR(255) NULL,
        CONSTRAINT uq_award_per_student_semester UNIQUE (student_id, semester_id)
    );

    CREATE INDEX idx_awards_semester ON StudentAwards(semester_id);

    PRINT '==> StudentAwards created.';
END
GO

PRINT '==> Migration 001 finished.';
GO
