# PROJECT CHARTER — UNITY LOCALIZATION STUDIO (ULS)

## 1. Mục Tiêu Sản Phẩm
Xây dựng một bộ công cụ Studio desktop chuyên nghiệp dành cho Nhà phát hành (NPH) và đội ngũ Việt hóa game Unity:
- Giao diện người dùng trực quan, co giãn chuẩn DPI, hỗ trợ Dark/Light theme, bố cục 3 phân vùng (Trái: DataGrid chi tiết, Phải: Sidebar 13 bước, Dưới: Live Console full ngang).
- Quy trình chuẩn hóa 13 bước khép kín từ trích xuất chuỗi, nhận diện cấu trúc engine, live test với Text mẫu / Font mẫu, xuất/nhập bản dịch, dọn dẹp sạch sẽ prototype và đóng gói bộ cài (Unpack ZIP / Installer EXE).
- Runtime Mod Plugin chạy độc lập, nhẹ, an toàn trên game Unity (Mono/IL2CPP), đồng bộ font và shader không lỗi pink material, bảo vệ font icon/emoji.

## 2. Ranh Giới Kỹ Thuật Bất Biến
1. **Không phụ thuộc AI ở Runtime:** Client phát hành và Mod Plugin hoạt động hoàn toàn offline, giải mã dữ liệu trên RAM bằng AES-256, không gửi dữ liệu ra ngoài, không nhúng AI API key.
2. **AI API là Tùy Chọn Trong Studio:** Tab `AI / API Lab` trong Studio chỉ dành cho NPH/Kỹ sư nghiên cứu log lỗi, đề xuất test case; mặc định `OFFLINE_NO_AI`.
3. **Bảo Vệ Bí Mật Tuyệt Đối:** API key chỉ được lưu bằng Windows DPAPI (`DataProtectionScope.CurrentUser`), không lưu plaintext trong project file, config hay source code.
4. **Nguyên Tắc Phi Phá Hủy:** Mọi thao tác cài đặt mod hay prototype đều phải có manifest quản lý để gỡ sạch 100%, bảo vệ tuyệt đối mod của người chơi và file gốc của game.
5. **Không Hack / Bypass DRM:** Không can thiệp anti-cheat, không bẻ khóa bảo vệ bản quyền, ưu tiên game offline hoặc game cho phép modding hợp lệ.
6. **Tạm Dừng Thao Tác Trực Tiếp Lên Game PlateUp:** Mọi thử nghiệm dùng synthetic fixture hoặc sandbox an toàn cho đến khi có chỉ thị mới.
