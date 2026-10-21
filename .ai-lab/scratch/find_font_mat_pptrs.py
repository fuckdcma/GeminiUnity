import struct

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets', 'rb') as f:
    data = f.read()

for pos in [285518224, 285523288]:
    chunk = data[pos:pos+300]
    name_len = struct.unpack('<I', data[pos-4:pos])[0]
    name = data[pos:pos+name_len].decode('ascii')
    
    # In TMP_FontAsset serialized:
    # m_Name: string
    # m_Version: string ("1.1.0")
    # m_FaceInfo:
    #   m_FamilyName: string
    #   m_StyleName: string
    #   m_PointSize: float
    #   m_Scale: float
    #   ...
    # After m_FaceInfo is m_Material (PPtr: FileID int32, PathID int64)
    # Let's search for "Alphakind" or let's find the PPtr
    print(f"=== Font: '{name}' ===")
    # let's find where '1.1.0' is
    vpos = chunk.find(b'1.1.0')
    print(f"Version pos: {vpos}")
    # After version string, let's see FaceInfo:
    # FaceInfo has 2 strings (familyName, styleName), then floats/ints
    # Let's search in chunk for any PPtr
    # In resources.assets, material PathIDs are around 100-200.
    for i in range(vpos+8, len(chunk)-12):
        fid, pid = struct.unpack('<iq', chunk[i:i+12])
        if fid == 0 and 0 < pid < 1000:
            print(f"Found candidate PPtr at offset {i-vpos}: FileID={fid}, PathID={pid}")
