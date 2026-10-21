import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

cap_start = 106044
chunk = data[cap_start:cap_start+2500]

for prop in [b'_GradientScale', b'_UnderlayOffsetX', b'_UnderlayOffsetY', b'_UnderlaySoftness', b'_UnderlayDilate', b'_OutlineWidth', b'_ScaleRatioA', b'_ScaleRatioB', b'_ScaleRatioC']:
    idx = chunk.find(prop)
    if idx >= 0:
        str_len = struct.unpack('<I', chunk[idx-4:idx])[0]
        val_off = idx + str_len
        while val_off % 4 != 0: val_off += 1
        val = struct.unpack('<f', chunk[val_off:val_off+4])[0]
        print(f"Float {prop.decode()}: {val}")

for prop in [b'_UnderlayColor', b'_OutlineColor', b'_FaceColor']:
    idx = chunk.find(prop)
    if idx >= 0:
        str_len = struct.unpack('<I', chunk[idx-4:idx])[0]
        val_off = idx + str_len
        while val_off % 4 != 0: val_off += 1
        r, g, b, a = struct.unpack('<ffff', chunk[val_off:val_off+16])
        print(f"Color {prop.decode()}: rgba({r}, {g}, {b}, {a})")
