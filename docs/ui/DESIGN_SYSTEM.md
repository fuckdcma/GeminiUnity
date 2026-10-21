# DESIGN SYSTEM — UNITY LOCALIZATION STUDIO (ULS)

## 1. Triết Lý Thiết Kế
- **Phong cách:** Fluent / Modern Windows 11 Desktop Professional Tool.
- **Mục tiêu:** Tối ưu hóa cho Nhà phát hành: dễ đọc, co giãn chuẩn xác theo DPI (100%, 125%, 150%, 200%), không che chữ, không chồng control, không hardcode tọa độ chết.
- **Tiêu chuẩn màu sắc:** Đạt chuẩn tương phản tối thiểu WCAG AA (tỷ lệ tương phản ≥ 4.5:1).

## 2. Bảng Màu (Color Tokens)

### Theme Tối (Dark Slate - Mặc định chuyên nghiệp)
- **Nền chính cửa sổ (Background Base):** `#0F172A` (Slate 900)
- **Nền thẻ / Panel (Surface Card):** `#1E293B` (Slate 800)
- **Nền phụ / Header (Surface Accent):** `#334155` (Slate 700)
- **Đường viền phân cách (Border):** `#475569` (Slate 600)
- **Màu văn bản chính (Text Primary):** `#F8FAFC` (Slate 50)
- **Màu văn bản phụ (Text Secondary):** `#94A3B8` (Slate 400)
- **Màu chủ đạo thao tác (Primary Action):** `#0EA5E9` (Sky 500) / Hover: `#0284C7` (Sky 600)
- **Màu thành công (Success):** `#10B981` (Emerald 500)
- **Màu cảnh báo (Warning):** `#F59E0B` (Amber 500)
- **Màu lỗi / Nguy hiểm (Danger / Delete):** `#EF4444` (Red 500)

## 3. Typography
- **Font hệ thống chuẩn:** `Segoe UI` (hỗ trợ hiển thị tiếng Việt hoàn hảo trên toàn bộ các phiên bản Windows).
- **Hệ thống kích thước (Size Scale):**
  - **Tiêu đề cửa sổ (App Title):** 12pt Bold
  - **Tiêu đề bước / Section Header:** 11pt Bold
  - **Văn bản mô tả (Body Text):** 9pt Regular
  - **Nhãn phụ / Status Chip:** 8.5pt Regular / Semibold
  - **Mã nguồn / Live Console Log:** `Consolas` 9pt Regular (Monospace)

## 4. Bố Cục 3 Phân Vùng Chuẩn (3-Pane Layout)
1. **App Header (Trên cùng):**
   - Logo ULS, Tên dự án đang mở, Trạng thái (Đã lưu / Chưa lưu), Cụm nút: Tạo Mới, Mở, Lưu, Xóa.
2. **Navigation Tab Bar (Dưới header):**
   - Các Tab chuyển đổi: `Quy trình 13 bước`, `Dữ liệu chuỗi (Text)`, `Ma trận phông (Font)`, `Live Inspector`, `Đóng gói & Phát hành`, `AI / API Lab (R&D)`.
3. **Vùng Làm Việc Chính (Trái, chiếm ~68% chiều ngang):**
   - DataGridView lớn với các cột chi tiết, bộ lọc, tìm kiếm, hiển thị văn bản không bị cắt ngắn.
4. **Sidebar 13 Bước Thu Gọn (Phải, chiếm ~32% chiều ngang):**
   - Danh sách 13 bước với số thứ tự, biểu tượng trạng thái trực quan, tóm tắt kết quả từng bước.
5. **Live Console (Dưới cùng, full chiều ngang 100%):**
   - Cửa sổ log thời gian thực với phân loại màu sắc (Xanh / Vàng / Đỏ / Cyan).
