from package_current_art import *
O=R/'Outputs/ProtagonistCheer_v1';A=R/'Assets/Images/Characters/Protagonist/Cheer/S'
O.mkdir(parents=True,exist_ok=True)
metas={str(p.relative_to(R)):digest(p) for p in (R/'Assets/Images/Characters').rglob('*.meta')}
(O/'metadata_before.json').write_text(json.dumps(metas,indent=2))
pieces=extract(sys.argv[1],8,2)
ref=Image.open(R/'Assets/Images/Characters/Protagonist/Idle/S/idle_s_00.png')
height=ref.getbbox()[3]-ref.getbbox()[1];scale=height/pieces[0][0].height
frames=[];report=[]
for i,(p,box) in enumerate(pieces):
 b=clean(p.resize((round(p.width*scale),round(p.height*scale)),Image.Resampling.LANCZOS))
 # Grounded action: all frames share the same floor; no per-frame scale.
 a=np.array(b);ys,xs=np.where((a[:,:,3]>128)&(np.arange(b.height)[:,None]>=b.height-50))
 center=(int(xs.min())+int(xs.max())+1)/2
 x=round(256-center);y=947-b.height
 assert x>0 and y>0 and x+b.width<512
 f=Image.new('RGBA',(512,960));f.paste(b,(x,y));arr=np.array(f)
 assert len(groups(arr[:,:,3]>0))==1 and not np.any(arr[arr[:,:,3]==0,:3])
 frames.append(f);report.append({'source_bbox':box,'output_bbox':f.getbbox()})
duration=[240,120,120,120,300,180,180,300]
preview(frames,O,duration,4);shutil.copyfile(sys.argv[1],O/'source.png')
A.mkdir(parents=True,exist_ok=True)
for i,f in enumerate(frames):
 name=f'cheer_s_{i:02d}.png'; assert not (A/name).exists(), 'Existing destination must not be silently replaced'
 f.save(O/name);f.save(A/name)
assert all(digest(R/p)==s for p,s in metas.items())
result={'frames':report,'png_count':8,'canvas':[512,960],'pivot_bottom_left':[256,13],'scale_shared_across_frames':scale,'duration_ms':duration,'one_shot':True,'existing_metadata_verified':len(metas),'metadata_changes':0,'detached_components':0,'hidden_rgb':0,'edge_contact':0}
(O/'verification.json').write_text(json.dumps(result,indent=2))
print('8 front-view cheer PNGs saved; metadata unchanged; animation duration',sum(duration),'ms')
