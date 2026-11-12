# Hearth & Hollow — ticket 54: pixel food art source.
# Draws every ingredient and dish as a 16x16 sprite from little letter maps + palettes,
# previews them as a PNG sheet and writes Assets/HearthHollow/Scripts/UI/FoodArtData.cs.
# Re-run from the project root:
#   python3 Tools/FoodArt/food_art.py Tools/FoodArt Assets/HearthHollow/Scripts/UI/FoodArtData.cs
import sys

N = 16
OUT = (40, 26, 24)

def C(h):
    h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))

def grid(rows):
    return [list(r) for r in rows]

def blank(w=N, h=N):
    return [['.'] * w for _ in range(h)]

def stamp(g, rows, ox, oy, remap=None):
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == '.': continue
            if remap and ch in remap: ch = remap[ch]
            X, Y = ox + x, oy + y
            if 0 <= Y < len(g) and 0 <= X < len(g[0]): g[Y][X] = ch

def centred(rows, w=N, h=N, dy=0):
    """Put a map in the middle of a w x h canvas (1px left free for the outline)."""
    rows = [r.rstrip('.') for r in rows]
    while rows and not rows[0].strip('.'): rows.pop(0)
    while rows and not rows[-1].strip('.'): rows.pop()
    left = min(len(r) - len(r.lstrip('.')) for r in rows if r.strip('.'))
    rows = [r[left:] for r in rows]
    bw = max(len(r) for r in rows); bh = len(rows)
    g = blank(w, h)
    stamp(g, rows, (w - bw) // 2, (h - bh) // 2 + dy)
    return g

# ── ingredient shapes ──────────────────────────────────────────────
LEAF = [
    "..........bbb.",
    "........bbaaab",
    ".......baaaaac",
    "......baaasaac",
    ".....baaasaacc",
    "..bb.baasaacc.",
    ".baab.aasacc..",
    ".baaac.sacc...",
    "..aacc.s......",
    "...cc.s.......",
    ".....s........",
    "....s.........",
]
GRAIN = [
    ".....w.....",
    "....bab....",
    "...b.a.b...",
    "..baa.aab..",
    "...ca.ac...",
    "..baa.aab..",
    "...ca.ac...",
    "..baa.aab..",
    "...ca.ac...",
    "....cac....",
    ".....s.....",
    ".....s.....",
    "....ss.....",
    ".....s.....",
]
BALL = [".bab.", "bwaaa", "baaac", "aaacc", ".acc."]
MUSH = [
    "....bbbbbb....",
    "..bbwwaaaaab..",
    ".bwwaadaaaaac.",
    ".baaaaaaadaac.",
    "baadaaaaaaaacc",
    "baaaaaaaaaaacc",
    ".cccccccccccc.",
    ".....xxxx.....",
    ".....xwxx.....",
    ".....xxxx.....",
    ".....xxxe.....",
    "....xxxxee....",
]
CRYSTALS = [
    "......w.......",
    ".....wba......",
    "....wbaac.....",
    "...bbaaacc.b..",
    "..wbaacacc.bc.",
    ".wbbaaacaccacc",
    "dddddddddddddd",
    ".eeeeeeeeeeee.",
    "..eeeeeeeeee..",
]
FISH = [
    "....bbbb.......",
    "..bbwaaab...cc.",
    ".bwkaaaaab.cac.",
    "baaaaaaaaaacac.",
    ".aaaddddaaacac.",
    "..ccdddcacc.cc.",
    "....cccc.......",
]
EGG = [
    "...bbb...",
    "..bwwab..",
    ".bwwaaab.",
    ".bwaadaa.",
    "bwaaaaaac",
    "baadaaaac",
    "baaaaadac",
    "aaaaaaacc",
    ".aaaaacc.",
    "..ccccc..",
]
LUMP = [
    "....bbbb....",
    "..bbaaaab...",
    ".bwaadaaac..",
    ".baaaaadaac.",
    "baadaaaaaacc",
    "baaaaadaaacc",
    ".aaaaaaaacc.",
    "..ccaaaccc..",
    "....cccc....",
]
CHILI = [
    "...........e..",
    "..........e...",
    "........dde...",
    ".......ddee...",
    "......baaac...",
    ".....bwaaac...",
    "....bwaaacc...",
    "...baaaacc....",
    "..baaaacc.....",
    ".baaacc.......",
    ".aacc.........",
    ".cc...........",
]
GARLIC = [
    "......s......",
    "......s......",
    ".....bab.....",
    "....bwaab....",
    "...bwaacab...",
    "..bwaacaaac..",
    ".bwaacaaaacc.",
    ".baacaaacaacc",
    ".aaacaaacaacc",
    "..aacaacaacc.",
    "...cccccccc..",
    "....d.d.d....",
]
MEAT = [
    "....bbbb.....",
    "..bbaaaab....",
    ".bwaaddaac...",
    "bwaadaaaacc..",
    "baaaaaadacc..",
    "aadaaaaaacc..",
    ".aaaaaaacc...",
    "..ccaaccc....",
    "....xx.......",
    ".....xx......",
    "....wxxw.....",
    "...wwx.ww....",
    "....w...w....",
]
JAR = [
    "..dddddd..",
    "..eeeeee..",
    "...xxxx...",
    ".xbaaaabx.",
    "xbwaaaaacx",
    "xbwaaaaacx",
    "xbaaaaaacx",
    "xaaaaaaacx",
    "xaaaaaaccx",
    ".xccccccx.",
]
ROOT = [
    "........bb...",
    ".......baac..",
    "..bb..baac...",
    ".baab.baac...",
    ".baaabaaac...",
    "..baaaaaaabb.",
    ".baaaaaaaaaac",
    "baaacaaaacaac",
    "baacc.aacc.cc",
    ".cc....cc....",
]
LOG = [
    ".bbbbbbbbbddd..",
    "baaaaaaaaadxxd.",
    "aacaaaaacadxexd",
    "aaaacaaaaadxexd",
    "caaaaacaaadxxd.",
    ".ccccccccc.dd..",
]
STONE = [
    ".....bbbb.....",
    "...bbwaaab....",
    "..bwaaaaaabb..",
    ".bwaaacaaaaac.",
    ".baaaaaaacaac.",
    "baacaaaaaaaacc",
    ".aaaaaacaaaccc",
    "..ccccccccccc.",
]
AMPHORA = [
    "...bbbbbb...",
    "....baac....",
    ".....aa.....",
    "...bbaaab...",
    "..bwaaaaac..",
    ".bwaddddaac.",
    ".baaaaaaaac.",
    ".baddddddac.",
    ".aaaaaaaacc.",
    "..aaaaaacc..",
    "...cccccc...",
]
BOLT = [
    "..bbbb..",
    ".bwaaab.",
    "bwaaaaac",
    "baadaaac",
    "baaaaacc",
    ".ccccc..",
    "...aa...",
    "...ac...",
    "..bac...",
    "...ac...",
    "..bac...",
    "...ac...",
    "...cc...",
]
MOSS = [
    "....w.........",
    "...wdw........",
    "....w.....w...",
    "..........wdw.",
    "..bb..bb...w..",
    ".baab.baab.bb.",
    "baaaabaaaabaab",
    "aaacaaacaaacaa",
    "cccccccccccccc",
]
COMB = [
    "...bbbbbbbb...",
    "..baaxaaxaab..",
    ".baxaaxaaxaac.",
    ".axwaxaaxaaac.",
    ".axaaxaaxdaac.",
    "..aaxaaxaaac..",
    "...cccccccc...",
    "......dd......",
    ".......d......",
]

def berries(remap=None):
    g = blank()
    stamp(g, ["..s.....", "...s..s.", "....ss.."], 4, 1)
    stamp(g, BALL, 2, 4); stamp(g, BALL, 8, 4); stamp(g, BALL, 5, 8); stamp(g, BALL, 10, 9)
    return g

# palettes: a base, b light, c shade, w highlight, d accent, e accent shade, s stem, x extra, k eye
P = lambda **kw: {k: C(v) for k, v in kw.items()}
GREEN_STEM = '4f7a2c'

ING = {
    "herbs":   (LEAF,  P(a='5aa83c', b='8fd65a', c='3a7a2a', s=GREEN_STEM)),
    "mint":    (LEAF,  P(a='5fd0a0', b='a8f0cc', c='35957a', s='3d7a5c')),
    "nettle":  (LEAF,  P(a='3d7d34', b='68aa48', c='26552a', s='5a4a2a')),
    "wheat":   (GRAIN, P(a='d9b25a', b='f2d98c', c='a8823a', w='fff2c4', s='b08c48')),
    "golden_wheat": (GRAIN, P(a='f0c030', b='ffe680', c='c08a18', w='ffffff', s='d0a030')),
    "berries": ("berries", P(a='5a5ad0', b='8c8cf0', c='35359a', w='d8d8ff', s='4f7a2c')),
    "frost_berry": ("berries", P(a='8ccbe8', b='c8ecff', c='5a92b8', w='ffffff', s='6a8aa0')),
    "mushroom": (MUSH, P(a='b5653a', b='d9895a', c='7d3f22', w='f2c09a', d='f0dcc0', x='efe2c8', e='c8b494')),
    "glowcap": (MUSH, P(a='3a9ae0', b='7cd0ff', c='2462a0', w='e0f8ff', d='d0ffff', x='d8f0e8', e='a8c8c0')),
    "truffle": (LUMP,  P(a='6a4a3a', b='8f6a52', c='45302a', w='b08a70', d='3a281f')),
    "ember_coal": (LUMP, P(a='3a3438', b='5a5258', c='241f22', w='7a7076', d='ff8a2a')),
    "salt":    (CRYSTALS, P(a='e8ecf0', b='f8fbff', c='b4bec8', w='ffffff', d='b08050', e='86603a')),
    "fish":    (FISH,  P(a='6f9fc0', b='a8d0e8', c='466e8c', w='e8f6ff', d='e0e8ec', k='1c1c28')),
    "cave_egg": (EGG,  P(a='bfe6a8', b='e2f8d4', c='86b47a', w='ffffff', d='8fe8e0')),
    "chili":   (CHILI, P(a='d8352a', b='f2685a', c='9a2018', w='ffb0a0', d='5aa83c', e='3a7a2a')),
    "garlic":  (GARLIC, P(a='e8dccc', b='f8f2ea', c='b8a690', w='ffffff', d='9a8a70', s='8a9a5a')),
    "ember_meat": (MEAT, P(a='b8483a', b='e0725a', c='7a2a22', w='f0e6d0', d='f0a080', x='e8dcc0')),
    "fire_honey": (JAR, P(a='f08a20', b='ffc050', c='b85a10', w='fff0b0', d='c84a3a', e='8a2a22', x='d8e4e8')),
    "ginger":  (ROOT,  P(a='d8b070', b='f0d498', c='a07c44')),
    "timber":  (LOG,   P(a='8a5a32', b='b07a48', c='5e3a1e', d='6a4426', x='e0b878', e='a8804a')),
    "fieldstone": (STONE, P(a='8c8c94', b='b0b0b8', c='5e5e66', w='d8d8e0')),
    "clay":    (AMPHORA, P(a='c06a40', b='e08a5a', c='88442a', w='f2b48a', d='8a4a2a')),
    "iron_scrap": (BOLT, P(a='8a96a0', b='b8c4cc', c='5a646c', w='e8f0f4', d='4a545c')),
    "starmoss": (MOSS, P(a='4aa07a', b='7cd0a4', c='2e6a52', w='fff8c0', d='ffe060')),
    "wild_honeycomb": (COMB, P(a='f0b030', b='ffd870', c='b87a18', x='c8881c', w='fff4c0', d='f09a10')),
}

# colours a dish uses for an ingredient's chunks/filling: (main, light, shade)
FOODC = {
    "herbs": ('5aa83c', '8fd65a', '3a7a2a'), "mint": ('5fd0a0', 'a8f0cc', '35957a'), "nettle": ('3d7d34', '68aa48', '26552a'),
    "wheat": ('d9b25a', 'f2d98c', 'a8823a'), "golden_wheat": ('f0c030', 'ffe680', 'c08a18'),
    "berries": ('5a5ad0', '8c8cf0', '35359a'), "frost_berry": ('8ccbe8', 'c8ecff', '5a92b8'),
    "mushroom": ('b5653a', 'd9895a', '7d3f22'), "glowcap": ('3a9ae0', '7cd0ff', '2462a0'), "truffle": ('6a4a3a', '8f6a52', '45302a'),
    "salt": ('e8ecf0', 'ffffff', 'b4bec8'), "fish": ('e8d8c8', 'fff4ea', 'b8a090'), "cave_egg": ('f8e070', 'fff4b0', 'd0b040'),
    "chili": ('d8352a', 'f2685a', '9a2018'), "garlic": ('e8dccc', 'f8f2ea', 'b8a690'), "ember_meat": ('a8483a', 'd0725a', '6a2a22'),
    "fire_honey": ('f08a20', 'ffc050', 'b85a10'), "ginger": ('d8b070', 'f0d498', 'a07c44'),
    "starmoss": ('4aa07a', '7cd0a4', '2e6a52'), "wild_honeycomb": ('f0b030', 'ffd870', 'b87a18'),
}

# ── dish shapes ───────────────────────────────────────────────────
BOWL = [
    "....v...v...v...",
    ".....v...v...v..",
    "....v...v...v...",
    "................",
    "....bbaaabba....",
    "..xabaayaabxa...",
    ".aayaaxaabaayaa.",
    "dddddddddddddddd",
    ".deeeeeeeeeeeed.",
    "..deeeeeeeeeed..",
    "...ddeeeeeedd...",
    ".....dddddd.....",
    "......dddd......",
]
SANDWICH = [
    "...bbbbbbbbbb...",
    "..bwwwwwwwwwwb..",
    ".bwwwwwwwwwwwwb.",
    ".bccccccccccccb.",
    ".xyxxyxxyxxyxxy.",
    ".zzzzzzzzzzzzzz.",
    ".bwwwwwwwwwwwwb.",
    ".bccccccccccccb.",
    "..bbbbbbbbbbbb..",
]
LOAF = [
    "....bbbbbbb.....",
    "..bbaaaaaaabb...",
    ".baadaaadaaaab..",
    ".baaaaaaaaaaab..",
    "baaadaaadaaaaac.",
    "baaaaaaaaaaaacc.",
    ".cccccccccccccc.",
]
SLICE = [
    "..bbbb..bbbb..",
    ".bwwwwbbwwwwb.",
    ".bwxwyxwwyxwb.",
    ".bwwwxwwxwwwb.",
    ".bwywwwywwxwb.",
    ".bwwwxwwwwwwb.",
    ".bwwwwwwwwwwb.",
    ".bbbbbbbbbbbb.",
]
WRAP = [
    ".........bbbb.",
    ".......bbxyxyb",
    ".....bbwwbxyxb",
    "...bbwwwwwbbb.",
    ".bbwwwwwwwwb..",
    "bwwwwwwwwwb...",
    "bwwwwwwwwb....",
    ".bbwwwwbb.....",
    "...bbbb.......",
]
STEAK = [
    "...bbbbbb.....",
    ".bbaaaaaabb...",
    "baaadaaaaaab..",
    "baaaaaaadaaac.",
    ".aaadaaaaaaac.",
    "..aaaaaaaaacc.",
    "...ccaaaaacc..",
    ".....ccccc....",
]
RIBS = [
    "..x..x..x..x..",
    "..x..x..x..x..",
    ".bbbbbbbbbbbb.",
    "baaaaaaaaaaaab",
    "baadaaadaaadac",
    ".aaaaaaaaaaacc",
    "..cccccccccc..",
]
PLATE = [
    ".pppppppppppppp.",
    "pqqqqqqqqqqqqqqp",
    ".pppppppppppppp.",
]
CHUNK_A = [".abb.", "abbbc", "aabcc", "aaacc", ".acc."]
STAR = [".g.", "ggg", ".g."]

SOUP = {  # pot dishes: soup colours (main, light)
    "herb_stew": ('8a6a3a', 'b08a52'), "herb_stew_2": ('8a6a3a', 'b08a52'), "berry_porridge": ('e8d8c0', 'fff0dc'),
    "marsh_chowder": ('efe4cc', 'fff8ea'), "nettle_soup": ('4f8a3a', '74b056'), "truffle_risotto": ('efe0b8', 'fff4d8'),
    "lantern_soup": ('e8c050', 'fbe48a'), "drake_hotpot": ('b8482a', 'e0703a'),
}
BOWL_COL = ('8a5a32', 'b07a48')   # wooden bowl (rim, wall)
BREAD = ('c88a40', 'f0d8a8', 'a86a2a')   # crust, crumb, shade

def dish_bowl(rid, ings):
    soup = SOUP.get(rid, (FOODC.get(ings[0], ('8a6a3a', 'b08a52', ''))[0], FOODC.get(ings[0], ('8a6a3a', 'b08a52', ''))[1]))
    bits = [FOODC[i] for i in ings if i in FOODC]
    x = bits[0][0] if bits else 'ffffff'
    y = bits[1][1] if len(bits) > 1 else (bits[0][1] if bits else 'ffffff')
    pal = P(a=soup[0], b=soup[1], x=x, y=y, d=BOWL_COL[1], e=BOWL_COL[0])
    pal['v'] = (240, 240, 245, 150)
    return centred(BOWL), pal

def dish_skewer(rid, ings):
    g = blank()
    for i in range(14):   # the stick, bottom-left to top-right
        X, Y = 1 + i, 14 - i
        g[Y][X] = 's'
    cols = [FOODC[i] for i in ings if i in FOODC] or [('b5653a', 'd9895a', '7d3f22')]
    pal = P(s='c8a070')
    keys = [('a', 'b', 'c'), ('d', 'e', 'f')]
    for n, (cx, cy) in enumerate([(1, 9), (5, 5), (9, 1)]):
        ci = cols[n % len(cols)] if n != 1 else cols[(1 if len(cols) > 1 else 0)]
        k = keys[n % 2]
        stamp(g, CHUNK_A, cx, cy, remap={'a': k[0], 'b': k[1], 'c': k[2]})
        pal[k[0]] = C(ci[0]); pal[k[1]] = C(ci[1]); pal[k[2]] = C(ci[2])
    # char marks on the chunks
    for (X, Y) in [(3, 11), (7, 7), (11, 3), (2, 12), (6, 8), (10, 4)]:
        if g[Y][X] != '.': g[Y][X] = 'k'
    pal['k'] = C('3a2418')
    return g, pal

def dish_plate(main, char=True, shape=None):
    s0, pal = ING[main]
    shape = shape or s0
    pal = dict(pal)
    g = blank()
    stamp(g, PLATE, 0, 12)
    rows = [r.rstrip('.') for r in shape]
    bw = max(len(r) for r in rows)
    stamp(g, rows, (N - bw) // 2, 12 - len(rows) + 1)
    if char:
        for Y in range(N):
            for X in range(N):
                if g[Y][X] == 'a' and (X + Y) % 4 == 0: g[Y][X] = 'k'
        pal['k'] = C('5a2a18')
    pal['p'] = C('c8ccd4'); pal['q'] = C('eef0f4')
    return g, pal

def dish_sandwich(rid, ings):
    fill = [FOODC[i] for i in ings if i in FOODC and i not in ('wheat', 'golden_wheat')] or [FOODC['herbs']]
    x = fill[0]; y = fill[1] if len(fill) > 1 else fill[0]
    crumb = 'fff0b0' if 'golden_wheat' in ings else BREAD[1]
    pal = P(b=BREAD[0], w=crumb, c=BREAD[2], x=x[0], y=y[1], z=FOODC['herbs'][1] if rid != 'bramble_jam' else x[2])
    return centred(SANDWICH), pal

def dish_loaf(rid, ings):
    return centred(LOAF), P(a='d09a50', b='f0c070', c='9a6428', d='f8e0b0')

def dish_slice(rid, ings):
    fill = [FOODC[i] for i in ings if i in FOODC and i != 'wheat']
    x = fill[0] if fill else FOODC['berries']; y = fill[1] if len(fill) > 1 else x
    return centred(SLICE), P(b=BREAD[0], w=BREAD[1], x=x[0], y=y[0])

def dish_wrap(rid, ings):
    fill = [FOODC[i] for i in ings if i in FOODC and i != 'wheat']
    x = fill[0]; y = fill[1] if len(fill) > 1 else fill[0]
    return centred(WRAP), P(b='c8a060', w='f0dca8', x=x[0], y=y[1])

DISHES = {
    "herb_stew": ("bowl", ["herbs", "mushroom"]), "herb_stew_2": ("bowl", ["herbs", "mushroom", "starmoss"]),
    "berry_porridge": ("bowl", ["berries", "wheat"]), "marsh_chowder": ("bowl", ["fish", "herbs"]),
    "nettle_soup": ("bowl", ["nettle", "herbs"]), "truffle_risotto": ("bowl", ["truffle", "mushroom"]),
    "lantern_soup": ("bowl", ["cave_egg", "ginger"]), "drake_hotpot": ("bowl", ["ember_meat", "ginger"]),
    "mushroom_skewer": ("skewer", ["mushroom", "herbs"]), "mushroom_skewer_2": ("skewer", ["mushroom", "wild_honeycomb"]),
    "truffle_caps": ("skewer", ["truffle", "mushroom"]), "glowcap_kebab": ("skewer", ["glowcap", "garlic"]),
    "firecracker_skewer": ("skewer", ["chili", "mushroom"]),
    "grilled_fish": ("plate", "fish"), "honey_ribs": ("plate", "ember_meat"), "ember_steak": ("plate", "ember_meat"),
    "bread": ("loaf", []), "herb_butty": ("sandwich", ["wheat", "herbs"]), "herb_butty_2": ("sandwich", ["golden_wheat", "herbs"]),
    "bramble_jam": ("sandwich", ["berries"]), "marsh_butty": ("sandwich", ["fish", "herbs"]),
    "glow_egg_sandwich": ("sandwich", ["cave_egg", "truffle"]), "trail_wrap": ("wrap", ["mint", "herbs"]),
    "frostberry_toast": ("slice", ["frost_berry", "fire_honey"]),
}
TIER2 = {"herb_stew_2", "mushroom_skewer_2", "herb_butty_2"}
# a dish this file doesn't know is drawn by its station: Pot → bowl, BBQ → skewer, Sandwich → sandwich
STATION_DEFAULT = {"Pot": ("bowl", ["herbs", "mushroom"]), "BBQ": ("skewer", ["mushroom", "herbs"]), "Sandwich": ("sandwich", ["herbs"])}

def make_dish(rid, kind, ings):
    if kind == "bowl": g, p = dish_bowl(rid, ings)
    elif kind == "skewer": g, p = dish_skewer(rid, ings)
    elif kind == "plate":
        g, p = dish_plate(ings, shape={"honey_ribs": RIBS, "ember_steak": STEAK}.get(rid))
        if rid == "honey_ribs":   # a honey glaze
            for Y in range(N):
                for X in range(N):
                    if g[Y][X] == 'b': g[Y][X] = 'h'
            p['h'] = C('f0a030')
        if rid == "ember_steak":
            p['d'] = C('5aa83c')   # a sprig on top
    elif kind == "sandwich": g, p = dish_sandwich(rid, ings)
    elif kind == "loaf": g, p = dish_loaf(rid, ings)
    elif kind == "slice": g, p = dish_slice(rid, ings)
    elif kind == "wrap": g, p = dish_wrap(rid, ings)
    if rid in TIER2:
        stamp(g, STAR, 13, 0); p['g'] = C('ffe050')
    return g, p

def make_ing(iid):
    shape, pal = ING[iid]
    g = berries() if shape == "berries" else centred(shape)
    if iid == "golden_wheat":
        stamp(g, STAR, 12, 1); pal = dict(pal); pal['g'] = C('ffffff')
    return g, pal

def outline(g, pal):
    h, w = len(g), len(g[0])
    out = [row[:] for row in g]
    for y in range(h):
        for x in range(w):
            if g[y][x] != '.': continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                X, Y = x + dx, y + dy
                if 0 <= X < w and 0 <= Y < h and g[Y][X] != '.' and g[Y][X] not in ('v', 'g'):
                    out[y][x] = 'o'; break
    pal = dict(pal); pal['o'] = OUT
    return out, pal

def render(g, pal):
    from PIL import Image
    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    for y in range(N):
        for x in range(N):
            ch = g[y][x]
            if ch == '.': continue
            c = pal.get(ch, (255, 0, 255))
            img.putpixel((x, y), c if len(c) == 4 else c + (255,))
    return img

def all_sprites():
    out = {}
    for iid in ING: out["ing:" + iid] = outline(*make_ing(iid))
    for rid, (kind, ings) in DISHES.items(): out["dish:" + rid] = outline(*make_dish(rid, kind, ings))
    for st, (kind, ings) in STATION_DEFAULT.items(): out["station:" + st] = outline(*make_dish("", kind, ings))
    return out

def cs(sprites):
    L = ["// Hearth & Hollow — ticket 54: pixel food art, GENERATED by Tools/FoodArt/food_art.py. Don't edit by hand:",
         "// change the maps there and re-run it. Each entry is a palette (RRGGBBAA) and 16 rows, top row first,",
         "// one palette index per character (0-9, a-z; '.' = clear).",
         "namespace HearthHollow.UI", "{", "    public static partial class FoodArt", "    {",
         "        static readonly System.Collections.Generic.Dictionary<string, string[]> Data = new System.Collections.Generic.Dictionary<string, string[]>", "        {"]
    digits = "0123456789abcdefghijklmnopqrstuvwxyz"
    for key, (g, pal) in sprites.items():
        used = sorted({ch for row in g for ch in row if ch != '.'})
        idx = {ch: digits[i] for i, ch in enumerate(used)}
        cols = []
        for ch in used:
            c = pal.get(ch, (255, 0, 255)); c = c if len(c) == 4 else c + (255,)
            cols.append("%02X%02X%02X%02X" % c)
        rows = ["".join(idx.get(ch, '.') for ch in row) for row in g]
        L.append('            { "%s", new[] { "%s",' % (key, " ".join(cols)))
        L.append("                " + ", ".join('"%s"' % r for r in rows) + " } },")
    L += ["        };", "    }", "}", ""]
    return "\n".join(L)

if __name__ == "__main__":
    outdir = sys.argv[1] if len(sys.argv) > 1 else "."
    sp = all_sprites()
    if len(sys.argv) > 2:
        open(sys.argv[2], "w", encoding="utf-8").write(cs(sp))
    try:
        from PIL import Image
    except ImportError:
        print(len(sp), "sprites (no PIL: skipped the preview sheet)"); sys.exit(0)
    S = 6; cols = 10
    keys = list(sp.keys())
    sheet = Image.new("RGBA", (cols * (N * S + 8), ((len(keys) + cols - 1) // cols) * (N * S + 8)), (60, 52, 48, 255))
    for i, k in enumerate(keys):
        im = render(*sp[k]).resize((N * S, N * S), Image.NEAREST)
        sheet.alpha_composite(im, ((i % cols) * (N * S + 8) + 4, (i // cols) * (N * S + 8) + 4))
    sheet.save(outdir + "/food_sheet.png")
    print(len(sp), "sprites")
