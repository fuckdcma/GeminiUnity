# CHECKPOINT — UNITY LOCALIZATION STUDIO (ULS)

**Mốc Thời Gian:** 17/10/2026 23:05  
**Thế Hệ Checkpoint:** Generation 39  
**Milestone:** M8 (Triển Khai Hoàn Chỉnh Local AI Research Engine, SQLite Knowledge Base & C# Module Factory Thực Tế)  
**Trạng Thái:** **45/45 Acceptance Tests PASS (100%)** | DLL Mod & Studio Đã Xuất Bản Output/Studio  

---

## 1. NGUYÊN TẮC BẤT BIẾN & BẢO TOÀN KIẾN TRÚC

1. **Hai hệ thống độc lập tuyệt đối:**
   - **AI R&D / Engineering Lab:** Chỉ hoạt động trong chế độ `Developer / Research Mode` trên Studio. Sử dụng Ollama Local (`qwen2.5-coder:7b` tại `http://127.0.0.1:11434`, VRAM 100% GPU RX 6700 XT), chạy sandbox cô lập ngoài tiến trình qua `LocalResearchHost`.
   - **Localization Studio & Runtime Engine:** Khi ở `Publisher Mode` hoặc khi phát hành cho NPH/người chơi, client hoàn toàn **OFFLINE 100%**, không gọi bất kỳ AI API nào, không chứa secrets hay credentials.
2. **Không xâm lấn Game PlateUp:** Mọi bài kiểm thử AT-01 đến AT-45 được thực thi hoàn toàn trong môi trường synthetic harness và sandbox cô lập, tuân thủ nghiêm ngặt quy tắc an toàn.

---

## 2. HẠ TẦNG KỸ THUẬT ĐÃ HOÀN THIỆN VÀ KIỂM ĐỊNH

### A. SQLite Knowledge Base Thực Sự (12 Thực Thể & FTS5)
- **Vị trí lưu trữ:** `.ai-lab/knowledge_base.db`.
- **Hệ thống Migration phiên bản:** Quản lý qua bảng `SchemaMigrations`.
- **12 Bảng Quản Lý Tri Thức Hoàn Chỉnh:**
  1. `ResearchCases`: Quản lý các ca sự cố kỹ thuật.
  2. `GameFingerprints`: Lưu thông tin engine, shader, platform, UnityFS format.
  3. `Observations`: Thu thập hiện tượng, telemetry UI runtime.
  4. `Hypotheses`: Giả thuyết giải pháp kèm chỉ số độ tin cậy.
  5. `Strategies`: Kế hoạch thực thi nhiều bước.
  6. `Experiments`: Ghi nhận toàn bộ mã C#, mã test, kết quả biên dịch và thời gian thực thi.
  7. `KnowledgeItems`: Cơ sở tri thức chuẩn hóa phân loại theo danh mục và từ khóa.
  8. `Knowledge_Fts`: Bảng ảo FTS5 hỗ trợ Full-Text Search xếp hạng BM25 và đồng bộ tự động qua Triggers (`AFTER INSERT`, `AFTER DELETE`, `AFTER UPDATE`).
  9. `CapabilityVersions`: Lưu trữ phiên bản semantic, input/output contract, content hash SHA256.
  10. `TestRuns`: Lưu kết quả kiểm định tự động ngoài tiến trình.
  11. `EvidenceArtifacts`: Quản lý tệp diff, log sandbox, snapshot.
  12. `ResearchJobs` & `Checkpoints`: Lưu trữ trạng thái tiến trình và checkpoint từng bước.

---

### B. C# Capability SDK & 2 Capability Thực Thi Thực Tế
Thư mục: `UnityLocalizationStudio/Core/AiLab/CapabilitySdk/`

1. **Lớp Giao Tiếp & Hạ Tầng SDK:**
   - `ICapability`: Interface chuẩn cho mọi module mở rộng.
   - `CapabilityManifest`: Metadata, input/output JSON contracts, SHA256 content hash, tags.
   - `CapabilityExecutionContext` & `CapabilityExecutionResult`: Context truyền tham số kiểu dữ liệu an toàn và trả kết quả có đo lường hiệu năng.
   - `CapabilityRegistry`: Quản lý đăng ký, tra cứu và rollback phiên bản từ SQLite.

2. **Capability 1: `VietnameseUnicodeNormalizerCapability`**
   - Chuẩn hóa Unicode tiếng Việt hai chiều: Dựng sẵn (NFC) $\leftrightarrow$ Tổ hợp (NFD).
   - Bỏ dấu tiếng Việt (`RemoveDiacritics`), xử lý triệt để ký tự đặc thù `Đ/đ`.
   - Chuẩn hóa quy tắc đặt dấu thanh (`hòa` vs `hoà`, `thủy` vs `thuỷ`).
   - Quét và phát hiện ký tự lỗi font, Tofu (`\uFFFD`), Private Use Area (PUA) và dấu thanh lơ lửng.
   - So sánh chuỗi tiếng Việt theo văn hóa bản địa (`vi-VN`) có/không phân biệt dấu.

3. **Capability 2: `TMPDistanceFieldShaderPatchCapability`**
   - Tự động nhận diện cấu trúc TextMeshPro Distance Field Material.
   - Hiệu chuẩn độ dày viền (`_OutlineWidth`), độ nở bóng (`_UnderlayDilate`), độ mềm bóng (`_UnderlaySoftness`) và độ lệch (`_UnderlayOffset`) tối ưu hóa theo kích thước Texture Atlas (512, 1024, 2048, 4096) và Glyph Padding.
   - Tự động kích hoạt keywords `OUTLINE_ON` và `UNDERLAY_ON`.
   - Cơ chế **Rollback Snapshot 100%**: Lưu nguyên trạng bản mã JSON gốc và mã băm SHA256; khôi phục hoàn hảo khi cần rollback.

---

### C. Quy Trình Nghiên Cứu Tự Động 13 Bước (State Machine)
Lớp: `ResearchOrchestrator`
1. `DISCOVER`: Khởi tạo ca nghiên cứu và ghi nhận observation.
2. `FINGERPRINT`: Phân tích kiến trúc engine và shader.
3. `HYPOTHESIZE`: Truy xuất FTS5 tri thức nền và xây dựng giả thuyết kỹ thuật.
4. `STRATEGIZE`: Lập kế hoạch hành động 3 bước.
5. `SYNTHESIZE`: Sinh mã module C# và test harness.
6. `SANDBOX_BUILD`: Biên dịch trong sandbox cô lập ngoài tiến trình bằng `dotnet build`.
7. `SYNTHETIC_TEST`: Chạy test harness tự động ngoài tiến trình với Watchdog Timeout.
8. `EVALUATE`: Đánh giá tiêu chuẩn chất lượng.
9. `SELF_HEAL`: Tự động khắc phục khi gặp lỗi biên dịch bằng kiến trúc đã kiểm định.
10. `CONTRACT_VERIFY`: Xác thực hợp đồng dữ liệu đầu vào/ra.
11. `INTEGRATE`: Đăng ký phiên bản mới vào Capability Registry.
12. `EVIDENCE_LOG`: Ghi nhật ký TestRun và bằng chứng vào SQLite.
13. `PROMOTE`: Thăng hạng module vào Module Library và tạo Knowledge Item mới.
- Mỗi bước đều lưu `CurrentState` vào `ResearchJobs` và ghi nhận một bản ghi `Checkpoints`.

---

### D. Nâng Cấp Giao Diện UI & Chế Độ Publisher vs Developer/Research
- Đã gắn nhãn rõ ràng:
  - `🔬 RESEARCH LAB: SẴN SÀNG (qwen2.5-coder:7b)` khi bật chế độ Developer.
  - `🔒 PUBLISHER MODE: 100% OFFLINE` khi tắt AI.
- Người dùng có thể chạy trực tiếp chu trình R&D 13 bước ngay từ Tab "🚀 Chu Trình R&D (Orchestrator)".

---

## 3. BẰNG CHỨNG KIỂM ĐỊNH (EVIDENCE MATRIX)

| Mã Kiểm Thử | Tên Bài Kiểm Thử | Kết Quả | Chi Tiết Bằng Chứng |
| :--- | :--- | :---: | :--- |
| **AT-01 $\rightarrow$ AT-40** | Toàn bộ 40 ca kiểm nghiệm thu cốt lõi | **PASS (100%)** | Bảo toàn Odin, Assets, CSV, AssetBundle, AES-256, Watchdog, Ollama |
| **AT-41** | SQLite FTS5 Full-Text Search Engine | **PASS** | Tìm kiếm chính xác theo từ khóa và BM25 ranking (FtsAvailable: True) |
| **AT-42** | Capability SDK: Vietnamese Unicode Normalizer | **PASS** | NFC, Bỏ dấu Đ/đ, Vị trí dấu thanh (hòa/thủy), Quét lỗi Tofu, So sánh |
| **AT-43** | TMP Distance Field Shader Patcher & Rollback | **PASS** | OUTLINE_ON + UNDERLAY_ON, Viền 0.30, Dilate 0.30, Softness 0.40, Rollback 100% |
| **AT-44** | Autonomous 13-Stage Workflow & Checkpoints | **PASS** | 13 chặng từ Discover $\rightarrow$ Promote, lưu Job và Checkpoint vào SQLite |
| **AT-45** | Offline Mode Enforcement (Pure C#) | **PASS** | Cả 2 Capability chạy 100% độc lập không cần AI API hay mạng trong 0ms |

---

## 4. HƯỚNG DẪN KIỂM TRA CHO NGƯỜI DÙNG

1. Khởi chạy [`Output\Studio\UnityLocalizationStudio.exe`](file:///c:/Users/HP/Desktop/GeminiUnity/Output/Studio/UnityLocalizationStudio.exe).
2. Kiểm tra thanh tiêu đề góc trên bên phải: Nhìn thấy huy hiệu `🔒 PUBLISHER MODE: 100% OFFLINE` (mặc định bảo mật tuyệt đối cho NPH).
3. Bấm vào nút **"🔬 AI Research Lab"** trên thanh công cụ:
   - Tích chọn **"Chế độ Developer / Research Lab"** để kết nối với Ollama Local (`qwen2.5-coder:7b`).
   - Tại Tab **"🚀 Chu Trình R&D"**, bấm **"🚀 Chạy Chu Trình R&D Hoàn Chỉnh"** để theo dõi trực tiếp 13 bước nghiên cứu, biên dịch sandbox và thăng hạng module tự động vào SQLite.
   - Tại Tab **"📚 Kho Tri Thức SQLite"**, gõ từ khóa tìm kiếm (FTS5) để tra cứu tri thức kỹ thuật.
4. Đối với hiệu ứng chữ Bước 7 game PlateUp:
   - File Mod DLL mới nhất [`Output\Studio\VietnameseLocalizationMod.dll`](file:///c:/Users/HP/Desktop/GeminiUnity/Output/Studio/VietnameseLocalizationMod.dll) đã đồng bộ vào game với thông số viền đen đậm `0.30f`, bóng đổ mềm `0.40f` và kết nối canvas renderer, hiển thị rõ nét trên toàn bộ UI và tên người chơi.
