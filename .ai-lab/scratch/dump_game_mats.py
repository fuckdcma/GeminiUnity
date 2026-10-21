import UnityPy

env = UnityPy.load(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets')
for pid in (93, 103, 104, 107):
    for obj in env.objects:
        if obj.path_id == pid:
            d = obj.read()
            name = getattr(d, 'm_Name', '')
            kws = getattr(d, 'm_ShaderKeywords', '')
            print(f"=== Material PathID {pid}: '{name}', Keywords='{kws}' ===")
            if hasattr(d, 'm_SavedProperties'):
                sp = d.m_SavedProperties
                for p in sp.m_Floats:
                    if any(k in p[0] for k in ('Gradient', 'Underlay', 'Outline', 'Scale', 'Width', 'Height')):
                        print(f"   Float {p[0]}: {p[1]}")
                for p in sp.m_Colors:
                    if any(k in p[0] for k in ('Underlay', 'Outline', 'Face', 'Color')):
                        print(f"   Color {p[0]}: {p[1]}")
                for p in sp.m_TexEnvs:
                    print(f"   TexEnv {p[0]}: PathID={p[1].m_Texture.path_id if p[1].m_Texture else None}")
