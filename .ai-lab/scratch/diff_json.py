import json

with open(r'c:\Users\HP\Desktop\CourierPrime GOC.json', 'r', encoding='utf-8') as f:
    goc = json.load(f)

with open(r'c:\Users\HP\Desktop\Game Việt Hoá\IRON NEST Heavy Turret Simulator Build 24881463\TEXT\FONT\MONO\CourierPrime thay the.json', 'r', encoding='utf-8') as f:
    thay_the = json.load(f)

print("m_Material goc:", goc.get('m_Material'))
print("m_Material thay:", thay_the.get('m_Material'))
print("m_AtlasTextures goc:", goc.get('m_AtlasTextures'))
print("m_AtlasTextures thay:", thay_the.get('m_AtlasTextures'))
print("m_AtlasWidth goc:", goc.get('m_AtlasWidth'), "thay:", thay_the.get('m_AtlasWidth'))
print("m_AtlasHeight goc:", goc.get('m_AtlasHeight'), "thay:", thay_the.get('m_AtlasHeight'))
print("m_AtlasPadding goc:", goc.get('m_AtlasPadding'), "thay:", thay_the.get('m_AtlasPadding'))
