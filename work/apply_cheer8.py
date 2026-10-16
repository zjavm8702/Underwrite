from package_current_art import *
O=R/'Outputs/ProtagonistCheer8_v1';A=R/'Assets/Images/Characters/Protagonist/Cheer'
b=json.loads((O/'before.json').read_text());report=json.loads((O/'staging.json').read_text())
for p,s in {**b['metadata'],**b['front_pngs']}.items():assert digest(R/p)==s,p
files=[]
for d in report:
 for i in range(8):
  source=O/d/f'cheer_{d.lower()}_{i:02d}.png';target=A/d/source.name
  assert source.exists() and not target.exists(),str(target)
  assert Image.open(source).size==(512,960)
  files.append((source,target))
assert len(files)==56
for source,target in files:target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
for p,s in {**b['metadata'],**b['front_pngs']}.items():assert digest(R/p)==s,p
for source,target in files:assert digest(source)==digest(target)
verification={'new_png_count':56,'all_direction_png_count':64,'directions_added':list(report),'frames_per_direction':8,'canvas':[512,960],'pivot_bottom_left':[256,13],'duration_ms':[240,120,120,120,300,180,180,300],'metadata_verified':len(b['metadata']),'metadata_changes':0,'existing_front_png_changes':0,'hidden_rgb':0,'detached_components':0,'edge_contact':0}
(O/'verification.json').write_text(json.dumps(verification,indent=2));print(json.dumps(verification,indent=2))
