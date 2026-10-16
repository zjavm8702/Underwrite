from package_current_art import *
out=O
report={'existing_metadata_changes':[], 'frames':{}}
baseline=json.loads((O/'Jackpot_S/metadata_before.json').read_text())
for p,s in baseline.items():
 if digest(Path(p))!=s:report['existing_metadata_changes'].append(p)
paths=list((R/'Assets/Images/Characters/Heroes/HumanFemale/Idle').rglob('*.png'))+list((R/'Assets/Images/Characters/Protagonist/Walking/Jackpot_S').glob('*.png'))
for p in paths:
 im=Image.open(p).convert('RGBA');a=np.array(im);bbox=im.getbbox();gs=groups(a[:,:,3]>0)
 hidden=int(np.any(a[a[:,:,3]==0,:3]!=0,axis=1).sum());edge=bool(np.any(a[0,:,3]) or np.any(a[-1,:,3]) or np.any(a[:,0,3]) or np.any(a[:,-1,3]))
 report['frames'][str(p.relative_to(R))]={'canvas':im.size,'bbox':bbox,'components':len(gs),'hidden_rgb':hidden,'edge_contact':edge}
 assert len(gs)==1 and hidden==0 and not edge,p
assert not report['existing_metadata_changes']
report['png_count']=len(paths);report['existing_metadata_verified']=len(baseline)
(out/'validation.json').write_text(json.dumps(report,indent=2));print(json.dumps({'png_count':len(paths),'metadata_changes':0,'detached_pixels':0,'hidden_rgb':0,'edge_contact':0}))
