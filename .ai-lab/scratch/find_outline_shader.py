with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

idx = 0
found = 0
while True:
    idx = data.find(b'_OutlineWidth', idx)
    if idx < 0 or found >= 5: break
    chunk = data[idx-100:idx+300]
    # check if shader
    if b'OUTLINE' in chunk or b'outline' in chunk or b'float' in chunk:
        print(f"Match at {idx}:")
        print(chunk)
        found += 1
    idx += 13
