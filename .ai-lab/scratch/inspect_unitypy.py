bundle_path = r'C:\Users\HP\Desktop\GeminiUnity\Output\Studio\Assets\Fonts\Sample_vietnamese_font.bundle'
import UnityPy

env = UnityPy.load(bundle_path)
for obj in env.objects:
    if obj.type.name in ('Material', 'MonoBehaviour'):
        d = obj.read()
        name = getattr(d, 'name', '') or getattr(d, 'm_Name', '')
        print(f"Object: {name} ({obj.type.name})")
        if obj.type.name == 'Material':
            print("  m_ShaderKeywords:", getattr(d, 'm_ShaderKeywords', ''))
            if hasattr(d, 'm_SavedProperties'):
                sp = d.m_SavedProperties
                for p in sp.m_Floats:
                    if any(k in p[0] for k in ('Gradient', 'Underlay', 'Outline', 'Scale')):
                        print(f"    Float {p[0]}: {p[1]}")
                for p in sp.m_Colors:
                    if any(k in p[0] for k in ('Underlay', 'Outline', 'Face')):
                        print(f"    Color {p[0]}: {p[1]}")
