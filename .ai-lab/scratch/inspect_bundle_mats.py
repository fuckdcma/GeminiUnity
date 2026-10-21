import struct, lz4.block

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
    
    num_nodes = struct.unpack('>I', meta_data[pos:pos+4])[0]
    pos += 4
    nodes = []
    for _ in range(num_nodes):
        offset, size, status = struct.unpack('>QQI', meta_data[pos:pos+20])
        pos += 20
        name = meta_data[pos:meta_data.find(b'\0', pos)].decode('ascii')
        pos += len(name) + 1
        nodes.append((offset, size, status, name))
    print('Nodes:', nodes)
    
    f.seek(608)
    decompressed = bytearray()
    for ub_size, cb_size, b_flags in blocks:
        block_bytes = f.read(cb_size)
        comp_type = b_flags & 0x3F
        if comp_type == 3:
            decompressed.extend(lz4.block.decompress(block_bytes, uncompressed_size=ub_size))
        elif comp_type == 0:
            decompressed.extend(block_bytes)
    print('Total decompressed size:', len(decompressed))

import re
for m in re.finditer(rb'([A-Za-z0-9_-]+ Atlas Material)', decompressed):
    mat_name = m.group().decode('ascii')
    start = m.start()
    print(f"\n================ Material: {mat_name} at {start} ================")
    chunk = decompressed[start:start+4000]
    
    name_len = struct.unpack('<I', decompressed[start-4:start])[0]
    kw_off = start + name_len
    while kw_off % 4 != 0: kw_off += 1
    file_id, path_id = struct.unpack('<iq', decompressed[kw_off:kw_off+12])
    kw_off += 12
    kw_len = struct.unpack('<I', decompressed[kw_off:kw_off+4])[0]
    kw = decompressed[kw_off+4:kw_off+4+kw_len].decode('ascii', errors='ignore') if kw_len < 200 else '??'
    print(f"Shader PPtr: ({file_id}, {path_id}), Keywords: \"{kw}\"")
    
    for prop in ['_OutlineWidth', '_OutlineSoftness', '_UnderlayOffsetX', '_UnderlayOffsetY', '_UnderlayDilate', '_UnderlaySoftness', '_FaceDilate', '_GradientScale', '_ScaleRatioA', '_ScaleRatioB', '_ScaleRatioC', '_TextureWidth', '_TextureHeight']:
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
