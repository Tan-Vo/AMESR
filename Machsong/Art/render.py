"""Render the original textured sprites for the Mach Song rescue mission."""

from pathlib import Path
import math
import random
import uuid

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


OUT = Path(__file__).resolve().parents[1] / "Assets/Resources/MachSong"


def mix(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


def smooth(image, size):
    return image.resize(size, Image.Resampling.LANCZOS)


def save(name, image):
    path = OUT / name
    if path.suffix == ".jpg":
        image.convert("RGB").save(path, quality=92, subsampling=0, optimize=True)
    else:
        image.save(path, optimize=True)
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "mach-song/art/" + name).hex
    path.with_name(path.name + ".meta").write_text(f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureType: 0
  textureShape: 1
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  alphaIsTransparency: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
""", encoding="utf-8")


def nebula(seed, title=False):
    w, h = 1760, 990
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:1:complex(h), 0:1:complex(w)]
    field = np.zeros((h,w),dtype=np.float32)
    for grid, amp in ((12,.63), (26,.31), (61,.17), (141,.085)):
        noise = Image.fromarray(rng.integers(0,255,(grid,grid),dtype=np.uint8))
        field += amp * np.asarray(noise.resize((w,h),Image.Resampling.BICUBIC))/255
    cx, cy = (.74,.43) if title else (.63,.51)
    distance = ((x-cx)*1.6)**2+((y-cy)*1.23)**2
    vein = np.exp(-13*(y-.57+.28*(x-.47)+.19*np.sin(x*5))**2)
    plume = (np.maximum(field-.44,0)*2.0)**1.5 * vein
    halo = np.exp(-distance*8)
    rim = np.exp(-((np.sqrt(distance)-.33)*8)**2)*(.38 + .62*field)
    blue = plume*1.45 + halo*.35 + field*.17
    brass = rim*plume*.85 + np.exp(-distance*16)*.19
    rgb = np.stack((6+31*blue+108*brass,15+76*blue+53*brass,24+99*blue+38*brass),axis=2)
    rgb += rng.normal(0,1.45,(h,w,1))
    scene = Image.fromarray(np.uint8(np.clip(rgb,0,255)),mode="RGB").convert("RGBA")
    painter = ImageDraw.Draw(scene)
    pseudo = random.Random(seed)
    for i in range(440):
        sx, sy = pseudo.randrange(w), pseudo.randrange(h)
        intensity = pseudo.randint(17,97)
        painter.point((sx,sy),fill=(28+intensity,48+intensity,62+intensity))
        if i%37==0:
            painter.line((sx-2,sy,sx+2,sy),fill=(124,168,173))
            painter.line((sx,sy-2,sx,sy+2),fill=(147,181,184))
    return scene


def living_ribs(scene, title=False):
    w,h = scene.size
    layer = Image.new("RGBA", scene.size)
    d = ImageDraw.Draw(layer)
    origin_x,origin_y = (w*.735,h*.47) if title else (w*.68,h*.57)
    for i in reversed(range(11)):
        factor = 1.19**i
        x = origin_x + i*29
        y = origin_y - i*9
        rx, ry = 105*factor,136*factor
        box = (x-rx,y-ry,x+rx,y+ry)
        d.arc(box,215,542,fill=(5,12,20,235),width=max(7,round(12*factor)))
        d.arc(box,219,384,fill=(72,124,120,132),width=max(1,round(2.6*factor)))
        d.arc(box,29,126,fill=(184,111,87,75),width=max(2,round(2.1*factor)))
        for dot in range(7):
            angle = math.radians(225 + 15*dot)
            sx = x+rx*math.cos(angle)
            sy = y+ry*math.sin(angle)
            size = 1.6*factor
            d.ellipse((sx-size,sy-size,sx+size,sy+size),fill=(90,205,179,114))
    scene.alpha_composite(layer)
    glow = Image.new("RGBA",scene.size)
    g = ImageDraw.Draw(glow)
    for r, alpha in ((146,27),(93,25),(62,26),(39,25)):
        g.ellipse((origin_x-r,origin_y-r,origin_x+r,origin_y+r),fill=(72,210,175,alpha))
    scene.alpha_composite(glow.filter(ImageFilter.GaussianBlur(28)))
    return scene


def silhouettes(scene):
    w,h = scene.size
    layer = Image.new("RGBA",scene.size)
    d = ImageDraw.Draw(layer)
    rng = random.Random(53)
    for side in (0,1):
        base = h if side else 0
        sign = -1 if side else 1
        line = [(0,base)]
        for x in range(0,w+100,62):
            y = base+sign*(30+rng.randint(20,100)+95*max(0,math.cos(x/220+side)))
            line.append((x,y))
        d.polygon(line+[(w,base)],fill=(4,10,16,230))
        d.line(line[1:],fill=(58,93,94,143),width=2)
    for i in range(17):
        x,y = rng.randrange(w),rng.randrange(145,h-175)
        r = rng.randrange(6,26)
        d.polygon([(x-r,y+r//2),(x-r//3,y-r),(x+r,y-r//4),(x+r//2,y+r//2)],
                  fill=(12,27,39,180))
    scene.alpha_composite(layer)


def pilot(boost, frame):
    img = Image.new("RGBA",(420,360))
    d = ImageDraw.Draw(img)
    t = frame / 12 * math.tau
    y = round(6*math.sin(t))
    if boost:
        length = round(77+21*math.sin(t*2))
        d.polygon([(126,132+y),(126-length,161+y),(125,188+y)],"#2d5067")
        d.polygon([(121,138+y),(137-length,161+y),(120,177+y)],"#368c9e")
        d.polygon([(121,148+y),(149-length,161+y),(122,170+y)],"#d8fff0")
    suit = [(146,112+y),(181,96+y),(216,104+y),(255,131+y),(284,142+y),
            (279,183+y),(216,178+y),(212,208+y),(193,255+y),(167,263+y),
            (149,248+y),(169,201+y),(157,191+y),(132,207+y),(104,218+y),
            (91,194+y),(124,174+y),(131,139+y)]
    d.polygon(suit,fill="#e7e6cf",outline="#112733",width=6)
    d.polygon([(139,130+y),(182,111+y),(213,127+y),(229,169+y),(209,189+y),
               (174,179+y),(147,193+y),(130,185+y)],"#b0bab0")
    d.polygon([(151,143+y),(194,120+y),(219,134+y),(212,175+y),(181,173+y),
               (154,183+y)],"#46676a")
    d.line([(152,139+y),(192,116+y),(218,129+y)],fill="#dcebd7",width=5)
    d.rounded_rectangle((118,105+y,154,176+y),radius=12,fill="#db9660",outline="#162a35",width=5)
    d.line((129,118+y,128,159+y),fill="#ffe3a1",width=8)
    d.polygon([(154,196+y),(180,205+y),(159,245+y),(139,246+y),(129,233+y)],"#354b54")
    d.polygon([(196,204+y),(212,213+y),(202,254+y),(187,268+y),(158,264+y),
               (156,248+y),(175,246+y)],"#374c52")
    d.line([(175,246+y),(196,251+y)],fill="#f2a36d",width=8)
    d.line([(130,227+y),(151,235+y)],fill="#e79e69",width=7)
    d.rounded_rectangle((197,71+y,310,165+y),radius=35,fill="#dfdfc6",outline="#142634",width=6)
    d.pieslice((212,68+y,313,170+y),277,102,fill="#303d49")
    d.rounded_rectangle((240,87+y,308,147+y),radius=23,fill="#173746",outline="#1c2c38",width=5)
    d.polygon([(251,94+y),(286,93+y),(298,107+y),(269,123+y),(251,119+y)],"#71c8c5")
    d.line([(258,97+y),(276,97+y),(289,102+y)],fill="#d0fff1",width=4)
    d.line([(221,82+y),(253,77+y),(279,80+y)],fill="#fff3bc",width=5)
    d.ellipse((213,113+y,233,134+y),fill="#b27159",outline="#283b44",width=4)
    arm = round(3*math.sin(t+.7))
    d.polygon([(180,147+y),(204,148+y),(244,172+y+arm),(278,167+y+arm),
               (288,184+y+arm),(272,197+y+arm),(239,197+y+arm),(185,176+y)],
              fill="#dde2cd",outline="#233943",width=5)
    d.rounded_rectangle((273,168+y+arm,311,192+y+arm),radius=7,fill="#527879",
                        outline="#253643",width=4)
    d.line((291,175+y+arm,309,175+y+arm),fill="#d5fff0",width=5)
    for px,py in ((181,134),(219,139),(157,169),(191,200)):
        d.ellipse((px,py+y,px+6,py+6+y),fill="#dda568")
    return smooth(img,(280,240))


def pod():
    img = Image.new("RGBA",(600,390))
    d = ImageDraw.Draw(img)
    outline = [(34,191),(67,112),(150,79),(435,74),(505,112),(552,162),
               (563,221),(512,281),(425,314),(153,315),(75,276)]
    d.polygon(outline,fill="#75847e",outline="#19343f",width=9)
    d.polygon([(48,190),(85,122),(159,93),(430,89),(493,122),(531,164),(531,226),
               (476,256),(179,264),(86,244)],fill="#e9dfbb")
    d.polygon([(50,193),(85,220),(161,245),(481,240),(552,203),(512,281),
               (425,314),(150,315),(74,271)],fill="#697d76")
    d.polygon([(87,135),(166,102),(425,103),(488,129),(520,159),(527,193),
               (492,215),(424,229),(171,228),(92,206),(72,171)],fill="#24434c")
    d.rounded_rectangle((153,106,444,216),radius=47,fill="#16343f",outline="#7fa59e",width=7)
    d.polygon([(172,124),(231,111),(403,119),(416,138),(356,181),(192,195),(169,183)],
              fill="#3f8588")
    d.polygon([(175,134),(241,113),(379,117),(347,137),(211,151),(177,175)],fill="#97d5c3")
    d.ellipse((278,127,332,181),fill="#366370")
    d.polygon([(270,190),(289,161),(329,157),(358,185),(337,207),(287,209)],fill="#325262")
    d.polygon([(79,123),(122,104),(139,121),(98,151)],fill="#df8c5a")
    d.polygon([(443,106),(480,123),(513,155),(482,160),(456,135)],fill="#e08959")
    d.line([(132,276),(191,291),(423,291),(489,267)],fill="#bec9ab",width=5)
    d.line([(164,94),(321,91),(425,94)],fill="#fff0c2",width=6)
    d.rounded_rectangle((211,253,397,277),radius=10,fill="#253e46")
    for i in range(8):
        color = "#7dedcd" if i in (0,1,2,6) else "#466369"
        d.rectangle((224+i*20,261,236+i*20,268),fill=color)
    d.ellipse((29,170,88,220),fill="#d0b582",outline="#294652",width=6)
    d.ellipse((42,181,68,208),fill="#59d5c0",outline="#35525b",width=5)
    d.ellipse((522,154,555,175),fill="#ef9b66")
    return smooth(img,(480,312))


def armor(seed, variant=0):
    img = Image.new("RGBA",(544,320))
    d = ImageDraw.Draw(img)
    rng = random.Random(seed)
    metal = (63,88,96) if variant == 0 else (75,91,104)
    lattice = [(16,96),(59,43),(491,40),(526,70),(522,243),(465,278),(81,281),
               (18,243)]
    d.polygon(lattice,fill="#263d48",outline="#101d2a",width=7)
    d.polygon([(31,105),(77,62),(477,61),(506,84),(505,230),(461,254),(89,258),
               (33,228)],fill=metal,outline="#85a2a3",width=3)
    d.polygon([(32,178),(82,229),(458,236),(507,208),(505,230),(461,254),
               (89,258),(33,228)],fill="#253c48")
    for i in range(7):
        x=60+i*65
        indent = rng.randrange(-11,13)
        d.polygon([(x+indent,78),(x+52,85),(x+49,218),(x-7,224)],
                  fill=mix(metal,(126,142,136),.08+.13*(i%3)),outline="#203945",width=2)
        d.line((x+indent,80,x+52,85),fill="#9aa9a0",width=2)
        if i in (0,2,5):
            for j in range(6):
                d.line((x+5,113+j*10,x+29,113+j*10),fill="#273a49",width=4)
        for y in (96,209):
            d.ellipse((x+27,y,x+35,y+8),fill="#bdd1b5",outline="#2c4450",width=2)
    for i in range(245):
        x,y = rng.randrange(62,477),rng.randrange(76,227)
        d.line((x,y,x+rng.randrange(1,13),y),fill=rng.choice(("#56757a","#455e67","#79928b")),width=1)
    for i in range(8):
        x=91+i*49
        color = "#e2b277" if i%3==0 else "#344851"
        d.polygon([(x,231),(x+20,231),(x+13,241),(x-7,241)],fill=color)
    d.line((71,62,476,62),fill="#b0dbcb",width=3)
    return smooth(img,(496,292))


def beacon():
    img = Image.new("RGBA",(384,384))
    d = ImageDraw.Draw(img)
    d.polygon([(61,192),(96,92),(173,55),(258,76),(316,147),(315,222),
               (268,294),(157,319),(85,275)],fill="#364c5a",outline="#191e2d",width=7)
    d.ellipse((101,92,285,279),fill="#7f9791",outline="#2c5561",width=14)
    d.ellipse((124,114,264,257),fill="#102d3c",outline="#d2dfbb",width=6)
    for r,c in ((59,"#31585e"),(49,"#386f6f"),(39,"#3f8984"),(31,"#66b7a7"),
                (20,"#91e0be"),(9,"#eaffd4")):
        d.ellipse((194-r,186-r,194+r,186+r),fill=c)
    for a in range(0,360,45):
        x,y=194+88*math.cos(math.radians(a)),186+88*math.sin(math.radians(a))
        d.ellipse((x-5,y-5,x+5,y+5),fill="#ccad72")
    return smooth(img,(256,256))


def ring():
    extent = 512
    y,x = np.mgrid[-1:1:complex(extent),-1:1:complex(extent)]
    r = np.sqrt(x*x+y*y)
    a = np.exp(-((r-.842)/.022)**2)*.75 + np.exp(-((r-.815)/.085)**2)*.31
    rgba = np.empty((extent,extent,4),dtype=np.uint8)
    rgba[:,:,:3] = (202,255,232)
    rgba[:,:,3] = np.uint8(np.clip(a,0,1)*255)
    return Image.fromarray(rgba)


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    gradient = Image.new("RGBA",(256,2))
    for x in range(256):
        opacity = round(222 * (1-max(0,min(1,(x/255-.23)/.51)))**1.55)
        for y in range(2):
            gradient.putpixel((x,y),(5,14,23,opacity))
    save("menu_gradient.png",gradient)
    for name, seed, menu in (("wreck.jpg",8026,False),("title.jpg",2297,True)):
        sky = living_ribs(nebula(seed,menu),menu)
        silhouettes(sky)
        if menu:
            icon = pilot(False,1).resize((480,411),Image.Resampling.LANCZOS)
            capsule = pod().resize((536,348),Image.Resampling.LANCZOS)
            sky.alpha_composite(capsule.rotate(12,resample=Image.Resampling.BICUBIC,expand=True),(1127,517))
            sky.alpha_composite(icon.rotate(13,resample=Image.Resampling.BICUBIC,expand=True),(925,220))
            flare = Image.new("RGBA",sky.size)
            d = ImageDraw.Draw(flare)
            d.line((1158,459,1309,555,1370,621),fill=(135,236,207,138),width=3)
            sky.alpha_composite(flare.filter(ImageFilter.GaussianBlur(5)))
            sky.alpha_composite(flare)
        save(name,sky)
    for boost,name in ((False,"pilot_idle.png"),(True,"pilot_boost.png")):
        atlas = Image.new("RGBA",(4*280,3*240))
        for i in range(12):
            atlas.paste(pilot(boost,i),((i%4)*280,(i//4)*240))
        save(name,atlas)
    for name,art in (("capsule.png",pod()),("plate.png",armor(357)),
                     ("armor.png",armor(267,1)),("receiver.png",beacon()),("pulse.png",ring())):
        save(name,art)


if __name__ == "__main__":
    main()
