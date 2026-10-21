import UnityPy

bundle_path = r'C:\Users\HP\Desktop\GeminiUnity\Output\Studio\Assets\Fonts\Sample_vietnamese_font.bundle'
env = UnityPy.load(bundle_path)
for obj in env.objects:
    if obj.type.name == 'MonoBehaviour':
        d = obj.read()
        name = getattr(d, 'm_Name', '')
        # print all attributes
        for attr in ['m_AtlasPadding', 'm_AtlasWidth', 'm_AtlasHeight', 'm_AtlasRenderMode', 'm_Material']:
            if hasattr(d, attr):
                val = getattr(d, attr)
                print(f"[{name}] {attr} = {val}")
