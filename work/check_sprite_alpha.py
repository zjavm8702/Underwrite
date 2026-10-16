"""Read-only alpha and connected-component inspection of an eight-view sheet."""
import json
import sys
from pathlib import Path
import numpy as np
from PIL import Image
from collections import deque

path = Path(sys.argv[1])
im = Image.open(path).convert('RGBA')
a = np.asarray(im)[:, :, 3]
h, w = a.shape
report = {'file': str(path), 'size': [w, h], 'alpha_min': int(a.min()),
          'alpha_max': int(a.max()), 'transparent_pixels': int((a == 0).sum()),
          'partial_alpha_pixels': int(((a > 0) & (a < 255)).sum()), 'thresholds': {}}
for threshold in (1, 32, 128):
    remaining = set(np.flatnonzero(a.ravel() >= threshold).tolist())
    count = 0
    small_count = 0
    small_pixels = 0
    large = []
    while remaining:
        first = remaining.pop()
        queue = deque([first])
        area = 0
        xmin = xmax = first % w
        ymin = ymax = first // w
        while queue:
            p = queue.popleft()
            x, y = p % w, p // w
            area += 1
            xmin, xmax = min(xmin, x), max(xmax, x)
            ymin, ymax = min(ymin, y), max(ymax, y)
            for ny in range(max(0, y-1), min(h, y+2)):
                for nx in range(max(0, x-1), min(w, x+2)):
                    n = ny*w+nx
                    if n in remaining:
                        remaining.remove(n)
                        queue.append(n)
        count += 1
        if area >= 1000:
            large.append({'area': area, 'bbox': [xmin, ymin, xmax+1, ymax+1]})
        else:
            small_count += 1
            small_pixels += area
    report['thresholds'][threshold] = {'components': count, 'large': large,
        'small_components': small_count, 'small_component_pixels': small_pixels}
print(json.dumps(report, ensure_ascii=False, indent=2))
if len(sys.argv) > 2:
    Path(sys.argv[2]).write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
