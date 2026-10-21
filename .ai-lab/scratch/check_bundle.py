import struct, re

bundle_path = r'C:\Users\HP\Desktop\GeminiUnity\Output\Studio\Assets\Fonts\Sample_vietnamese_font.bundle'
with open(bundle_path, 'rb') as f:
    data = f.read()

print(f"Bundle size: {len(data)}")

# Search for font names and material names in the bundle
names = re.findall(rb'([A-Za-z0-9_\-\.\s]{4,40}(?:SDF|Material|Font))', data)
for n in set(names):
    print("Found:", n.decode('ascii', errors='ignore'))

# Search for _GradientScale, _Underlay, _Outline in bundle
for prop in [b'_GradientScale', b'_UnderlayOffsetX', b'_UnderlaySoftness', b'_OutlineWidth', b'UNDERLAY_ON', b'OUTLINE_ON']:
    matches = [m.start() for m in re.finditer(prop, data)]
    print(f"Prop {prop.decode()}: count={len(matches)}")
