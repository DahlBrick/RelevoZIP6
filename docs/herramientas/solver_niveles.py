"""Solver abstracto de los niveles de RELEVO (BFS por casillas).

Modela: batería (suelo/mano/base), bases A/D con puentes B/C, palancas L/K con pasarelas P/Q,
cajas k (Sokoban), placas o (compuerta g más cercana) y u (puente R), reserva de energía y
condensadores z (sostienen el puente de su base unos pasos tras retirar la batería).
"""
import sys
from collections import deque

FLOOR = set("#cmvADLKEXNYkouz")
BOX_OK = set("#cmkoug")


def solve(rows, reserve=0, cond=None, hold=0, charge_rate=1.0, max_states=6_000_000, trace=False):
    H, W = len(rows), len(rows[0])
    ch = lambda x, y: rows[y][x] if 0 <= x < W and 0 <= y < H else 'w'
    find = lambda c: [(x, y) for y in range(H) for x in range(W) if rows[y][x] == c]
    N = find('N')[0]
    Y = find('Y')[0]
    bases = {c: find(c)[0] for c in 'AD' if find(c)}
    bridge_of = {'A': 'B', 'D': 'C'}
    levers = {c: find(c)[0] for c in 'LK' if find(c)}
    walk_of = {'L': 'P', 'K': 'Q'}
    E = find('E')[0]
    boxes0 = tuple(sorted(find('k')))
    os_ = find('o'); us = find('u'); gs = find('g')
    gate_of_plate = {}
    for o in os_:
        g = min(gs, key=lambda g: (g[0]-o[0])**2 + (g[1]-o[1])**2)
        gate_of_plate[o] = g
    cond = cond or {}  # base -> True
    hold_max = hold

    def near(a, b, r=1.5):
        return (a[0]-b[0])**2 + (a[1]-b[1])**2 <= r*r + 1e-6

    def reach(a, b):  # alcance 3.6 m = 1.8 casillas
        return near(a, b, 1.8)

    # state: pos, bat ('Y','H','A','D'), levers(frozenset), boxes, res(int*2), charges tuple
    start = (N, 'Y', frozenset(), boxes0, reserve*2, tuple(0 for _ in 'AD'))
    idx = {'A': 0, 'D': 1}

    def bridge_up(b, bat, charges):
        return bat == b or (cond.get(b) and charges[idx[b]] > 0)

    def walkable(p, bat, lev, boxes, charges, pos):
        c = ch(*p)
        if p in boxes: return False
        if c in FLOOR: return True
        if c == 'B': return bridge_up('A', bat, charges)
        if c == 'C': return bridge_up('D', bat, charges)
        if c == 'P': return 'L' in lev
        if c == 'Q': return 'K' in lev
        if c == 'R': return any(u in boxes for u in us) or pos in us
        if c == 'g':
            return any(gate_of_plate[o] == p and (o in boxes or o == pos) for o in os_)
        return False

    def bat_pos(bat):
        if bat == 'Y': return Y
        if bat in bases: return bases[bat]
        return None

    def tick(bat, charges, waited=False):
        cs = list(charges)
        for b in 'AD':
            if not cond.get(b): continue
            i = idx[b]
            if bat == b: cs[i] = min(hold_max*10, cs[i] + int(charge_rate*10))
            elif cs[i] > 0: cs[i] = max(0, cs[i] - 10)
        return tuple(cs)

    def on_bridge_dead(pos, bat, lev, boxes, charges):
        c = ch(*pos)
        if c in 'BCRgPQ' and not walkable(pos, bat, lev, boxes, charges, pos):
            # Nilo sobre un puente que ya no está
            if c == 'R': return not (any(u in boxes for u in us))
            return c in 'BC'
        return False

    def res_update(pos, bat, res, cost):
        if reserve == 0: return reserve*2
        bp = bat_pos(bat)
        if bat == 'H' or (bp and near(pos, bp)):
            return reserve*2
        return res - cost

    seen = {start: None}
    q = deque([start])
    while q:
        s = q.popleft()
        pos, bat, lev, boxes, res, charges = s
        if bat == 'H' and reach(pos, E):
            # reconstruct
            path = []
            k = s
            while seen[k] is not None:
                k, a = seen[k]
                path.append(a)
            return list(reversed(path)), len(seen)
        if len(seen) > max_states:
            return None, len(seen)
        succ = []
        # moves
        for d, (dx, dy) in {'n': (0, -1), 's': (0, 1), 'w': (-1, 0), 'e': (1, 0)}.items():
            np_ = (pos[0]+dx, pos[1]+dy)
            nboxes = boxes
            cost = 2
            if np_ in boxes:
                tgt = (np_[0]+dx, np_[1]+dy)
                tc = ch(*tgt)
                if tc not in BOX_OK or tgt in boxes: continue
                if tc == 'g' and not walkable(tgt, bat, lev, boxes, charges, pos): continue
                nboxes = tuple(sorted([b for b in boxes if b != np_] + [tgt]))
                cost = 3
            if not walkable(np_, bat, lev, nboxes, charges, pos): continue
            ncharges = tick(bat, charges)
            nres = res_update(np_, bat, res, cost)
            if reserve and nres <= 0: continue
            if on_bridge_dead(np_, bat, lev, nboxes, ncharges): continue
            succ.append(((np_, bat, lev, nboxes, nres, ncharges), d))
        # interactions
        acts = []
        if bat == 'Y' and reach(pos, Y): acts.append(('pick', 'H'))
        for b, bp in bases.items():
            if reach(pos, bp):
                if bat == 'H': acts.append(('ins'+b, b))
                elif bat == b:
                    if ch(*pos) != bridge_of[b]: acts.append(('rem'+b, 'H'))
        for a, nb in acts:
            ncharges = tick(nb, charges) if a.startswith('ins') else charges
            nres = res_update(pos, nb, res, 0)
            if on_bridge_dead(pos, nb, lev, boxes, ncharges): continue
            succ.append(((pos, nb, lev, boxes, nres, ncharges), a))
        for l, lp in levers.items():
            if l not in lev and reach(pos, lp):
                succ.append(((pos, bat, lev | {l}, boxes, res, tick(bat, charges)), 'pull'+l))
        if cond:
            ncharges = tick(bat, charges)
            if ncharges != charges and not on_bridge_dead(pos, bat, lev, boxes, ncharges):
                succ.append(((pos, bat, lev, boxes, res, ncharges), '.'))
        for ns, a in succ:
            if ns not in seen:
                seen[ns] = (s, a)
                q.append(ns)
    return None, len(seen)


def compress(path):
    out = []
    for a in path:
        if out and out[-1][0] == a and len(a) == 1:
            out[-1][1] += 1
        else:
            out.append([a, 1])
    return ' '.join(a if n == 1 else f"{a}{n}" for a, n in out)
