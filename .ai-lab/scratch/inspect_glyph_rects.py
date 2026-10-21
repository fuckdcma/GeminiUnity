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
    
    f.seek(608)
    cab_data = bytearray()
    for ub_size, cb_size, b_flags in blocks[:3]:
        block_bytes = f.read(cb_size)
        cab_data.extend(lz4.block.decompress(block_bytes, uncompressed_size=ub_size))
        if len(cab_data) >= 222600: break
    cab_data = cab_data[:222600]

# In CAB, find the TMP_FontAsset for Mohr
idx = cab_data.find(b'Vietnamese_Mohr_Rounded_Black_SDF')
print('Mohr font asset at:', idx)

# Let's search for m_AtlasPadding or padding
# In Unity TextMeshPro TMP_FontAsset:
# m_AtlasPadding is int32
# m_AtlasWidth, m_AtlasHeight
for prop in [b'm_AtlasPadding', b'm_Padding', b'm_AtlasWidth', b'm_AtlasHeight']:
    p = cab_data.find(prop)
    print(f'{prop.decode()}: at {p}')

# Also let's inspect the atlas texture dimensions and glyph rects
# Let's search for glyphRect
