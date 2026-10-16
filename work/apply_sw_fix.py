"""Copy approved staged PNG keyframes to existing paths, preserving every .meta."""
from pathlib import Path
import hashlib
import json
import shutil
from PIL import Image

root=Path(r'C:\Underwrite'); out=root/'Outputs/WalkingArmFix_SW_v3'
before=json.loads((out/'before.json').read_text(encoding='utf-8'))
staged=json.loads((out/'keyframe_staging.json').read_text(encoding='utf-8'))
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
for relative,expected in before['metadata_hashes'].items():
    assert digest(root/relative)==expected,'Metadata changed during work: '+relative
for relative,info in before['frames'].items():
    assert digest(root/relative)==info['sha256'],'PNG changed during work: '+relative
assert 2 <= len(staged) <= 6
changes=[]
for key,info in staged.items():
    direction,index=key.split('_')
    assert direction in ['SW'] and index in ['04','05']
    source=Path(info['stage'])
    target=root/'Assets/Images/Characters/Protagonist/Walking'/direction/f'walk_{direction.lower()}_{index}.png'
    assert target.is_file() and target.with_suffix('.png.meta').is_file()
    assert Image.open(source).size==Image.open(target).size==(640,724)
    shutil.copyfile(source,target)
    changes.append(str(target.relative_to(root)))
for relative,expected in before['metadata_hashes'].items():
    assert digest(root/relative)==expected,'Metadata mismatch after PNG copy: '+relative
for relative,info in before['frames'].items():
    if relative not in changes:
        assert digest(root/relative)==info['sha256'],'Unexpected PNG change: '+relative
result={'changed_pngs':changes,'metadata_files_verified':len(before['metadata_hashes']),
        'metadata_changes':0,'unchanged_target_direction_frames':8-len(changes),
        'backup':before['backup']}
(out/'applied.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))

