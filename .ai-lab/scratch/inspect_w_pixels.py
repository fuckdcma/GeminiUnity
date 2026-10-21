from PIL import Image
import numpy as np

im = Image.open('crop_wiki.png')
arr = np.array(im)
h, w, c = arr.shape

# Let's find where 'W' is
# 'W' is near the center, text is white
# Let's find coordinates of white pixels (R > 240, G > 240, B > 240)
white_pixels = np.argwhere((arr[:, :, 0] > 240) & (arr[:, :, 1] > 240) & (arr[:, :, 2] > 240))
# Let's find 'W' in 'Wiki'
# 'Open Wiki' is in the top half
top_white = [p for p in white_pixels if p[0] < h * 0.5]
# 'W' is around x = 110 to 140
w_pixels = [p for p in top_white if 100 <= p[1] <= 140]
if w_pixels:
    min_y = min(p[0] for p in w_pixels)
    max_y = max(p[0] for p in w_pixels)
    min_x = min(p[1] for p in w_pixels)
    max_x = max(p[1] for p in w_pixels)
    print(f"'W' box: y=[{min_y}, {max_y}], x=[{min_x}, {max_x}]")
    
    # Let's inspect a horizontal slice across 'W' at middle y
    mid_y = (min_y + max_y) // 2
    print(f"Horizontal profile at y={mid_y}:")
    for x in range(min_x - 10, max_x + 15):
        rgb = arr[mid_y, x][:3]
        print(f"x={x}: RGB=({rgb[0]:3d}, {rgb[1]:3d}, {rgb[2]:3d})")
        
    # Let's inspect a vertical slice down from 'W' into shadow
    # Find a column in 'W'
    test_x = min_x + 5
    print(f"\nVertical profile at x={test_x}:")
    for y in range(max_y - 5, max_y + 20):
        rgb = arr[y, test_x][:3]
        print(f"y={y}: RGB=({rgb[0]:3d}, {rgb[1]:3d}, {rgb[2]:3d})")
