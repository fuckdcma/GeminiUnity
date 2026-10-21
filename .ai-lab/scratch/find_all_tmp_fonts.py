import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

for m in re.finditer(rb'Reference - [A-Za-z0-9 ]+', data):
    name = m.group(0).decode('ascii')
    if 'Atlas' not in name:
        pos = m.start()
        print(f"Candidate font: '{name}' at {pos}")
        chunk = data[pos:pos+400]
        # let's see if it has m_Version 1.1.0 or similar
        if b'1.1.0' in chunk or b'm_FaceInfo' in chunk:
            print(f"   -> Confirmed TMP_FontAsset: '{name}'")
