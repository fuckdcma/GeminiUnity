import os, sys

managed = r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed'

# Let's inspect Unity.TextMeshPro.dll using pefile or raw metadata
# But wait, we can compile a tiny C# console app using dotnet!
# Let's see what dotnet can do.
print("Managed dir exists:", os.path.exists(managed))
