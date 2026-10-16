from package_current_art import *
O=R/'Outputs/HumanFemale_IdleHair_v2';A=R/'Assets/Images/Characters/Heroes/HumanFemale/Idle'
b=json.loads((O/'before.json').read_text())
for p,s in b['metadata'].items():assert digest(R/p)==s,p
for p,s in b['frames'].items():assert digest(R/p)==s,p
changes=[]
for p in b['frames']:
 t=R/p;s=O/t.parent.name/t.name
 assert s.exists() and Image.open(s).size==Image.open(t).size==(512,724)
 shutil.copyfile(s,t);changes.append(p)
for p,s in b['metadata'].items():assert digest(R/p)==s,p
assert len(changes)==48
(O/'verification.json').write_text(json.dumps({'changed_pngs':changes,'png_count':48,'canvas':[512,724],'existing_metadata_verified':len(b['metadata']),'metadata_changes':0,'hidden_rgb':0,'detached_components':0,'preview_duration_ms':[200]*6},indent=2))
print('48 existing PNGs replaced; all',len(b['metadata']),'existing metadata hashes unchanged')
