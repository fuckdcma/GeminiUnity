from PIL import Image
import numpy as np
import UnityPy

bundle_path = r'C:\Users\HP\Desktop\GeminiUnity\Output\Studio\Assets\Fonts\Sample_vietnamese_font.bundle'
env = UnityPy.load(bundle_path)
for obj in env.objects:
    if obj.type.name == 'Texture2D' and 'ARIAL' in getattr(obj.read(), 'm_Name', ''):
        atlas_img = obj.read().image
        break

atlas = np.array(atlas_img)[:, :, 3].astype(np.float32) / 255.0

# Find a glyph: find a region where alpha has values around 0.5 and > 0.8
# Let's crop a 150x150 block around a character
# find max alpha area
rows = np.where(atlas > 0.9)
r_min, r_max = rows[0].min(), rows[0].max()
c_min, c_max = rows[1].min(), rows[1].max()

# Take a 120x120 patch containing a glyph
patch = atlas[rows[0][0]-10:rows[0][0]+110, rows[1][0]-10:rows[1][0]+110]

def render_tmp_sdf(d, outline_w=0.0, outline_on=False, 
                   underlay_on=False, ox=0, oy=0, softness=0.3, dilate=0.0):
    h, w = d.shape
    # Face:
    face_alpha = np.clip((d - 0.5) / 0.05 + 0.5, 0, 1)
    
    # Outline:
    if outline_on and outline_w > 0:
        out_min = 0.5 - outline_w
        outline_alpha = np.clip((d - out_min) / 0.05 + 0.5, 0, 1)
        outline_alpha = outline_alpha * (1.0 - face_alpha)
    else:
        outline_alpha = np.zeros_like(d)
        
    # Underlay:
    if underlay_on:
        # shifted d
        d_shift = np.zeros_like(d)
        sy = int(oy)
        sx = int(ox)
        src_y_start = max(0, -sy)
        src_y_end = min(h, h - sy)
        dst_y_start = max(0, sy)
        dst_y_end = min(h, h + sy)
        
        src_x_start = max(0, -sx)
        src_x_end = min(w, w - sx)
        dst_x_start = max(0, sx)
        dst_x_end = min(w, w + sx)
        
        d_shift[dst_y_start:dst_y_end, dst_x_start:dst_x_end] = d[src_y_start:src_y_end, src_x_start:src_x_end]
        
        # smoothstep between 0.5 - dilate - softness and 0.5 - dilate + softness
        edge0 = 0.5 - dilate - softness
        edge1 = 0.5 - dilate + softness
        if edge1 > edge0:
            u = np.clip((d_shift - edge0) / (edge1 - edge0), 0, 1)
            underlay_alpha = (u * u * (3 - 2 * u)) * 0.75
        else:
            underlay_alpha = np.zeros_like(d)
        # underlay behind face and outline
        combined_face = face_alpha + outline_alpha
        underlay_alpha = underlay_alpha * (1.0 - combined_face)
    else:
        underlay_alpha = np.zeros_like(d)

    # Composite on background (wood floor color: rgb(170, 115, 95))
    bg = np.array([170, 115, 95], dtype=np.float32)
    face_col = np.array([255, 255, 255], dtype=np.float32)
    out_col = np.array([20, 15, 25], dtype=np.float32) # Dark outline
    und_col = np.array([10, 5, 15], dtype=np.float32)  # Dark shadow

    res = np.zeros((h, w, 3), dtype=np.float32)
    for c in range(3):
        res[:, :, c] = bg[c] * (1.0 - face_alpha - outline_alpha - underlay_alpha) + \
                       face_col[c] * face_alpha + \
                       out_col[c] * outline_alpha + \
                       und_col[c] * underlay_alpha
    return np.clip(res, 0, 255).astype(np.uint8)

# Case 1: Previous (flat white, no outline, blown softness)
c1 = render_tmp_sdf(patch, outline_w=0.0, outline_on=False, underlay_on=True, ox=4, oy=4, softness=0.74, dilate=-0.05)
Image.fromarray(c1).save(r'C:\Users\HP\Desktop\GeminiUnity\.ai-lab\scratch\sim_old.png')

# Case 2: Crisp outline + soft deep shadow
c2 = render_tmp_sdf(patch, outline_w=0.12, outline_on=True, underlay_on=True, ox=6, oy=6, softness=0.25, dilate=0.10)
Image.fromarray(c2).save(r'C:\Users\HP\Desktop\GeminiUnity\.ai-lab\scratch\sim_new.png')

print("Simulations saved to sim_old.png and sim_new.png")
