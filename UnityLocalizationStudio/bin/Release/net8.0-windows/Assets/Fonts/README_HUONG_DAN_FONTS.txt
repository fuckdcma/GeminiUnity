========================================================================================
   THƯ MỤC LƯU TRỮ VÀ TỰ ĐỘNG ĐÓNG GÓI FONT ASSETBUNDLE CHO UNITYLOCALIZATIONSTUDIO
========================================================================================

1. MỤC ĐÍCH CỦA THƯ MỤC NÀY:
   - Nơi bạn đặt các tệp Font AssetBundle (.bundle) do bạn xuất từ Unity Editor.
   - Ví dụ: `vietnamese_font.bundle`, `vietnamese_fonts.bundle`, `*.bundle`, `*.assets`.
   - Nơi bạn đặt tệp cấu hình ma trận ánh xạ: `Font_Mapping_Config.json`.

2. CƠ CHẾ HOẠT ĐỘNG:
   - Khi bạn chọn Đóng gói ZIP Portable ([5]) hoặc Biên dịch Installer .EXE ([6]),
     UnityLocalizationStudio sẽ TỰ ĐỘNG quét thư mục này và nhúng toàn bộ:
     + Tệp `*.bundle` (SDF Font Assets)
     + Tệp `Font_Mapping_Config.json` (Bảng ma trận ánh xạ 1-1)
     + Tệp `vietnamese_fallback.ttf` (Font dự phòng hệ thống)
     + Tệp `Vietnamese.dat` (Dữ liệu bản dịch đã mã hóa AES-256)
     + Tệp `VietnameseLocalizationMod.dll` (Plugin BepInEx Runtime)
   - Người dùng cuối khi chạy `Setup_VietHoa_PlateUp.exe` hoặc giải nén file ZIP sẽ có
     đầy đủ 100% font và bản dịch mà KHÔNG cần phải copy thủ công bất kỳ tệp nào!

3. DANH SÁCH TÊN FONT KHUYẾN NGHỊ TRONG ASSETBUNDLE (ĐÃ CÓ TRONG BẢN MẪU):
   - `ARIAL SDF` (Dùng để kiểm thử sự thay đổi font trực quan)
   - `Vietnamese_Chewy_SDF` (Dành cho Menu chính và nút bấm)
   - `Vietnamese_Capriola_SDF` (Dành cho văn bản chính, HUD, công thức nấu ăn)
   - `Vietnamese_Grandstander-clean_SDF` (Dành cho tiêu đề bảng popup, tên nhà hàng)
   - `Vietnamese_ARCO_SDF`
   - `Vietnamese_MilkyNice_SDF`
========================================================================================
