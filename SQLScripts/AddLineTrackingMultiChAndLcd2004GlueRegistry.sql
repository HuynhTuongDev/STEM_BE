-- Line Tracking đa kênh (3/5 kênh, bổ sung không thay thế bản 1 kênh cũ) +
-- dọn orphan lcd2004 (2026-07-28) — 3 dòng mới (additive). Chạy tay qua
-- throwaway console seeder giống các script trước (dotnet ef migrations vẫn
-- không dùng được trong repo này).

INSERT INTO "ComponentGlueRegistry" ("ComponentType", "CreatedAt", "Label", "PinRequirementsJson", "Supported", "UpdatedAt")
VALUES
    ('wokwi-line-tracking-3ch', '2026-07-28T00:00:00Z', 'Line Tracking Sensor (3 kênh)', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT1","kind":"digital_input"},{"name":"OUT2","kind":"digital_input"},{"name":"OUT3","kind":"digital_input"}]}', true, '2026-07-28T00:00:00Z'),
    ('wokwi-line-tracking-5ch', '2026-07-28T00:00:00Z', 'Line Tracking Sensor (5 kênh)', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT1","kind":"digital_input"},{"name":"OUT2","kind":"digital_input"},{"name":"OUT3","kind":"digital_input"},{"name":"OUT4","kind":"digital_input"},{"name":"OUT5","kind":"digital_input"}]}', true, '2026-07-28T00:00:00Z'),
    ('wokwi-lcd2004', '2026-07-28T00:00:00Z', 'LCD 20x4 I2C', '{"pins":[{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"},{"name":"SDA","kind":"i2c"},{"name":"SCL","kind":"i2c"}]}', true, '2026-07-28T00:00:00Z')
ON CONFLICT ("ComponentType") DO NOTHING;
