from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent
source = ROOT.parent.parent / 'codex-pet' / 'bluebell' / 'spritesheet.webp'
atlas = Image.open(source).convert('RGBA')
assert atlas.size == (1536, 2288)
atlas.save(ROOT / 'atlas.png', optimize=True)
frame = atlas.crop((0, 0, 192, 208))
character = frame.crop(frame.getchannel('A').getbbox())
character.thumbnail((224, 224), Image.Resampling.LANCZOS)
icon = Image.new('RGBA', (256, 256))
icon.alpha_composite(character, ((256-character.width)//2, (256-character.height)//2))
icon.save(ROOT / 'pet.ico', sizes=[(16,16), (24,24), (32,32), (48,48), (64,64), (128,128), (256,256)])
print(f'Prepared embedded transparent atlas and icon: {atlas.size}')
