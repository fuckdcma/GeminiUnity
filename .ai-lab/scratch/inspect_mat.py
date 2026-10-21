import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

offsets = [m.start() for m in re.finditer(rb'\x0d\x00\x00\x00_OutlineWidth\x00\x00\x00', data)]

for off in offsets:
    ow = struct.unpack('<f', data[off+20:off+24])[0]
    if abs(ow - 0.085) < 0.001:
        start = data.rfind(rb'Alphakind Atlas Material', 0, off)
        name_len = struct.unpack('<I', data[start-4:start])[0]
        kw_off = start + name_len
        while kw_off % 4 != 0: kw_off += 1
        kw_off += 12 # skip shader PPtr
        kw_len = struct.unpack('<I', data[kw_off:kw_off+4])[0]
        kw = data[kw_off+4:kw_off+4+kw_len].decode('ascii', errors='ignore')
        print(f'*** Found PlateUp Menu Material: Keywords="{kw}" ***')
        
        chunk = data[start:start+4000]
        for prop in ['_OutlineWidth', '_OutlineSoftness', '_UnderlayOffsetX', '_UnderlayOffsetY', '_UnderlayDilate', '_UnderlaySoftness', '_FaceDilate', '_GradientScale', '_ScaleRatioA', '_ScaleRatioB', '_ScaleRatioC']:
            pattern = re.compile(re.escape(prop.encode('ascii')) + rb'\x00+')
            m = pattern.search(chunk)
            if m:
                pos = m.end()
                val = struct.unpack('<f', chunk[pos:pos+4])[0]
                print(f'   {prop} = {val}')
        for cprop in ['_OutlineColor', '_UnderlayColor', '_FaceColor']:
            pattern = re.compile(re.escape(cprop.encode('ascii')) + rb'\x00+')
            m = pattern.search(chunk)
            if m:
                pos = m.end()
                vals = struct.unpack('<ffff', chunk[pos:pos+16])
                print(f'   {cprop} = {vals}')
        break
