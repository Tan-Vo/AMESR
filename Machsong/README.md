# Cosmic Horizon — Mạch Sống

Bản chơi thử cứu hộ 2D. Một nhịp co của sinh vật làm xác tàu đổi hình. Dùng dây neo để giữ tấm giáp hoặc để nó trượt vào cầu, rồi kéo khoang còn người sống về Nebula Cantina.

## Chơi

- **WASD / phím mũi tên** để bay. Giữ **Shift** hoặc **Space** để tăng lực đẩy.
- **E** để ghìm giáp, móc khoang hoặc tháo dây khi đang móc.
- **Q** để đánh nhịp sau khi đến máy thu và vượt qua cửa cầu.
- **Esc** để tạm dừng. Âm thanh và giảm rung có trong tùy chọn.

Đến máy thu qua khe trên. Ghìm tấm giáp nếu muốn giữ cầu trên. Nếu bỏ neo, tấm giáp sẽ chặn cầu và ống bảo trì phía dưới sẽ mở. Bạn có thể bám vào khoang trước khi đánh nhịp để đi theo nó, hoặc tiếp cận và móc khoang sau khi xác tàu dừng. Kéo khoang qua khe đã mở rồi vào vòng sáng ở bến.

## Unity

Nếu tải từ GitHub, chạy `git lfs pull` với Git LFS đã cài để lấy hai font. Bản ZIP dự án đã chứa font.

Mở chính thư mục **Machsong** bằng Unity **6000.5.7f1**, chọn **Mach Song → Open Rescue Scene** rồi bấm Play. Chọn **Mach Song → Build Windows** để tạo bản 64-bit trong `Builds/MachSong`.

Bộ hình nằm trong Resources. Để render lại, chạy `python Art/render.py` với Pillow và NumPy. Font Barlow dùng SIL Open Font License, bản giấy phép nằm cùng font.
