"""Deterministic, synthetic PNG decoder fixtures. No avatar or external image data.
PNG layout: https://www.w3.org/TR/png-3/ (Adam7, filtering, sample packing, tRNS).
Expected RGBA is derived from the source samples, never from NyaForge's decoder.
"""
import base64
import json
from pathlib import Path
import struct
import zlib

DEST = Path(__file__).resolve().parents[1] / 'Assets/StreamingAssets/NyaForgeVerification/PngVariants'
PASSES = [(0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4),
          (0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2)]
WIDTH, HEIGHT = 9, 11


def chunk(kind, data):
    body = kind.encode('ascii') + data
    return struct.pack('>I', len(data)) + body + struct.pack('>I', zlib.crc32(body))


def pack(samples, depth):
    if depth == 16:
        return b''.join(struct.pack('>H', n) for n in samples)
    if depth == 8:
        return bytes(samples)
    result = bytearray((len(samples) * depth + 7) // 8)
    for i, n in enumerate(samples):
        result[i * depth // 8] |= n << (8 - depth - i * depth % 8)
    return bytes(result)


def paeth(a, b, c):
    p = a + b - c
    distances = [abs(p - a), abs(p - b), abs(p - c)]
    return [a, b, c][distances.index(min(distances))]


def filtered(row, previous, bpp, kind):
    encoded = bytearray([kind])
    for i, value in enumerate(row):
        left = row[i-bpp] if i >= bpp else 0
        up = previous[i] if previous else 0
        corner = previous[i-bpp] if previous and i >= bpp else 0
        prediction = [0, left, up, (left + up)//2, paeth(left, up, corner)][kind]
        encoded.append((value - prediction) & 255)
    return encoded


def fixture(color, depth, interlaced):
    name = f'color{color}-depth{depth}-' + ('adam7' if interlaced else 'filters')
    palette_count = min(1 << depth, 16)
    palette = [(i*37 % 256, (i*71+29) % 256, (i*13+113) % 256) for i in range(palette_count)]
    alphas = [0, 127]  # remaining entries implicitly opaque
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[color]
    rows, rgba = [], []
    for y in range(HEIGHT):
        samples, pixels = [], []
        for x in range(WIDTH):
            n = (x*31 + y*47) % 256
            if color == 3:
                index = (x+3*y) % palette_count
                value = [index]
                pixel = (*palette[index], alphas[index] if index < len(alphas) else 255)
            elif color in (0, 4):
                g = n % (1 << depth) if depth < 8 else n
                alpha = (x*53+y*17) % 256
                value = [g] + ([alpha] if color == 4 else [])
                gray = g*255//((1 << depth)-1) if depth < 8 else g
                pixel = (gray, gray, gray, alpha if color == 4 else (0 if g == 0 else 255))
            else:
                value = [n, (n*3) % 256, (n*7) % 256]
                alpha = (x*53+y*17) % 256 if color == 6 else (0 if n == 0 else 255)
                pixel = (*value, alpha)
                if color == 6:
                    value.append(alpha)
            sample = [v*257 for v in value] if depth == 16 else value
            if depth == 16 and color in (0, 2) and x == 1 and y == 0:
                # Same 8-bit color as the transparent origin, but the full 16-bit sample differs.
                sample = [1] + [0]*(channels-1)
                pixel = (0, 0, 0, 255)
            samples.append(sample)
            pixels.extend(pixel)
        rows.append(samples)
        rgba.append(bytes(pixels))
    raw = bytearray()
    for x0, y0, dx, dy in PASSES if interlaced else [(0, 0, 1, 1)]:
        previous = None
        for y in range(y0, HEIGHT, dy):
            row = pack([v for x in range(x0, WIDTH, dx) for v in rows[y][x]], depth)
            raw.extend(filtered(row, previous, max(1, (channels*depth+7)//8), 0 if interlaced else y % 5))
            previous = row
    png = b'\x89PNG\r\n\x1a\n' + chunk('IHDR', struct.pack('>IIBBBBB', WIDTH, HEIGHT, depth, color, 0, 0, int(interlaced)))
    png += chunk('sRGB', b'\0')
    if color == 3:
        png += chunk('PLTE', bytes(v for p in palette for v in p)) + chunk('tRNS', bytes(alphas))
    elif color in (0, 2):
        png += chunk('tRNS', bytes(2 if color == 0 else 6))
    png += chunk('IDAT', zlib.compress(raw)) + chunk('IEND', b'')
    (DEST / (name+'.png')).write_bytes(png)
    return {'name': name, 'width': WIDTH, 'height': HEIGHT,
            'rgbaBottomLeft': base64.b64encode(b''.join(reversed(rgba))).decode('ascii')}


if __name__ == '__main__':
    DEST.mkdir(parents=True, exist_ok=True)
    entries = [fixture(color, depth, interlace)
               for color, depths in [(0, [1, 2, 4, 8, 16]), (2, [8, 16]), (3, [1, 2, 4, 8]), (4, [8, 16]), (6, [8, 16])]
               for depth in depths for interlace in (False, True)]
    (DEST / 'manifest.json').write_text(json.dumps({'entries': entries}, indent=2)+'\n', encoding='utf-8')
    print(f'Generated {len(entries)} fixtures: {DEST}')
