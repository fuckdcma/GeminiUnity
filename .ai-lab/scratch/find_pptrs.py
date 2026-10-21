import UnityPy

env = UnityPy.load(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets')
for obj in env.objects:
    if obj.path_id == 1415:
        raw = obj.get_raw_data()
        print(f"PathID 1415 length: {len(raw)}")
        # Look for PPtrs: FileID (int32), PathID (int64)
        for i in range(0, len(raw)-12, 4):
            fid = int.from_bytes(raw[i:i+4], 'little')
            pid = int.from_bytes(raw[i+4:i+12], 'little', signed=True)
            if fid == 0 and 0 < pid < 10000:
                print(f"Offset {i}: FileID={fid}, PathID={pid}")
