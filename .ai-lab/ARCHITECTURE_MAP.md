# ARCHITECTURE MAP — UNITY LOCALIZATION STUDIO

## 1. Cấu Trúc Thành Phần & Quyền Sở Hữu

```
c:\Users\HP\Desktop\GeminiUnity\
├── UnityLocalizationStudio/                  [Project: Desktop WinForms .NET 8.0-windows]
│   ├── Program.cs                            # Entry point, CLI dispatcher (hỗ trợ lệnh "test-at")
│   ├── Core/                                 # Business Logic & Infrastructure
│   │   ├── AppPathResolver.cs                # Tìm kiếm tài nguyên (BepInEx zip, mod dll, font, sample data)
│   │   ├── GameDetector.cs                   # Quét cấu trúc game (*_Data, Mono vs IL2CPP, Unity version)
│   │   ├── ProjectManager.cs                 # Quản lý project (.ulsproj), CRUD dự án
│   │   ├── PrototypeManager.cs               # Cài đặt BepInEx, Mod, Sample Text, Font; dọn sạch 100%
│   │   ├── SampleTestManager.cs              # Đánh giá Text mẫu (LIVETEST_X) & Font mẫu (ARIAL SDF), tạo chẩn đoán
│   │   ├── LiveTextManager.cs                # Xử lý chuỗi thực tế thu từ Telemetry
│   │   ├── LiveFontInspector.cs              # Đọc snapshot Live_Screen_Fonts.json từ game
│   │   ├── LanguageClassifier.cs             # Nhận diện ngôn ngữ Unicode & mã hóa chuẩn
│   │   ├── FontScanner.cs                    # Quét font thô từ tệp .assets
│   │   ├── UnityAssetScanner.cs              # Quét nhị phân Unity Assets (Odin Serializer)
│   │   ├── TextExtractor.cs                  # Trích xuất chuỗi từ Managed DLLs
│   │   ├── CsvHandler.cs                     # Nhập / Xuất CSV bảo toàn Excel
│   │   ├── EncryptionService.cs              # Mã hóa AES-256 dữ liệu Vietnamese.dat
│   │   ├── PackageBuilder.cs                 # Đóng gói ZIP unpack & EXE installer
│   │   └── GameProcessRunner.cs              # Tự động khởi chạy & dừng tiến trình game
│   ├── Models/                               # Entity DTOs
│   │   ├── LocalizationProject.cs            # Dự án & danh sách 13 bước
│   │   ├── FontMappingEntry.cs               # Cấu hình ánh xạ font
│   │   ├── InstalledFileManifest.cs          # Manifest theo dõi tệp cài đặt để dọn sạch
│   │   └── TranslationEntry.cs               # Định danh chuỗi, context path, bản dịch
│   ├── UI/                                   # Presentation Layer
│   │   └── MainStudioForm.cs                 # Form chính 3 phân vùng (DataGrid, Stepper, Live Console)
│   ├── Assets/                               # Template Assets
│   │   ├── BepInEx/                          # BepInEx_x64_5.4.22.0.zip
│   │   ├── Fonts/                            # vietnamese_font.bundle (chứa ARIAL SDF)
│   │   └── SampleData/                       # Sample_LiveTest_Texts.csv, Sample_Font_Mapping.json
│   └── Tests/                                # Acceptance Tests
│       └── AcceptanceTestRunner.cs           # 12 bài kiểm thử hồi quy tự động (AT-01 -> AT-12)
│
├── RuntimeModPlugin/                         [Project: Unity Mod Plugin .NET Standard 2.0]
│   ├── Plugin.cs                             # BepInPlugin hook HarmonyX (TMP, UGUI, Button, Menu)
│   ├── LocalizationManager.cs                # Nạp từ điển RAM, dịch thuật, ghi nhận chuỗi thực tế
│   ├── FontFallbackPatcher.cs                # Ánh xạ font 1-1, đồng bộ Material/Shader
│   ├── LiveFontTelemetry.cs                  # Chụp trạng thái font đang render (F9 / Timer)
│   └── MemoryDecrypter.cs                    # Giải mã dữ liệu Vietnamese.dat trực tiếp trên RAM
│
└── Output/Studio/                            [Distribution Folder - Sẵn Sàng Vận Hành]
    ├── UnityLocalizationStudio.exe           # Bản chạy Studio chính thức
    ├── VietnameseLocalizationMod.dll         # DLL Mod đã biên dịch
    └── Assets/                               # Đầy đủ BepInEx, Fonts, SampleData
```

## 2. Luồng Dữ Liệu (Data Flows)

1. **Khởi Tạo & Quét:**
   `MainStudioForm` -> `GameDetector` -> Quét thư mục game -> `ProjectManager.CreateProject` -> Lưu `.ulsproj`.
2. **Quét Thô:**
   `UnityAssetScanner` + `TextExtractor` -> Trích xuất chuỗi -> `CsvHandler.ExportToProvenanceCsv` -> Lưu `Raw_Extracted_Texts.csv`.
3. **Cài Đặt Prototype (Kèm Text mẫu & Font mẫu):**
   `PrototypeManager.InstallPrototype` -> Giải nén BepInEx, sao chép Mod DLL, `Sample_LiveTest_Texts.csv`, `Font_Mapping_Config.json`, `vietnamese_font.bundle` -> Ghi `vietnamese_prototype.manifest`.
4. **Live Test & Chẩn Đoán:**
   Game chạy -> `RuntimeModPlugin` hoạt động -> Hotkey F9 chụp Telemetry -> `SampleTestManager` phân tích ngôn ngữ, tiêm text mẫu `LIVETEST_X`, kiểm tra shader/material -> Xuất báo cáo ra Live Console.
5. **Dọn Sạch Phi Phá Hủy:**
   `PrototypeManager.CleanPrototype` -> Đọc manifest -> Xóa chính xác các tệp đã cài -> Trả game về nguyên bản sạch 100%.
6. **Đóng Gói:**
   `PackageBuilder` -> Tạo ZIP unpack & EXE installer -> Mã hóa AES-256 dữ liệu bản dịch vào `Vietnamese.dat`.
