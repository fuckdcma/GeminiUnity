import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

# Also let's inspect the shader table in resources.assets
matches = [m.start() for m in re.finditer(rb'Alphakind Atlas Material', data)]
for i in [8, 13]:
    start = matches[i]
    chunk = data[start:start+3000]
    name_len = struct.unpack('<I', data[start-4:start])[0]
    kw_off = start + name_len
    while kw_off % 4 != 0: kw_off += 1
    # shader PPtr is 12 bytes: FileID (int), PathID (long)
    file_id, path_id = struct.unpack('<iq', data[kw_off:kw_off+12])
    kw_off += 12
    kw_len = struct.unpack('<I', data[kw_off:kw_off+4])[0]
    kw = data[kw_off+4:kw_off+4+kw_len].decode('ascii', errors='ignore')
    print(f'Instance [{i}]: Shader FileID={file_id}, PathID={path_id}, Keywords="{kw}"')
