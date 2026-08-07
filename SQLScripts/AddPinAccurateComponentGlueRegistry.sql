-- Component mới (2026-07-28, task "pin/visual chuẩn theo thực tế") — 4 dòng
-- mới (additive, không đụng các dòng cũ). Chạy tay qua throwaway console
-- seeder giống AddComponentLibraryGlueRegistry.sql (dotnet ef migrations vẫn
-- không dùng được trong repo này) — script này giữ lại để tham chiếu/áp dụng
-- thủ công nếu cần (vd: môi trường khác).

INSERT INTO "ComponentGlueRegistry" ("ComponentType", "CreatedAt", "Label", "PinRequirementsJson", "Supported", "UpdatedAt")
VALUES
    ('wokwi-mpu6050', '2026-07-28T00:00:00Z', 'IMU MPU6050', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"SCL","kind":"i2c"},{"name":"SDA","kind":"i2c"},{"name":"XDA","kind":"i2c"},{"name":"XCL","kind":"i2c"},{"name":"AD0","kind":"digital_input"},{"name":"INT","kind":"digital_output"}]}', true, '2026-07-28T00:00:00Z'),
    ('wokwi-esc', '2026-07-28T00:00:00Z', 'ESC (Electronic Speed Controller)', '{"pins":[{"name":"SIG","kind":"digital_input"},{"name":"GND","kind":"ground"},{"name":"BATT+","kind":"power"},{"name":"BATT-","kind":"ground"},{"name":"OUT+","kind":"power"},{"name":"OUT-","kind":"ground"}]}', true, '2026-07-28T00:00:00Z'),
    ('wokwi-heating-element', '2026-07-28T00:00:00Z', 'Heating Element', '{"pins":[{"name":"+","kind":"power"},{"name":"-","kind":"ground"}]}', true, '2026-07-28T00:00:00Z'),
    ('wokwi-ph-sensor', '2026-07-28T00:00:00Z', 'pH Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"PO","kind":"analog"}]}', true, '2026-07-28T00:00:00Z')
ON CONFLICT ("ComponentType") DO NOTHING;
