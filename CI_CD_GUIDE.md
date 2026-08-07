# Hướng Dẫn Cấu Hình CI/CD Cho STEM_BE

## 📋 Nội Dung

Workflow CI/CD này bao gồm 3 jobs chính:

### 1. **Build and Test** (Biên dịch và Kiểm thử)
- ✅ Checkout code từ repository
- ✅ Cài đặt .NET 8.0
- ✅ Restore dependencies (tải xuống các gói NuGet cần thiết)
- ✅ Build solution ở chế độ Release
- ✅ Chạy tất cả unit tests
- ✅ Tạo NuGet packages

### 2. **Code Quality** (Chất Lượng Code)
- ✅ Kiểm tra code style với StyleCop
- ✅ Phát hiện các vấn đề về định dạng code

### 3. **Security Scan** (Quét Bảo Mật)
- ✅ Kiểm tra các gói phụ thuộc có lỗ hổng bảo mật
- ✅ Cảnh báo về các phiên bản không an toàn

---

## 🚀 Cách Sử Dụng

### Bước 1: Push Workflow File
Workflow file đã được tạo tại: `.github/workflows/dotnet-ci.yml`

Push nó lên GitHub:
\`\`\`bash
git add .github/workflows/dotnet-ci.yml
git commit -m "Add CI/CD workflow"
git push origin main
\`\`\`

### Bước 2: Kiểm Tra Workflow
Truy cập GitHub > Repository của bạn > **Actions** tab

Bạn sẽ thấy workflow chạy tự động khi:
- Push code lên branch `main` hoặc `develop`
- Tạo Pull Request vào 2 branch này

---

## 🔧 Tùy Chỉnh Workflow

### Thay Đổi .NET Version
Sửa trong file `.github/workflows/dotnet-ci.yml`:
\`\`\`yaml
DOTNET_VERSION: '9.0.x'  # hoặc phiên bản khác
\`\`\`

### Thêm Branches
Sửa phần `on:`:
\`\`\`yaml
on:
  push:
    branches: [ "main", "develop", "staging" ]  # Thêm branch tại đây
  pull_request:
    branches: [ "main", "develop", "staging" ]
\`\`\`

### Chạy Tests Cụ Thể
Thay đổi lệnh test:
\`\`\`yaml
- name: Run unit tests
  run: dotnet test ${{ env.SOLUTION_PATH }} --filter "Category=Unit" --configuration Release
\`\`\`

---

## 📊 Các Job Chạy Song Song

- **build-and-test**: Chạy trên Ubuntu
- **code-quality**: Chạy song song trên Ubuntu
- **security-scan**: Chạy song song trên Ubuntu

Thời gian: ~5-10 phút tùy thuộc vào kích thước dự án

---

## ✅ Kiểm Tra Status

Trong GitHub repository:
- 🟢 **Green**: Tất cả tests passed
- 🔴 **Red**: Có lỗi cần fix
- 🟡 **Yellow**: Đang chạy

---

## 🐛 Troubleshooting

### Nếu tests fail:
1. Kiểm tra logs trong GitHub Actions
2. Fix issues cục bộ trước khi push
3. Commit và push lại

### Nếu builds fail:
1. Đảm bảo .NET version phù hợp
2. Run `dotnet restore` và `dotnet build` cục bộ
3. Fix compile errors

---

## 📝 Ví Dụ Logs

\`\`\`
✓ Checkout code
✓ Setup .NET 8.0.x
✓ Restore dependencies
✓ Build solution (Release)
✓ Run unit tests (5/5 passed)
✓ Create NuGet artifact
✓ Upload artifacts
\`\`\`

---

## 🔐 Bảo Mật

Workflow này không yêu cầu secrets. Nếu bạn muốn deploy:
- Thêm deployment step với credentials
- Lưu secrets trong GitHub Settings > Secrets

---

## 📚 Thêm Tính Năng

Bạn có thể thêm:
- ✨ Docker containerization
- ✨ Deploy to Azure App Service
- ✨ Database migrations
- ✨ SonarQube analysis
- ✨ Slack notifications

Bảo tôi nếu bạn muốn thêm bất cứ tính năng nào!
