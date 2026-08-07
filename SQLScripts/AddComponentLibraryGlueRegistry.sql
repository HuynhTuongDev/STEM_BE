-- Thư viện linh kiện mở rộng (Component Library, 2026-07-27) — 44 dòng mới
-- (additive, không đụng các dòng cũ). Chạy tay vì `dotnet ef migrations
-- add/database update` hiện KHÔNG dùng được trong repo này (design-time
-- factory resolve nhầm sang SQL Server thay vì Npgsql — xem
-- AddRobotDeliveryKitComponentGlueRegistry.sql cho chi tiết lỗi). Đã insert
-- trực tiếp vào DB thật qua throwaway console seeder — script này giữ lại chỉ
-- để tham chiếu/áp dụng thủ công nếu cần (vd: môi trường khác).

INSERT INTO "ComponentGlueRegistry" ("ComponentType", "CreatedAt", "Label", "PinRequirementsJson", "Supported", "UpdatedAt")
VALUES
    ('wokwi-flame-sensor', '2026-07-27T12:00:00Z', 'Flame Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"DOUT","kind":"digital_input"},{"name":"AOUT","kind":"analog"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-gas-sensor', '2026-07-27T12:00:00Z', 'MQ Gas Sensor', '{"pins":[{"name":"AOUT","kind":"analog"},{"name":"DOUT","kind":"digital_input"},{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-pir-motion-sensor', '2026-07-27T12:00:00Z', 'PIR Motion Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"OUT","kind":"digital_input"},{"name":"GND","kind":"ground"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-photoresistor-sensor', '2026-07-27T12:00:00Z', 'Light Sensor / LDR', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"DO","kind":"digital_input"},{"name":"AO","kind":"analog"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-ntc-temperature-sensor', '2026-07-27T12:00:00Z', 'Temperature Sensor (NTC)', '{"pins":[{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"},{"name":"OUT","kind":"analog"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-hx711', '2026-07-27T12:00:00Z', 'Load Cell HX711', '{"pins":[{"name":"VCC","kind":"power"},{"name":"DT","kind":"digital_input"},{"name":"SCK","kind":"digital_output"},{"name":"GND","kind":"ground"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-ir-receiver', '2026-07-27T12:00:00Z', 'IR Receiver', '{"pins":[{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"},{"name":"DAT","kind":"digital_input"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-membrane-keypad', '2026-07-27T12:00:00Z', 'Keypad (4x4)', '{"pins":[{"name":"R1","kind":"digital_output"},{"name":"R2","kind":"digital_output"},{"name":"R3","kind":"digital_output"},{"name":"R4","kind":"digital_output"},{"name":"C1","kind":"digital_input"},{"name":"C2","kind":"digital_input"},{"name":"C3","kind":"digital_input"},{"name":"C4","kind":"digital_input"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-ssd1306', '2026-07-27T12:00:00Z', 'OLED SSD1306', '{"pins":[{"name":"DATA","kind":"i2c"},{"name":"CLK","kind":"i2c"},{"name":"DC","kind":"digital_output"},{"name":"RST","kind":"digital_output"},{"name":"CS","kind":"digital_output"},{"name":"3V3","kind":"power"},{"name":"VIN","kind":"power"},{"name":"GND","kind":"ground"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-lcd1602', '2026-07-27T12:00:00Z', 'LCD 16x2', '{"pins":[{"name":"VSS","kind":"ground"},{"name":"VDD","kind":"power"},{"name":"V0","kind":"analog"},{"name":"RS","kind":"digital_output"},{"name":"RW","kind":"digital_output"},{"name":"E","kind":"digital_output"},{"name":"D0","kind":"digital_output"},{"name":"D1","kind":"digital_output"},{"name":"D2","kind":"digital_output"},{"name":"D3","kind":"digital_output"},{"name":"D4","kind":"digital_output"},{"name":"D5","kind":"digital_output"},{"name":"D6","kind":"digital_output"},{"name":"D7","kind":"digital_output"},{"name":"A","kind":"power"},{"name":"K","kind":"ground"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-lcd1602-i2c', '2026-07-27T12:00:00Z', 'LCD 16x2 I2C', '{"pins":[{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"},{"name":"SDA","kind":"i2c"},{"name":"SCL","kind":"i2c"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-neopixel', '2026-07-27T12:00:00Z', 'NeoPixel / LED Strip', '{"pins":[{"name":"VDD","kind":"power"},{"name":"DOUT","kind":"digital_output"},{"name":"VSS","kind":"ground"},{"name":"DIN","kind":"digital_input"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-led-bar-graph', '2026-07-27T12:00:00Z', 'LED Bar Graph', '{"pins":[{"name":"A1","kind":"digital_output"},{"name":"A2","kind":"digital_output"},{"name":"A3","kind":"digital_output"},{"name":"A4","kind":"digital_output"},{"name":"A5","kind":"digital_output"},{"name":"A6","kind":"digital_output"},{"name":"A7","kind":"digital_output"},{"name":"A8","kind":"digital_output"},{"name":"A9","kind":"digital_output"},{"name":"A10","kind":"digital_output"},{"name":"C1","kind":"ground"},{"name":"C2","kind":"ground"},{"name":"C3","kind":"ground"},{"name":"C4","kind":"ground"},{"name":"C5","kind":"ground"},{"name":"C6","kind":"ground"},{"name":"C7","kind":"ground"},{"name":"C8","kind":"ground"},{"name":"C9","kind":"ground"},{"name":"C10","kind":"ground"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-7segment', '2026-07-27T12:00:00Z', 'Seven Segment Display', '{"pins":[{"name":"COM.1","kind":"power_or_ground"},{"name":"COM.2","kind":"power_or_ground"},{"name":"A","kind":"digital_output"},{"name":"B","kind":"digital_output"},{"name":"C","kind":"digital_output"},{"name":"D","kind":"digital_output"},{"name":"E","kind":"digital_output"},{"name":"F","kind":"digital_output"},{"name":"G","kind":"digital_output"},{"name":"DP","kind":"digital_output"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-stepper-motor', '2026-07-27T12:00:00Z', 'Stepper Motor', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-ili9341', '2026-07-27T12:00:00Z', 'TFT Display (ILI9341)', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-dht11', '2026-07-27T12:00:00Z', 'DHT11', '{"pins":[{"name":"VCC","kind":"power"},{"name":"SDA","kind":"digital_bidirectional"},{"name":"NC","kind":"none"},{"name":"GND","kind":"ground"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-relay-module', '2026-07-27T12:00:00Z', 'Relay Module', '{"pins":[{"name":"VCC","kind":"power"},{"name":"IN","kind":"digital_input"},{"name":"GND","kind":"ground"},{"name":"NO","kind":"switch"},{"name":"COM","kind":"switch"},{"name":"NC","kind":"switch"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-fan', '2026-07-27T12:00:00Z', 'Fan / DC Fan', '{"pins":[{"name":"+","kind":"power"},{"name":"-","kind":"ground"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-water-pump', '2026-07-27T12:00:00Z', 'Water Pump / Mini Pump', '{"pins":[{"name":"+","kind":"power"},{"name":"-","kind":"ground"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-water-leak-sensor', '2026-07-27T12:00:00Z', 'Water Leak Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"S","kind":"digital_input"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-rain-sensor', '2026-07-27T12:00:00Z', 'Rain Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"DO","kind":"digital_input"},{"name":"AO","kind":"analog"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-soil-moisture-sensor', '2026-07-27T12:00:00Z', 'Soil Moisture Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"DO","kind":"digital_input"},{"name":"AO","kind":"analog"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-ir-obstacle-sensor', '2026-07-27T12:00:00Z', 'IR Obstacle Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT","kind":"digital_input"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-line-tracking-sensor', '2026-07-27T12:00:00Z', 'Line Tracking Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT","kind":"digital_input"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-color-sensor', '2026-07-27T12:00:00Z', 'Color Sensor', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"SDA","kind":"i2c"},{"name":"SCL","kind":"i2c"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-vibration-sensor', '2026-07-27T12:00:00Z', 'Vibration Sensor / SW-420', '{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT","kind":"digital_input"}]}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-solenoid-valve', '2026-07-27T12:00:00Z', 'Solenoid / Valve', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-esp32-cam', '2026-07-27T12:00:00Z', 'ESP32-CAM', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-wifi-cloud-node', '2026-07-27T12:00:00Z', 'WiFi / Cloud Node', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-dashboard-cloud', '2026-07-27T12:00:00Z', 'Dashboard / Cloud', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-robot-arm-base', '2026-07-27T12:00:00Z', 'Robot Arm Base', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-gripper', '2026-07-27T12:00:00Z', 'Gripper', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-conveyor-belt', '2026-07-27T12:00:00Z', 'Conveyor Belt', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-sorting-box', '2026-07-27T12:00:00Z', 'Sorting Box', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-ball', '2026-07-27T12:00:00Z', 'Ball', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-fire-extinguisher', '2026-07-27T12:00:00Z', 'Fire Extinguisher', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-water-tank', '2026-07-27T12:00:00Z', 'Water Tank', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-drone-frame', '2026-07-27T12:00:00Z', 'Drone Frame', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-propeller', '2026-07-27T12:00:00Z', 'Propeller', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-drone-motor', '2026-07-27T12:00:00Z', 'Drone Motor', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-stair-obstacle', '2026-07-27T12:00:00Z', 'Stair / Obstacle Block', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-trash-object', '2026-07-27T12:00:00Z', 'Trash Object', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z'),
    ('wokwi-delivery-item', '2026-07-27T12:00:00Z', 'Delivery Package / Item', '{"pins":[],"visualOnly":true}', true, '2026-07-27T12:00:00Z')
ON CONFLICT ("ComponentType") DO NOTHING;

-- Verify sau khi chạy:
-- SELECT COUNT(*) FROM "ComponentGlueRegistry" WHERE "ComponentType" LIKE 'wokwi-%';
-- Kỳ vọng: 10 (Robot Delivery Kit) + 1 (RGB LED) + 44 (Component Library) + các dòng cũ trước đó.
