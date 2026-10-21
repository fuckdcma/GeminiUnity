import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

matches = [m.start() for m in re.finditer(rb'Alphakind Atlas Material', data)]
for i, start in enumerate(matches):
    chunk = data[start:start+2500]
    for prop in [b'_UnderlayOffsetX', b'_UnderlayOffsetY', b'_UnderlayColor']:
        idx = chunk.find(prop)
        if idx >= 0:
            str_len = struct.unpack('<I', chunk[idx-4:idx])[0]
            val_off = idx + str_len
            while val_off % 4 != 0: val_off += 1
            if prop == b'_UnderlayColor':
                r, g, b, a = struct.unpack('<ffff', chunk[val_off:val_off+16])
                print(f"[{i}] {prop.decode()}: rgba({r}, {g}, {b}, {a})")
            else:
                val = struct.unpack('<f', chunk[val_off:val_off+4])[0]
                print(f"[{i}] {prop.decode()}: {val}")
