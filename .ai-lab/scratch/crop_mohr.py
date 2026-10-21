from PIL import Image
import numpy as np

im = Image.open(r'C:/Users/HP/.gemini/antigravity/brain/2718d163-fe44-4901-b34e-d106d2d7ade9/.user_uploaded/media_1792233186810_6931ec49.png')
arr = np.array(im)
h, w, c = arr.shape
print("Image size:", w, h)

# Let's crop around 'Play' or 'Options'
# 'Play' is inside the grey button at the top
# Let's find the grey button
# In media_1792233186810_6931ec49.png, the button is grey: R,G,B around 110-130
# Let's crop 'Play'
crop_play = im.crop((int(w*0.35), int(h*0.18), int(w*0.65), int(h*0.30)))
crop_play.save('crop_play_mohr.png')

# Let's crop 'Options'
crop_options = im.crop((int(w*0.35), int(h*0.30), int(w*0.65), int(h*0.40)))
crop_options.save('crop_options_mohr.png')

print("Saved crop_play_mohr.png and crop_options_mohr.png")
