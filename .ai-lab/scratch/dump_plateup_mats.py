import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

matches = [m.start() for m in re.finditer(rb'Alphakind Atlas Material', data)]
for idx, start in enumerate(matches):
    chunk = data[start:start+4000]
    name_len = struct.unpack('<I', data[start-4:start])[0]
    kw_off = start + name_len
    while kw_off % 4 != 0: kw_off += 1
    file_id, path_id = struct.unpack('<iq', data[kw_off:kw_off+12])
    kw_off += 12
    kw_len = struct.unpack('<I', data[kw_off:kw_off+4])[0]
    kw = data[kw_off+4:kw_off+4+kw_len].decode('ascii', errors='ignore')
    if kw:
        print(f'Match #{idx} at {start}: Keywords="{kw}"')
        for prop in ['_OutlineWidth', '_OutlineSoftness', '_UnderlayOffsetX', '_UnderlayOffsetY', '_UnderlayDilate', '_UnderlaySoftness', '_FaceDilate', '_GradientScale', '_TextureWidth', '_TextureHeight', '_ScaleRatioA']:
            m = re.search(re.escape(prop.encode('ascii')) + rb'\x00+', chunk)
            if m:
                pos = m.end()
                while pos % 4 != 0: pos += 1
                val = struct.unpack('<f', chunk[pos:pos+4])[0]
                print(f'   {prop} = {val}')
        for cprop in ['_OutlineColor', '_UnderlayColor', '_FaceColor']:
            m = re.search(re.escape(cprop.encode('ascii')) + rb'\x00+', chunk)
            if m:
                pos = m.end()
                while pos % 4 != 0: pos += 1
                vals = struct.unpack('<ffff', chunk[pos:pos+16])
                print(f'   {cprop} = {vals}')
