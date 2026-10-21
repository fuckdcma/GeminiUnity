import struct, lz4.block

with open(r'c:\Users\HP\Desktop\GeminiUnity\Output\Studio\Assets\Fonts\Sample_vietnamese_font.bundle', 'rb') as f:
    data = f.read()

pos = 0
sig = data[pos:data.find(b'\0', pos)].decode()
pos += len(sig) + 1
ver = struct.unpack('>I', data[pos:pos+4])[0]
pos += 4
player_ver = data[pos:data.find(b'\0', pos)].decode()
pos += len(player_ver) + 1
engine_ver = data[pos:data.find(b'\0', pos)].decode()
pos += len(engine_ver) + 1

file_size, cblock_size, ublock_size, flags = struct.unpack('>QIII', data[pos:pos+20])
pos += 20

print(f"UnityFS: ver={ver}, engine={engine_ver}, file_size={file_size}, cblock_size={cblock_size}, ublock_size={ublock_size}, flags={flags:#x}")

comp_type = flags & 0x3F
if flags & 0x80: # header at end of file
    header_data = data[file_size - cblock_size:file_size]
else:
    header_data = data[pos:pos+cblock_size]
    pos += cblock_size

if comp_type == 3:
    header_uncomp = lz4.block.decompress(header_data, uncompressed_size=ublock_size)
else:
    header_uncomp = header_data

# parse header_uncomp
guid = header_uncomp[:16]
hpos = 16
num_blocks = struct.unpack('>I', header_uncomp[hpos:hpos+4])[0]
hpos += 4
blocks = []
for _ in range(num_blocks):
    ub_size, cb_size, b_flags = struct.unpack('>IIH', header_uncomp[hpos:hpos+10])
    hpos += 10
    blocks.append((ub_size, cb_size, b_flags))

num_nodes = struct.unpack('>I', header_uncomp[hpos:hpos+4])[0]
hpos += 4
nodes = []
for _ in range(num_nodes):
    offset, size, status = struct.unpack('>QQI', header_uncomp[hpos:hpos+20])
    hpos += 20
    name = header_uncomp[hpos:header_uncomp.find(b'\0', hpos)].decode('ascii')
    hpos += len(name) + 1
    nodes.append((offset, size, status, name))

print("Nodes in bundle:", nodes)

stream_pos = pos
decompressed_stream = bytearray()
for ub_size, cb_size, b_flags in blocks:
    b_comp = b_flags & 0x3F
    b_data = data[stream_pos:stream_pos+cb_size]
    stream_pos += cb_size
    if b_comp == 3:
        decompressed_stream.extend(lz4.block.decompress(b_data, uncompressed_size=ub_size))
    else:
        decompressed_stream.extend(b_data)

print(f"Total decompressed stream size: {len(decompressed_stream)} bytes")

import re
for m in re.finditer(rb'([A-Za-z0-9_-]+) Atlas Material', decompressed_stream):
    mat_name = m.group().decode('ascii')
    start = m.start()
    print(f"\n--- Material: {mat_name} at {start} ---")
    chunk = decompressed_stream[start:start+4000]
    for prop in ['_OutlineWidth', '_OutlineSoftness', '_UnderlayOffsetX', '_UnderlayOffsetY', '_UnderlayDilate', '_UnderlaySoftness', '_FaceDilate', '_GradientScale', '_ScaleRatioA', '_ScaleRatioB', '_ScaleRatioC', '_TextureWidth', '_TextureHeight']:
        pattern = re.compile(re.escape(prop.encode('ascii')) + rb'\x00+')
        pm = pattern.search(chunk)
        if pm:
            ppos = pm.end()
            val = struct.unpack('<f', chunk[ppos:ppos+4])[0]
            print(f"   {prop} = {val}")
    for cprop in ['_OutlineColor', '_UnderlayColor', '_FaceColor']:
        pattern = re.compile(re.escape(cprop.encode('ascii')) + rb'\x00+')
        pm = pattern.search(chunk)
        if pm:
            ppos = pm.end()
            vals = struct.unpack('<ffff', chunk[ppos:ppos+16])
            print(f"   {cprop} = {vals}")
