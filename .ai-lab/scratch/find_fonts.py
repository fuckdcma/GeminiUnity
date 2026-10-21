import struct, re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

# Search for "Reference - Main Menu Main Text"
idx = data.find(b'Reference - Main Menu Main Text')
print("Reference - Main Menu Main Text at", idx)
if idx >= 0:
    chunk = data[idx:idx+500]
    # In TMP_FontAsset:
    # m_Name: string
    # m_Version: string
    # m_FaceInfo: ...
    # m_Material: PPtr<Material>
    # let's search for "Material" or PPtr
    # Let's inspect bytes
    print(chunk[:100])

# Search for "Reference - Primary Text"
idx2 = data.find(b'Reference - Primary Text')
print("Reference - Primary Text at", idx2)

# Search for "Capriola-Regular SDF"
idx3 = data.find(b'Capriola-Regular SDF')
print("Capriola-Regular SDF at", idx3)
