# FILE INDEX — UNITY LOCALIZATION STUDIO

| Đường Dẫn Tệp | Mục Đích / Mô Tả | Trạng Thái |
| :--- | :--- | :---: |
| `AGENTS.md` | Chỉ dẫn ngắn gọn cho Agent Antigravity | Hoạt động |
| `Antigravity_UnityLocalizationStudio_Master_Prompt_V2.md` | Master Prompt V2 chính thức | Quy chuẩn tối cao |
| `UnityLocalizationStudio/Program.cs` | Điểm khởi động ứng dụng & test CLI | Hoạt động |
| `UnityLocalizationStudio/Core/AppPathResolver.cs` | Dò tìm đường dẫn tài nguyên bundle/zip/mock | Hoạt động |
| `UnityLocalizationStudio/Core/GameDetector.cs` | Nhận diện engine Unity (Mono/IL2CPP) | Hoạt động |
| `UnityLocalizationStudio/Core/ProjectManager.cs` | Quản lý vòng đời dự án `.ulsproj` | Hoạt động |
| `UnityLocalizationStudio/Core/PrototypeManager.cs` | Cài đặt và dọn sạch Prototype phi phá hủy | Hoạt động |
| `UnityLocalizationStudio/Core/SampleTestManager.cs` | Đánh giá Text mẫu & Font mẫu, chẩn đoán lỗi | Hoạt động |
| `UnityLocalizationStudio/Core/LiveTextManager.cs` | Quản lý text telemetry từ game | Hoạt động |
| `UnityLocalizationStudio/Core/LiveFontInspector.cs` | Đọc snapshot font đang render | Hoạt động |
| `UnityLocalizationStudio/Core/LanguageClassifier.cs` | Nhận diện ngôn ngữ Unicode | Hoạt động |
| `UnityLocalizationStudio/Core/FontScanner.cs` | Quét font nhị phân từ assets | Hoạt động |
| `UnityLocalizationStudio/Core/UnityAssetScanner.cs` | Quét binary assets Odin Serializer | Hoạt động |
| `UnityLocalizationStudio/Core/TextExtractor.cs` | Trích xuất text từ Managed DLLs | Hoạt động |
| `UnityLocalizationStudio/Core/CsvHandler.cs` | Đọc / Ghi CSV an toàn Excel | Hoạt động |
| `UnityLocalizationStudio/Core/EncryptionService.cs` | Mã hóa AES-256 cơ sở dữ liệu bản dịch | Hoạt động |
| `UnityLocalizationStudio/Core/PackageBuilder.cs` | Đóng gói ZIP unpack & EXE installer | Hoạt động |
| `UnityLocalizationStudio/Core/GameProcessRunner.cs` | Điều khiển tiến trình game | Hoạt động |
| `UnityLocalizationStudio/Models/LocalizationProject.cs` | Entity dự án & định nghĩa 13 bước | Hoạt động |
| `UnityLocalizationStudio/Models/FontMappingEntry.cs` | Cấu hình ánh xạ font | Hoạt động |
| `UnityLocalizationStudio/Models/InstalledFileManifest.cs`| Manifest theo dõi tệp cài đặt | Hoạt động |
| `UnityLocalizationStudio/Models/TranslationEntry.cs` | Chuỗi văn bản, context và bản dịch | Hoạt động |
| `UnityLocalizationStudio/UI/MainStudioForm.cs` | Giao diện Studio 3 phân vùng | Cần refactor M1 |
| `UnityLocalizationStudio/Tests/AcceptanceTestRunner.cs` | 12 ca kiểm thử hồi quy tự động | Hoạt động (12/12 PASS) |
| `RuntimeModPlugin/Plugin.cs` | Plugin BepInEx hook Harmony | Hoạt động |
| `RuntimeModPlugin/LocalizationManager.cs` | Quản lý nạp từ điển & dịch trên RAM | Hoạt động |
| `RuntimeModPlugin/FontFallbackPatcher.cs` | Ánh xạ font 1-1 & bảo vệ icon | Hoạt động |
| `RuntimeModPlugin/LiveFontTelemetry.cs` | Chụp telemetry font active trên màn hình | Hoạt động |
| `RuntimeModPlugin/MemoryDecrypter.cs` | Giải mã dữ liệu trên RAM | Hoạt động |
| `Output/Studio/UnityLocalizationStudio.exe` | Bản build phát hành chạy được | Sẵn sàng |
