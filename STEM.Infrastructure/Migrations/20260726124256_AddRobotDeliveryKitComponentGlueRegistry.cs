using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEM.Infrastructure.Migrations
{
    /// <summary>
    /// Viết tay, KHÔNG sinh qua `dotnet ef migrations add` — công cụ đó trong môi
    /// trường này resolve nhầm sang design-time factory SQL Server thay vì
    /// Npgsql thật (xác nhận qua lỗi kết nối Named Pipes Provider khi chạy thử),
    /// sinh ra hàng loạt AlterColumn đổi kiểu cột Postgres -> SQL Server cho các
    /// bảng không liên quan — đã bị hủy ngay, không áp dụng. Migration này chỉ
    /// làm đúng 1 việc: InsertData 10 dòng ComponentGlueRegistry mới cho bộ
    /// Robot giao hàng mini, khớp 100% với STEM.Infrastructure/Data/StemDbContext.cs
    /// OnModelCreating (robotKitSeedTime = 2026-07-26). Không đổi bất kỳ bảng/
    /// cột nào khác — additive thuần tuý, không ảnh hưởng 6 dòng seed cũ.
    /// </summary>
    public partial class AddRobotDeliveryKitComponentGlueRegistry : Migration
    {
        private static readonly DateTime SeedTime = new DateTime(2026, 7, 26, 0, 0, 0, 0, DateTimeKind.Utc);

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ComponentGlueRegistry",
                columns: new[] { "ComponentType", "CreatedAt", "Label", "PinRequirementsJson", "Supported", "UpdatedAt" },
                values: new object[,]
                {
                    { "wokwi-hc-sr04", SeedTime, "HC-SR04 Ultrasonic Sensor", "{\"pins\":[{\"name\":\"VCC\",\"kind\":\"power\"},{\"name\":\"TRIG\",\"kind\":\"digital_output\"},{\"name\":\"ECHO\",\"kind\":\"digital_input\"},{\"name\":\"GND\",\"kind\":\"ground\"}]}", true, SeedTime },
                    { "wokwi-l298n", SeedTime, "L298N Motor Driver", "{\"pins\":[{\"name\":\"IN1\",\"kind\":\"digital_output\"},{\"name\":\"IN2\",\"kind\":\"digital_output\"},{\"name\":\"IN3\",\"kind\":\"digital_output\"},{\"name\":\"IN4\",\"kind\":\"digital_output\"},{\"name\":\"ENA\",\"kind\":\"pwm_output\"},{\"name\":\"ENB\",\"kind\":\"pwm_output\"},{\"name\":\"OUT1\",\"kind\":\"motor_output\"},{\"name\":\"OUT2\",\"kind\":\"motor_output\"},{\"name\":\"OUT3\",\"kind\":\"motor_output\"},{\"name\":\"OUT4\",\"kind\":\"motor_output\"},{\"name\":\"VIN\",\"kind\":\"power\"},{\"name\":\"GND\",\"kind\":\"ground\"},{\"name\":\"5V\",\"kind\":\"power\"}]}", true, SeedTime },
                    { "wokwi-dc-motor", SeedTime, "DC Geared Motor / TT Motor", "{\"pins\":[{\"name\":\"terminal1\",\"kind\":\"motor_terminal\"},{\"name\":\"terminal2\",\"kind\":\"motor_terminal\"}]}", true, SeedTime },
                    { "wokwi-battery-pack", SeedTime, "Battery Pack 7.4V (2x18650)", "{\"pins\":[{\"name\":\"+\",\"kind\":\"power\"},{\"name\":\"-\",\"kind\":\"ground\"}]}", true, SeedTime },
                    { "wokwi-power-switch", SeedTime, "Power Switch", "{\"pins\":[{\"name\":\"IN\",\"kind\":\"power\"},{\"name\":\"OUT\",\"kind\":\"power\"}]}", true, SeedTime },
                    { "wokwi-robot-wheel", SeedTime, "Robot Wheel", "{\"pins\":[],\"visualOnly\":true}", true, SeedTime },
                    { "wokwi-caster-wheel", SeedTime, "Caster Wheel", "{\"pins\":[],\"visualOnly\":true}", true, SeedTime },
                    { "wokwi-robot-chassis", SeedTime, "Robot Chassis", "{\"pins\":[],\"visualOnly\":true}", true, SeedTime },
                    { "wokwi-breadboard", SeedTime, "Breadboard", "{\"pins\":[],\"visualOnly\":true}", true, SeedTime },
                    { "wokwi-delivery-box", SeedTime, "Mini Delivery Box", "{\"pins\":[],\"visualOnly\":true}", true, SeedTime },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-hc-sr04");
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-l298n");
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-dc-motor");
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-battery-pack");
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-power-switch");
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-robot-wheel");
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-caster-wheel");
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-robot-chassis");
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-breadboard");
            migrationBuilder.DeleteData(table: "ComponentGlueRegistry", keyColumn: "ComponentType", keyValue: "wokwi-delivery-box");
        }
    }
}
