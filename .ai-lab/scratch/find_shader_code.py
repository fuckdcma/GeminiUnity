import re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

# Search for TextMeshPro Distance Field shader code
# Look for 'underlay' or 'UNDERLAY_ON' in shader HLSL
idx = data.find(b'UNDERLAY_ON')
while idx >= 0:
    chunk = data[idx-200:idx+800]
    if b'half' in chunk or b'float' in chunk or b'tex2D' in chunk or b'bias' in chunk:
        print("Found shader code snippet near", idx)
        print(chunk[:500].decode('ascii', errors='ignore'))
        break
    idx = data.find(b'UNDERLAY_ON', idx+1)
