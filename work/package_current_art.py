from pathlib import Path
from collections import deque
import json, sys, shutil, hashlib
import numpy as np
from PIL import Image
R=Path(r'C:\Underwrite'); O=R/'Outputs/CharacterArt_v3'; O.mkdir(parents=True,exist_ok=True)
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def groups(a):
 h,w=a.shape; todo=set(np.flatnonzero(a).tolist()); result=[]
 while todo:
  p=todo.pop(); q=deque([p]); g=[p]
  while q:
   p=q.popleft(); x,y=p%w,p//w
   for yy in range(max(0,y-1),min(h,y+2)):
    for xx in range(max(0,x-1),min(w,x+2)):
     n=yy*w+xx
     if n in todo: todo.remove(n);q.append(n);g.append(n)
  result.append(g)
 return sorted(result,key=len,reverse=True)
def cut(a,g):
 h,w=a.shape[:2]; ps=np.array(g); ys,xs=ps//w,ps%w
 box=(int(xs.min()),int(ys.min()),int(xs.max())+1,int(ys.max())+1)
 b=a[box[1]:box[3],box[0]:box[2]].copy(); mask=np.zeros(b.shape[:2],bool);mask[ys-box[1],xs-box[0]]=True;b[~mask]=0
 return Image.fromarray(b),box
def clean(im):
 a=np.array(im); g=groups(a[:,:,3]>=32)[0]; b,box=cut(a,g);return b
def extract(path,count,rows):
 a=np.array(Image.open(path).convert('RGBA'));h,w=a.shape[:2]; gs=groups(a[:,:,3]>=32)[:count]
 assert len(gs)==count and min(map(len,gs))>1000
 gs.sort(key=lambda g:np.mean(np.array(g)//w))
 cols=count//rows; ordered=[]
 for row in range(rows): ordered+=sorted(gs[row*cols:(row+1)*cols],key=lambda g:min(p%w for p in g))
 return [cut(a,g) for g in ordered]
def preview(frames,dest,durations,cols):
 w,h=frames[0].size; sheet=Image.new('RGBA',(w*cols,h*((len(frames)+cols-1)//cols)))
 for i,f in enumerate(frames):sheet.paste(f,((i%cols)*w,(i//cols)*h))
 sheet.save(dest/'sheet.png'); frames[0].save(dest/'preview.png',save_all=True,append_images=frames[1:],duration=durations,loop=0,disposal=0,blend=0)
def jackpot(path):
 dest=O/'Jackpot_S';dest.mkdir(exist_ok=True); target=R/'Assets/Images/Characters/Protagonist/Walking/Jackpot_S'
 metas={str(p):digest(p) for p in (R/'Assets/Images/Characters').rglob('*.meta')}
 (dest/'metadata_before.json').write_text(json.dumps(metas,indent=2))
 backup=dest/'Before';backup.mkdir(exist_ok=True)
 pieces=extract(path,8,2); scale=699/pieces[0][0].height;frames=[]
 for i,(piece,box) in enumerate(pieces):
  b=clean(piece.resize((round(piece.width*scale),round(piece.height*scale)),Image.Resampling.LANCZOS));x=(512-b.width)//2;y=947-b.height
  assert x>0 and y>0
  f=Image.new('RGBA',(512,960));f.paste(b,(x,y));name=f'jackpot_s_{i:02d}.png';f.save(dest/name);frames.append(f)
  t=target/name;assert t.exists() and Image.open(t).size==f.size
  shutil.copyfile(t,backup/name);shutil.copyfile(dest/name,t)
 assert all(digest(Path(p))==s for p,s in metas.items())
 shutil.copyfile(path,dest/'source.png');preview(frames,dest,[280,160,120,140,280,180,200,350],4)
 (dest/'verification.json').write_text(json.dumps({'frames':8,'canvas':[512,960],'metadata_files':len(metas),'metadata_changes':0,'motion':'grounded celebration, front view','loop':False},indent=2))
def female(manifest):
 dest=O/'HumanFemale_Idle';dest.mkdir(exist_ok=True); asset=R/'Assets/Images/Characters/Heroes/HumanFemale/Idle';anims={};report={}
 allpieces={d:extract(info['path'],6,1) for d,info in manifest.items()}
 # One shared scale for all directions, height shorter than protagonist.
 scale=620/max(p.height for pieces in allpieces.values() for p,box in pieces)
 for d,pieces in allpieces.items():
  folder=dest/d;folder.mkdir(exist_ok=True);af=asset/d;af.mkdir(parents=True,exist_ok=True);frames=[]
  for i,(p,box) in enumerate(pieces):
   b=clean(p.resize((round(p.width*scale),round(p.height*scale)),Image.Resampling.LANCZOS));x=(512-b.width)//2;y=711-b.height;assert x>0 and y>0
   f=Image.new('RGBA',(512,724));f.paste(b,(x,y));name=f'idle_{d.lower()}_{i:02d}.png';f.save(folder/name);f.save(af/name);frames.append(f)
  shutil.copyfile(manifest[d]['path'],folder/'source.png');preview(frames,folder,[900,350,80,110,120,700],6);anims[d]=frames;report[d]={'frames':6,'scale':scale,'canvas':[512,724],'pivot_bottom_left':[256,13]}
 order=['S','SE','E','NE','N','NW','W','SW'];montage=[]
 for i in range(6):
  page=Image.new('RGBA',(2048,1448))
  for j,d in enumerate(order):page.paste(anims[d][i],((j%4)*512,(j//4)*724))
  montage.append(page)
 montage[0].save(dest/'Idle_8directions_preview.png',save_all=True,append_images=montage[1:],duration=[900,350,80,110,120,700],loop=0,disposal=0,blend=0);montage[0].save(dest/'Idle_8directions_contact.png')
 (dest/'export_report.json').write_text(json.dumps(report,indent=2));print('Female 48 PNGs exported',flush=True)
if __name__=='__main__':
 if sys.argv[1]=='jackpot':jackpot(sys.argv[2])
 else:female(json.loads(Path(sys.argv[2]).read_text()))
