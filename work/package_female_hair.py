from package_current_art import *
O=R/'Outputs/HumanFemale_IdleHair_v2'; A=R/'Assets/Images/Characters/Heroes/HumanFemale/Idle'
before=json.loads((O/'before.json').read_text());manifest=json.loads(Path(sys.argv[1]).read_text())
def footcenter(im):
 a=np.asarray(im)[:,:,3]>128;ys,xs=np.where(a);bottom=ys.max()+1
 ys,xs=np.where(a & (np.arange(im.height)[:,None]>=bottom-45))
 return (xs.min()+xs.max()+1)/2
report={};animations={}
for d,info in manifest.items():
 folder=O/d;folder.mkdir(exist_ok=True);pieces=extract(info['path'],6,1)
 old=[Image.open(O/'Before'/d/f'idle_{d.lower()}_{i:02d}.png').convert('RGBA') for i in range(6)]
 scale=float(np.median([f.getbbox()[3]-f.getbbox()[1] for f in old]))/float(np.median([p.height for p,b in pieces]))
 frames=[];details=[]
 for i,(p,box) in enumerate(pieces):
  b=clean(p.resize((round(p.width*scale),round(p.height*scale)),Image.Resampling.LANCZOS))
  x=round(footcenter(old[i])-footcenter(b));y=old[i].getbbox()[3]-b.height
  assert x>0 and y>0 and x+b.width<512 and y+b.height<724
  f=Image.new('RGBA',(512,724));f.paste(b,(x,y));name=f'idle_{d.lower()}_{i:02d}.png';f.save(folder/name)
  a=np.array(f);assert len(groups(a[:,:,3]>0))==1
  assert not np.any(a[a[:,:,3]==0,:3])
  frames.append(f);details.append({'source_bbox':box,'output_bbox':f.getbbox()})
 preview(frames,folder,[200]*6,6);shutil.copyfile(info['path'],folder/'source.png');animations[d]=frames
 report[d]={'scale_shared_across_frames':scale,'frames':details,'duration_ms':[200]*6}
order=['S','SE','E','NE','N','NW','W','SW'];montage=[]
for i in range(6):
 page=Image.new('RGBA',(2048,1448))
 for j,d in enumerate(order):page.paste(animations[d][i],((j%4)*512,(j//4)*724))
 montage.append(page)
montage[0].save(O/'Idle_8directions_preview.png',save_all=True,append_images=montage[1:],duration=[200]*6,loop=0,disposal=0,blend=0)
montage[0].save(O/'contact.png')
(O/'staging_report.json').write_text(json.dumps(report,indent=2))
print('48 frames staged, Assets unchanged')
