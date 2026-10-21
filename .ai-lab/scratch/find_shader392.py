import struct

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

# Let's parse the SerializedFile header
# In Unity serialized file:
# Metadata size, file size, version, target platform...
# Let's inspect the asset header
header_size, file_size, version, data_offset = struct.unpack('>IIII', data[:16])
print(f'Header: metadata_size={header_size}, file_size={file_size}, version={version}, data_offset={data_offset}')

# Unity 5.5+ header
endianess = data[16]
print('Endianess:', endianess)

# Let's search for 392 in object table or find the shader name
# In serialized files, objects have PathID.
# Or let's search for Shader objects in the file
import re
for m in re.finditer(rb'TextMeshPro', data):
    start = m.start()
    print('TextMeshPro ref at:', start, data[start-10:start+40])
    if start > 200000: break
