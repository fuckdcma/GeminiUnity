import struct

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

for pos in [191788036, 191788212]:
    chunk = data[pos:pos+500]
    # let's look at strings and PPtrs
    # In TMP_FontAsset:
    # m_Name is at start
    name_len = struct.unpack('<I', data[pos-4:pos])[0]
    name = data[pos:pos+name_len].decode('ascii')
    print(f"Font Asset: '{name}' at {pos}")
    # let's find m_Material PPtr
    # m_Material is after m_FaceInfo
    # m_FaceInfo starts after version string
    # Let's search for "m_Material" or let's inspect the chunk
    print(chunk[:100])
