import UnityPy

env = UnityPy.load(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets')
for obj in env.objects:
    if obj.path_id in (1415, 1416, 1419, 1432, 1360, 1361):
        raw = obj.get_raw_data()
        name_len = int.from_bytes(raw[28:32], 'little')
        name = raw[32:32+name_len].decode('ascii', errors='ignore')
        # In TMP_FontAsset:
        # m_GameObject (12)
        # m_Enabled (4)
        # m_Script (12)
        # m_Name (4 + name_len + pad)
        # m_Version (4 + ver_len + pad)
        off = 32 + name_len
        while off % 4 != 0: off += 1
        ver_len = int.from_bytes(raw[off:off+4], 'little')
        off += 4 + ver_len
        while off % 4 != 0: off += 1
        # m_FaceInfo is next (PointSize, scale, lines, etc.)
        # After FaceInfo (about 70-80 bytes) is m_Material (PPtr: 12 bytes: int32 file_id, int64 path_id)
        # Let's search for FileID=0 and PathID in range 90..140
        print(f"[{name}] PathID={obj.path_id}:")
        for i in range(off, min(off+200, len(raw)-12), 4):
            fid = int.from_bytes(raw[i:i+4], 'little')
            pid = int.from_bytes(raw[i+4:i+12], 'little', signed=True)
            if fid == 0 and 90 <= pid <= 140:
                print(f"   Found Material PPtr at offset {i}: FileID={fid}, PathID={pid}")
                break
