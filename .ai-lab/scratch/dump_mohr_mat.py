import struct, lz4.block, re

with open(r'c:\Users\HP\Desktop\GeminiUnity\Output\Studio\Assets\Fonts\Sample_vietnamese_font.bundle', 'rb') as f:
    f.seek(64)
    meta = f.read(538)
    meta_data = lz4.block.decompress(meta, uncompressed_size=1123)
    
    block_count = struct.unpack('>I', meta_data[16:20])[0]
    pos = 20
    blocks = []
    for _ in range(block_count):
        ub_size, cb_size, b_flags = struct.unpack('>IIH', meta_data[pos:pos+10])
        blocks.append((ub_size, cb_size, b_flags))
        pos += 10
    
    f.seek(608)
    cab_data = bytearray()
    for ub_size, cb_size, b_flags in blocks[:3]:
        block_bytes = f.read(cb_size)
        cab_data.extend(lz4.block.decompress(block_bytes, uncompressed_size=ub_size))
        if len(cab_data) >= 222600:
            break
    cab_data = cab_data[:222600]

start = 166620
chunk = cab_data[start:start+4000]

# Material name:
name_len = struct.unpack('<I', cab_data[start-4:start])[0]
name = cab_data[start:start+name_len].decode('ascii')
print('Material name:', name)

kw_off = start + name_len
while kw_off % 4 != 0: kw_off += 1
file_id, path_id = struct.unpack('<iq', cab_data[kw_off:kw_off+12])
kw_off += 12
kw_len = struct.unpack('<I', cab_data[kw_off:kw_off+4])[0]
kw = cab_data[kw_off+4:kw_off+4+kw_len].decode('ascii', errors='ignore')
print(f"Shader PPtr: ({file_id}, {path_id}), Keywords: \"{kw}\"")

# Parse all properties
for prop in ['_OutlineWidth', '_OutlineSoftness', '_UnderlayOffsetX', '_UnderlayOffsetY', '_UnderlayDilate', '_UnderlaySoftness', '_FaceDilate', '_GradientScale', '_ScaleRatioA', '_ScaleRatioB', '_ScaleRatioC', '_TextureWidth', '_TextureHeight', '_WeightNormal', '_WeightBold']:
    idx = chunk.find(prop.encode('ascii') + b'\x00')
    if idx >= 0:
        slen = struct.unpack('<I', chunk[idx-4:idx])[0]
        vpos = idx + slen
        while vpos % 4 != 0: vpos += 1
        val = struct.unpack('<f', chunk[vpos:vpos+4])[0]
        print(f"   {prop}: {val}")

for cprop in ['_OutlineColor', '_UnderlayColor', '_FaceColor']:
    idx = chunk.find(cprop.encode('ascii') + b'\x00')
    if idx >= 0:
        slen = struct.unpack('<I', chunk[idx-4:idx])[0]
        vpos = idx + slen
        while vpos % 4 != 0: vpos += 1
        vals = struct.unpack('<ffff', chunk[vpos:vpos+16])
        print(f"   {cprop}: {vals}")
