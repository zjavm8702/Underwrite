from package_current_art import *
O=R/'Outputs/HumanFemale_IdleHair_v2';frames=[]
for i in range(6):
 page=Image.new('RGBA',(1536,724))
 for j,d in enumerate(['S','E','N']):page.paste(Image.open(O/d/f'idle_{d.lower()}_{i:02d}.png'),(j*512,0))
 frames.append(page)
frames[0].save(O/'Hair_motion_preview.png',save_all=True,append_images=frames[1:],duration=[200]*6,loop=0,disposal=0,blend=0)
