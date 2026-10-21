import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

offsets = [m.start() for m in re.finditer(rb'\x0d\x00\x00\x00_OutlineWidth\x00\x00\x00', data)]
print(f'Total materials with _OutlineWidth: {len(offsets)}')

for off in offsets:
    ow = struct.unpack('<f', data[off+20:off+24])[0]
    # find material name
    start = data.rfind(rb'Material', 0, off)
    if start >= 0:
        # scan backwards for length
        # Let's search backwards for string
        chunk = data[max(0, off-800):off]
        names = re.findall(rb'[\x20-\x7E]{4,}Material[\x20-\x7E]*', chunk)
        mat_name = names[-1].decode('ascii', errors='ignore') if names else 'unknown'
        
        # find keywords
        # m_ShaderKeywords string is before m_SavedProperties
        # search between start and off for keywords
        kw_m = re.findall(rb'[A-Z0-9_]{3,}_ON', chunk)
        kw_str = ','.join(set(k.decode('ascii') for k in kw_m))
        
        if ow > 0.05 or kw_str:
            print(f'Material: {mat_name:35} | Outline: {ow:6.3f} | Keywords: {kw_str}')
