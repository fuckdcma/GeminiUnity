import UnityPy

env = UnityPy.load(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets')
for pid in (1415, 1416, 1419, 1360):
    for obj in env.objects:
        if obj.path_id == pid:
            raw = obj.get_raw_data()
            name_len = int.from_bytes(raw[28:32], 'little')
            name = raw[32:32+name_len].decode('ascii', errors='ignore')
            # Look at offset 68 (or nearby):
            # In TMP_FontAsset, m_Material is right after m_FaceInfo
            for i in range(40, 120, 4):
                fid = int.from_bytes(raw[i:i+4], 'little')
                mat_pid = int.from_bytes(raw[i+4:i+12], 'little', signed=True)
                if fid == 0 and 90 <= mat_pid <= 140:
                    print(f"Font '{name}' (PathID {pid}) -> Material PathID {mat_pid} at offset {i}")
                    break
