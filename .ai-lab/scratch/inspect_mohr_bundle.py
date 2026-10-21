import re, struct

with open(r'c:\Users\HP\Desktop\GeminiUnity\Output\Studio\Assets\Fonts\Sample_vietnamese_font.bundle', 'rb') as f:
    data = f.read()

# Let's search for Mohr in bundle
pos = data.find(b'Vietnamese_Mohr')
print('Mohr found at:', pos)

# Find GradientScale, TextureWidth, TextureHeight, etc.
for prop in [b'_GradientScale', b'_TextureWidth', b'_TextureHeight', b'_ScaleRatioA', b'_ScaleRatioC']:
    matches = [m.start() for m in re.finditer(re.escape(prop) + rb'\x00', data)]
    print(f'{prop.decode()}: matches at {matches}')
    for m in matches[:3]:
        slen = struct.unpack('<I', data[m-4:m])[0]
        vpos = m + slen
        while vpos % 4 != 0: vpos += 1
        val = struct.unpack('<f', data[vpos:vpos+4])[0]
        print(f'   val = {val}')
