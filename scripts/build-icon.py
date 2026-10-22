# /// script
# requires-python = ">=3.10"
# dependencies = ["resvg-py"]
# ///
# Genera EyeReminder/Assets/EyeReminder.ico a partir de los SVG de design/icon:
#   uv run scripts/build-icon.py
# Cada tamaño se renderiza desde el vector (no se reduce el de 256). Los tamaños de la bandeja
# (16 y 20) usan la versión simplificada, sin insignia de reloj, para que se lea en pocos píxeles.
import struct
from pathlib import Path

import resvg_py

root = Path(__file__).resolve().parent.parent
full = (root / "design/icon/EyeReminder.svg").read_text(encoding="utf-8")
small = (root / "design/icon/EyeReminder-small.svg").read_text(encoding="utf-8")
output = root / "EyeReminder/Assets/EyeReminder.ico"

sizes = [16, 20, 24, 32, 40, 48, 64, 256]
images = [bytes(resvg_py.svg_to_bytes(svg_string=small if s <= 20 else full, width=s, height=s)) for s in sizes]

# ICO con entradas PNG (Windows Vista en adelante): cabecera, directorio y luego los datos.
header = struct.pack("<HHH", 0, 1, len(sizes))
offset = len(header) + 16 * len(sizes)
directory = b""
for size, png in zip(sizes, images):
    dim = 0 if size == 256 else size
    directory += struct.pack("<BBBBHHII", dim, dim, 0, 0, 1, 32, len(png), offset)
    offset += len(png)

output.write_bytes(header + directory + b"".join(images))
print(f"{output.relative_to(root)}: {', '.join(map(str, sizes))} px")
