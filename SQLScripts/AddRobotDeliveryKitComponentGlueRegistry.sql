-- Robot giao hàng mini — thêm 10 dòng ComponentGlueRegistry mới (additive,
-- không đụng 6 dòng cũ). Chạy tay 1 lần trên DB thật vì `dotnet ef migrations
-- add/database update` hiện KHÔNG dùng được trong repo này — design-time
-- factory resolve nhầm sang SQL Server (Named Pipes Provider) thay vì Npgsql
-- thật, xác nhận lúc thử tạo migration. Migration tương ứng đã viết tay ở
-- STEM.Infrastructure/Migrations/20260726124256_AddRobotDeliveryKitComponentGlueRegistry.cs
-- (không "applied" được qua CLI, chỉ để đúng lịch sử code — nếu sau này ai đó
-- sửa xong lỗi provider và chạy `dotnet ef database update`, migration đó sẽ
-- cố INSERT lại 10 dòng này lần nữa và báo lỗi trùng khoá — lúc đó xoá tay
-- phần INSERT trong file migration đó, giống cách FixVirtualLabComponentTypeMismatch.sql
-- đã ghi chú tiền lệ này).

INSERT INTO "ComponentGlueRegistry" ("ComponentType", "CreatedAt", "Label", "PinRequirementsJson", "Supported", "UpdatedAt")
VALUES
    ('wokwi-hc-sr04', '2026-07-26T00:00:00Z', 'HC-SR04 Ultrasonic Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"TRIG","kind":"digital_output"},{"name":"ECHO","kind":"digital_input"},{"name":"GND","kind":"ground"}]}', true, '2026-07-26T00:00:00Z'),
    ('wokwi-l298n', '2026-07-26T00:00:00Z', 'L298N Motor Driver', '{"pins":[{"name":"IN1","kind":"digital_output"},{"name":"IN2","kind":"digital_output"},{"name":"IN3","kind":"digital_output"},{"name":"IN4","kind":"digital_output"},{"name":"ENA","kind":"pwm_output"},{"name":"ENB","kind":"pwm_output"},{"name":"OUT1","kind":"motor_output"},{"name":"OUT2","kind":"motor_output"},{"name":"OUT3","kind":"motor_output"},{"name":"OUT4","kind":"motor_output"},{"name":"VIN","kind":"power"},{"name":"GND","kind":"ground"},{"name":"5V","kind":"power"}]}', true, '2026-07-26T00:00:00Z'),
    ('wokwi-dc-motor', '2026-07-26T00:00:00Z', 'DC Geared Motor / TT Motor', '{"pins":[{"name":"terminal1","kind":"motor_terminal"},{"name":"terminal2","kind":"motor_terminal"}]}', true, '2026-07-26T00:00:00Z'),
    ('wokwi-battery-pack', '2026-07-26T00:00:00Z', 'Battery Pack 7.4V (2x18650)', '{"pins":[{"name":"+","kind":"power"},{"name":"-","kind":"ground"}]}', true, '2026-07-26T00:00:00Z'),
    ('wokwi-power-switch', '2026-07-26T00:00:00Z', 'Power Switch', '{"pins":[{"name":"IN","kind":"power"},{"name":"OUT","kind":"power"}]}', true, '2026-07-26T00:00:00Z'),
    ('wokwi-robot-wheel', '2026-07-26T00:00:00Z', 'Robot Wheel', '{"pins":[],"visualOnly":true}', true, '2026-07-26T00:00:00Z'),
    ('wokwi-caster-wheel', '2026-07-26T00:00:00Z', 'Caster Wheel', '{"pins":[],"visualOnly":true}', true, '2026-07-26T00:00:00Z'),
    ('wokwi-robot-chassis', '2026-07-26T00:00:00Z', 'Robot Chassis', '{"pins":[],"visualOnly":true}', true, '2026-07-26T00:00:00Z'),
    ('wokwi-breadboard', '2026-07-26T00:00:00Z', 'Breadboard', '{"pins":[],"visualOnly":true}', true, '2026-07-26T00:00:00Z'),
    ('wokwi-delivery-box', '2026-07-26T00:00:00Z', 'Mini Delivery Box', '{"pins":[],"visualOnly":true}', true, '2026-07-26T00:00:00Z')
ON CONFLICT ("ComponentType") DO NOTHING;

-- Verify sau khi chạy:
-- SELECT "ComponentType", "Label", "Supported" FROM "ComponentGlueRegistry" WHERE "ComponentType" LIKE 'wokwi-%' ORDER BY "ComponentType";
-- Kỳ vọng: đúng 10 dòng mới ở trên + wokwi-buzzer/wokwi-led/wokwi-servo/wokwi-pushbutton/wokwi-potentiometer (đã vá trước đó qua FixVirtualLabComponentTypeMismatch.sql).
