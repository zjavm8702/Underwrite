"""Package generated strips, clean authorized alpha noise, register sprite frames."""
from collections import deque
from pathlib import Path
import json
import sys
from PIL import Image
import numpy as np

root = Path(r'C:\Underwrite')
manifest = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
out = root / 'Outputs/ProtagonistIdle8_v1'
assets = root / 'Assets/Images/Characters/Protagonist/Idle8_v1'
cw, ch, baseline = 362, 724, 711
durations = [900, 350, 80, 110, 120, 700]
animations, report = {}, {}
for direction, info in manifest.items():
    image = Image.open(info['path']).convert('RGBA')
    sw, sh = image.size
    sheet_rgba = np.array(image)
    remaining = set(np.flatnonzero(sheet_rgba[:,:,3].ravel() >= 32).tolist())
    groups = []
    while remaining:
        first = remaining.pop()
        queue, group = deque([first]), [first]
        while queue:
            p = queue.popleft()
            x, y = p % sw, p // sw
            for ny in range(max(0,y-1), min(sh,y+2)):
                for nx in range(max(0,x-1), min(sw,x+2)):
                    n = ny*sw+nx
                    if n in remaining:
                        remaining.remove(n)
                        queue.append(n)
                        group.append(n)
        groups.append(group)
    groups = sorted(groups,key=len,reverse=True)[:6]
    if len(groups) != 6 or min(map(len,groups)) < 1000:
        raise ValueError(f'{direction}: six complete silhouettes not found')
    groups.sort(key=lambda g: min(p % sw for p in g))
    raw, details = [], []
    for idx, largest in enumerate(groups):
        rgba = sheet_rgba.copy()
        alpha = rgba[:,:,3]
        mask = np.zeros(sw*sh, dtype=bool)
        mask[largest] = True
        mask = mask.reshape(sh,sw)
        removed = int(((alpha > 0) & (alpha < 32)).sum())
        rgba[~mask] = 0
        frame = Image.fromarray(rgba)
        bbox = frame.getbbox()
        raw.append(frame.crop(bbox))
        details.append({'source_bbox': bbox, 'removed_alpha_pixels': removed})
    # One uniform scale per direction, never a separate scale per frame.
    scale = min(699/max(f.height for f in raw), 330/max(f.width for f in raw))
    target = out / direction
    target_assets = assets / direction
    target.mkdir(parents=True, exist_ok=True)
    target_assets.mkdir(parents=True, exist_ok=True)
    frames = []
    for idx, f in enumerate(raw):
        scaled = f.resize((round(f.width*scale),round(f.height*scale)), Image.Resampling.LANCZOS)
        pixels = np.array(scaled)
        pixels[pixels[:,:,3] < 32] = 0
        visible = pixels[:,:,3] > 0
        padded = np.pad(visible,1)
        neighbors = np.zeros(visible.shape,dtype=np.uint8)
        for oy in range(3):
            for ox in range(3):
                if (oy,ox)!=(1,1):
                    neighbors += padded[oy:oy+visible.shape[0],ox:ox+visible.shape[1]]
        pixels[visible & (neighbors==0)] = 0
        scaled = Image.fromarray(pixels)
        a = pixels[:,:,3]
        bottom = np.where(a > 0)[0].max()+1
        feet_y, feet_x = np.where((a > 0) & (np.arange(a.shape[0])[:,None] >= bottom-70))
        foot_center = (int(feet_x.min())+int(feet_x.max())+1)/2
        x, y = round(cw/2-foot_center), baseline-int(bottom)
        frame = Image.new('RGBA',(cw,ch))
        frame.paste(scaled,(x,y))
        if x < 0 or x+scaled.width > cw or y < 0:
            raise ValueError(f'{direction} frame {idx}: clipping')
        filename = f'idle_{direction}_{idx:02d}.png'
        frame.save(target/filename)
        frame.save(target_assets/filename)
        frames.append(frame)
        details[idx].update({'export_bbox': frame.getbbox(), 'canvas': [cw,ch]})
    sheet = Image.new('RGBA',(cw*6,ch))
    for idx,f in enumerate(frames):
        sheet.paste(f,(idx*cw,0))
    sheet.save(target/f'idle_{direction}_sheet.png')
    frames[0].save(target/f'Idle_{direction}_preview.png',save_all=True,
                   append_images=frames[1:],duration=durations,loop=0,disposal=0,blend=0)
    animations[direction] = frames
    report[direction] = {'scale':scale,'frames':details,'duration_ms':durations}
    print(direction, 'exported', flush=True)

# Eight direction previews arranged clockwise, with a separate frame for each
# point in time. This is packaging existing sprites, no artwork is repainted.
order = ['S','SE','E','NE','N','NW','W','SW']
montage = []
for phase in range(6):
    page = Image.new('RGBA',(cw*4,ch*2))
    for pos,direction in enumerate(order):
        page.paste(animations[direction][phase],((pos%4)*cw,(pos//4)*ch))
    montage.append(page)
montage[0].save(out/'Idle_8directions_preview.png',save_all=True,
                append_images=montage[1:],duration=durations,loop=0,disposal=0,blend=0)
montage[0].save(out/'Idle_8directions_contact.png')
(out/'export_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
(out/'source_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('48 frames packaged; 8 strips and animated previews exported.')
