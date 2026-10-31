"""Authoring script for War Machine's tactical low-poly Rifle_001 GLB visual.

Artwork only. +Y up; barrel forward -Z; unit = meter. Scene root/gameplay
muzzle remains a separate Godot Marker3D at (0, .055, -.81).
Uses grouped material primitives to reduce draw calls; no functional internals.
"""
from pathlib import Path
from collections import defaultdict
import math
import numpy as np
import trimesh
from trimesh.visual.material import PBRMaterial

OUT=Path(__file__).resolve().parent.parent / 'Rifle_001_ReferenceStyle.glb'
TRIS=defaultdict(list)
MAT={
 'gunmetal':dict(rgb=(84,91,94),met=.53,rough=.67),
 'graphite':dict(rgb=(48,55,58),met=.42,rough=.76),
 'metal_edge':dict(rgb=(110,118,115),met=.62,rough=.61),
 'olive':dict(rgb=(105,111,87),met=.08,rough=.92),
 'olive_shadow':dict(rgb=(77,86,68),met=.06,rough=.94),
 'rubber':dict(rgb=(33,35,37),met=.01,rough=.97),
 'recess':dict(rgb=(18,23,27),met=.22,rough=.91),
 'steel':dict(rgb=(119,119,111),met=.7,rough=.47),
 'mark':dict(rgb=(138,134,109),met=.05,rough=.94),
}

def tri(mat,a,b,c):
    TRIS[mat].append((tuple(a),tuple(b),tuple(c)))

def quad(mat,a,b,c,d):
    tri(mat,a,b,c);tri(mat,a,c,d)

def rot_x(p,degree):
    a=math.radians(degree);x,y,z=p
    return np.array([x,math.cos(a)*y-math.sin(a)*z,math.sin(a)*y+math.cos(a)*z])

def rotate(p,rot):
    if not rot:return np.array(p,dtype=float)
    a=rot_x(p,rot[0]);x,y,z=a
    if len(rot)>1 and rot[1]:
        t=math.radians(rot[1]);x,z=math.cos(t)*x+math.sin(t)*z,-math.sin(t)*x+math.cos(t)*z
    if len(rot)>2 and rot[2]:
        t=math.radians(rot[2]);x,y=math.cos(t)*x-math.sin(t)*y,math.sin(t)*x+math.cos(t)*y
    return np.array((x,y,z))

def section8(z,rx,ry,cy=0,cx=0, bevel=.14):
    x=rx; y=ry; c=min(x,y)*bevel
    return [np.array((cx+X,cy+Y,z)) for X,Y in [(-x+c,-y),(x-c,-y),(x,-y+c),(x,y-c),(x-c,y),(-x+c,y),(-x,y-c),(-x,-y+c)]]

def loft(mat,sections,rotation=None,translate=(0,0,0)):
    sections=[section8(*spec) for spec in sections]
    def tr(v):return rotate(v,rotation)+np.array(translate,dtype=float)
    centreA=np.mean(sections[0],axis=0);centreB=np.mean(sections[-1],axis=0)
    for i in range(8):
        j=(i+1)%8
        tri(mat,tr(centreA),tr(sections[0][j]),tr(sections[0][i]))
        tri(mat,tr(centreB),tr(sections[-1][i]),tr(sections[-1][j]))
    for p in range(len(sections)-1):
        ra,rb=sections[p],sections[p+1]
        for i in range(8):
            j=(i+1)%8
            quad(mat,tr(ra[i]),tr(ra[j]),tr(rb[j]),tr(rb[i]))

def box(mat,center,size,chamfer=.08,rotation=None):
    cx,cy,cz=center; sx,sy,sz=size
    x=sx/2;y=sy/2;z=sz/2
    d=min(z,.016,max(0.0002,min(sx,sy)*.16))
    # Ends inset subtly to create angled hard-surface chamfers.
    f=1-min(.24,chamfer*1.4)
    loft(mat,[(cz-z,x*f,y*f,cy,cx),(cz-z+d,x,y,cy,cx),
              (cz+z-d,x,y,cy,cx),(cz+z,x*f,y*f,cy,cx)],rotation)

def panel(mat, side, verts, thickness=.006):
    """Polygon on one receiver side; vertices=(Y,Z). Closed extruded mesh."""
    x=side;dx=thickness
    a=[np.array([x,y,z]) for y,z in verts]
    b=[np.array([x+dx,y,z]) for y,z in verts]
    # face winding is corrected from panel's intended +X/-X facing
    for points,sgn in [(a,-1),(b,1)]:
        cx=np.mean(points,axis=0)
        for i in range(len(points)):
            j=(i+1)%len(points)
            p=points[i];q=points[j]
            n=np.cross(p-cx,q-cx)[0]
            if n*sgn>0: tri(mat,cx,p,q)
            else:tri(mat,cx,q,p)
    for i in range(len(a)):
        j=(i+1)%len(a)
        quad(mat,a[i],b[i],b[j],a[j])

def tube(mat,center,radius,length,axis='z',segments=12,inner_radius=0):
    cx,cy,cz=center
    def v(angle,ring,level):
        a=angle*2*math.pi/segments
        if axis=='z':return np.array([cx+ring*math.cos(a),cy+ring*math.sin(a),cz+level])
        if axis=='x':return np.array([cx+level,cy+ring*math.cos(a),cz+ring*math.sin(a)])
        return np.array([cx+ring*math.cos(a),cy+level,cz+ring*math.sin(a)])
    z0=-length/2;z1=length/2
    for i in range(segments):
        j=(i+1)%segments
        a,b,c,d=v(i,radius,z0),v(j,radius,z0),v(j,radius,z1),v(i,radius,z1)
        # Correct outward orientation with radial check.
        mid=(a+b+c)/3-np.array(center)
        ax={'z':2,'x':0,'y':1}[axis];mid[ax]=0
        cross=np.cross(b-a,c-a)
        if np.dot(cross,mid)<0:quad(mat,a,d,c,b)
        else:quad(mat,a,b,c,d)
        if inner_radius>0:
            aa,bb,cc,dd=v(i,inner_radius,z0),v(j,inner_radius,z0),v(j,inner_radius,z1),v(i,inner_radius,z1)
            quad(mat,aa,dd,cc,bb)
            quad(mat,aa,bb,b,a)
            quad(mat,d,c,cc,dd)
        else:
            front=np.array(center);front[ax]+=z0
            back=np.array(center);back[ax]+=z1
            tri(mat,front,b,a);tri(mat,back,d,c)

# ------------------------------------------------------------
# REAR STOCK: shaped butt / elevated cheek comb and open lower truss
# ------------------------------------------------------------
loft('graphite',[(.060,.069,.069,.020,0),(.075,.082,.075,.017,0),(.240,.095,.070,.015,0),(.310,.092,.078,.012,0)],translate=(0,0,0))
box('olive_shadow',(0,.079,.215),(.174,.054,.27),.11)
box('olive',(0,.100,.229),(.175,.029,.25),.10)
box('graphite',(0,-.070,.220),(.102,.033,.260),.09)
box('gunmetal',(0,-.037,.337),(.176,.157,.040),.16)
box('rubber',(0,-.032,.382),(.210,.231,.044),.22)
box('graphite',(0,-.027,.408),(.168,.163,.018),.16)
box('metal_edge',(0,.061,.371),(.171,.012,.014),.13)
# Stock diagonal side struts, deliberate narrow negative-space openings.
for x in (-.066,.066):
    box('graphite',(x,-.030,.254),(.028,.034,.185),.10,rotation=(16,0,0))
    box('gunmetal',(x,-.058,.337),(.021,.068,.045),.13)
# Small stock latch and rear cheek rest seams
box('recess',(.092,.054,.175),(.008,.029,.075),.08)
box('metal_edge',(.101,.045,.135),(.006,.019,.024),.12)

# ------------------------------------------------------------
# RECEIVER: shaped upper / lower, angled rear body, selector, bolt
# ------------------------------------------------------------
loft('gunmetal',[(-.363,.101,.069,.056,0),(-.351,.110,.078,.063,0),
                 (-.095,.116,.088,.047,0),(.060,.112,.082,.037,0),(.075,.089,.070,.030,0)])
loft('graphite',[(-.353,.106,.064,-.045,0),(-.330,.113,.071,-.034,0),
                 (.026,.113,.076,-.041,0),(.061,.090,.060,-.038,0)])
# Upper edge facets along left/right rail housing
for x in (-.105,.105):
    box('metal_edge',(x,.105,-.122),(.017,.018,.31),.14)
# Enlarged rear socket, faceted profile to match target reference
loft('graphite',[(.025,.105,.093,.048,0),(.045,.119,.099,.051,0),
                 (.108,.126,.103,.049,0),(.128,.110,.079,.044,0)])
# Brass deflector / bump to right of ejection port
box('gunmetal',(.121,.070,-.235),(.035,.043,.047),.12)
# Dark opening and steel bolt on RIGHT side
panel('recess',.115,[(.058,-.253),(.110,-.254),(.116,-.100),(.051,-.095)],.007)
panel('steel',.125,[(.070,-.221),(.096,-.221),(.097,-.128),(.069,-.128)],.006)
box('gunmetal',(.135,.074,-.168),(.010,.021,.073),.11)
# Side receiver panel bevels and body stamp
for side in (-1,1):
    x=side*.117
    panel('graphite',x,[(-.076,-.303),(-.098,-.231),(-.089,-.066),(-.034,-.048),(-.010,-.149),(-.030,-.292)],.007*side)
    # receiver captive pin heads
    for z in (-.282,-.035):
        tube('metal_edge',(side*.130,-.063,z),.012,.004,axis='x',segments=10)
    # trigger selector and bolt release mark
    tube('recess',(side*.130,-.013,.000),.019,.009,axis='x',segments=12)
    box('metal_edge',(side*.137,-.013,-.006),(.014,.009,.034),.09,rotation=(0,0,18))
# Large low-profile olive receiver shroud brings the grounded two-tone look
# of the reference into the first-person visible rear half of the gun.
for side in (-1,1):
    panel('olive_shadow',side*.132,[(-.076,-.282),(-.086,-.198),(-.072,-.073),(.021,-.044),(.052,-.078),(.044,-.248)],.005*side)
    panel('olive',side*.138,[(-.064,-.272),(-.074,-.202),(-.061,-.090),(.015,-.058),(.040,-.088),(.035,-.239)],.005*side)
    # Angled recessed polymer slot at the bottom, retained by small torx-looking pins.
    panel('recess',side*.146,[(-.056,-.238),(-.046,-.223),(-.035,-.132),(-.052,-.145)],.003*side)
    for z in (-.250,-.100):
        tube('recess',(side*.150,.004,z),.008,.005,axis='x',segments=10)

# small serial designation plate (no text textures)
box('olive_shadow',(-.126,.021,-.025),(.009,.027,.092),.08)
for z in (-.055,-.039,-.022,-.005):box('mark',(-.132,.026,z),(.003,.003,.007),.05)

# ------------------------------------------------------------
# LOWER: pistol grip, trigger bow, angular magazine
# ------------------------------------------------------------
loft('gunmetal',[(-.246,.075,.055,-.083,0),(-.234,.085,.063,-.100,0),
                 (-.062,.079,.065,-.093,0),(-.037,.068,.052,-.077,0)])
box('rubber',(0,-.144,.065),(.090,.201,.091),.16,rotation=(-16,0,0))
box('olive_shadow',(0,-.153,.072),(.083,.174,.080),.14,rotation=(-16,0,0))
for x in (-.040,.040):
    for y in (-.095,-.126,-.158,-.189):
        box('graphite',(x,y,.094+(y+.14)*.20),(.009,.006,.060),.08,rotation=(-16,0,0))
# Trigger and real opening under upper receiver
box('graphite',(0,-.130,-.082),(.012,.077,.018),.10,rotation=(-12,0,0))
box('gunmetal',(0,-.161,-.114),(.017,.020,.119),.09)
box('gunmetal',(0,-.141,-.175),(.020,.070,.013),.09)
box('gunmetal',(0,-.128,-.040),(.021,.063,.013),.09)
# Magazine well and angled magazine body
loft('graphite',[(-.307,.069,.038,-.091,0),(-.294,.086,.045,-.108,0),
                 (-.165,.088,.047,-.114,0),(-.157,.070,.043,-.112,0)])
box('olive_shadow',(0,-.187,-.238),(.120,.183,.100),.12,rotation=(-9,0,0))
box('olive',(0,-.180,-.237),(.116,.171,.094),.18,rotation=(-9,0,0))
box('graphite',(0,-.281,-.227),(.132,.026,.106),.10,rotation=(-9,0,0))
for side in (-1,1):
    for y in (-.144,-.173,-.203,-.233):
        box('olive_shadow',(side*.062,y,-.236),(.008,.012,.078),.08,rotation=(-9,0,0))

# ------------------------------------------------------------
# HANDGUARD: tapered metal skeleton, olive accessory panels, slots
# ------------------------------------------------------------
loft('graphite',[(-.708,.076,.065,.046,0),(-.689,.095,.078,.048,0),
                 (-.376,.112,.090,.045,0),(-.351,.111,.076,.045,0)])
loft('gunmetal',[(-.687,.075,.058,.059,0),(-.668,.093,.073,.057,0),
                 (-.385,.103,.076,.057,0)])
box('graphite',(0,-.019,-.522),(.135,.029,.327),.13)
for side in (-1,1):
    x=side*.111
    # Olive layered angular plate, with clipped ends, dark gasket behind
    panel('olive_shadow',x,[(-.011,-.676),(.075,-.680),(.082,-.375),(-.016,-.378),(-.037,-.420),(-.035,-.633)],.010*side)
    panel('olive',x+side*.009,[(-.003,-.656),(.062,-.655),(.075,-.403),(-.012,-.406),(-.026,-.431),(-.023,-.629)],.004*side)
    # Vent slots run lengthwise, glossy dark recesses
    for z in (-.604,-.536,-.468):
        box('recess',(side*.127,.026,z),(.008,.030,.041),.10)
        box('metal_edge',(side*.131,.041,z),(.004,.005,.040),.10)
    # Large attachment pocket edge near rear with two screws
    box('graphite',(side*.124,-.017,-.403),(.012,.035,.045),.11)
    for z in (-.624,-.414):
        tube('metal_edge',(side*.136,-.016,z),.009,.005,axis='x',segments=10)
# Front locking ring and gas-block split
loft('gunmetal',[(-.698,.089,.083,.045,0),(-.685,.092,.087,.045,0),
                 (-.658,.092,.087,.045,0),(-.647,.082,.076,.045,0)])

# ------------------------------------------------------------
# MIL-STD TOP RAIL: central spine + many teeth, no holographic optic
# ------------------------------------------------------------
box('recess',(0,.140,-.324),(.077,.030,.655),.09)
box('gunmetal',(0,.155,-.325),(.091,.012,.620),.06)
for n in range(22):
    z=-.634+n*.0265
    box('graphite',(0,.169,z),(.102,.024,.016),.11)
    box('metal_edge',(0,.182,z),(.082,.004,.012),.06)
# Rail side detail and screw heads
for side in (-1,1):
    box('gunmetal',(side*.043,.149,-.329),(.012,.019,.639),.08)

# ------------------------------------------------------------
# BARREL / GAS SYSTEM / MUZZLE; bore centered at approved game Muzzle marker
# ------------------------------------------------------------
tube('gunmetal',(0,.055,-.700),.021,.204,axis='z',segments=16)
tube('steel',(0,.055,-.682),.029,.050,axis='z',segments=12)
tube('graphite',(0,.055,-.766),.033,.062,axis='z',segments=12)
tube('gunmetal',(0,.055,-.798),.029,.022,axis='z',segments=12)
# Front inner bore is a shallow open-looking dark ring, no internals
tube('recess',(0,.055,-.807),.015,.004,axis='z',segments=16)
for side in (-1,1):
    box('recess',(side*.027,.055,-.789),(.007,.009,.023),.06)

# ------------------------------------------------------------
# COMPACT IRON SIGHTS instead of holographic sight
# Rear folding notch has an unobstructed centre; front narrow post with ears
# ------------------------------------------------------------
box('gunmetal',(0,.168,-.052),(.068,.027,.049),.14)
for side in (-1,1):
    box('graphite',(side*.025,.192,-.050),(.012,.033,.029),.12)
    box('metal_edge',(side*.025,.208,-.050),(.014,.006,.027),.10)
# rear folded mount catches
box('recess',(0,.180,-.054),(.019,.008,.025),.09)
box('graphite',(0,.141,-.682),(.082,.026,.049),.10)
for side in (-1,1):
    box('gunmetal',(side*.032,.187,-.682),(.011,.067,.024),.12)
    box('metal_edge',(side*.032,.220,-.682),(.014,.008,.025),.11)
box('recess',(0,.190,-.682),(.009,.062,.013),.10)
# Front sight post slightly lower than ear tops
box('metal_edge',(0,.224,-.682),(.012,.008,.014),.06)

# ------------------------------------------------------------
# OPTIONAL small grounded engraving accents (no emissive sci-fi light strips)
# ------------------------------------------------------------
for side in (-1,1):
    box('mark',(side*.133,-.026,-.072),(.003,.005,.055),.06)

# Combine geometry by material to minimize draw calls. Preserve sharp edges.
scene=trimesh.Scene()
for name,triangles in TRIS.items():
    arr=np.asarray(triangles,dtype=np.float32)
    verts=arr.reshape(-1,3)
    faces=np.arange(len(verts),dtype=np.int64).reshape(-1,3)
    mesh=trimesh.Trimesh(vertices=verts,faces=faces,process=False,validate=False)
    props=MAT[name]
    rgb=props['rgb']
    material=PBRMaterial(
        name=name, baseColorFactor=[*tuple(v/255.0 for v in rgb),1.0],
        metallicFactor=props['met'],roughnessFactor=props['rough'], doubleSided=False
    )
    mesh.visual=trimesh.visual.TextureVisuals(material=material)
    scene.add_geometry(mesh,node_name=f'Mat_{name}',geom_name=f'Rifle_{name}')

OUT.write_bytes(scene.export(file_type='glb'))
loaded=trimesh.load(str(OUT),force='scene')
tri_count=sum(len(g.faces) for g in loaded.geometry.values())
print('GLB exported:',OUT,'bytes',OUT.stat().st_size,'primitive groups',len(loaded.geometry),'triangles',tri_count)
print('Bounds (xyz)',loaded.bounds)
for gname,g in loaded.geometry.items():
 print(gname,len(g.faces),getattr(g.visual.material,'name',None))
