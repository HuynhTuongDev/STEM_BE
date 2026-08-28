# STEM Virtual Lab — Final Lab Manual Test Guide

CLOSE REMAINING FINAL-LAB GAPS task (2026-08-25), STEP 12. Manual,
click-by-click guide for a human tester in a real authenticated browser
session — same reason as `FINAL_ACCEPTANCE_CHECKLIST.md`: everything below
was verified at the source/API/runtime level this task (real Docker/QEMU
compiles where noted, real xUnit tests) but not visually confirmed in the
rendered UI by this audit. Run all 9 tests in order. Check the box that
matches what you actually see — don't check PASS from memory.

---

## TEST 1 — Nút nhấn điều khiển LED (Push Button → LED)

**Đi đến**: Virtual Lab → "Chọn bài tập mẫu" → chọn bài **"Nút nhấn điều
khiển LED"**.

**Bấm**: Nút "Run/Compile", chờ nạp firmware xong. Sau đó nhấn giữ chuột lên
biểu tượng nút nhấn trên canvas, giữ vài giây, rồi thả ra.

**Giá trị nhập**: Không cần nhập số nào — chỉ nhấn giữ/thả bằng chuột.

**Kết quả mong đợi**: LED sáng ngay khi đang nhấn giữ, tắt ngay khi thả ra —
trong CÙNG 1 lần Run (không cần bấm Run lại giữa các lần nhấn/thả). Serial
Monitor in "Nut: DA NHAN - LED: ON" / "Nut: DA THA - LED: OFF" khớp đúng
từng lần.

PASS ☐ FAIL ☐

---

## TEST 2 — Chiết áp điều khiển LED theo ngưỡng (Potentiometer)

**Đi đến**: Virtual Lab → "Chọn bài tập mẫu" → chọn bài **"Chiết áp điều
khiển LED theo ngưỡng"**.

**Bấm**: "Run/Compile", chờ nạp xong. Kéo thanh trượt chiết áp trên canvas
từ thấp lên cao, rồi kéo ngược lại xuống thấp.

**Giá trị nhập**: Kéo thanh trượt qua mốc giữa (ngưỡng là ADC = 2000/4095).

**Kết quả mong đợi**: LED tắt khi giá trị < 2000, bật khi giá trị >= 2000,
đổi đúng lúc kéo qua ngưỡng, không cần Restart. Serial Monitor in đúng giá
trị ADC kèm trạng thái LED mỗi lần.

PASS ☐ FAIL ☐

---

## TEST 3 — Cảm biến ánh sáng điều khiển đèn ngủ (Light Sensor)

**Đi đến**: Virtual Lab → "Chọn bài tập mẫu" → chọn bài **"Cảm biến ánh sáng
điều khiển đèn ngủ"**.

**Bấm**: "Run/Compile", chờ nạp xong. Kéo thanh trượt độ sáng trên canvas.

**Giá trị nhập**: Kéo qua mốc giữa (ngưỡng là giá trị = 1500/4095).

**Kết quả mong đợi**: NGƯỢC với Bài Chiết áp — LED BẬT khi giá trị < 1500
("tối"), TẮT khi >= 1500 ("sáng"). Mặc định lúc chưa kéo gì, LED phải đang
BẬT (giá trị mặc định là 0, coi như tối).

PASS ☐ FAIL ☐

---

## TEST 4 — Kịch bản DHT22 (Sensor Scenario)

**Đi đến**: Virtual Lab → "Chọn bài tập mẫu" → chọn bài **"Trạm đo nhiệt độ
độ ẩm DHT"** (Bài 10).

**Bấm**: Mở panel "Kịch bản cảm biến" (biểu tượng cạnh DHT trên canvas hoặc
nút riêng trong sandbox) — xác nhận phần **"Xem trước"** hiển thị đúng 4 mốc:
`0s → 25°C / 60%`, `3s → 30°C / 65%`, `6s → 38°C / 70%`, `9s → 26°C / 60%`.
Đóng panel, bấm "Run/Compile".

**Giá trị nhập**: Không cần đổi gì — dùng đúng kịch bản mặc định của bài.

**Kết quả mong đợi**: Serial Monitor in nhiệt độ/độ ẩm khớp đúng 4 mốc theo
đúng thứ tự thời gian. LED (GPIO13) CHỈ bật trong khoảng mốc 38°C (giây thứ
6-9), tắt các thời điểm còn lại. Serial Monitor in "CANH BAO: NHIET DO CAO!"
đúng lúc LED bật.

PASS ☐ FAIL ☐

---

## TEST 5 — HC-SR04 (Cảm biến khoảng cách)

**Đi đến**: Virtual Lab → "Chọn bài tập mẫu" → chọn bài có HC-SR04 (ví dụ
"[Robot Giao Hàng Mini] LAB05" hoặc bài "Tránh vật cản").

**Bấm**: "Run/Compile", mở "Kịch bản cảm biến" trước khi Run nếu muốn đổi
mốc khoảng cách, hoặc dùng kịch bản mặc định.

**Giá trị nhập**: Dùng kịch bản mặc định của bài (hoặc tự thêm 1 mốc mới
qua nút "Thêm mốc" để xác nhận panel hoạt động).

**Kết quả mong đợi**: Serial Monitor in đúng khoảng cách (cm) theo từng mốc
thời gian trong kịch bản; hành vi robot (dừng/tránh) đúng theo threshold
trong code.

PASS ☐ FAIL ☐

---

## TEST 6 — LAB06 (Dừng xe khi gặp vật cản — cả 2 động cơ)

**Đi đến**: Virtual Lab → "Chọn bài tập mẫu" → mục "Robot Giao Hàng Mini" →
**"[Robot Giao Hàng Mini] LAB06 — Dừng Xe Khi Gặp Vật Cản"**.

**Bấm**: "Run/Compile", chờ compile QEMU thật xong (có thể mất 20-90 giây
lần đầu), quan sát canvas + Serial Monitor.

**Giá trị nhập**: Không cần đổi gì — kịch bản mặc định: 100cm ở giây 0, 15cm
ở giây thứ 5.

**Kết quả mong đợi**: Ở giai đoạn 100cm, CẢ 2 card động cơ hiện "Tiến" và cả
2 hình động cơ/bánh xe đều quay. Sau khi kịch bản chuyển sang 15cm, CẢ 2
động cơ chuyển "Dừng" và cả 2 hình đều đứng yên — trong CÙNG 1 lần Run,
không cần bấm Run lại.

PASS ☐ FAIL ☐

---

## TEST 7 — LAB08 (Chỉ 1 động cơ quay lúc rẽ)

**Đi đến**: Virtual Lab → "Chọn bài tập mẫu" → mục "Robot Giao Hàng Mini" →
**"[Robot Giao Hàng Mini] LAB08"**.

**Bấm**: "Run/Compile", chờ compile xong, theo dõi Serial Monitor VÀ canvas
liên tục từ đầu đến khi thấy "DELIVERED".

**Giá trị nhập**: Không cần đổi gì — dùng kịch bản mặc định.

**Kết quả mong đợi**: Trình tự đầy đủ: BAT DAU GIAO HANG → MOVING (cả 2 bánh
quay) → OBSTACLE (cả 2 bánh dừng) → TURNING (**CHỈ ĐÚNG 1 bánh quay, bánh
còn lại đứng yên** — đây là điểm quan trọng nhất của bài test này, quan sát
kỹ) → MOVING lại (cả 2 quay) → DELIVERED (cả 2 dừng hẳn, không đổi trạng
thái nữa dù chờ thêm).

PASS ☐ FAIL ☐

---

## TEST 8 — Lưu và tải lại (Save → Reload giữ nguyên Sensor Scenario)

**Đi đến**: Mở bất kỳ bài nào có Sensor Scenario (ví dụ Bài 10 DHT hoặc
LAB06), chỉnh sửa 1-2 mốc trong panel "Kịch bản cảm biến" (đổi số).

**Bấm**: Bấm nút Lưu (Save) diagram. Sau đó rời trang (hoặc F5 tải lại
trang), mở lại đúng bài đó.

**Giá trị nhập**: Đổi ít nhất 1 giá trị số trong 1 mốc kịch bản trước khi
lưu, ghi nhớ giá trị đó để đối chiếu sau khi tải lại.

**Kết quả mong đợi**: Sau khi tải lại, panel "Kịch bản cảm biến" hiển thị
ĐÚNG giá trị vừa sửa (không bị reset về mặc định, không mất mốc nào).

PASS ☐ FAIL ☐

---

## TEST 9 — Học sinh nộp bài (Student Submit giữ nguyên đủ 4 phần)

**Đi đến**: Đăng nhập tài khoản Học sinh, mở 1 Assignment có gắn Lab với
Sensor Scenario đã cấu hình (ví dụ Bài 10 hoặc LAB06). Chạy thử (Run) ít
nhất 1 lần, sau đó bấm "Nộp bài" (Submit).

**Bấm**: Sau khi nộp, đăng nhập lại bằng tài khoản Giáo viên, mở bài nộp đó
để xem lại (Xem chi tiết bài nộp).

**Giá trị nhập**: Không cần nhập gì thêm — dùng đúng diagram/code/kịch bản
đã có sẵn khi Run.

**Kết quả mong đợi**: Bài nộp giáo viên xem lại phải còn ĐẦY ĐỦ: sơ đồ mạch
(diagram), code, kịch bản cảm biến (sensorScenario, nếu bài có), và liên kết
cơ khí (mechanicalLinks, nếu bài có robot wheel/motor) — không phần nào bị
mất so với lúc học sinh Run/Submit. (Đã có test tự động xác nhận điều này ở
tầng backend — `VirtualLabSubmissionRoundTripTests.cs` — bài test này ở đây
là để xác nhận thêm 1 lần nữa từ góc nhìn giao diện thật.)

PASS ☐ FAIL ☐

---

## PHASE NEXT — 5 Lab mới từ danh sách (1).docx (SMOKE TEST, không bắt buộc full E2E)

Đã có bằng chứng test tự động thật (diagram validation + real Docker/QEMU
runtime, xem `PhaseNextNewLabsDiagramTests.cs` +
`RobotDeliveryQemuIntegrationTests.cs`) cho cả 5 bài dưới đây. Phần này chỉ
cần **smoke test** — bấm Run, quan sát ~10 giây, không cần theo dõi hết kịch
bản đầy đủ như TEST 1-9 ở trên.

**SMOKE TEST — Robot nhặt rác lớp học**: Mở, bấm Run, xác nhận không có lỗi
compile, robot di chuyển rồi dừng khi khoảng cách gần. PASS ☐ FAIL ☐

**SMOKE TEST — Robot leo cầu thang nâng cao**: Mở, bấm Run, xác nhận trình
tự BAT DAU LEO → CAN BANG → TIEP TUC LEO → HOAN THANH xuất hiện trên Serial.
PASS ☐ FAIL ☐

**SMOKE TEST — Robot bóng đá mini**: Mở, bấm Run, xác nhận robot bám line
("DI THANG") rồi chuyển "SUT BONG" khi bóng vào tầm. PASS ☐ FAIL ☐

**SMOKE TEST — Robot chữa cháy tự động**: Mở, bấm Run, xác nhận "TUAN TRA"
rồi chuyển "DANG DAP LUA" khi có lửa theo kịch bản. PASS ☐ FAIL ☐

**SMOKE TEST — Hệ thống sấy nông sản thông minh**: Mở, bấm Run, xác nhận
Serial in nhiệt độ + "DANG SAY"/"DU NHIET" đổi đúng theo ngưỡng 40°C.
PASS ☐ FAIL ☐

**AUTOMATED-ONLY** (không cần test tay riêng, đã có test tự động thật):
Robot giao hàng mini, Robot an ninh PIR, Cảnh báo rò rỉ nước, Xe tự hành dò
line — đều là REUSE từ lab đã có, đã PASS ở TEST 1-9 hoặc test tự động
trước đó.

**BLOCKED_BY_RUNTIME** (không test — chưa triển khai, xem báo cáo chính):
Cánh tay robot lắp ráp, Robot gắp phân loại sản phẩm, Robot AI nhận diện
hình dạng, Drone mini, IoT ghi dữ liệu Farm lên Cloud.

**BLOCKED_BY_COMPONENT**: Robot pha chế đồ uống mini (Water Flow Sensor
chưa có runtime model).

**PARTIAL** (đã reuse phần alert, phần display bị BLOCKED_BY_RUNTIME):
Giám sát độ rung máy bơm.

---

## Nếu có FAIL

Ghi lại: bài nào, bước nào, Serial Monitor/canvas hiển thị gì thực tế (khác
với "Kết quả mong đợi" ở chỗ nào), có lỗi console nào không (F12 → Console).
Không tự suy đoán nguyên nhân nếu chưa có bằng chứng — báo lại đúng những gì
quan sát được.
