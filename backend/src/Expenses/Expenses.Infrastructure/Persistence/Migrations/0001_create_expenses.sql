-- Expenses.Api database (expenses.db)
-- Migration 0001: Expenses table.
-- Executed once by the SqliteMigrator inside a transaction and recorded in SchemaVersions.
-- The Domain is the source of truth for business rules; these CHECKs are defense in depth.
-- "Not in the future" cannot be a CHECK (SQLite forbids non-deterministic functions there); the Domain enforces it.

CREATE TABLE Expenses (
    Id            TEXT    NOT NULL PRIMARY KEY
                  CHECK (length(Id) = 36),                         -- Guid "D" format, lower case
    UserId        TEXT    NOT NULL
                  CHECK (length(UserId) = 36),                     -- JWT "sub"; users live in identity.db, so no FK
    Description   TEXT    NOT NULL
                  CHECK (length(trim(Description)) BETWEEN 3 AND 200),
    AmountCents   INTEGER NOT NULL
                  CHECK (AmountCents BETWEEN 1 AND 100000000),     -- 0.01 to 1,000,000.00; money is never REAL
    Category      TEXT    NOT NULL
                  CHECK (Category IN ('Food', 'Transport', 'Housing', 'Utilities', 'Health',
                                      'Entertainment', 'Shopping', 'Education', 'Other')),
    ExpenseDate   TEXT    NOT NULL
                  CHECK (date(ExpenseDate, '+0 days') IS ExpenseDate  -- the modifier normalizes 2026-02-30 to 2026-03-02, so it fails; also rejects times and other formats
                         AND ExpenseDate >= '2000-01-01'),         -- calendar date, no time, no time zone
    CreatedAtUtc  TEXT    NOT NULL
                  CHECK (CreatedAtUtc GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]T*Z'),
    UpdatedAtUtc  TEXT    NOT NULL
                  CHECK (UpdatedAtUtc GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]T*Z'),
    CHECK (UpdatedAtUtc >= CreatedAtUtc)
) STRICT;

-- Serves the main query: one user's expenses, newest first, optional date range.
CREATE INDEX IX_Expenses_UserId_ExpenseDate ON Expenses (UserId, ExpenseDate DESC, CreatedAtUtc DESC);
