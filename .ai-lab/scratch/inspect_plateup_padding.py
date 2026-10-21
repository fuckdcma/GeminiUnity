import UnityPy

env = UnityPy.load(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets')
for obj in env.objects:
    if obj.path_id in (1415, 1416, 1419, 1360):
        d = obj.read()
        name = getattr(d, 'm_Name', '')
        for attr in ['m_AtlasPadding', 'm_AtlasWidth', 'm_AtlasHeight', 'm_AtlasRenderMode', 'm_Material']:
            if hasattr(d, attr):
                val = getattr(d, attr)
                print(f"[{name}] {attr} = {val}")
