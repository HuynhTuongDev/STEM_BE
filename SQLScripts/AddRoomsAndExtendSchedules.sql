-- =====================================================
-- Migration Script: Add Rooms and Extend Schedules
-- Database: PostgreSQL (Supabase)
-- =====================================================

-- 1. Create Rooms table
CREATE TABLE IF NOT EXISTS "Rooms" (
    "Id" SERIAL PRIMARY KEY,
    "RoomCode" VARCHAR(50) NOT NULL UNIQUE,
    "RoomName" VARCHAR(100) NOT NULL,
    "Building" VARCHAR(100),
    "Floor" INTEGER,
    "Capacity" INTEGER NOT NULL DEFAULT 30,
    "HasProjector" BOOLEAN NOT NULL DEFAULT FALSE,
    "HasAirConditioner" BOOLEAN NOT NULL DEFAULT TRUE,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Available',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 2. Add columns to Schedules table
ALTER TABLE "Schedules" ADD COLUMN IF NOT EXISTS "RoomId" INTEGER;
ALTER TABLE "Schedules" ADD COLUMN IF NOT EXISTS "TeacherId" INTEGER;
ALTER TABLE "Schedules" ADD COLUMN IF NOT EXISTS "Note" VARCHAR(500);
ALTER TABLE "Schedules" ADD COLUMN IF NOT EXISTS "Status" VARCHAR(20) NOT NULL DEFAULT 'Scheduled';

-- 3. Add foreign key constraints
ALTER TABLE "Schedules" ADD CONSTRAINT "FK_Schedules_Rooms_RoomId"
    FOREIGN KEY ("RoomId") REFERENCES "Rooms"("Id") ON DELETE SET NULL;

ALTER TABLE "Schedules" ADD CONSTRAINT "FK_Schedules_Users_TeacherId"
    FOREIGN KEY ("TeacherId") REFERENCES "Users"("Id") ON DELETE SET NULL;

-- 4. Create indexes
CREATE INDEX IF NOT EXISTS "IX_Schedules_RoomId" ON "Schedules"("RoomId");
CREATE INDEX IF NOT EXISTS "IX_Schedules_TeacherId" ON "Schedules"("TeacherId");

-- 5. Seed sample rooms
INSERT INTO "Rooms" ("RoomCode", "RoomName", "Building", "Floor", "Capacity", "HasProjector", "HasAirConditioner", "Status")
VALUES 
    ('101', 'Phòng 101 - Tầng 1', 'Tòa A', 1, 30, TRUE, TRUE, 'Available'),
    ('102', 'Phòng 102 - Tầng 1', 'Tòa A', 1, 40, TRUE, TRUE, 'Available'),
    ('103', 'Phòng 103 - Tầng 1', 'Tòa A', 1, 25, FALSE, TRUE, 'Available'),
    ('201', 'Phòng 201 - Tầng 2', 'Tòa A', 2, 35, TRUE, TRUE, 'Available'),
    ('202', 'Phòng 202 - Tầng 2', 'Tòa A', 2, 30, TRUE, TRUE, 'Available'),
    ('301', 'Phòng 301 - Tầng 3', 'Tòa B', 3, 50, TRUE, TRUE, 'Available'),
    ('302', 'Phòng 302 - Tầng 3', 'Tòa B', 3, 40, TRUE, TRUE, 'Available'),
    ('LAB1', 'Phòng Lab 1 - Tầng 1', 'Tòa C', 1, 25, TRUE, TRUE, 'Available'),
    ('LAB2', 'Phòng Lab 2 - Tầng 2', 'Tòa C', 2, 30, TRUE, TRUE, 'Available'),
    ('MT1', 'Phòng Máy 1 - Tầng 1', 'Tòa C', 1, 35, TRUE, TRUE, 'Available')
ON CONFLICT ("RoomCode") DO NOTHING;

-- =====================================================
-- Verification queries
-- =====================================================

-- Check if Rooms table was created
-- SELECT * FROM "Rooms";

-- Check if columns were added to Schedules
-- SELECT "Id", "RoomId", "TeacherId", "Note", "Status" FROM "Schedules" LIMIT 5;

-- =====================================================
-- Rollback script (if needed)
-- =====================================================
/*
-- Drop foreign keys first
ALTER TABLE "Schedules" DROP CONSTRAINT IF EXISTS "FK_Schedules_Rooms_RoomId";
ALTER TABLE "Schedules" DROP CONSTRAINT IF EXISTS "FK_Schedules_Users_TeacherId";

-- Drop columns from Schedules
ALTER TABLE "Schedules" DROP COLUMN IF EXISTS "RoomId";
ALTER TABLE "Schedules" DROP COLUMN IF EXISTS "TeacherId";
ALTER TABLE "Schedules" DROP COLUMN IF EXISTS "Note";
ALTER TABLE "Schedules" DROP COLUMN IF EXISTS "Status";

-- Drop indexes
DROP INDEX IF EXISTS "IX_Schedules_RoomId";
DROP INDEX IF EXISTS "IX_Schedules_TeacherId";

-- Drop Rooms table
DROP TABLE IF EXISTS "Rooms";
*/
