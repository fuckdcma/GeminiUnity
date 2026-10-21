import UnityPy

bundle_path = r'C:\Users\HP\Desktop\GeminiUnity\Output\Studio\Assets\Fonts\Sample_vietnamese_font.bundle'
env = UnityPy.load(bundle_path)
for obj in env.objects:
    if obj.type.name == 'Texture2D':
        d = obj.read()
        print(f"Texture2D '{getattr(d, 'm_Name', '')}': {d.m_Width}x{d.m_Height}, format={d.m_TextureFormat}")
    if obj.type.name == 'Material':
        d = obj.read()
        print(f"Material '{getattr(d, 'm_Name', '')}': Shader={d.m_Shader.path_id if d.m_Shader else None}")
