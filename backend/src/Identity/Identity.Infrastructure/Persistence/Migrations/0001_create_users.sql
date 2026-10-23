-- Identity.Api database (identity.db)
-- Migration 0001: Users table.
-- Executed once by the SqliteMigrator inside a transaction and recorded in SchemaVersions.
-- STRICT enforces column types (SQLite 3.37+, bundled with Microsoft.Data.Sqlite).

CREATE TABLE Users (
    Id            TEXT NOT NULL PRIMARY KEY
                  CHECK (length(Id) = 36),                         -- Guid "D" format, lower case
    Email         TEXT NOT NULL COLLATE NOCASE
                  CHECK (length(Email) BETWEEN 3 AND 254
                         AND Email LIKE '%_@_%'),                  -- full validation lives in the Domain
    DisplayName   TEXT NOT NULL
                  CHECK (length(trim(DisplayName)) BETWEEN 2 AND 80),
    PasswordHash  TEXT NOT NULL
                  CHECK (length(PasswordHash) > 0),                -- PBKDF2 output of PasswordHasher, never plain text
    CreatedAtUtc  TEXT NOT NULL
                  CHECK (CreatedAtUtc GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]T*Z')
) STRICT;

-- Case-insensitive uniqueness (inherits COLLATE NOCASE from the column).
CREATE UNIQUE INDEX UX_Users_Email ON Users (Email);
