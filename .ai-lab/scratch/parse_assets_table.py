import struct

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

# Asset header
meta_size, file_size, version, data_offset, endian = struct.unpack('>IIIII', data[:20])
print(f"Header: version={version}, data_offset={data_offset}, file_size={file_size}")

# Unity 2020/2021 format
# Let's search for "Alphakind" and "Reference - Main Menu"
for name in [b'Reference - Main Menu Main Text', b'Reference - Primary Text', b'Capriola-Regular SDF']:
    pos = data.find(name)
    if pos >= 0:
        print(f"Found font asset '{name.decode()}' at {pos}")

for mat_name in [b'Alphakind Atlas Material']:
    pos = 0
    while True:
        pos = data.find(mat_name, pos)
        if pos < 0: break
        print(f"Found material '{mat_name.decode()}' at {pos}")
        pos += len(mat_name)
