import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

# Let's find Match #8 or any Material that has UNDERLAY_ON
pos = 130780 # from our previous scan
chunk = data[pos:pos+4000]

# In Material, after keywords string, there is m_LightmapFlags (uint), m_EnableInstancingVariants (bool), etc.
# Let's find "m_Floats" or let's search for the float map:
# A map of string -> float starts with size (uint32)
# each element is string (len uint32 + text + padding) + float32.

# Let's locate _UnderlayOffsetX
idx = chunk.find(b'_UnderlayOffsetX')
if idx >= 0:
    print("Found _UnderlayOffsetX at relative offset:", idx)
    # let's look at the bytes around it
    print("Bytes:", chunk[idx-8:idx+32])
    # The string is uint32 length (16), '_UnderlayOffsetX', then float32!
    # len = 16 (0x10, 0x00, 0x00, 0x00)
    # string = 16 bytes: _UnderlayOffsetX
    # float = 4 bytes!
    float_val = struct.unpack('<f', chunk[idx+16:idx+20])[0]
    print("Parsed _UnderlayOffsetX float:", float_val)

for p in [b'_UnderlayOffsetX', b'_UnderlayOffsetY', b'_UnderlayDilate', b'_UnderlaySoftness', b'_OutlineWidth', b'_FaceDilate', b'_GradientScale', b'_TextureWidth', b'_TextureHeight']:
    idx = chunk.find(p)
    if idx >= 0:
        l = len(p)
        val = struct.unpack('<f', chunk[idx+l:idx+l+4])[0]
        print(f"{p.decode()}: {val}")

for p in [b'_OutlineColor', b'_UnderlayColor', b'_FaceColor']:
    idx = chunk.find(p)
    if idx >= 0:
        l = len(p)
        vals = struct.unpack('<ffff', chunk[idx+l:idx+l+16])
        print(f"{p.decode()}: {vals}")
