from package_current_art import *
O=R/'Outputs/ProtagonistCheer8_v1';A=R/'Assets/Images/Characters/Protagonist/Cheer'
O.mkdir(parents=True,exist_ok=True)
if sys.argv[1]=='snapshot':
 baseline={'metadata':{str(p.relative_to(R)):digest(p) for p in (R/'Assets/Images/Characters').rglob('*.meta')},'front_pngs':{str(p.relative_to(R)):digest(p) for p in (A/'S').glob('*.png')}}
 (O/'before.json').write_text(json.dumps(baseline,indent=2));print('Recorded existing metadata and front PNGs');sys.exit()
manifest=json.loads(Path(sys.argv[2]).read_text());before=json.loads((O/'before.json').read_text())
reference=Image.open(A/'S/cheer_s_00.png');height=reference.getbbox()[3]-reference.getbbox()[1]
duration=[240,120,120,120,300,180,180,300];animations={};report={}
for d,info in manifest.items():
 assert d in ['SE','E','NE','N','NW','W','SW']
 dest=O/d;dest.mkdir(exist_ok=True);pieces=extract(info['path'],8,2);scale=height/pieces[0][0].height
 frames=[];details=[]
 for i,(p,box) in enumerate(pieces):
  b=clean(p.resize((round(p.width*scale),round(p.height*scale)),Image.Resampling.LANCZOS));arr=np.array(b)
  ys,xs=np.where((arr[:,:,3]>128)&(np.arange(b.height)[:,None]>=b.height-50))
  center=(int(xs.min())+int(xs.max())+1)/2;x=round(256-center);y=947-b.height
  assert x>0 and y>0 and x+b.width<512,(d,i,x,y,b.size)
  f=Image.new('RGBA',(512,960));f.paste(b,(x,y));arr=np.array(f)
  assert len(groups(arr[:,:,3]>0))==1 and not np.any(arr[arr[:,:,3]==0,:3])
  f.save(dest/f'cheer_{d.lower()}_{i:02d}.png');frames.append(f);details.append({'source_bbox':box,'output_bbox':f.getbbox()})
 preview(frames,dest,duration,4);shutil.copyfile(info['path'],dest/'source.png');animations[d]=frames
 report[d]={'scale_shared_across_frames':scale,'frames':details}
 print(d,'staged',flush=True)
animations['S']=[Image.open(A/'S'/f'cheer_s_{i:02d}.png').convert('RGBA') for i in range(8)]
order=['S','SE','E','NE','N','NW','W','SW'];montage=[]
for i in range(8):
 page=Image.new('RGBA',(2048,1920))
 for j,d in enumerate(order):page.paste(animations[d][i],((j%4)*512,(j//4)*960))
 montage.append(page)
montage[0].save(O/'Cheer_8directions_preview.png',save_all=True,append_images=montage[1:],duration=duration,loop=0,disposal=0,blend=0)
peak=Image.new('RGBA',(2048,1920))
for j,d in enumerate(order):peak.paste(animations[d][4],((j%4)*512,(j//4)*960))
peak.save(O/'Cheer_8directions_peak.png')
(O/'staging.json').write_text(json.dumps(report,indent=2))
print('56 new frames staged. No Assets written.')
