import re

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Kitchen.Common.dll', 'rb') as f:
    data = f.read()

# Let's search for strings in ButtonElement
idx = data.find(b'ButtonElement')
# search for SetStyle, SetLabel, Label, Background, Border
for term in [b'SetStyle', b'SetLabel', b'Label', b'Border', b'Shadow', b'Font', b'Material', b'Color']:
    matches = [m.start() for m in re.finditer(term, data)]
    print(f"Term {term.decode()}: count={len(matches)}")
