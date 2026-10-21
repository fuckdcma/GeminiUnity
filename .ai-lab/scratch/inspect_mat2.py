import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

offsets = [m.start() for m in re.finditer(rb'\x0d\x00\x00\x00_OutlineWidth\x00\x00\x00', data)]

for off in offsets:
    ow = struct.unpack('<f', data[off+20:off+24])[0]
    if abs(ow - 0.085) < 0.001:
        start = data.rfind(rb'Alphakind Atlas Material', 0, off)
        # Search for m_SavedProperties
        # In Unity Material: m_TexEnvs, m_Floats, m_Colors
        # Let's search m_Floats
        floats_pos = data.find(rb'm_Floats', start)
        # Let's parse all floats from start to start+3000
        # Float map entry: string (len + chars + padding), float (4 bytes)
        # Let's inspect the exact bytes around _UnderlayOffsetY
        for prop in ['_UnderlayOffsetX', '_UnderlayOffsetY', '_UnderlayColor', '_FaceColor', '_GradientScale', '_ScaleRatioA', '_ScaleRatioB', '_ScaleRatioC']:
            idx = data.find(prop.encode('ascii'), start, start+4000)
            if idx >= 0:
                # String length is 4 bytes before
                slen = struct.unpack('<I', data[idx-4:idx])[0]
                val_pos = idx + slen
                while val_pos % 4 != 0: val_pos += 1
                if 'Color' in prop:
                    vals = struct.unpack('<ffff', data[val_pos:val_pos+16])
                    print(f'{prop}: {vals}')
                else:
                    v = struct.unpack('<f', data[val_pos:val_pos+4])[0]
                    print(f'{prop}: {v}')
        break
