import struct

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

# Unity serialized file header:
# Big-endian:
# uint32 metadata_size
# uint32 file_size
# uint32 version
# uint32 data_offset
# byte endianess
# byte reserved[3]
# if version >= 22:
# uint32 metadata_size
# uint64 file_size
# uint64 data_offset
# int64 unknown
metadata_size, file_size, version, data_offset = struct.unpack('>IIII', data[:16])
if version >= 22:
    # Large files:
    metadata_size, file_size, data_offset = struct.unpack('>IQQ', data[20:40])
    print(f'v22: meta_size={metadata_size}, file_size={file_size}, data_offset={data_offset}')
    # Metadata starts at 48 or after
    pos = 48
    # unity version string
    ver_str = data[pos:data.find(b'\x00', pos)].decode('ascii')
    print('Unity Version:', ver_str)
    pos += len(ver_str) + 1
    # target platform
    platform = struct.unpack('<I', data[pos:pos+4])[0]
    pos += 4
    # enable type tree
    enable_type_tree = data[pos]
    pos += 1
    print('Enable Type Tree:', enable_type_tree)
    # type count
    type_count = struct.unpack('<I', data[pos:pos+4])[0]
    pos += 4
    print('Type count:', type_count)
    # skip types
    for _ in range(type_count):
        class_id = struct.unpack('<i', data[pos:pos+4])[0]
        pos += 4
        is_stripped = data[pos]
        pos += 1
        script_type_index = struct.unpack('<h', data[pos:pos+2])[0]
        pos += 2
        if class_id == 114:
            pos += 16 # script hash
        pos += 16 # type hash
        if enable_type_tree:
            # type tree is complex, let's find object count
            pass
    # If type tree was enabled, pos is farther.
