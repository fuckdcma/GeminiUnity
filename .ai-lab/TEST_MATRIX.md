# TEST MATRIX — CHỨNG CỨ KIỂM THỬ THỰC TẾ

## 1. Kiểm Thử Hồi Quy 35 Ca Nghiệm Thu (Acceptance Tests AT-01 -> AT-35)

**Lệnh thực thi:** `dotnet run --project UnityLocalizationStudio test-at`  
**Thời gian:** 16/10/2026 22:45  
**Exit code:** `0`  
**Kết quả tổng quan:** 35/35 PASS (100%), 0 FAIL  

| Test ID | Tên Ca Kiểm Thử | Trạng Thái | Bằng Chứng Thực Tế |
| :---: | :--- | :---: | :--- |
| **AT-01** | Bookings Stand in Game Assets | `PASS` | `ID=13D7F88E0F84CB21, AssetFile=sharedassets1.assets, Field=Name, Class=GameText` |
| **AT-02** | `VIEW_CARDS` -> View Cards | `PASS` | `Key=VIEW_CARDS, Text='View Cards', Class=GameText` |
| **AT-03** | OdinStruct `Start day` | `PASS` | `Origin=OdinStruct, Field=Name, Class=GameText` |
| **AT-04** | Distinct instances of `Start day` | `PASS` | `AT-03 (Struct) != AT-04 (Dict Key=START_DAY_READY) độc lập không bị đè ID` |
| **AT-05** | Gameplay description in Assets | `PASS` | `Class=GameText, Text='Add chopped cheese to burgers'` |
| **AT-06** | `Newtonsoft.Json` excluded | `PASS` | `Class=Technical, Status=Ignored` |
| **AT-07** | `websocket-sharp OPTIONS` protected | `PASS` | `Class=Protected, Status=Protected` |
| **AT-08** | `ChatClient` classified Technical | `PASS` | `Class=Technical, Status=Ignored` |
| **AT-09** | `RecipeViewElement` preserved | `PASS` | `Class=GameText, Status=Translate` |
| **AT-10** | Placeholders & markup invariant | `PASS` | `Tokens bảo toàn: {0}\|<sprite name="heat">\|$upgradable$` |
| **AT-11** | Stable record identities | `PASS` | `ID tính toán ổn định: AB6F017A1B38666F == AB6F017A1B38666F` |
| **AT-12** | Excel safe numbers & PathID | `PASS` | `AssetPathId='000123', RefId='1E10', Unicode='Bít tết đặc biệt với 000456 phần'` |
| **AT-13** | WindowsSecretStore DPAPI Roundtrip | `PASS` | `DPAPI Protect/Unprotect/Delete hoạt động chính xác với DataProtectionScope.CurrentUser` |
| **AT-14** | AiLab Redaction of sensitive user/keys/paths | `PASS` | `Đã che phủ thành công UserName, User Path, Gemini Key, OpenAI Key` |
| **AT-15** | AiLab Offline Enforcement (Zero Remote Calls) | `PASS` | `Khóa an toàn Offline chặn mọi request khi IsEnabled=false` |
| **AT-16** | Synthetic Telemetry Generator (TMP + UGUI Mixed) | `PASS` | `Đã tạo thành công 14 phần tử UI đa dạng component TMP và UGUI` |
| **AT-17** | Synthetic Protected Icon & Emoji Fonts Isolation | `PASS` | `Bảo vệ thành công 3 font icon/emoji (Reference - Icons, NotoEmoji-Regular SDF)` |
| **AT-18** | Synthetic Text Hook LIVETEST_1..N Markup Invariant | `PASS` | `Tiêm thành công 14 text mẫu, ngôn ngữ chính: English (en-US), bảo toàn token {0} & <sprite>` |
| **AT-19** | Synthetic Font Hook Multi-Font 1-1 with 0% Pink Material Risk | `PASS` | `Đã mapping 5 font 1-1, bảo vệ 2 font icon, nguy cơ lỗi shader: 0.0%` |
| **AT-20** | Synthetic Post-Localization Audit (Step 9) | `PASS` | `Đánh giá Bước 9 hoàn hảo: Tofu=0, PinkRisk=0%, RAM Leak=False` |
| **AT-21** | Multi-Font 1-1 Mapping Matrix Resolution (Role-Based) | `PASS` | `Phân giải chính xác 5 phông theo 5 vai trò (Menu, Primary, Title, Card, Monospace), không dùng 1 font cho tất cả` |
| **AT-22** | TMP Material Preset Cloning & Effects Preservation | `PASS` | `Bản sao Material Preset bảo toàn 100% Outline (0.28), Underlay (0.42), màu sắc và Shader Keywords` |
| **AT-23** | Zero Pink Material Risk Verification (Distance Field Audit) | `PASS` | `Kiểm định tương thích họ Shader và Texture Atlas: Nguy cơ màn hình hồng = 0.0% (Tuyệt đối không lỗi)` |
| **AT-24** | Protected Icon & Emoji Font Isolation | `PASS` | `Bảo vệ tuyệt đối 3 phông biểu tượng (Reference - Icons, NotoEmoji, Stars), không bao giờ bị ghi đè` |
| **AT-25** | AssetBundle Font Metadata & Header Audit | `PASS` | `AssetBundle 'vietnamese_font.bundle' (3994 KB) hợp lệ định dạng UnityFS, sẵn sàng cho Runtime Mod` |
| **AT-26** | Missing Format Placeholder & Tag Integrity Validation | `PASS` | `Phát hiện chính xác 2 lỗi mất placeholder/sprite tag (STR_REWARD_01, STR_HUD_HEAT)` |
| **AT-27** | Multiline & Vietnamese Diacritics Unicode CSV Roundtrip | `PASS` | `Bảo toàn 100% văn bản nhiều dòng (\r\n), dấu ngoặc kép và toàn bộ ký tự tiếng Việt có dấu chuẩn UTF-8` |
| **AT-28** | Excel Safe Formatting & Formula Injection Sanitization | `PASS` | `Bảo toàn số có tiền tố 0 ('000123'), số mũ ('1E10') và phát hiện cảnh báo nguy cơ Formula Injection (=SUM)` |
| **AT-29** | Multi-Occurrence Key Isolation & Context Preservation | `PASS` | `Bảo vệ độc lập hai chuỗi có cùng nội dung tiếng Anh ('Ready') ở hai vị trí UI khác nhau mà không bị gộp trùng key` |
| **AT-30** | Font AssetBundle Binary Validation & Verification Gate | `PASS` | `Cổng kiểm định nhị phân AssetBundle xác thực chính xác UnityFS hợp lệ và loại bỏ ngay lập tức tệp bundle hỏng/giả mạo` |
| **AT-31** | Non-Destructive Prototype Cleanup & Pre-Existing Mod Preservation | `PASS` | `Dọn sạch 100% tệp prototype (4 tệp), bảo toàn tuyệt đối file game Steam gốc và mod của người chơi (KitchenModPlus.dll)` |
| **AT-32** | Portable Prerelease ZIP Packaging & Structure Validation | `PASS` | `Gói Portable ZIP hoàn chỉnh cấu trúc: winhttp.dll, doorstop_config.ini, plugin DLL, bản dịch CSV thô, Uninstall_VietHoa.cmd và SHA256SUMS.txt` |
| **AT-33** | AES-256 Encrypted LastPrerelease Packaging & RAM Decryption Verification | `PASS` | `Mã hóa nhị phân AES-256-CBC (Magic: VMOD) hoàn toàn che giấu bản dịch trên đĩa, giải mã tức thời 100% trên RAM` |
| **AT-34** | Non-Invasive Uninstall Batch Script Syntax & Safety Verification | `PASS` | `Script Uninstall_VietHoa.cmd chuẩn UTF-8, chỉ gỡ chính xác file mod việt hóa, bảo vệ tuyệt đối thư mục BepInEx gốc và các mod khác` |
| **AT-35** | Publisher Release Gatekeeper, SHA-256 Checksums & Provenance Report | `PASS` | `Báo cáo Release_Summary_Report.md chuẩn Publisher kèm mã băm SHA-256 (94267e9a..., dfa6e1ba...) và cam kết phi xâm lấn 100%` |

---

## 2. Kiểm Thử Tiến Trình GUI (Direct Launch & Sửa Lỗi CLI Fallback)

**Lệnh thực thi:** `.\Output\Studio\UnityLocalizationStudio.exe`  
**Kết quả kiểm chứng:**  
- Cửa sổ Desktop Windows Forms (1300x820) mở trực tiếp, Responding = True, không còn ngoại lệ `InvalidOperationException` từ SplitContainer.
- Hoàn toàn không còn tình trạng bị rơi xuống màn hình Console/CLI.
- Tất cả 13 bước trong danh mục quy trình bên phải hiển thị rõ ràng từ Bước 1 đến Bước 13 không bị che khuất.
- **Trạng thái:** `PASS`.
