-- =====================================================
-- Quick Fix: Add missing columns for Schedules
-- Run this on Supabase Dashboard > SQL Editor
-- =====================================================

-- Add columns if they don't exist
ALTER TABLE "Schedules" ADD COLUMN IF NOT EXISTS "RoomId" INTEGER;
ALTER TABLE "Schedules" ADD COLUMN IF NOT EXISTS "TeacherId" INTEGER;
ALTER TABLE "Schedules" ADD COLUMN IF NOT EXISTS "Note" VARCHAR(500);
ALTER TABLE "Schedules" ADD COLUMN IF NOT EXISTS "Status" VARCHAR(20) NOT NULL DEFAULT 'Scheduled';

-- Create Rooms table if not exists
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

-- Seed sample rooms
INSERT INTO "Rooms" ("RoomCode", "RoomName", "Building", "Floor", "Capacity", "HasProjector", "HasAirConditioner", "Status")
VALUES 
    ('101', 'Phòng 101', 'Tòa A', 1, 30, TRUE, TRUE, 'Available'),
    ('102', 'Phòng 102', 'Tòa A', 1, 40, TRUE, TRUE, 'Available'),
    ('103', 'Phòng 103', 'Tòa A', 1, 25, FALSE, TRUE, 'Available'),
    ('201', 'Phòng 201', 'Tòa A', 2, 35, TRUE, TRUE, 'Available'),
    ('202', 'Phòng 202', 'Tòa A', 2, 30, TRUE, TRUE, 'Available'),
    ('301', 'Phòng 301', 'Tòa B', 3, 50, TRUE, TRUE, 'Available'),
    ('302', 'Phòng 302', 'Tòa B', 3, 40, TRUE, TRUE, 'Available')
ON CONFLICT ("RoomCode") DO NOTHING;

-- Add foreign key for RoomId
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_Schedules_Rooms_RoomId'
    ) THEN
        ALTER TABLE "Schedules" ADD CONSTRAINT "FK_Schedules_Rooms_RoomId" 
        FOREIGN KEY ("RoomId") REFERENCES "Rooms"("Id") ON DELETE SET NULL;
    END IF;
END $$;

-- Add foreign key for TeacherId
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_Schedules_Users_TeacherId'
    ) THEN
        ALTER TABLE "Schedules" ADD CONSTRAINT "FK_Schedules_Users_TeacherId" 
        FOREIGN KEY ("TeacherId") REFERENCES "Users"("Id") ON DELETE SET NULL;
    END IF;
END $$;

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_Schedules_RoomId" ON "Schedules"("RoomId");
CREATE INDEX IF NOT EXISTS "IX_Schedules_TeacherId" ON "Schedules"("TeacherId");

-- Verify
SELECT 'Tables updated successfully!' as result;
