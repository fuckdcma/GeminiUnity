# MAPPING: MÔ TẢ CẤU TRÚC KIẾN TRÚC & QUY CHUẨN DỮ LIỆU

## 1. TỔNG QUAN HỆ THỐNG (SYSTEM OVERVIEW)
Hệ thống là một giải pháp hoàn chỉnh từ A-Z cho việc Việt hóa game Unity trên Windows (.exe) với nguyên tắc cốt lõi:
**"100% NON-INVASIVE (KHÔNG XÂM PHẠM FILE GỐC)"**. Mọi can thiệp diễn ra trên bộ nhớ RAM ở thời gian thực (Runtime Memory Hooking).

```
[ Game Executable (Unity .exe) ]
           │
           ▼ (Nạp tài nguyên vào RAM khi chạy)
[ RAM Memory Space ]
   ├── [ BepInEx Core Injector (winhttp.dll) ]
   │         │
   │         ▼
   ├── [ Custom Localization Mod Plugin (VietnameseMod.dll) ]
   │         ├── Decrypt Stream (AES-256) từ Vietnamese.dat trực tiếp vào RAM
   │         ├── Context-Aware Resolver (Tra cứu theo Context Path + Original Text)
   │         └── Dynamic TextMeshPro Font Injector (Nạp Fallback Font tiếng Việt)
   │
   ▼ (Ghi đè hiển thị trên RAM trước khi vẽ đồ họa)
[ Màn hình Game hiển thị Tiếng Việt hoàn hảo không lỗi '口口' ]
```

---

## 2. CẤU TRÚC THƯ MỤC DỰ ÁN (PROJECT REPOSITORY STRUCTURE)

```
c:\Users\HP\Desktop\GeminiUnity\
├── Checkpoint.md                     <-- Tiến trình và dữ liệu resume nhanh
├── MAPPING.md                        <-- Bản đồ cấu trúc & quy chuẩn hệ thống (tệp này)
│
├── UnityLocalizationStudio/          <-- Ứng dụng Studio chính (C# .NET Core)
│   ├── UnityLocalizationStudio.csproj
│   ├── Program.cs                    <-- Giao diện điều khiển / CLI / Menu xử lý
│   ├── Core/
│   │   ├── GameDetector.cs           <-- Phân loại Engine, Mono vs IL2CPP, TMP/Text
│   │   ├── TextExtractor.cs          <-- Quét & trích xuất chuỗi thô từ Game/Assemblies
│   │   ├── CsvHandler.cs             <-- Xuất / Nhập CSV phân cấp ngữ cảnh (UTF-8 BOM)
│   │   ├── ContextManager.cs         <-- Băm SHA256 Key Composite, khử trùng lặp
│   │   ├── EncryptionService.cs      <-- Mã hóa AES-256 cơ sở dữ liệu bản dịch
│   │   ├── FontManager.cs            <-- Quản lý Font Bundle TextMeshPro tiếng Việt
│   │   └── ModPackager.cs            <-- Đóng gói dạng Zip Portable & tạo Installer .exe
│   └── Assets/                       <-- Chứa BepInEx binaries, font bundles mẫu
│
├── RuntimeModPlugin/                 <-- Mã nguồn Plugin C# tiêm vào game khi chạy
│   ├── RuntimeModPlugin.csproj       <-- Target .NET Standard 2.0 / .NET Framework 4.x
│   ├── Plugin.cs                     <-- BepInEx BaseUnityPlugin khởi tạo
│   ├── MemoryDecrypter.cs            <-- Giải mã Vietnamese.dat trực tiếp trên RAM Stream
│   ├── HarmonyHooks.cs               <-- Hook TMP_Text.text, Text.text, Localization API
│   └── FontFallbackPatcher.cs        <-- Đưa Font tiếng Việt vào TextMeshPro Fallbacks
│
├── Output/                           <-- Thư mục xuất dữ liệu
│   ├── Translations/                 <-- File CSV xuất ra cho nhóm dịch
│   ├── EncryptedData/                <-- File Vietnamese.dat mã hóa
│   ├── ModZip/                       <-- Bản Mod dạng Portable .zip
│   └── Installer/                    <-- Bộ cài đặt tự động Setup_VietHoa.exe
```

---

## 3. QUY CHUẨN CẤU TRÚC FILE CSV DỊCH THUẬT (CSV SCHEMA)

Tệp CSV được mã hóa chuẩn **UTF-8 with BOM** (đảm bảo tương thích tuyệt đối với Microsoft Excel, Google Sheets, Notepad++).

| Cột (Header) | Kiểu dữ liệu | Ý nghĩa | Ví dụ |
| :--- | :--- | :--- | :--- |
| `Key_ID` | String (Hex 8 ký tự) | Mã băm định danh duy nhất (Hash SHA256) | `0x8F12A09C` |
| `Context_Path` | String | Đường dẫn phân cấp GameObject UI / Scope | `UI/MainMenu/ModeSelect/Btn_Match` |
| `Original_Text` | String | Chuỗi tiếng Anh gốc | `Match` |
| `Vietnamese_Translation` | String | Chuỗi tiếng Việt dịch thuật | `Trận đấu` |
| `Status` | String | Trạng thái bản dịch | `Pending` / `Translated` / `Approved` |
| `Notes` | String | Ghi chú ngữ cảnh, tham số biến đổi | `Menu chế độ thể thao` |

### Xử lý Xung đột từ đồng âm khác nghĩa (Disambiguation):
- Dòng 1: `Key_ID=0x8F12A09C` | `Context_Path=UI/MainMenu/ModeSelect/Btn_Match` | `Original_Text=Match` -> `Trận đấu`
- Dòng 2: `Key_ID=0x3C901FF2` | `Context_Path=Items/Survival/Match` | `Original_Text=Match` -> `Que diêm`
- Dòng 3: `Key_ID=0x7A110D5B` | `Context_Path=UI/Social/Dating/Header_Match` | `Original_Text=Match` -> `Cặp đôi`
- Dòng 4: `Key_ID=0xGLOBAL01` | `Context_Path=GLOBAL_FALLBACK` | `Original_Text=Match` -> `Trận đấu` (Dự phòng nếu không khớp ContextPath)

---

## 4. QUY TRÌNH BẢO MẬT & MÃ HÓA (ENCRYPTION SPECIFICATION)
- **Thuật toán**: AES-256-CBC với PKCS7 Padding
- **Khóa bí mật (Key)**: 32 bytes sinh ngẫu nhiên hoặc theo project key
- **Vector khởi tạo (IV)**: 16 bytes
- **Cấu trúc tệp mã hóa (`Vietnamese.dat`)**:
  - Byte 0..15: Salt / IV ngẫu nhiên
  - Byte 16..n: Khối dữ liệu nhị phân mã hóa
- **Nguyên lý Runtime In-Memory Decryption**:
  - Khi game bật: Plugin mở `FileStream("Vietnamese.dat")`
  - Đọc và giải mã qua `CryptoStream` trực tiếp vào `Dictionary<string, string>` trên RAM
  - **KHÔNG sinh ra bất kỳ tệp plaintext nào trên ổ đĩa**, chống sao chép và sửa đổi trái phép.

---

## 5. QUY CHUẨN BỘ CÀI ĐẶT & GÓI MOD (PACKAGING & INSTALLER)
1. **Dạng Zip Portable**:
   - Chứa `BepInEx/` (Core, Plugins, Config)
   - Chứa `Vietnamese.dat` (đã mã hóa)
   - Chứa `winhttp.dll` / `doorstop_config.ini`
   - Chứa `TMP Font Bundle`
   - Người dùng chỉ cần giải nén đè vào thư mục game.
2. **Dạng Trình Cài Đặt (.EXE Installer)**:
   - Một tệp duy nhất `Setup_VietHoa.exe`
   - Tự động nhận diện thư mục cài đặt Steam của PlateUp
   - Tự động kiểm tra phiên bản game
   - Có tùy chọn **Cài đặt (Install)** và **Gỡ bỏ (Uninstall sạch 100%)**
   - Không can thiệp bất kỳ tệp nào trong `PlateUp_Data/*.assets` hoặc `PlateUp.exe`. Khi gỡ bỏ, chỉ cần xóa các file mod loader đi là game nguyên bản 100%.
