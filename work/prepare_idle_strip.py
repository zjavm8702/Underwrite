"""Export generated Idle frames; remove authorized alpha specks without repainting."""
from pathlib import Path
from collections import deque
import json
import sys
from PIL import Image
import numpy as np

source = Path(sys.argv[1])
out = Path(sys.argv[2])
cols = int(sys.argv[3])
rows = int(sys.argv[4])
out.mkdir(parents=True, exist_ok=True)
im = Image.open(source).convert('RGBA')
w, h = im.size
cw, ch = w // cols, h // rows
frames, report = [], []
for idx in range(cols*rows):
    col, row = idx % cols, idx // cols
    frame = im.crop((col*cw, row*ch, (col+1)*cw, (row+1)*ch))
    rgba = np.array(frame)
    alpha = rgba[:, :, 3]
    remaining = set(np.flatnonzero(alpha.ravel() >= 32).tolist())
    groups = []
    while remaining:
        first = remaining.pop()
        queue, group = deque([first]), [first]
        while queue:
            p = queue.popleft()
            x, y = p % cw, p // cw
            for ny in range(max(0, y-1), min(ch, y+2)):
                for nx in range(max(0, x-1), min(cw, x+2)):
                    n = ny*cw+nx
                    if n in remaining:
                        remaining.remove(n)
                        queue.append(n)
                        group.append(n)
        groups.append(group)
    keep = max(groups, key=len)
    mask = np.zeros(cw*ch, dtype=bool)
    mask[keep] = True
    mask = mask.reshape((ch, cw))
    removed = int(((alpha > 0) & ~mask).sum())
    # Clear hidden color in discarded transparent pixels as well so texture
    # filtering cannot pull stray red/white RGB into the antialiased outline.
    rgba[~mask] = 0
    cleaned = Image.fromarray(rgba)
    # Fixed-size frame registration: translate the entire cutout by its shoe
    # baseline. Do not scale, deform, repaint, or trim individual frame canvases.
    ys, xs = np.where(mask & (np.arange(ch)[:, None] >= ch-90))
    foot_center = (int(xs.min()) + int(xs.max()) + 1) / 2
    bottom = int(ys.max()) + 1
    dx, dy = round(cw/2-foot_center), (ch-13)-bottom
    registered = Image.new('RGBA', (cw, ch))
    registered.paste(cleaned, (dx, dy))
    cleaned = registered
    cleaned.save(out / f'idle_front_{idx:02d}.png')
    frames.append(cleaned)
    report.append({'frame': idx, 'bbox': cleaned.getbbox(), 'removed_alpha_pixels': removed,
                   'retained_components': 1, 'cell': [cw, ch], 'registration_offset': [dx,dy]})
sheet = Image.new('RGBA', (cw*cols, ch*rows))
for idx, frame in enumerate(frames):
    sheet.paste(frame, ((idx % cols)*cw, (idx // cols)*ch))
sheet.save(out / 'protagonist_idle_front_sheet_v1.png')
durations = [900, 350, 80, 110, 120, 700]
frames[0].save(out / 'Idle_front_preview.png', save_all=True, append_images=frames[1:],
               duration=durations[:len(frames)], loop=0, disposal=0, blend=0)
(out / 'frame_check.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps({'source_size': [w,h], 'grid': [cols, rows], 'frames': report}, indent=2))
