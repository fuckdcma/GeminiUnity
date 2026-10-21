from PIL import Image

img = Image.open(r'C:\Users\HP\.gemini\antigravity\brain\2718d163-fe44-4901-b34e-d106d2d7ade9\.user_uploaded\media_1792237252024_d96f744e.png')
w, h = img.size

# Find y ranges of white text
white_rows = []
pixels = img.load()
for y in range(h):
    count = sum(1 for x in range(w) if pixels[x, y][0] > 230 and pixels[x, y][1] > 230 and pixels[x, y][2] > 230)
    if count > 10:
        white_rows.append(y)

print("White text rows range:", min(white_rows) if white_rows else None, max(white_rows) if white_rows else None)

# Group rows into lines
lines = []
curr = []
for y in white_rows:
    if not curr or y == curr[-1] + 1:
        curr.append(y)
    else:
        lines.append((min(curr), max(curr)))
        curr = [y]
if curr: lines.append((min(curr), max(curr)))

print("Text lines (y_min, y_max):")
for i, (ymin, ymax) in enumerate(lines):
    print(f"Line {i}: y=[{ymin}, {ymax}]")
    crop = img.crop((0, max(0, ymin-10), w, min(h, ymax+15)))
    crop.save(rf'C:\Users\HP\Desktop\GeminiUnity\.ai-lab\scratch\line_{i}.png')
