# AGENTS.md — Chỉ Dẫn Tác Vụ Cho Antigravity & Subagents

> **Dự án:** UnityLocalizationStudio (ULS)  
> **Workspace:** `c:\Users\HP\Desktop\GeminiUnity`  
> **Master Prompt Tham Chiếu:** `Antigravity_UnityLocalizationStudio_Master_Prompt_V2.md`  
> **Lệnh Khôi Phục:** `Tiếp tục từ checkpoint`

---

## 1. NGUYÊN TẮC BẤT BIẾN (NON-NEGOTIABLE)

1. **Làm tiếp trên mã nguồn thật hiện hữu:** KHÔNG tạo lại project, KHÔNG làm lại từ đầu. Giữ nguyên dữ liệu `.ulsproj`, code, tests, manifest.
2. **Hai hệ thống độc lập:**
   - *AI R&D / Engineering Lab:* Môi trường phát triển của Antigravity để nghiên cứu, viết code, viết tests, lưu tri thức vào `.ai-lab/`.
   - *Localization Studio & Runtime Engine:* Sản phẩm phát hành cho NPH và người chơi. **KHÔNG phụ thuộc AI API trong runtime**. Client phát hành hoàn toàn offline và không chứa secrets.
3. **An toàn kiểm thử:** TẠM DỪNG Live Test với game PlateUp! Tuyệt đối không can thiệp, cài/gỡ mod hay can thiệp tệp game PlateUp khi chưa có sự cho phép rõ ràng. Dùng fixture hợp pháp/synthetic trong `.ai-lab/fixtures/` hoặc tests giả lập.
4. **Trung thực về bằng chứng:** Phân biệt rõ `SOURCE_EXISTS`, `COMPILES`, `UNIT_PASS`, `INTEGRATION_PASS`, `LIVE_PASS`, `NOT_TESTED`. Không khẳng định "hoàn thành 100%" nếu chưa có bằng chứng thực thi tương ứng.
5. **Quản lý Checkpoint bền vững:** Luôn cập nhật `.ai-lab/STATE.json` và `.ai-lab/CHECKPOINT.md` sau mỗi atomic action. Khi người dùng nhập `Tiếp tục từ checkpoint`, đọc checkpoint và thực thi ngay `nextAtomicAction`.

---

## 2. CHỈ MỤC TRÍ NHỚ BẮT BUỘC TRONG `.ai-lab/`

Khi bắt đầu phiên mới hoặc resume:
1. Đọc `.ai-lab/STATE.json` để lấy `taskId`, `taskStatus`, `nextAtomicAction`, `relevantFiles`.
2. Đọc `.ai-lab/CHECKPOINT.md` để nắm ngữ cảnh kỹ thuật gần nhất.
3. Đọc `.ai-lab/ROADMAP.md` và `.ai-lab/ARCHITECTURE_MAP.md` theo đúng phạm vi task cần làm.
4. **KHÔNG** quét đệ quy toàn bộ repository hoặc đọc lại hàng chục ngàn dòng log cũ nếu không liên quan đến task.

---

## 3. LỆNH NHANH

- `Tiếp tục từ checkpoint` -> Khôi phục ngữ cảnh và làm tiếp `nextAtomicAction` ngay.
- `Lưu checkpoint` -> Flush state, validate JSON, cập nhật checkpoint.
- `Dừng an toàn` -> Dọn tác vụ tạm, lưu checkpoint, kết thúc an toàn.
