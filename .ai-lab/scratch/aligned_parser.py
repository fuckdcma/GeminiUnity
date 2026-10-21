import struct

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

pos = 130780
chunk = data[pos:pos+4000]

def read_aligned_string_and_float(name):
    bname = name.encode('ascii')
    idx = chunk.find(bname)
    if idx < 0: return None
    # name starts after 4 byte length
    pos = idx + len(bname)
    while pos % 4 != 0: pos += 1
    val = struct.unpack('<f', chunk[pos:pos+4])[0]
    return val

def read_aligned_string_and_color(name):
    bname = name.encode('ascii')
    idx = chunk.find(bname)
    if idx < 0: return None
    pos = idx + len(bname)
    while pos % 4 != 0: pos += 1
    vals = struct.unpack('<ffff', chunk[pos:pos+16])
    return vals

for p in ['_OutlineWidth', '_OutlineSoftness', '_UnderlayOffsetX', '_UnderlayOffsetY', '_UnderlayDilate', '_UnderlaySoftness', '_FaceDilate', '_GradientScale', '_TextureWidth', '_TextureHeight', '_ScaleRatioA', '_ScaleRatioB', '_ScaleRatioC']:
    print(f"{p}: {read_aligned_string_and_float(p)}")

for c in ['_OutlineColor', '_UnderlayColor', '_FaceColor']:
    print(f"{c}: {read_aligned_string_and_color(c)}")
