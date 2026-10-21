import struct, lz4.block

with open(r'c:\Users\HP\Desktop\GeminiUnity\Output\Studio\Assets\Fonts\Sample_vietnamese_font.bundle', 'rb') as f:
    signature = f.read(7)
    version = struct.unpack('>I', f.read(4))[0]
    unity_version = b''
    while True:
        c = f.read(1)
        if c == b'\x00': break
        unity_version += c
    generator_version = b''
    while True:
        c = f.read(1)
        if c == b'\x00': break
        generator_version += c
    file_size = struct.unpack('>Q', f.read(8))[0]
    c_meta_size = struct.unpack('>I', f.read(4))[0]
    u_meta_size = struct.unpack('>I', f.read(4))[0]
    flags = struct.unpack('>I', f.read(4))[0]
    
    meta_data = f.read(c_meta_size)
    if flags & 0x3F == 3: # LZ4
        meta_data = lz4.block.decompress(meta_data, uncompressed_size=u_meta_size)
    
    # parse blocks
    guid = meta_data[:16]
    block_count = struct.unpack('>I', meta_data[16:20])[0]
    pos = 20
    blocks = []
    for _ in range(block_count):
        u_size, c_size, b_flags = struct.unpack('>IIH', meta_data[pos:pos+10])
        blocks.append((u_size, c_size, b_flags))
        pos += 10
    
    # decompress blocks
    decompressed_data = bytearray()
    for u_size, c_size, b_flags in blocks:
        block_bytes = f.read(c_size)
        comp_type = b_flags & 0x3F
        if comp_type == 3: # LZ4
            decompressed_data.extend(lz4.block.decompress(block_bytes, uncompressed_size=u_size))
        elif comp_type == 0:
            decompressed_data.extend(block_bytes)

print('Decompressed size:', len(decompressed_data))

# Search for all strings matching Material or Font in decompressed_data
import re
text = decompressed_data.decode('ascii', errors='ignore')
matches = re.findall(r'[A-Za-z0-9_\- ]+(?:Material|SDF)[A-Za-z0-9_\- ]*', text)
print('Assets in bundle:', set(matches))

# Find properties of Vietnamese_Mohr_Rounded_Black_SDF
for prop in ['_OutlineWidth', '_UnderlayOffsetX', '_UnderlayOffsetY', '_UnderlayDilate', '_UnderlaySoftness', '_GradientScale', '_ScaleRatioA', '_ScaleRatioB', '_ScaleRatioC']:
    idx = decompressed_data.find(prop.encode('ascii') + b'\x00')
    if idx >= 0:
        val = struct.unpack('<f', decompressed_data[idx+len(prop)+1+2:idx+len(prop)+1+6])[0]
        print(f'{prop}: {val}')
