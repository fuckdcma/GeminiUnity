from PIL import Image

img = Image.open(r'C:\Users\HP\.gemini\antigravity\brain\2718d163-fe44-4901-b34e-d106d2d7ade9\.user_uploaded\media_1792237252024_d96f744e.png')
w, h = img.size
print("Image size:", w, h)

# Let's crop around 'Quit' button
# 'Quit' is near the bottom
quit_crop = img.crop((w*0.3, h*0.5, w*0.7, h*0.6))
quit_crop.save(r'C:\Users\HP\Desktop\GeminiUnity\.ai-lab\scratch\quit_crop.png')

# Let's inspect colors around the text in quit_crop
# Find white pixels (the letters) and pixels directly adjacent to them
pixels = quit_crop.load()
qw, qh = quit_crop.size
border_colors = []
for y in range(qh):
    for x in range(qw):
        r, g, b = pixels[x, y][:3]
        if r > 200 and g > 200 and b > 200:
            # text pixel
            # check 2 pixels around it
            for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1), (1, 1), (0, 2), (1, 2)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < qw and 0 <= ny < qh:
                    nr, ng, nb = pixels[nx, ny][:3]
                    if nr < 100 and ng < 100 and nb < 100:
                        border_colors.append((nr, ng, nb))

print(f"Found {len(border_colors)} dark pixels around white text.")
if border_colors:
    print("Sample dark border pixels:", border_colors[:10])
