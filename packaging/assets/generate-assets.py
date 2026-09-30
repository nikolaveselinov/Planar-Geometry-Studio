"""Regenerate the native installer artwork. Requires Pillow; no network access."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent
scale = 3
icon = Image.new("RGBA", (512 * scale, 512 * scale))
draw = ImageDraw.Draw(icon)
draw.rounded_rectangle((16*scale, 16*scale, 496*scale, 496*scale), 112*scale, fill="#101829")
draw.ellipse((103*scale, 121*scale, 409*scale, 427*scale), outline="#473b8b", width=5*scale)
vertices = [(256*scale,110*scale),(100*scale,382*scale),(412*scale,382*scale)]
for start, end in [(vertices[0],(256*scale,382*scale)),(vertices[1],(334*scale,246*scale)),(vertices[2],(178*scale,246*scale))]:
    for index in range(0,24,2):
        a, b = index/24, (index+1)/24
        draw.line([(start[0]+(end[0]-start[0])*a,start[1]+(end[1]-start[1])*a),
                   (start[0]+(end[0]-start[0])*b,start[1]+(end[1]-start[1])*b)], fill="#8c82bc", width=4*scale)
draw.line(vertices+[vertices[0]], fill="#a99eff", width=16*scale, joint="curve")
for x,y in vertices+[(256*scale,292*scale)]:
    draw.ellipse((x-13*scale,y-13*scale,x+13*scale,y+13*scale), fill="#e7defe")
icon = icon.resize((512,512),Image.Resampling.LANCZOS)
icon.save(root/"studio.png")
icon.save(root/"studio.ico",sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
small = Image.new("RGB",(55,55),"#0a0f1d")
badge=icon.resize((48,48),Image.Resampling.LANCZOS)
small.paste(badge,(3,3),badge)
small.save(root/"wizard-small.bmp")
wizard=Image.new("RGB",(164,314),"#0a0f1d")
d=ImageDraw.Draw(wizard)
for y in range(314):
    d.line((0,y,164,y), fill=(10+int(y/314*6),15+int(y/314*9),29+int(y/314*13)))
badge=icon.resize((134,134),Image.Resampling.LANCZOS)
wizard.paste(badge,(15,50),badge)
font=ImageFont.load_default(size=14)
label=ImageFont.load_default(size=10)
d.text((24,208),"PLANAR",font=font,fill="#f7f8fc")
d.text((24,230),"GEOMETRY",font=font,fill="#f7f8fc")
d.text((24,252),"STUDIO",font=font,fill="#a99eff")
d.line((24,286,140,286),fill="#6d5ef7",width=2)
wizard.save(root/"wizard.bmp")
