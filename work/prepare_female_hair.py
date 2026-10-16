from package_current_art import *
O=R/'Outputs/HumanFemale_IdleHair_v2';O.mkdir(parents=True,exist_ok=True)
A=R/'Assets/Images/Characters/Heroes/HumanFemale/Idle'
B=O/'Before';B.mkdir(exist_ok=True)
manifest={'frames':{},'metadata':{str(p.relative_to(R)):digest(p) for p in (R/'Assets/Images/Characters').rglob('*.meta')}}
for p in A.rglob('*.png'):
 t=B/p.relative_to(A);t.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,t)
 manifest['frames'][str(p.relative_to(R))]=digest(p)
(O/'before.json').write_text(json.dumps(manifest,indent=2))
print('Backed up',len(manifest['frames']),'PNGs and recorded',len(manifest['metadata']),'metadata hashes')
