"""Rebuild the original IX-compatible segmented zombie GLB. Python 3 + Pillow only.
Coordinates are metres, Y up, face -Z. Geometry belongs to named animation pivots.
No runtime geometry generation, skinning, collision or animation tracks in this asset.
"""
from pathlib import Path
import json, math, struct, io, random
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parents[1] / 'ZombieFirstPass.glb'
rng = random.Random(1984)
# One embedded atlas: grime, weave, wounds and a deliberately authored face panel.
im = Image.new('RGB', (768,512)); px = im.load()
base = [(125,132,105),(74,87,80),(125,132,105),(58,64,67),(34,32,28),(28,29,24)]
for y in range(512):
    for x in range(768):
        tile=(x//256)+3*(y//256); noise=rng.randrange(-12,13)
        weave=3 if (x+y)%4==0 else 0
        px[x,y]=tuple(max(0,min(255,c+noise+weave)) for c in base[tile])
d=ImageDraw.Draw(im)
# Fabric seams, tears and irregular blood; all deterministic and opaque.
for tile in (0,1,3):
    ox=(tile%3)*256; oy=(tile//3)*256
    for _ in range(24):
        x=ox+rng.randrange(15,240); y=oy+rng.randrange(15,240)
        r=rng.randrange(2,13)
        d.ellipse((x-r,y-r,x+r,y+r*2),fill=(69,42,33) if tile==0 else (40,43,37))
    if tile:
        d.line((ox+35,oy+4,ox+42,oy+250),fill=(102,104,88),width=2)
        d.line((ox+211,oy+4,ox+205,oy+250),fill=(30,34,31),width=3)
face_image=im.crop((0,0,256,256)); d=ImageDraw.Draw(face_image)
# Head front maps longitude 0.5 to front centre; eyes sit on the upper head.
d.ellipse((91,67,119,91),fill=(49,49,40)); d.ellipse((138,70,165,93),fill=(46,45,36))
d.ellipse((103,75,109,81),fill=(177,168,117)); d.ellipse((146,77,152,83),fill=(165,157,112))
d.polygon([(109,125),(151,129),(147,151),(114,146)],fill=(43,24,22))
for x in range(116,146,6): d.rectangle((x,129,x+3,134),fill=(163,152,119))
d.line((157,94,165,122,153,144),fill=(100,40,32),width=5)
im.paste(face_image,(512,0))
bio=io.BytesIO(); im.save(bio,format='PNG'); png=bio.getvalue()
blob=bytearray(); views=[]; access=[]; nodes=[]; meshes=[]

def raw(data,target=None):
    while len(blob)%4: blob.append(0)
    off=len(blob); blob.extend(data); v={'buffer':0,'byteOffset':off,'byteLength':len(data)}
    if target: v['target']=target
    views.append(v); return len(views)-1

def acc(values,kind,components):
    flat=[v for row in values for v in (row if isinstance(row,(tuple,list)) else [row])]
    integer=kind==5125; data=struct.pack('<'+('I' if integer else 'f')*len(flat),*flat)
    a={'bufferView':raw(data,34963 if integer else 34962),'componentType':kind,'count':len(values),'type':{1:'SCALAR',2:'VEC2',3:'VEC3'}[components]}
    if components==3:
        a['min']=[min(v[i] for v in values) for i in range(3)]; a['max']=[max(v[i] for v in values) for i in range(3)]
    access.append(a); return len(access)-1

def node(name,pos=(0,0,0),parent=None):
    i=len(nodes); nodes.append({'name':name,'translation':list(pos)})
    if parent is not None: nodes[parent].setdefault('children',[]).append(i)
    return i

# Mesh accumulator per articulation: a single atlas/material surface per moving segment.
geometry={}
def contour(owner,rings,tile=0,n=16,face=False):
    """Closed tapered ring surface: (height, halfwidth, halfdepth, centreZ)."""
    p,norm,uv,idx=geometry.setdefault(owner,([],[],[],[])); start=len(p)
    ox=(tile%3)/3; oy=(tile//3)*.5
    for j,(y,rx,rz,cz) in enumerate(rings):
        for k in range(n+1):
            a=2*math.pi*k/n; sx=math.sin(a); zz=math.cos(a)
            p.append((rx*sx,y,cz+rz*zz))
            # Smooth radial normals include local taper slope.
            lo=rings[max(0,j-1)]; hi=rings[min(len(rings)-1,j+1)]
            dy=hi[0]-lo[0]; dr=((hi[1]-lo[1])*abs(sx)+(hi[2]-lo[2])*abs(zz))
            normal=(sx/max(rx,.01),-dr/max(abs(dy),.001)/max(rx,rz,.01),zz/max(rz,.01))
            length=math.sqrt(sum(v*v for v in normal)); norm.append(tuple(v/length for v in normal))
            # Image rows increase downward; glTF UV v=0 is image top.
            v=(y-rings[0][0])/(rings[-1][0]-rings[0][0])
            uv.append((ox+.006+.32*k/n,oy+.01+.48*(1-v)))
    for j in range(len(rings)-1):
        for k in range(n):
            a=start+j*(n+1)+k; b=a+n+1
            idx.extend([a,a+1,b,a+1,b+1,b])
    # End caps: separate normals keep soles/cuffs crisp.
    for j,sign in [(0,-1),(len(rings)-1,1)]:
        y,rx,rz,cz=rings[j]; c=len(p); p.append((0,y,cz)); norm.append((0,sign,0)); uv.append((ox+1/6,oy+.25))
        for k in range(n):
            a=start+j*(n+1)+k; b=a+1
            idx.extend([c,b,a] if sign<0 else [c,a,b])

def ellipsoid(owner,centre,size,tile=0,n=12):
    x,y,z=centre; rx,ry,rz=size
    # Build centred at X=0 then shift new vertices, allowing face/clothing details.
    p=geometry.setdefault(owner,([],[],[],[]))[0]; start=len(p)
    contour(owner,[(y+ry*math.sin(a),max(.002,rx*math.cos(a)),max(.002,rz*math.cos(a)),z) for a in [-math.pi/2,-1.1,-.55,0,.55,1.1,math.pi/2]],tile,n)
    for i in range(start,len(p)): p[i]=(p[i][0]+x,p[i][1],p[i][2])

root=node('ZombieFirstPass'); upper=node('UpperBody',(0,.85,0),root)
# Torso with waist, ribcage, sloped shoulders and collar. Existing waist pivot retained.
contour(upper,[(-.015,.17,.125,.012),(.06,.18,.13,.005),(.18,.205,.14,-.015),(.32,.235,.145,-.035),(.43,.28,.135,-.045),(.49,.18,.11,-.035),(.52,.095,.085,-.03)],1)
hips=node('Hips',(0,-.02,.015),upper)
contour(hips,[(-.085,.155,.12,0),(0,.18,.14,0),(.085,.165,.125,0)],3)
ellipsoid(upper,(.015,.575,-.035),(.075,.105,.075),0)
head=node('Head',(.022,.70,-.047),upper)
# Cheekbones, tapered jaw, brows, nose and ears replace the featureless round head.
contour(head,[(-.19,.055,.065,-.033),(-.155,.083,.085,-.028),(-.09,.108,.099,-.005),(0,.123,.113,0),(.075,.119,.113,.005),(.14,.10,.095,.01),(.18,.045,.045,.01)],2,24)
ellipsoid(head,(0,-.028,-.119),(.031,.060,.038),0)
for s in [-1,1]:
    ellipsoid(head,(s*.126,-.015,.005),(.023,.049,.030),0)
    ellipsoid(head,(s*.068,.069,-.099),(.054,.018,.024),0)
# Sparse scalp, chin wound and collar flap are geometry within their segment.
contour(head,[(.115,.106,.10,.012),(.16,.084,.077,.015),(.184,.035,.034,.012)],5)
for s,label in [(-1,'Left'),(1,'Right')]:
    arm=node(label+'Arm',(s*.32,.45,0),upper)
    contour(arm,[(-.275,.070,.078,0),(-.23,.077,.082,0),(-.12,.092,.096,0),(-.025,.105,.10,0),(.045,.080,.075,0)],1)
    elbow=node('Elbow',(0,-.27,0),arm)
    contour(elbow,[(-.26,.043,.049,-.007),(-.18,.055,.062,0),(-.085,.073,.073,0),(.02,.065,.068,0)],0)
    ellipsoid(elbow,(0,-.286,-.018),(.056,.083,.043),0)
    # Four short fingers and thumb, baked together with forearm: no extra draw nodes.
    for f in range(4): ellipsoid(elbow,((f-1.5)*.023,-.356,-.028),(.012,.038+(f in (1,2))*.007,.017),0,8)
    ellipsoid(elbow,(-s*.061,-.294,-.035),(.018,.047,.021),0,8)
    leg=node(label+'Leg',(s*.13,.82,0),root)
    contour(leg,[(-.36,.075,.086,0),(-.27,.09,.10,0),(-.13,.105,.123,.007),(.045,.10,.12,.012)],3)
    knee=node('Knee',(0,-.36,0),leg)
    contour(knee,[(-.34,.056,.068,.004),(-.26,.065,.077,.009),(-.13,.085,.097,.015),(.025,.075,.086,0)],3)
    ellipsoid(knee,(0,-.39,-.046),(.089,.069,.151),4)
    # Sole and tongue shape; ground contact at y=0 with the current hip/knee spacing.
    contour(knee,[(-.46,.089,.148,-.046),(-.425,.091,.15,-.046),(-.39,.079,.135,-.043)],4)

for owner,(p,no,uv,ix) in geometry.items():
    primitive={'attributes':{'POSITION':acc(p,5126,3),'NORMAL':acc(no,5126,3),'TEXCOORD_0':acc(uv,5126,2)},'indices':acc(ix,5125,1),'material':0}
    meshes.append({'name':nodes[owner]['name']+'Art','primitives':[primitive]})
    visual=node(nodes[owner]['name']+'Art',parent=owner); nodes[visual]['mesh']=len(meshes)-1
image_view=raw(png)
doc={'asset':{'version':'2.0','generator':'war-machine original segmented zombie authoring v1'},'scene':0,'scenes':[{'nodes':[root]}], 'nodes':nodes,'meshes':meshes,'accessors':access,'bufferViews':views,'buffers':[{'byteLength':len(blob)}], 'images':[{'bufferView':image_view,'mimeType':'image/png'}], 'samplers':[{'magFilter':9729,'minFilter':9987,'wrapS':33071,'wrapT':33071}], 'textures':[{'sampler':0,'source':0}], 'materials':[{'name':'ZombieAtlas','pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicFactor':0,'roughnessFactor':.94}}]}
j=json.dumps(doc,separators=(',',':')).encode(); j+=b' '*((-len(j))%4); blob+=b'\0'*((-len(blob))%4)
OUT.write_bytes(struct.pack('<III',0x46546c67,2,28+len(j)+len(blob))+struct.pack('<II',len(j),0x4e4f534a)+j+struct.pack('<II',len(blob),0x004e4942)+blob)
print(f'{OUT.name}: {OUT.stat().st_size} bytes, {len(meshes)} surfaces, {sum(len(v[3])//3 for v in geometry.values())} triangles')
