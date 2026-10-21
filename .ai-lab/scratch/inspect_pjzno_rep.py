from PIL import Image

img = Image.open(r'C:\Users\HP\.gemini\antigravity\brain\2718d163-fe44-4901-b34e-d106d2d7ade9\.user_uploaded\media_1792226422723_b3c76bea.png')
w, h = img.size
crop = img.crop((w*0.3, h*0.85, w*0.7, h*0.98))
pixels = crop.load()
cw, ch = crop.size

# Find the bounding box of letter 'P' in the replacement image
# Find first white pixel
for y in range(ch):
    for x in range(cw):
        if pixels[x, y][0] > 240:
            py, px = y, x
            break
    else: continue
    break

print(f"Found white pixel at x={px}, y={py}")
for y in range(max(0, py-4), min(ch, py+35), 2):
    line_str = ""
    for x in range(max(0, px-10), min(cw, px+40)):
        r, g, b = pixels[x, y][:3]
        if r > 240: line_str += "#"
        elif r > 180: line_str += "+"
        elif r > 120: line_str += "."
        elif r > 90: line_str += ":"
        else: line_str += " "
    print(f"y={y:02d}: {line_str}")
