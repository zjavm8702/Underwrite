from pathlib import Path
import hashlib
import json
import shutil
from PIL import Image

root=Path(r'C:\Underwrite'); out=root/'Outputs/WalkingArmFix_v2'
before=json.loads((out/'before.json').read_text(encoding='utf-8'))
applied=json.loads((out/'applied.json').read_text(encoding='utf-8'))
staged=json.loads((out/'keyframe_staging.json').read_text(encoding='utf-8'))
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
sources=out/'SelectedSources'; sources.mkdir(exist_ok=True)
for key,info in staged.items():
    shutil.copyfile(info['source'],sources/(key+'_source.png'))
    direction,index=key.split('_')
    target=root/'Assets/Images/Characters/Protagonist/Walking'/direction/f'walk_{direction.lower()}_{index}.png'
    assert digest(target)==digest(Path(info['stage']))
    assert Image.open(target).size==(640,724)
for relative,expected in before['metadata_hashes'].items():
    assert digest(root/relative)==expected,relative
result={'copied_pngs_verified':len(staged),'metadata_files_verified':len(before['metadata_hashes']),
        'metadata_changes':0,'canvas':[640,724],'existing_paths_and_filenames_preserved':True}
(out/'verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
