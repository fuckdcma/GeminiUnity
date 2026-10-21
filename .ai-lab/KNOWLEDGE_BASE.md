# KNOWLEDGE BASE — KINH NGHIỆM VÀ TRI THỨC VIỆT HÓA GAME UNITY

## 1. Cơ Chế Font & Shader TextMeshPro (TMP)
1. **Shader Mismatch & Pink Material:**
   - TextMeshPro sử dụng shader khoảng cách (Distance Field), phổ biến nhất là `TextMeshPro/Distance Field` hoặc `TextMeshPro/Mobile/Distance Field`.
   - Khi thay thế `font`, nếu gán một font asset mà không đồng bộ `fontSharedMaterial`, hoặc shader bị thiếu, Unity sẽ render toàn bộ chữ thành khối chữ nhật màu hồng tím (Missing Shader Material).
   - **Giải pháp chuẩn:** Khi gán font mới trong `FontFallbackPatcher`, luôn gán đồng thời `textComponent.fontSharedMaterial = customFont.material`, gọi `textComponent.SetAllDirty()` và `textComponent.ForceMeshUpdate(true, true)`. Đồng bộ cả `TMP_SubMesh` và `TMP_SubMeshUI`.

2. **Font Bảo Vệ (Protected Fonts):**
   - Trong game Unity, nhiều ký tự icon (như nút tay cầm Xbox A/B/X/Y, phím mũi tên, biểu tượng máu, tiền vàng) được lưu dưới dạng glyph trong phông riêng (ví dụ `Reference - Icons`, `NotoEmoji-Regular SDF`, `Icons`, `Emoji`).
   - Nếu thay thế nhầm phông này bằng phông chữ thông thường, toàn bộ icon trong game sẽ biến thành ô vuông rỗng (Tofu `□`).
   - **Quy tắc bắt buộc:** Mọi font chứa từ khóa `Icon`, `Emoji`, `Sprite`, `Star` phải có trạng thái `KEEP_ORIGINAL` (Bảo vệ tuyệt đối).

3. **Ký Tự Tiếng Việt & Glyph Coverage:**
   - Tiếng Việt có các tổ hợp dấu đặc trưng: `ă, ắ, ằ, ẳ, ẵ, ặ`, `â, ấ, ầ, ẩn, ẫ, ậ`, `ê, ế, ề, ể, ễ, ệ`, `ô, ố, ồ, ổ, ỗ, ộ`, `ơ, ớ, ờ, ở, ỡ, ợ`, `ư, ứng, ừ, ử, nữ, ự`, `đ`.
   - Phông mẫu `ARIAL SDF` bao phủ toàn bộ Unicode Latin Extended-A và Latin Extended-B, đảm bảo không bị thiếu ký tự khi render tiếng Việt.

## 2. Trích Xuất Dữ Liệu Nhị Phân (Binary Assets)
1. **Odin Serializer:**
   - Các game Unity sử dụng Sirenix Odin Serializer thường lưu chuỗi dạng struct nhị phân hoặc dictionary nội suy.
   - Quét string thô thông thường sẽ làm mất khóa ngữ cảnh (Context Key) và dễ đè ID giữa các chuỗi trùng nội dung.
   - Phải tính hash ổn định kết hợp `AssetFile + Origin + Field/Key` để đảm bảo định danh bất biến (AT-04, AT-11).

2. **Excel Formatting & Roundtrip:**
   - Các định danh dạng `000123` nếu xuất ra CSV không có quote hoặc không kiểm soát sẽ bị Excel tự đổi thành số nguyên `123`.
   - Chuỗi dạng `1E10` (khoa học) sẽ bị Excel chuyển thành `10000000000`.
   - **Quy tắc:** Mọi trường mã hóa số hoặc PathID trong CSV xuất ra phải được bọc nháy kép và định dạng an toàn cho Excel.
