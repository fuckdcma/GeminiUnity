import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

matches = [m.start() for m in re.finditer(rb'Alphakind Atlas Material', data)]
for i, start in enumerate(matches):
    chunk = data[start:start+2500]
    name_len = struct.unpack('<I', data[start-4:start])[0]
    if name_len != len('Alphakind Atlas Material'): continue
    kw_off = start + name_len
    while kw_off % 4 != 0: kw_off += 1
    file_id, path_id = struct.unpack('<iq', data[kw_off:kw_off+12])
    kw_off += 12
    kw_len = struct.unpack('<I', data[kw_off:kw_off+4])[0]
    kw = data[kw_off+4:kw_off+4+kw_len].decode('ascii', errors='ignore') if kw_len < 100 else 'INVALID'
    
    # Check UnderlaySoftness in chunk
    s_idx = chunk.find(b'_UnderlaySoftness')
    softness = -1
    if s_idx >= 0:
        str_len = struct.unpack('<I', chunk[s_idx-4:s_idx])[0]
        val_off = s_idx + str_len
        while val_off % 4 != 0: val_off += 1
        softness = struct.unpack('<f', chunk[val_off:val_off+4])[0]
    print(f"[{i}] kw='{kw}', softness={softness}")
