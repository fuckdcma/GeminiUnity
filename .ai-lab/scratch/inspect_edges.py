from PIL import Image

for idx in (0, 7, 9, 11):
    crop = Image.open(rf'C:\Users\HP\Desktop\GeminiUnity\.ai-lab\scratch\line_{idx}.png')
    pixels = crop.load()
    w, h = crop.size
    print(f"\n--- Line {idx} ({w}x{h}) ---")
    # find characters
    # let's look at vertical profile of colors across a letter
    for x in range(w//2 - 50, w//2 + 50, 10):
        col_list = [pixels[x, y][:3] for y in range(h)]
        # see if there is white in this column
        whites = [y for y, c in enumerate(col_list) if c[0] > 200]
        if whites:
            # print color before first white and color after last white
            first_w = min(whites)
            last_w = max(whites)
            top_edge = [col_list[y] for y in range(max(0, first_w-3), first_w)]
            bottom_edge = [col_list[y] for y in range(last_w+1, min(h, last_w+6))]
            print(f"Col x={x}: Top edge={top_edge}, Bottom edge={bottom_edge}")
            break
