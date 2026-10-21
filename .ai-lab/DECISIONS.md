# DECISIONS LOG (ADR) — UNITY LOCALIZATION STUDIO

## ADR-001: Duy Trì Kiến Trúc Hai Tầng (Studio Desktop + Runtime Mod Plugin)
- **Bối cảnh:** Cần công cụ cho nhà phát hành quản lý toàn bộ vòng đời Việt hóa game Unity, đồng thời cần một runtime mod chạy mượt mà trong engine game.
- **Quyết định:** Giữ nguyên 2 project riêng biệt: `UnityLocalizationStudio` (.NET 8.0 Windows Forms) và `RuntimeModPlugin` (.NET Standard 2.0 tương thích BepInEx 5 / HarmonyX).
- **Lý do:** Đảm bảo tính độc lập hoàn toàn, runtime mod không phụ thuộc vào framework UI hay môi trường máy của nhà phát hành.

## ADR-002: Layout Giao Diện 3 Phân Vùng Chuẩn Hóa
- **Bối cảnh:** Người dùng yêu cầu bố cục trực quan: Bảng dữ liệu lớn bên trái, Bảng 13 bước nhỏ gọn bên phải, Live Console full ngang dưới cùng.
- **Quyết định:** Tái cơ cấu UI của `MainStudioForm.cs` theo đúng cấu trúc này. Bố trí `SplitContainer` để co giãn mượt mà theo độ phân giải và DPI màn hình.
- **Hệ quả:** Nhà phát hành có không gian tối đa để kiểm tra chuỗi dịch ở bên trái, dễ dàng theo dõi tiến độ ở bên phải, và đọc log chẩn đoán toàn màn hình ở dưới cùng.

## ADR-003: Cơ Chế Text Mẫu & Font Mẫu Dùng Chung Toàn Cục
- **Bối cảnh:** Nhà phát hành muốn kiểm chứng phương pháp hook/mock trong game trước khi bỏ thời gian dịch thuật hàng ngàn dòng text.
- **Quyết định:**
  - Cung cấp `Sample_LiveTest_Texts.csv` dạng `ID,TEXT_MẪU` (`1,LIVETEST_1`, `2,LIVETEST_2`...).
  - Cung cấp `vietnamese_font.bundle` với asset `ARIAL SDF` chứa đầy đủ Unicode thế giới và tiếng Việt có dấu.
  - Tự động bảo vệ font Icon/Emoji (`Reference - Icons`, `NotoEmoji-Regular SDF`) không bị thay thế.
- **Hệ quả:** Bước 6 và Bước 7 có thể chạy thử nghiệm tức thì trên mọi game Unity.

## ADR-004: Bảo Mật API Key Qua Windows DPAPI
- **Bối cảnh:** Cần thêm tab `AI / API Lab` cho Studio mà không làm lộ secret hay lưu plaintext.
- **Quyết định:** Sử dụng `System.Security.Cryptography.ProtectedData` với `DataProtectionScope.CurrentUser`. Không bao giờ lưu plaintext API key vào project `.ulsproj` hay settings file.
- **Hệ quả:** Tuyệt đối an toàn trên máy người dùng, không bao giờ bị leak vào source control hay release package.
