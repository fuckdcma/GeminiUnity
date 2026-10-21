# Let's inspect TMPro.ShaderUtilities.UpdateShaderRatios in Unity.TextMeshPro.dll
import struct

with open(r'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Unity.TextMeshPro.dll', 'rb') as f:
    data = f.read()

# Let's see if there are any floats in the assembly near ScaleRatio
# In TMPro source code:
# ScaleRatioA = (scale - 1) / scale;
# ScaleRatioB = 1 / (scale * ... ) or what?
# Let's find out how 0.7439024 was calculated!
# Notice: GradientScale = 41.0.
# 41.0 - 1 = 40.0. 40.0 / 41.0 = 0.975609756! Which is EXACTLY _ScaleRatioA (0.975609779)!
# Now what is 0.74390244?
# Notice: 0.74390244 * 41.0 = 30.5!
# What is 0.78292679 * 41.0? 32.1!
# What is 0.40609756 * 41.0? 16.65!
print("ScaleRatioA calculation:", (41.0 - 1.0) / 41.0)
print("0.74390244 * 41 =", 0.74390244 * 41.0)
print("0.78292679 * 41 =", 0.78292679 * 41.0)
