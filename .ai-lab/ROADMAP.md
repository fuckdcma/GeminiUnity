# ROADMAP — UNITY LOCALIZATION STUDIO (ULS)

## Tổng Quan Milestones

| Milestone | Tên Milestone | Trạng Thái | Mô Tả & Tiêu Chí Nghiệm Thu |
| :---: | :--- | :---: | :--- |
| **M0** | **Audit & Durable Memory** | **DONE** | Kiểm chứng source code thực tế, chạy 12 AT tests, thiết lập hệ thống file `.ai-lab/` và `AGENTS.md`. |
| **M1** | **UI/UX Foundation & Main Layout** | **DONE** | Chuẩn hóa bố cục 3 phân vùng (Trái: DataGrid lớn, Phải: 13 bước nhỏ gọn, Dưới: Live Console full ngang), App Header + Nav Tab Bar, DPI scaling, Segoe UI, không lỗi clipping. |
| **M2** | **AI / API Lab Tab & Secret Guardrails** | **DONE** | Chế độ Offline mặc định, chọn Provider (Ollama Local / Gemini / OpenAI), lưu key an toàn qua Windows DPAPI, che giấu dữ liệu nhạy cảm. |
| **M3** | **Domain / Workflow / Capabilities** | **DONE** | Quản lý state machine 13 bước, project schema migration, capability registry versioned, tách bạch logic khỏi UI. |
| **M4** | **Discovery & Sample Prototypes** | **DONE** | Quét thô chuỗi & font, nạp Text mẫu (LIVETEST_1..N) và Font mẫu (ARIAL SDF), ma trận ánh xạ font 1-1. |
| **M5** | **Live Text/Font QA & Telemetry** | **DONE** | Kết nối Telemetry chuẩn schema, phát hiện lỗi shader hồng (0%), tự động phát hiện đóng/mở tiến trình game (AT-36). |
| **M6** | **Import / Export & Validation** | **DONE** | Xử lý CSV/JSON an toàn Excel (BOM, 1E10, PathID, leading zeros), validator kiểm tra placeholder, tag integrity và font bundle UnityFS. |
| **M7** | **AI Local Research Lab (Ollama & SQLite)** | **DONE** | Tích hợp Ollama Local (`qwen2.5-coder:7b`), SQLite Knowledge Base (`knowledge_base.db`), LocalResearchHost sandbox build/run watchdog, ResearchOrchestrator khép kín chu trình R&D tự động, UI 5 Tab R&D workbench (AT-37 -> AT-40). |
| **M8** | **Deployment & Packaging Engine** | **DONE** | Đóng gói Portable ZIP, mã hóa AES-256 cơ sở dữ liệu trên RAM, Installer EXE, Uninstaller phi xâm lấn an toàn. |
| **M9** | **Generalization & Publisher Readiness** | **IN_PROGRESS** | Mở rộng tri thức tự động cho nhiều họ game Unity khác nhau, hoàn thiện tài liệu phát hành. |
