# QUOTA POLICY — ANTIGRAVITY AGENT

## 1. Nguyên Tắc Quản Trị Hạn Mức
1. **Chỉ ghi nhận thông tin có nguồn thật:** Nếu agent không thể đọc panel `/usage` của Antigravity CLI một cách hợp lệ, trường `source` trong `STATE.json` phải là `"UNAVAILABLE"` và giá trị phần trăm là `null`. Tuyệt đối không phỏng đoán số lượng token còn lại.
2. **Cơ Chế Vi Tác Vụ (Microtasks):** Vì quota không thể đọc trực tiếp từ shell, agent luôn tổ chức công việc theo từng microtask nguyên tử:
   - Đọc tệp cần thiết -> Thực hiện thay đổi -> Build & Test cục bộ -> Kiểm chứng -> Cập nhật State & Checkpoint.
3. **Ngưỡng Dừng An Toàn (Safe Stop Criteria):**
   - Nếu phát hiện tín hiệu `429`, `RESOURCE_EXHAUSTED` hoặc cảnh báo hạn mức: Lập tức hoàn tất ghi chép `STATE.json` và `CHECKPOINT.md` rồi dừng lại an toàn.
   - Khi có điểm chặn đòi hỏi quyền can thiệp vào game thật hoặc mua tài khoản: Chuyển trạng thái sang `BLOCKED / PENDING_USER_APPROVAL`.
4. **Tách Biệt Hạn Mức Studio:** Hạn mức API trong Studio (Google Gemini API / OpenAI API) là của người dùng Studio, hoàn toàn độc lập với hạn mức của Antigravity IDE.
