with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Unity.TextMeshPro.dll', 'rb') as f:
    data = f.read()

import re
matches = re.findall(rb'ID_[A-Za-z0-9_]+', data)
print("ID fields found in TextMeshPro.dll:")
for m in sorted(set(matches)):
    print("  ", m.decode())
