import struct

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

# In Unity assets, let's find the object info table to find PathID 279 and 280
# Or let's search for materials that match PathID 279 and 280
# Let's inspect the Match #s from our previous material search
# Earlier we found:
# Match #7 at 129020
# Match #8 at 130780
# Match #11 at 136252
# Match #12 at 138012
# Match #13 at 139772
# Let's check which Match corresponds to PathID 279 and PathID 280!

# In the object table of resources.assets:
# Header is 20 bytes (or 32 bytes for v22)
# Let's find table offset
meta_size, file_size, version, data_offset, endian = struct.unpack('>IIIII', data[:20])
print(f"data_offset: {data_offset}, meta_size: {meta_size}")
# In Unity 22: header has 12 byte padding or int64 offsets
# Let's parse the table:
pos = 20
if version >= 22:
    meta_size, file_size, data_offset = struct.unpack('>QQQ', data[20:44])
    pos = 48 # start of metadata
print(f"v22: meta_size={meta_size}, data_offset={data_offset}, pos={pos}")

# Unity version string
vlen = data.find(b'\x00', pos)
unity_version = data[pos:vlen].decode('ascii')
print("Unity version:", unity_version)
pos = vlen + 1
# target platform (int32)
target_platform = struct.unpack('<I', data[pos:pos+4])[0]
pos += 4
# enable type tree (bool)
enable_type_tree = data[pos]
pos += 1
# type tree count
type_count = struct.unpack('<I', data[pos:pos+4])[0]
pos += 4
print(f"Type count: {type_count}")

# skip type trees... or let's search object table directly:
# In object table:
# object count (int32)
# each object:
#   if version >= 14:
#     alignment
#     PathID: int64
#     Offset: int64 (or int32)
#     Size: int32
#     TypeID: int32
