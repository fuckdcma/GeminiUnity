import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

# Let's inspect match 8
m8 = [m.start() for m in re.finditer(rb'Alphakind Atlas Material', data)][8]
chunk = data[m8:m8+2500]

# Unity Material m_SavedProperties format:
# m_TexEnvs: vector of pair<string, TexEnv>
# m_Floats: vector of pair<string, float>
# m_Colors: vector of pair<string, ColorRGBA>

# Let's search for float props:
for prop in [b'_GradientScale', b'_UnderlayOffsetX', b'_UnderlayOffsetY', b'_UnderlaySoftness', b'_UnderlayDilate', b'_OutlineWidth', b'_ScaleRatioA', b'_ScaleRatioB', b'_ScaleRatioC']:
    idx = chunk.find(prop)
    if idx >= 0:
        # string in unity has 4-byte length prefix
        # after prop string and padding, there's float value (4 bytes)
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
