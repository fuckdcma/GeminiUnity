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

print('CAB asset file size:', len(cab_data))

# Find all occurrences of names
for m in re.finditer(rb'[A-Za-z0-9_\- ]{3,50}', cab_data):
    s = m.group().decode('ascii', errors='ignore')
    if any(k in s for k in ['SDF', 'Material', 'Mohr', 'ARIAL', 'TIMES', 'TextMeshPro', 'Distance']):
        print(f"At {m.start()}: {s}")
