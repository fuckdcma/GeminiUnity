from PIL import Image

crop = Image.open(r'C:\Users\HP\Desktop\GeminiUnity\.ai-lab\scratch\pjzno_goc.png')
pixels = crop.load()
cw, ch = crop.size

# Find the bounding box of letter 'P' (around x=70..100, y=55..85)
for y in range(55, 90, 2):
    line_str = ""
    for x in range(70, 110):
        r, g, b = pixels[x, y][:3]
        if r > 240:
            line_str += "#" # Face
        elif r > 180:
            line_str += "+" # Light edge / anti-alias
        elif r > 120:
            line_str += "." # Shadow / border
        elif r > 90:
            line_str += ":" # Dark shadow
        else:
            line_str += " " # Background floor
    print(f"y={y:02d}: {line_str}")
