import numpy as np, math, random, json, struct, pathlib
from PIL import Image, ImageDraw, ImageFont

ROOT=pathlib.Path(__file__).resolve().parent.parent
for name in ('Models','Materials','Previews','Source'): (ROOT/name).mkdir(parents=True,exist_ok=True)

class Mesh:
    def __init__(self):
        self.verts=[];self.normals=[];self.colors=[];self.indices=[]
    def triangle(self,a,b,c,color,expected=None):
        a=np.asarray(a,float);b=np.asarray(b,float);c=np.asarray(c,float)
        n=np.cross(b-a,c-a);length=np.linalg.norm(n)
        if length < 0.00001:return
        if expected is not None and np.dot(n,expected)<0:
            b,c=c,b;n=-n
        n/=length
        ix=len(self.verts)
        for v in (a,b,c):
            self.verts.append(v); self.normals.append(n); self.colors.append(color)
        self.indices.extend((ix,ix+1,ix+2))
    def count(self):return len(self.indices)//3

def trunk_xyz(y):
    return np.array([0.042*math.sin(y*.25)+0.011*y/22.5, y, -0.055*math.sin(y*.17)],float)

def create_trunk(rng,n_sides,rings):
    m=Mesh();angle0=rng.uniform(-.13,.13)
    pts=[]
    for y in rings:
        t=np.clip(y/22.4,0,1)
        r=.405*(1-t)**1.18+.045
        if y < 1.4:r*=1+0.40*(1-y/1.4)
        p=trunk_xyz(y)
        ring=[p+np.array([math.cos(angle0+k*math.tau/n_sides)*r,0,math.sin(angle0+k*math.tau/n_sides)*r]) for k in range(n_sides)]
        pts.append(ring)
    palette=np.array([[.31,.255,.204],[.37,.29,.237],[.285,.239,.197],[.34,.265,.218]])
    for j in range(len(rings)-1):
        for k in range(n_sides):
            p0=pts[j][k];p1=pts[j][(k+1)%n_sides];p2=pts[j+1][(k+1)%n_sides];p3=pts[j+1][k]
            col=palette[(k+int(j*.7))%len(palette)] * (1-0.08*float(rings[j]/22.4))
            # outward normal from trunk center; close rings to keep winding consistent.
            normal=(p0+p1+p2+p3)/4 - trunk_xyz((rings[j]+rings[j+1])/2)
            normal[1]=0
            m.triangle(p0,p2,p1,col,expected=normal)
            m.triangle(p0,p3,p2,col,expected=normal)
    # close base and top
    base=trunk_xyz(rings[0]);tip=trunk_xyz(rings[-1]);col=[.32,.26,.22]
    for k in range(n_sides):
        m.triangle(base,pts[0][k],pts[0][(k+1)%n_sides],col,expected=[0,-1,0])
        m.triangle(tip,pts[-1][(k+1)%n_sides],pts[-1][k],col,expected=[0,1,0])
    return m

def foliage_fan(m,angle,y,length,width,rng,n_segments=4,thickness_scale=1.0):
    d=np.array([math.cos(angle),0,math.sin(angle)])
    side=np.array([-math.sin(angle),0,math.cos(angle)])
    left=np.array([.26, .55, .78, 1.0]) if n_segments==4 else np.array([.26,.58,1.0])
    # broad irregular pine branch band, faceted and chunky; not a spherical lollipop.
    widths=[.16,.88,.98,.10] if n_segments==4 else [.18,1.0,.12]
    heights=[.15,.46,.27,.07] if n_segments==4 else [.18,.43,.08]
    bend=rng.uniform(-.20,.20)
    rnd_width=rng.uniform(.83,1.16)
    scale_col=rng.uniform(.86,1.15)
    colors=np.array([[.235,.36,.265],[.285,.43,.30],[.33,.49,.33],[.22,.34,.25],[.30,.43,.31]])
    color=colors[rng.randrange(len(colors))]*scale_col
    sections=[];centers=[]
    for i,t in enumerate(left):
        px=trunk_xyz(y)+d*(length*t)
        py=y + (.15*math.sin(math.pi*t) - .29*t*t)*rng.uniform(.7,1.1)
        center=np.array([px[0],py,px[2]]) + side*(bend*t*width*.23)
        centers.append(center)
        w=width*widths[i]*rnd_width
        h=heights[i]*rng.uniform(.85,1.14)*thickness_scale
        sections.append([center+np.array([0,h/2,0]),center+side*w/2,
                         center+np.array([0,-h/2,0]),center-side*w/2])
    for i in range(len(sections)-1):
        for j in range(4):
            a=sections[i][j];b=sections[i][(j+1)%4]
            c=sections[i+1][(j+1)%4];e=sections[i+1][j]
            expect=(a+b+c+e)/4 - (centers[i]+centers[i+1])/2
            if np.linalg.norm(expect)<0.01:expect=np.array([0,1,0])
            facecolor=color*np.array([1.09,.98,.85,.96])[j]
            m.triangle(a,b,c,facecolor,expect)
            m.triangle(a,c,e,facecolor,expect)
    # Two triangular facets at each end close the mass.
    for end,sgn in ((0,-1),(-1,1)):
        a,b,c,e=sections[end]
        m.triangle(a,b,c,color*.95,d*sgn)
        m.triangle(a,c,e,color*.95,d*sgn)

def foliage_lod3(m,angle,y,length,width,rng):
    d=np.array([math.cos(angle),0,math.sin(angle)])
    side=np.array([-math.sin(angle),0,math.cos(angle)])
    center=trunk_xyz(y)
    p0=center+d*.33 + np.array([0,.36,0]);
    p1=center+d*(length*.65)+side*(width*.50)+np.array([0,.72,0]);
    p2=center+d*length+np.array([0,-.52,0]);
    p3=center+d*(length*.65)-side*(width*.50)+np.array([0,-.42,0]);
    col=np.array([.26,.39,.29])*rng.uniform(.84,1.18)
    m.triangle(p0,p1,p2,col,expected=[0,1,0]);m.triangle(p0,p2,p3,col*.92,expected=[0,1,0]);
    m.triangle(p0,p2,p1,col*.78,expected=[0,-1,0]);m.triangle(p0,p3,p2,col*.80,expected=[0,-1,0])

def build(lod):
    rng=random.Random(620015+lod)
    if lod==1:
        ys=np.linspace(6.20,21.62,16); sides=9;rings=np.array([-.19,0,1.5,3,4.5,6,7.5,9,10.5,12,13.5,15,16.5,18,19.5,21,22.45]);fan_count=11;fan_seg=4
    elif lod==2:
        ys=np.linspace(6.25,21.52,12);sides=7;rings=np.array([-.19,0,2.5,5,7.5,10,12.5,15,18,20,22.45]);fan_count=5;fan_seg=3
    else:
        ys=np.linspace(6.4,21.3,8);sides=5;rings=np.array([-.19,4,10,16,22.45]);fan_count=3;fan_seg=2
    bark=create_trunk(rng,sides,rings);leaves=Mesh()
    for tier,y in enumerate(ys):
        rel=(y-6.2)/(21.62-6.2)
        radius=2.9*(1-rel)**0.85 + .38
        # small gaps and layered canopies—no solid circular cone.
        phase=rng.random()*math.tau + tier*0.18
        for j in range(fan_count):
            a=phase+j*(math.tau/fan_count)+rng.uniform(-.12,.12)
            y2=y+rng.uniform(-.28,.20)
            r=radius*rng.uniform(.82,1.16)
            w=(1.05*(1-rel)**0.5+.25)*rng.uniform(.75,1.20)
            if lod==3:
                foliage_lod3(leaves,a,y2,r,w*2.5,rng)
            else:
                foliage_fan(leaves,a,y2,r,w*(1.55 if lod==2 else 1.0),rng,fan_seg,thickness_scale=(2.6 if lod==2 else 1.0))
    return bark,leaves

def export(lod,bark,leaves):
    base=f'pine_tree_001_a_LOD{lod}';path=ROOT/'Models'/f'{base}.gltf'; binary=bytearray();accessors=[];views=[]
    # Create glTF data as two disjoint material surfaces so foliage can be shader-overridden without affecting bark.
    def store(arr,componentType,name):
        nonlocal binary
        a=np.asarray(arr)
        typ={1:'SCALAR',2:'VEC2',3:'VEC3',4:'VEC4'}[a.shape[1] if a.ndim>1 else 1]
        while len(binary)%4:binary.append(0)
        offset=len(binary);buf=a.tobytes();binary.extend(buf)
        views.append({'buffer':0,'byteOffset':offset,'byteLength':len(buf)})
        ac={'bufferView':len(views)-1,'componentType':componentType,'count':len(a),'type':typ}
        if name=='POSITION':ac['min']=np.min(a,axis=0).tolist();ac['max']=np.max(a,axis=0).tolist()
        accessors.append(ac);return len(accessors)-1
    prims=[]
    for m,mat in ((bark,0),(leaves,1)):
        p=np.array(m.verts,dtype='<f4');n=np.array(m.normals,dtype='<f4');c=np.array(m.colors,dtype='<f4');inds=np.array(m.indices,dtype='<u4')
        attrs={'POSITION':store(p,5126,'POSITION'),'NORMAL':store(n,5126,'NORMAL'),'COLOR_0':store(np.column_stack((c,np.ones(len(c),dtype=np.float32))).astype('<f4'),5126,'COLOR_0')}
        ix=store(inds,5125,'INDICES')
        prims.append({'attributes':attrs,'indices':ix,'material':mat,'mode':4})
    mats=[{'name':'PineTree001_Stylized_Bark','pbrMetallicRoughness':{'baseColorFactor':[1,1,1,1],'metallicFactor':0,'roughnessFactor':0.95},'doubleSided':False},
          {'name':'PineTree001_Stylized_Foliage','pbrMetallicRoughness':{'baseColorFactor':[1,1,1,1],'metallicFactor':0,'roughnessFactor':0.95},'doubleSided':False}]
    obj={'asset':{'version':'2.0','generator':'Final Machine - PineTree001 stylized procedural meshes'},'scene':0,
         'scenes':[{'nodes':[0],'name':'PineTree001'}],
         'nodes':[{'name':base,'mesh':0}], 'meshes':[{'name':base,'primitives':prims}],
         'buffers':[{'byteLength':len(binary),'uri':base+'.bin'}], 'bufferViews':views, 'accessors':accessors,'materials':mats,
         'extras':{'triangleCounts':{'bark':bark.count(),'foliage':leaves.count(),'total':bark.count()+leaves.count()},'heightMeters':22.64}}
    path.write_text(json.dumps(obj,indent=2));path.with_suffix('.bin').write_bytes(binary)
    return obj

assets=[]
for lod in (1,2,3):
    bark,leaves=build(lod);gltf=export(lod,bark,leaves);assets.append((lod,bark,leaves,gltf))
    print('LOD',lod,'bark',bark.count(),'foliage',leaves.count(),'total',bark.count()+leaves.count(),'bounds',gltf['accessors'][0]['min'],gltf['accessors'][0]['max'])

# Mesh preview—project actual generated triangles from a simple 3D camera into a software-rendered illustration.
# This is only a shape/colour preview; it is not an actual Godot rendering.
from PIL import ImageDraw
SW,SH=720,850

def image_from_asset(lod,bark,leaves,camera_az=25):
    img=Image.new('RGB',(SW,SH),(199,218,219))
    draw=ImageDraw.Draw(img)
    draw.ellipse([45,SH-82,SW-45,SH-30],fill=(142,164,148))
    az=math.radians(camera_az)
    u=np.array([math.cos(az),0,-math.sin(az)])
    camdir=np.array([math.sin(az),.19,math.cos(az)])
    camdir/=np.linalg.norm(camdir)
    up=np.cross(camdir,u);up/=np.linalg.norm(up)
    light=np.array([.4,.85,.1]);light/=np.linalg.norm(light)
    scale=29.4
    def project(v):
        center=np.array([0,11.0,0])
        v=v-center
        return SW//2 + float(np.dot(v,u))*scale, SH//2-float(np.dot(v,up))*scale
    tris=[]
    for m in (bark,leaves):
        # Skip true hidden face culling only for solid foliage; z-sort painter approximation.
        xyz=np.asarray(m.verts);norm=np.asarray(m.normals);colors=np.asarray(m.colors)
        for i in range(0,len(xyz),3):
            verts=xyz[i:i+3]
            normal=norm[i]
            if np.dot(normal,camdir)>0.04:continue
            dep=float(np.dot(verts.mean(axis=0),camdir))
            lum=.49+.42*max(0,float(np.dot(normal,light)))
            rgb=np.clip(colors[i]*lum*255,0,255).astype(np.uint8).tolist()
            polygon=[project(v) for v in verts]
            tris.append((dep,polygon,tuple(rgb)))
    for _,p,color in sorted(tris,key=lambda x:-x[0]):draw.polygon(p,fill=color)
    draw.text((30,26),f'PineTree001 — LOD{lod}',fill=(30,54,47))
    return img

for lod,bark,leaves,_ in assets:
    preview=image_from_asset(lod,bark,leaves,36)
    preview.save(ROOT/'Previews'/f'PineTree001_LOD{lod}_Preview.png')

# Build one contact sheet for comparisons.
contact=Image.new('RGB',(720*3,850),(221,229,226));
for i,(lod,bark,leaves,_) in enumerate(assets):contact.paste(image_from_asset(lod,bark,leaves,36),(720*i,0))
contact.save(ROOT/'Previews'/'PineTree001_AllLODs.png')
(ROOT/'Source'/'mesh_stats.json').write_text(json.dumps({f'LOD{lod}':{'bark_triangles':b.count(),'foliage_triangles':l.count(),'total_triangles':b.count()+l.count()} for lod,b,l,_ in assets},indent=2))
