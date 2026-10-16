"""
Construye Arte/Blender/liria_master.blend desde cero.

    blender -b --factory-startup --python Arte/Blender/scripts/construir_liria.py

El .blend es un resultado: lo que manda es este script. Si algo se ve mal, se corrige aquí y se
vuelve a generar, así el escenario siempre sale igual.

Convenciones (Blender, Z arriba, 1 unidad = 1 m):
- La cubierta del muelle está en z = 0, que es donde camina Nilo. El agua queda en AGUA_Z.
- El mar queda hacia -Y: el muelle sale de las casas hacia -Y.
- Rejilla de 2 m (la del kit de muelle: tablas de 2 x 4 m).

Fuentes (Arte/Fuentes, copiadas tal cual):
- woodendock_fbxgltftextures  kit modular de muelle: tablas, pilotes, barandillas, barcas, props.
- Houses_on_the_water         poblado de casas de chapa sobre pilotes, en un solo FBX.
- the-lighthouse              faro estilizado (aún no se usa).

Colecciones:
- 00_REF       medidas de Nilo y cámara; nunca se exporta.
- 10_KIT       una copia de cada pieza, oculta. Los bloques usan duplicados enlazados (comparten
               malla), así cambiar una pieza aquí la cambia en todo Liria.
- 20_BLOQUES   un hijo por bloque (B01_Muelle, ...). Es lo que se fusiona y exporta después.
- 90_EXPORT    vacía de momento: aquí irán las mallas unificadas por bloque.
"""
import math
import os
import random

import bmesh
import bpy
from mathutils import Matrix, Vector, noise

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
FUENTES = os.path.join(RAIZ, "Arte", "Fuentes")
MUELLE_GLB = os.path.join(FUENTES, "woodendock_fbxgltftextures", "Glb", "WoodenDockSet.glb")
CASAS_FBX = os.path.join(FUENTES, "Houses_on_the_water", "Houses_on_the_water", "Models", "houses_on_the_water.fbx")
ROCAS_FBX = os.path.join(FUENTES, "oga_rocks_set02", "extraido", "FBX")
DEPOSITO_BLEND = os.path.join(FUENTES, "oga_water_tower", "Low Poly Water Tower.blend")
ESCALA_ROCAS = 0.1  # el pack viene en decímetros: así las grandes miden unos 9 m

# Kits que ya están en el proyecto de Unity. Se cargan bajo demanda (solo las piezas que se usan)
# con un prefijo por kit: V_ casas modulares Medieval Village, KW_ barcas Kenney Watercraft,
# KP_ Kenney Pirate, KF_ Kenney Fantasy Town, KG_ Kenney Graveyard, QN_ Quaternius Nature.
TERCEROS = os.path.join(RAIZ, "Assets", "ThirdParty")
KITS_UNITY = {
    "V_": os.path.join(TERCEROS, "Quaternius_Village"),
    "KW_": os.path.join(TERCEROS, "Kenney_Watercraft"),
    "KP_": os.path.join(TERCEROS, "Kenney_Pirate"),
    "KF_": os.path.join(TERCEROS, "Kenney_FantasyTown"),
    "KG_": os.path.join(TERCEROS, "Kenney_Graveyard"),
    "QN_": os.path.join(TERCEROS, "Quaternius_Nature", "Models"),
}
SALIDA = os.path.join(RAIZ, "Arte", "Blender", "liria_master.blend")

AGUA_Z = -1.5

# Nilo, tal como está en Assets/Prefabs/Actores/Nilo.prefab y PlayerController.cs.
NILO_ALTO = 1.8
NILO_RADIO = 0.34
NILO_ESCALON = 0.3     # stepOffset: lo más alto que sube sin saltar (no hay salto)

# Colores de lectura para la vista previa de geometría (no son la textura final).
COLOR = {
    "muelle": (0.62, 0.48, 0.34, 1),
    "casa": (0.55, 0.57, 0.6, 1),
    "barca": (0.7, 0.42, 0.3, 1),
    "prop": (0.75, 0.62, 0.42, 1),
    "roca": (0.5, 0.5, 0.48, 1),
    "chapa": (0.55, 0.57, 0.6, 1),
    "agua": (0.15, 0.32, 0.42, 1),
    "nilo": (1.0, 0.45, 0.1, 1),
}


# ---------------------------------------------------------------- utilidades

def coleccion(nombre, padre=None):
    c = bpy.data.collections.new(nombre)
    (padre or bpy.context.scene.collection).children.link(c)
    return c


def mover_a(o, col):
    for c in list(o.users_collection):
        c.objects.unlink(o)
    col.objects.link(o)


def caja_local(malla):
    """(min, max) de los vértices de una malla, en sus coordenadas."""
    vs = [v.co for v in malla.vertices]
    return (Vector([min(v[i] for v in vs) for i in range(3)]),
            Vector([max(v[i] for v in vs) for i in range(3)]))


KIT = {}  # nombre -> objeto en 10_KIT, con la transformación ya aplicada a la malla

# Solo nos quedamos con la geometría: las texturas de los packs quedan en Arte/Fuentes como
# referencia y se rehacen. Cada material original se reduce a una categoría, que es lo que se
# texturizará después (un material de Unity por categoría).
CATEGORIAS = (
    ("LIRIA_Cristal", ("glass",)),
    ("LIRIA_Chapa", ("roofing", "metal", "generator", "drum", "can")),
    ("LIRIA_Tela", ("fabric", "rope", "material.001", "mattress")),
    ("LIRIA_Tejado", ("roof", "ceilng", "tiles")),
    ("LIRIA_Revoque", ("plaster",)),
    ("LIRIA_Piedra", ("brick",)),
    ("LIRIA_Roca", ("rock",)),
    ("LIRIA_Madera", ("wood", "dock", "floor", "beam", "boat", "door", "wall", "harpoon", "box")),
)
COLOR_CATEGORIA = {
    "LIRIA_Madera": (0.62, 0.48, 0.34, 1),
    "LIRIA_Chapa": (0.55, 0.57, 0.6, 1),
    "LIRIA_Tejado": (0.5, 0.36, 0.3, 1),
    "LIRIA_Tela": (0.75, 0.68, 0.55, 1),
    "LIRIA_Cristal": (0.4, 0.55, 0.6, 1),
    "LIRIA_Roca": (0.5, 0.5, 0.48, 1),
    "LIRIA_Follaje": (0.52, 0.56, 0.24, 1),
    "LIRIA_Montana": (0.42, 0.48, 0.44, 1),
    "LIRIA_FaroRojo": (0.72, 0.22, 0.17, 1),
    "LIRIA_Revoque": (0.86, 0.80, 0.66, 1),
    "LIRIA_Piedra": (0.62, 0.56, 0.50, 1),
    "LIRIA_Toldo": (0.27, 0.52, 0.58, 1),
    "LIRIA_Blanco": (0.92, 0.9, 0.84, 1),
    "LIRIA_Varios": (0.6, 0.6, 0.55, 1),
}
# Piezas cuyo material se fija por origen y no por nombre.
CATEGORIA_FIJA = {"R_": "LIRIA_Roca", "T_": "LIRIA_Chapa"}


def categoria(nombre_material):
    n = nombre_material.lower()
    for cat, claves in CATEGORIAS:
        if any(k in n for k in claves):
            return cat
    return "LIRIA_Varios"


def materiales_neutros():
    """Cambia cada material importado por su categoría y borra las imágenes."""
    neutros = {}
    for cat, color in COLOR_CATEGORIA.items():
        m = bpy.data.materials.new(cat)
        m.diffuse_color = color
        neutros[cat] = m
    for nombre, o in KIT.items():
        fija = CATEGORIA_FIJA.get(nombre[:2])
        # Las casas usan los mismos materiales de chapa en paredes y tejados; los tejados (piezas
        # "Roofing" por encima de 2 m) pasan a óxido, como en la hoja de referencia.
        if nombre.startswith("H_Roofing") and caja_local(o.data)[0].z > 2.0:
            fija = "LIRIA_Tejado"
        if fija and not o.material_slots:
            o.data.materials.append(neutros[fija])
        for slot in o.material_slots:
            if fija:
                slot.material = neutros[fija]
            elif slot.material is not None and slot.material.name not in neutros:
                slot.material = neutros[categoria(slot.material.name)]
    for m in [m for m in bpy.data.materials if m.name not in neutros]:
        bpy.data.materials.remove(m)
    for im in list(bpy.data.images):
        bpy.data.images.remove(im)


def importar_kits():
    kit = bpy.data.collections["10_KIT"]

    # Kit de muelle: un objeto por pieza; los "-colonly" son colisiones del autor y se descartan
    # (las colisiones las hará Unity con cajas simples).
    antes = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=MUELLE_GLB)
    nuevos = set(bpy.data.objects) - antes
    utiles = [o for o in nuevos if o.type == "MESH" and not o.name.endswith("-colonly")]
    for o in nuevos - set(utiles):
        bpy.data.objects.remove(o)
    for o in utiles:
        o.data = o.data.copy()
        o.data.transform(o.matrix_world)
        o.matrix_world = Matrix()
        o.parent = None
        mover_a(o, kit)
        KIT["D_" + o.name] = o
        o.name = "KIT_D_" + o.name

    # Poblado de casas: se guardan las piezas con su posición original, para recortar grupos.
    antes = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=CASAS_FBX)
    for o in set(bpy.data.objects) - antes:
        if o.type != "MESH":
            bpy.data.objects.remove(o)
            continue
        o.data = o.data.copy()
        o.data.transform(o.matrix_world)
        o.matrix_world = Matrix()
        mover_a(o, kit)
        KIT["H_" + o.name] = o
        o.name = "KIT_H_" + o.name

    # Rocas (OpenGameArt, CC0): una pieza por archivo, ya a escala.
    for archivo in sorted(os.listdir(ROCAS_FBX)):
        if not archivo.endswith(".fbx") or "_all" in archivo:
            continue
        antes = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=os.path.join(ROCAS_FBX, archivo))
        nuevos = set(bpy.data.objects) - antes
        mallas = [o for o in nuevos if o.type == "MESH"]
        for o in mallas:
            o.data = o.data.copy()
            o.data.transform(Matrix.Scale(ESCALA_ROCAS, 4) @ o.matrix_world)
            o.matrix_world = Matrix()
            o.parent = None
        for o in nuevos - set(mallas):
            bpy.data.objects.remove(o)
        bpy.ops.object.select_all(action="DESELECT")
        for o in mallas:
            o.select_set(True)
        bpy.context.view_layer.objects.active = mallas[0]
        if len(mallas) > 1:
            bpy.ops.object.join()
        o = bpy.context.view_layer.objects.active
        mover_a(o, kit)
        clave = "R_" + archivo[3:-4].replace("-", "_")   # LP_B-Rocks2_00.fbx -> R_B_Rocks2_00
        KIT[clave] = o
        o.name = "KIT_" + clave

    # Depósito de agua (OpenGameArt, CC0).
    with bpy.data.libraries.load(DEPOSITO_BLEND) as (origen, destino):
        destino.objects = [n for n in origen.objects if n == "Water_Tower"]
    o = destino.objects[0]
    o.data = o.data.copy()
    o.data.transform(o.matrix_world)
    o.matrix_world = Matrix()
    kit.objects.link(o)
    KIT["T_Water_Tower"] = o
    o.name = "KIT_T_Water_Tower"


# Colores de las paletas de Kenney (una textura de muestras) y la categoría LIRIA que les toca. Cada
# cara toma la categoría del color más parecido al de su muestra: así una barca conserva casco,
# franja y vela distintos sin usar la textura.
PALETA_KENNEY = (
    ((0.62, 0.45, 0.32), "LIRIA_Madera"), ((0.45, 0.31, 0.22), "LIRIA_Madera"),
    ((0.82, 0.62, 0.42), "LIRIA_Madera"), ((0.58, 0.60, 0.62), "LIRIA_Chapa"),
    ((0.33, 0.35, 0.38), "LIRIA_Chapa"), ((0.15, 0.15, 0.17), "LIRIA_Chapa"),
    ((0.95, 0.95, 0.92), "LIRIA_Blanco"), ((0.80, 0.24, 0.20), "LIRIA_FaroRojo"),
    ((0.30, 0.50, 0.75), "LIRIA_Toldo"), ((0.35, 0.65, 0.35), "LIRIA_Follaje"),
    ((0.95, 0.80, 0.30), "LIRIA_Tela"), ((0.65, 0.65, 0.62), "LIRIA_Roca"),
)


def categorias_por_color(o):
    """Para modelos de Kenney: una categoría por cara según el color de su muestra en la textura."""
    imagenes = {}
    for slot in o.material_slots:
        m = slot.material
        if m and m.node_tree:
            for n in m.node_tree.nodes:
                if n.type == "TEX_IMAGE" and n.image:
                    img = n.image
                    imagenes[m.name] = (img.size[0], img.size[1], list(img.pixels))
    if not imagenes or not o.data.uv_layers:
        return False
    w, h, px = next(iter(imagenes.values()))
    if len(px) < w * h * 4 or w == 0:
        return False
    uv = o.data.uv_layers.active.data
    usados = []
    indices = []
    for pol in o.data.polygons:
        u = sum(uv[i].uv.x for i in pol.loop_indices) / pol.loop_total
        v = sum(uv[i].uv.y for i in pol.loop_indices) / pol.loop_total
        ix = min(w - 1, max(0, int((u % 1.0) * w)))
        iy = min(h - 1, max(0, int((v % 1.0) * h)))
        k = (iy * w + ix) * 4
        c = px[k:k + 3]
        cat = min(PALETA_KENNEY, key=lambda e: sum((c[i] - e[0][i]) ** 2 for i in range(3)))[1]
        if cat not in usados:
            usados.append(cat)
        indices.append(usados.index(cat))
    o.data.materials.clear()
    for cat in usados:
        o.data.materials.append(bpy.data.materials[cat])
    for pol, i in zip(o.data.polygons, indices):
        pol.material_index = i
    return True


TRIS_MAX = 700


def cargar(nombre):
    """Importa una pieza de un kit de Unity la primera vez que se usa y la deja en 10_KIT, con la
    transformación aplicada, unida en una sola malla y con materiales de categoría."""
    for prefijo, carpeta in KITS_UNITY.items():
        if nombre.startswith(prefijo):
            break
    else:
        raise KeyError(nombre)
    ruta = os.path.join(carpeta, nombre[len(prefijo):] + ".fbx")
    antes = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=ruta)
    nuevos = set(bpy.data.objects) - antes
    mallas = [o for o in nuevos if o.type == "MESH"]
    # Las matrices de mundo se leen todas antes de tocar ninguna: si se aplanara primero el padre,
    # sus hijas (velas, mástiles, cañones) se moverían con él y quedarían sueltas.
    mundo = {o: o.matrix_world.copy() for o in mallas}
    for o in mallas:
        o.parent = None
    for o in mallas:
        o.data = o.data.copy()
        o.data.transform(mundo[o])
        o.matrix_world = Matrix()
    for o in nuevos - set(mallas):
        bpy.data.objects.remove(o)
    for o in mallas:
        # Las farolas de Graveyard son de hierro: su verde de muestra no es follaje.
        if prefijo == "KG_":
            o.data.materials.clear()
            o.data.materials.append(bpy.data.materials["LIRIA_Chapa"])
        # Solo los kits de Kenney colorean con una textura de muestras.
        elif not (prefijo.startswith("K") and categorias_por_color(o)):
            for slot in o.material_slots:
                if slot.material is not None and slot.material.name not in COLOR_CATEGORIA:
                    slot.material = bpy.data.materials[categoria(slot.material.name)]
    bpy.ops.object.select_all(action="DESELECT")
    for o in mallas:
        o.select_set(True)
    bpy.context.view_layer.objects.active = mallas[0]
    if len(mallas) > 1:
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    # Algunas piezas (esquinas de piedra, tejas) traen miles de triángulos de detalle que el
    # sombreado plano no aprovecha: se simplifican a ~TRIS_MAX.
    tris = sum(len(pol.vertices) - 2 for pol in o.data.polygons)
    if tris > TRIS_MAX:
        mod = o.modifiers.new("Simplificar", "DECIMATE")
        mod.ratio = TRIS_MAX / tris
        bpy.ops.object.modifier_apply(modifier=mod.name)
    mover_a(o, bpy.data.collections["10_KIT"])
    o.name = "KIT_" + nombre
    KIT[nombre] = o
    for m in [m for m in bpy.data.materials if m.users == 0 and m.name not in COLOR_CATEGORIA]:
        bpy.data.materials.remove(m)
    for im in list(bpy.data.images):
        bpy.data.images.remove(im)
    return o


def poner(col, nombre, x, y, z_arriba=None, z_abajo=None, giro=0, tipo="muelle", escala=1.0):
    """Duplicado enlazado de una pieza del kit, centrada en (x, y) en planta.
    z_arriba coloca su cara superior a esa altura; z_abajo, su base."""
    base = KIT[nombre] if nombre in KIT else cargar(nombre)
    o = bpy.data.objects.new(nombre.split("_", 1)[1], base.data)
    rot = Matrix.Rotation(math.radians(giro), 4, "Z") @ Matrix.Scale(escala, 4)
    vs = [rot @ v.co for v in base.data.vertices]
    mn = Vector([min(v[i] for v in vs) for i in range(3)])
    mx = Vector([max(v[i] for v in vs) for i in range(3)])
    centro = (mn + mx) / 2
    z = 0.0
    if z_arriba is not None:
        z = z_arriba - mx.z
    elif z_abajo is not None:
        z = z_abajo - mn.z
    o.matrix_world = Matrix.Translation((x - centro.x, y - centro.y, z)) @ rot
    o.color = COLOR[tipo]
    col.objects.link(o)
    return o


ESTRUCTURA_CASAS = ("Base", "Baw", "Columns", "Roofing", "Walls")


def grupo_casas(col, rect, destino, giro=0):
    """Copia las piezas del poblado cuyo centro cae en rect (x0, y0, x1, y1), giradas 'giro' grados
    sobre el centro del rect y trasladadas para que ese centro quede en destino (x, y). La cubierta
    de las casas ya está en z = 0."""
    x0, y0, x1, y1 = rect
    centro = Vector(((x0 + x1) / 2, (y0 + y1) / 2, 0))
    transformacion = (Matrix.Translation((destino[0], destino[1], 0))
                      @ Matrix.Rotation(math.radians(giro), 4, "Z")
                      @ Matrix.Translation(-centro))
    n = 0
    for nombre, base in KIT.items():
        if not nombre.startswith("H_"):
            continue
        mn, mx = caja_local(base.data)
        c = (mn + mx) / 2
        if not (x0 <= c.x <= x1 and y0 <= c.y <= y1):
            continue
        # Solo la estructura: los muebles sueltos del poblado se colocarán a mano.
        if not nombre[2:].startswith(ESTRUCTURA_CASAS):
            continue
        o = bpy.data.objects.new(nombre[2:], base.data)
        o.matrix_world = transformacion
        o.color = COLOR["barca" if nombre.startswith("H_Boat") else "casa"]
        col.objects.link(o)
        n += 1
    return n


# ---------------------------------------------------------------- referencias

def referencias(col, x, y):
    bpy.ops.mesh.primitive_cylinder_add(radius=NILO_RADIO, depth=NILO_ALTO - 2 * NILO_RADIO,
                                        location=(0, 0, NILO_ALTO / 2), vertices=24)
    cuerpo = bpy.context.object
    for z in (NILO_RADIO, NILO_ALTO - NILO_RADIO):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=NILO_RADIO, location=(0, 0, z), segments=24, ring_count=12)
    for o in bpy.context.scene.objects:
        o.select_set(o.type == "MESH" and o.users_collection[0] == bpy.context.scene.collection)
    bpy.context.view_layer.objects.active = cuerpo
    bpy.ops.object.join()
    cuerpo.name = "REF_Nilo_1.80m"

    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.9, 0, NILO_ESCALON / 2))
    ok = bpy.context.object
    ok.scale = (0.5, 0.5, NILO_ESCALON)
    ok.name = "REF_Escalon_0.30m_sube"
    bpy.ops.mesh.primitive_cube_add(size=1, location=(1.6, 0, 0.2))
    no = bpy.context.object
    no.scale = (0.5, 0.5, 0.4)
    no.name = "REF_Escalon_0.40m_bloquea"

    for o, color in ((cuerpo, COLOR["nilo"]), (ok, (0.3, 0.8, 0.3, 1)), (no, (0.85, 0.25, 0.25, 1))):
        mover_a(o, col)
        o.location.x += x
        o.location.y += y
        o.color = color


# ---------------------------------------------------------------- bloque de prueba

def pilotes(col, x, y, ancho=2.0, largo=4.0):
    """Cuatro pilotes bajo una tabla de ancho x largo m (x, y), desde la cubierta hasta el fondo."""
    for ex in (-(ancho / 2 - 0.15), ancho / 2 - 0.15):
        for ey in (-(largo / 2 - 0.15), largo / 2 - 0.15):
            poner(col, "D_Support_Post", x + ex, y + ey, z_arriba=-0.05)
            poner(col, "D_Support_Post_Half", x + ex, y + ey, z_arriba=-2.05)


def roca(col, nombre, x, y, giro=0, hundida=0.6):
    """Roca apoyada en el fondo: su base queda 'hundida' metros bajo el agua."""
    return poner(col, nombre, x, y, z_abajo=AGUA_Z - hundida, giro=giro, tipo="roca")


def entorno_rocoso(col):
    """Orillas de roca alrededor de B01, fuera de lo pisable, y el depósito de agua sobre un
    peñasco al este de las casas, como en la «Vista general» de la hoja de referencia."""
    # Peñasco del depósito, al noreste, entre el barrio de piedra y el canal.
    base = roca(col, "R_B_Rocks2_03", 22.0, 34.0, giro=20, hundida=1.2)
    cima = max((base.matrix_world @ v.co).z for v in base.data.vertices)
    # Zócalo de piedra bajo las patas: la roca es irregular y sin él alguna pata quedaba al aire.
    bm = bmesh.new()
    caja(bm, (22.0, 34.5, (AGUA_Z - 1.0 + cima - 0.5) / 2), (5.2, 5.2, cima - 0.5 - (AGUA_Z - 1.0)))
    zocalo = malla_suelta(col, "Zocalo_Deposito", bm, "LIRIA_Piedra")
    zocalo.rotation_euler.z = math.radians(15)
    poner(col, "T_Water_Tower", 22.0, 34.5, z_abajo=cima - 0.6, giro=15, tipo="chapa")
    roca(col, "R_B_Rocks2_00", 27.0, 40.0, giro=-30, hundida=1.5)
    roca(col, "R_S_Rock2_03", 11.0, 1.0, giro=60)
    roca(col, "R_S_Rock2_09", 12.0, 21.0, giro=10)

    # Entre B01 y el barrio residencial, al norte y al sur de la pasarela.
    roca(col, "R_B_Rocks2_04", -15.0, 16.0, giro=110, hundida=1.3)
    roca(col, "R_B_Rocks2_01", -10.0, -4.0, giro=200, hundida=0.8)
    roca(col, "R_S_Rock2_13", -11.0, 2.0, giro=30)
    roca(col, "R_S_Rock2_05", -19.0, 21.0, giro=-20)

    # Escollera al sur de la cabeza del muelle y piedras sueltas entre pilotes.
    roca(col, "R_B_Rocks2_02", -6.0, -27.0, giro=-60, hundida=1.6)
    roca(col, "R_S_Rock2_08", 7.0, -7.0, giro=45)
    roca(col, "R_S_Rock2_01", -6.5, -17.5, giro=80)
    roca(col, "R_S_Rock2_15", -4.5, -6.0, giro=0, hundida=0.9)
    roca(col, "R_S_Rock2_04", 3.0, -4.0, giro=130, hundida=0.5)


def bloque_muelle(col):
    """B01, el «Detalle de muelle» de la hoja de referencia: una plataforma con dos casas de chapa
    sobre pilotes y una pasarela de tablas que entra en el agua, con barcas amarradas."""

    # Las dos casas de la plataforma central del poblado (Base.001).
    grupo_casas(col, (-10.5, -23.0, 5.6, -7.5), destino=(0.0, 9.0))
    # Su borde hacia el mar queda en y = 9 - 7.75 = 1.25.

    # Pasarela: 2 m de ancho, del borde de la plataforma hacia -Y, 12 m. Empieza 0,25 m metida
    # bajo la pasarela de las casas para que no quede rendija entre las dos.
    px = 0.0  # alineada con la pasarela de las casas
    for i in range(3):
        y = -0.5 - 4 * i
        poner(col, "D_Floor_Dock", px, y, z_arriba=0)
        pilotes(col, px, y)

    # Cabeza en T de 6 x 4 m al final.
    yt = -0.5 - 4 * 3
    for i, x in enumerate((px - 2, px, px + 2)):
        poner(col, "D_Floor_Dock_End" if i == 1 else "D_Floor_Dock", x, yt, z_arriba=0)
        pilotes(col, x, yt)

    # Barandilla en el lado este de la pasarela y postes en las juntas.
    xb = px + 1.0
    for i in range(3):
        y = -0.5 - 4 * i
        poner(col, "D_Fence_Plane", xb - 0.05, y, z_abajo=0, giro=90)
        poner(col, "D_Fence_Post", xb - 0.05, y + 2.0, z_abajo=0)
    poner(col, "D_Fence_Post", xb - 0.05, -0.5 - 4 * 2 - 2.0, z_abajo=0)

    # Bolardos en el lado oeste (el de las barcas) y en la cabeza.
    for y in (-2.0, -6.0, -10.0):
        poner(col, "D_Dock_Bollard_1", px - 0.8, y, z_abajo=0)
    for x in (px - 2.6, px + 2.6):
        poner(col, "D_Dock_Bollard_2", x, yt - 1.6, z_abajo=0)

    # Barcas amarradas al lado oeste, con la línea de flotación en el agua.
    poner(col, "D_Boat_A_1", px - 2.2, -2.5, z_abajo=AGUA_Z - 0.25, tipo="barca")
    poner(col, "D_Boat_B_2", px - 2.3, -7.2, z_abajo=AGUA_Z - 0.25, giro=180, tipo="barca")

    # Carga en la cabeza del muelle y junto a la plataforma.
    poner(col, "D_Props_Barrel", px + 1.9, yt + 0.9, z_abajo=0, tipo="prop")
    poner(col, "D_Props_Barrel", px + 2.4, yt + 0.3, z_abajo=0, tipo="prop")
    poner(col, "D_Props_Box", px - 2.4, yt + 0.6, z_abajo=0, giro=20, tipo="prop")
    poner(col, "D_Props_Crate", px - 1.8, yt + 1.1, z_abajo=0, tipo="prop")
    poner(col, "D_Rope_Coil.001", px - 0.5, -3.0, z_abajo=0, tipo="prop")

    entorno_rocoso(col)
    islotes_cercanos(col)

    # Agua provisional.
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, -4, AGUA_Z))
    agua = bpy.context.object
    agua.scale = (60, 60, 1)
    agua.name = "Agua_provisional"
    agua.color = COLOR["agua"]
    mat_agua = bpy.data.materials.new("Agua_provisional")
    mat_agua.diffuse_color = COLOR["agua"]
    agua.data.materials.append(mat_agua)
    mover_a(agua, col)


# ---------------------------------------------------------------- piezas del pueblo

# Grupos de casas del pack Houses_on_the_water (rects en sus coordenadas originales).
CASA_GRANDE = (-7.2, -4.4, 5.6, 11.1)      # casa de dos alas con porche
CASA_MEDIANA = (-21.1, -14.1, -15.0, -5.2)  # casa de 6 x 8 m
CASA_ALARGADA = (-18.6, -32.7, -9.0, -23.8)
CASETA = (-15.6, -1.2, -12.0, 4.3)


def pasarela(col, a, b, z=-0.01):
    """Pasarela de tablas de 2 m de ancho entre dos puntos alineados en x o en y. Queda 1 cm por
    debajo de las cubiertas para que, donde se solapa con ellas, no parpadee."""
    (ax, ay), (bx, by) = a, b
    horizontal = abs(bx - ax) > abs(by - ay)
    largo = abs(bx - ax) if horizontal else abs(by - ay)
    n = max(1, round(largo / 4))
    paso = largo / n
    for i in range(n):
        t = (i + 0.5) / n
        x, y = ax + (bx - ax) * t, ay + (by - ay) * t
        pieza = poner(col, "D_Floor_Dock", x, y, z_arriba=z, giro=90 if horizontal else 0)
        # La tabla mide 4 m de largo: se estira o encoge para cubrir el tramo exacto.
        k = paso / 4.0
        pieza.matrix_world = (Matrix.Translation((x, y, 0)) @ Matrix.Diagonal((k, 1, 1, 1) if horizontal else (1, k, 1, 1))
                              @ Matrix.Translation((-x, -y, 0)) @ pieza.matrix_world)
        if horizontal:
            pilotes(col, x, y, ancho=paso, largo=2.0)
        else:
            pilotes(col, x, y, ancho=2.0, largo=paso)


def plataforma(col, x0, y0, nx, ny, z=0.0):
    """Plataforma de nx x ny tablas de 4 x 4 m con pilotes, desde la esquina (x0, y0)."""
    for i in range(nx):
        for j in range(ny):
            x, y = x0 + 2 + 4 * i, y0 + 2 + 4 * j
            poner(col, "D_Floor_Floor", x, y, z_arriba=z)
            pilotes(col, x, y, ancho=4.0, largo=4.0)


def barandilla(col, a, b):
    """Barandilla de paneles de 4 m entre dos puntos alineados."""
    (ax, ay), (bx, by) = a, b
    horizontal = abs(bx - ax) > abs(by - ay)
    largo = abs(bx - ax) if horizontal else abs(by - ay)
    n = max(1, round(largo / 4))
    for i in range(n):
        t = (i + 0.5) / n
        poner(col, "D_Fence_Plane", ax + (bx - ax) * t, ay + (by - ay) * t, z_abajo=0,
              giro=0 if horizontal else 90)
    for i in range(n + 1):
        t = i / n
        poner(col, "D_Fence_Post", ax + (bx - ax) * t, ay + (by - ay) * t, z_abajo=0)


def viga(bm, a, b, grosor):
    """Caja de sección cuadrada entre dos puntos."""
    a, b = Vector(a), Vector(b)
    d = b - a
    rot = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
    escala = Matrix.Diagonal((grosor, grosor, d.length, 1))
    bmesh.ops.create_cube(bm, size=1, matrix=Matrix.Translation((a + b) / 2) @ rot @ escala)


def caja(bm, centro, tam):
    bmesh.ops.create_cube(bm, size=1, matrix=Matrix.Translation(centro) @ Matrix.Diagonal((*tam, 1)))


def colocar(objetos, x, y, z, giro):
    for o in objetos:
        o.matrix_world = Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.radians(giro), 4, "Z")


def grua(col, x, y, giro=0):
    """Grúa de muelle como la de la hoja: torre de madera arriostrada, caseta de chapa, pluma de
    dos largueros que sale hacia +Y local, contrapeso y gancho colgando."""
    madera, chapa, cuerda, peso = bmesh.new(), bmesh.new(), bmesh.new(), bmesh.new()
    alto = 9.0
    for sx in (-1, 1):
        for sy in (-1, 1):
            viga(madera, (sx * 1.5, sy * 1.5, 0), (sx * 1.1, sy * 1.1, alto), 0.32)
    for z0, z1 in ((0.0, 3.0), (3.0, 6.0), (6.0, alto)):
        r0, r1 = 1.5 - 0.4 * z0 / alto, 1.5 - 0.4 * z1 / alto
        for k in range(4):
            a = math.radians(45 + 90 * k)
            b = math.radians(45 + 90 * (k + 1))
            p0 = Vector((math.cos(a), math.sin(a), 0)) * r0 * math.sqrt(2)
            p1 = Vector((math.cos(b), math.sin(b), 0)) * r1 * math.sqrt(2)
            q1 = Vector((math.cos(b), math.sin(b), 0)) * r0 * math.sqrt(2)
            viga(madera, p0 + Vector((0, 0, z0)), p1 + Vector((0, 0, z1)), 0.16)    # diagonal
            viga(madera, p0 + Vector((0, 0, z0)), q1 + Vector((0, 0, z0)), 0.2)     # anillo
    caja(madera, (0, 0, alto + 0.12), (3.2, 3.2, 0.25))
    caja(chapa, (0, -0.4, alto + 1.35), (2.4, 2.8, 2.2))
    caja(chapa, (0, -0.4, alto + 2.55), (2.9, 3.3, 0.18))
    # Pluma inclinada y contrapluma.
    for sx in (-0.45, 0.45):
        viga(madera, (sx, 1.2, alto + 0.8), (sx * 0.5, 15.0, alto + 6.0), 0.24)
        viga(madera, (sx, -1.6, alto + 0.8), (sx, -5.2, alto + 1.2), 0.24)
    for k in range(1, 7):
        t = k / 7
        yk, zk = 1.2 + (15.0 - 1.2) * t, alto + 0.8 + 5.2 * t
        w = 0.45 * (1 - 0.5 * t)
        viga(madera, (-w, yk, zk), (w, yk, zk), 0.12)
        viga(madera, (-w, yk, zk), (0, yk + 1.0, zk + 1.2), 0.1)
    viga(madera, (0, -1.0, alto + 2.6), (0, 15.0, alto + 6.0), 0.14)   # tirante superior
    caja(peso, (0, -5.0, alto + 0.6), (1.6, 1.3, 1.4))
    viga(cuerda, (0, 15.0, alto + 6.0), (0, 15.0, 4.2), 0.06)
    caja(chapa, (0, 15.0, 3.9), (0.35, 0.35, 0.5))
    objetos = [malla_suelta(col, "Grua_Madera", madera, "LIRIA_Madera"),
               malla_suelta(col, "Grua_Chapa", chapa, "LIRIA_Chapa"),
               malla_suelta(col, "Grua_Cable", cuerda, "LIRIA_Tela"),
               malla_suelta(col, "Grua_Contrapeso", peso, "LIRIA_Roca")]
    colocar(objetos, x, y, 0.0, giro)
    return objetos


def faro(col, x, y, z):
    """Faro de bandas rojas y blancas, galería con barandilla, linterna de cristal y tejado rojo."""
    bandas = {"LIRIA_FaroRojo": bmesh.new(), "LIRIA_Blanco": bmesh.new()}
    chapa, cristal, chapa_base = bmesh.new(), bmesh.new(), bmesh.new()
    n, alto_banda = 5, 2.6
    caja_z = 0.0
    # Base que baja hasta el agua: la cima de la roca no es plana y el faro no debe quedar volado.
    bmesh.ops.create_cone(bandas["LIRIA_Blanco"], cap_ends=True, segments=10, radius1=3.0, radius2=2.8,
                          depth=1.0, matrix=Matrix.Translation((0, 0, 0.5)))
    bmesh.ops.create_cone(chapa_base, cap_ends=True, segments=10, radius1=3.6, radius2=3.0,
                          depth=z - AGUA_Z + 1.0, matrix=Matrix.Translation((0, 0, -(z - AGUA_Z + 1.0) / 2)))
    caja_z = 1.0
    for k in range(n):
        r0 = 2.3 - 0.8 * k / n
        r1 = 2.3 - 0.8 * (k + 1) / n
        bm = bandas["LIRIA_FaroRojo" if k % 2 == 0 else "LIRIA_Blanco"]
        bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=r0, radius2=r1, depth=alto_banda,
                              matrix=Matrix.Translation((0, 0, caja_z + alto_banda / 2)))
        caja_z += alto_banda
    bmesh.ops.create_cone(chapa, cap_ends=True, segments=12, radius1=2.3, radius2=2.3, depth=0.3,
                          matrix=Matrix.Translation((0, 0, caja_z + 0.15)))
    for k in range(12):
        a, b = math.radians(30 * k), math.radians(30 * (k + 1))
        pa = Vector((math.cos(a) * 2.15, math.sin(a) * 2.15, caja_z + 0.3))
        pb = Vector((math.cos(b) * 2.15, math.sin(b) * 2.15, caja_z + 0.3))
        viga(chapa, pa, pa + Vector((0, 0, 0.9)), 0.07)
        viga(chapa, pa + Vector((0, 0, 0.9)), pb + Vector((0, 0, 0.9)), 0.07)
    bmesh.ops.create_cone(cristal, cap_ends=True, segments=10, radius1=1.15, radius2=1.15, depth=1.8,
                          matrix=Matrix.Translation((0, 0, caja_z + 1.2)))
    bmesh.ops.create_cone(bandas["LIRIA_FaroRojo"], cap_ends=True, segments=10, radius1=1.5, radius2=0.12,
                          depth=1.6, matrix=Matrix.Translation((0, 0, caja_z + 2.9)))
    objetos = [malla_suelta(col, "Faro_" + cat, bm, cat) for cat, bm in bandas.items()]
    objetos += [malla_suelta(col, "Faro_Zocalo", chapa_base, "LIRIA_Piedra"),
                malla_suelta(col, "Faro_Galeria", chapa, "LIRIA_Chapa"),
                malla_suelta(col, "Faro_Linterna", cristal, "LIRIA_Cristal")]
    colocar(objetos, x, y, z, 0)
    return objetos


PISO = 3.12          # altura de piso del kit Medieval Village
GROSOR_MURO = 0.095


def agrupar_giro(objetos, cx, cy, giro):
    """Gira un grupo de objetos ya colocados alrededor de (cx, cy)."""
    if giro == 0:
        return
    m = (Matrix.Translation((cx, cy, 0)) @ Matrix.Rotation(math.radians(giro), 4, "Z")
         @ Matrix.Translation((-cx, -cy, 0)))
    for o in objetos:
        o.matrix_world = m @ o.matrix_world


def casa_piedra(col, cx, cy, ancho, fondo, pisos, semilla, giro=0, balcon=False):
    """Casa del kit Medieval Village (misma lógica que CasaModular.cs): planta baja de piedra,
    pisos de revoque o entramado, tejado de teja con frontón y chimenea. La fachada mira a -Y y
    luego el conjunto se gira 'giro' grados sobre su centro."""
    azar = random.Random(semilla)
    w, d = ancho / 2, fondo / 2
    madera = azar.randrange(3) == 0
    puerta = azar.randrange(ancho // 2)
    piezas = []

    def p(nombre, x, y, z, g):
        piezas.append(poner(col, "V_" + nombre, cx + x, cy + y, z_abajo=z + 0.0, giro=g, tipo="casa"))

    for piso in range(pisos):
        z = piso * PISO
        for i in range(ancho // 2):
            x = -w + 1 + 2 * i
            y = -d + GROSOR_MURO
            if piso == 0 and i == puerta:
                p("Wall_UnevenBrick_Door_Round", x, y, z, 0)
                p("Door_1_Round", x, y, z, 0)
            elif piso == 0:
                p("Wall_UnevenBrick_Window_Wide_Round", x, y, z, 0)
                p("Window_Wide_Round1", x, y, z, 0)
            else:
                p("Wall_Plaster_Window_Wide_Flat2" if madera and azar.randrange(2) == 0 else "Wall_Plaster_Window_Wide_Flat", x, y, z, 0)
                p("Window_Wide_Flat1", x, y, z, 0)
                if azar.randrange(3) == 0:
                    p("WindowShutters_Wide_Flat_Open", x, y, z, 0)
        for i in range(ancho // 2):
            x = -w + 1 + 2 * i
            ventana = piso > 0 and azar.randrange(2) == 0
            p("Wall_UnevenBrick_Straight" if piso == 0 else ("Wall_Plaster_Window_Thin_Round" if ventana else "Wall_Plaster_Straight"), x, d - GROSOR_MURO, z, 180)
            if ventana:
                p("Window_Thin_Round1", x, d - GROSOR_MURO, z, 180)
        for j in range(fondo // 2):
            y = -d + 1 + 2 * j
            for lado in (-1, 1):
                ventana = piso > 0 and azar.randrange(3) == 0
                if piso == 0:
                    muro = "Wall_UnevenBrick_Straight"
                elif madera:
                    muro = "Wall_Plaster_WoodGrid"
                else:
                    muro = "Wall_Plaster_Window_Thin_Round" if ventana else "Wall_Plaster_Straight"
                p(muro, lado * (w - GROSOR_MURO), y, z, 90 * lado)
                if ventana and not madera:
                    p("Window_Thin_Round1", lado * (w - GROSOR_MURO), y, z, 90 * lado)
        for ex in (-1, 1):
            for ey in (-1, 1):
                g = {(-1, -1): 0, (1, -1): 90, (1, 1): 180, (-1, 1): -90}[(ex, ey)]
                p("Corner_Exterior_Brick" if piso == 0 else "Corner_Exterior_Wood", ex * w, ey * d, z, g)
    alto = pisos * PISO
    piezas.append(poner(col, f"V_Roof_RoundTiles_{ancho}x{fondo}", cx, cy, z_abajo=alto - 0.78, tipo="casa"))
    p(f"Roof_Front_Brick{ancho}", 0, -d + GROSOR_MURO, alto - 0.13, 0)
    p(f"Roof_Front_Brick{ancho}", 0, d - GROSOR_MURO, alto - 0.13, 180)
    if azar.randrange(2) == 0:
        p("Prop_Chimney" if azar.randrange(2) == 0 else "Prop_Chimney2",
          (1 if azar.randrange(2) else -1) * w * 0.5, d * 0.4, alto + 0.6, 0)
    if pisos > 1 and (balcon or azar.randrange(3) == 0):
        p("Balcony_Simple_Straight", -w + 1 + 2 * azar.randrange(ancho // 2), -d, PISO, 0)
    agrupar_giro(piezas, cx, cy, giro)
    return piezas


def muelle_piedra(col, x0, y0, x1, y1):
    """Muelle de piedra: un zócalo macizo desde el fondo hasta la cubierta, enlosado con las
    baldosas de piedra del kit Village y rematado con bordillo."""
    bm = bmesh.new()
    caja(bm, ((x0 + x1) / 2, (y0 + y1) / 2, (AGUA_Z - 1.2 - 0.04) / 2), (x1 - x0, y1 - y0, -(AGUA_Z - 1.2) - 0.04))
    malla_suelta(col, "Zocalo_Piedra", bm, "LIRIA_Piedra")
    for x in range(int(x0) + 1, int(x1), 2):
        for y in range(int(y0) + 1, int(y1), 2):
            poner(col, "V_Floor_UnevenBrick", x, y, z_arriba=0.0, tipo="casa")
    for x in range(int(x0) + 1, int(x1), 2):
        poner(col, "V_Prop_ExteriorBorder_Straight1", x, y0, z_abajo=-0.05, tipo="casa")
        poner(col, "V_Prop_ExteriorBorder_Straight1", x, y1, z_abajo=-0.05, giro=180, tipo="casa")
    for y in range(int(y0) + 1, int(y1), 2):
        poner(col, "V_Prop_ExteriorBorder_Straight1", x0, y, z_abajo=-0.05, giro=-90, tipo="casa")
        poner(col, "V_Prop_ExteriorBorder_Straight1", x1, y, z_abajo=-0.05, giro=90, tipo="casa")


def farol(col, x, y, tipo="pasarela"):
    """Farol de calle: el de Kenney Fantasy Town en pasarelas y el de Graveyard en la piedra."""
    if tipo == "pasarela":
        poner(col, "KF_lantern", x, y, z_abajo=0.0, escala=1.7, tipo="prop")
    else:
        poner(col, "KG_lightpost-single", x, y, z_abajo=0.0, escala=2.1, tipo="prop")


def tendedero(col, a, b, alto, semilla):
    """Cuerda de ropa tendida entre dos puntos, con prendas de colores colgando."""
    azar = random.Random(semilla)
    cuerda, prendas = bmesh.new(), {c: bmesh.new() for c in ("LIRIA_Tela", "LIRIA_Toldo", "LIRIA_FaroRojo", "LIRIA_Blanco")}
    a = Vector((a[0], a[1], alto))
    b = Vector((b[0], b[1], alto))
    viga(cuerda, a, b, 0.03)
    for poste in (a, b):
        viga(cuerda, poste - Vector((0, 0, alto)), poste, 0.12)
    n = max(3, int((b - a).length / 0.8))
    lado = (b - a).normalized()
    for i in range(1, n):
        if azar.random() < 0.25:
            continue
        c = a + (b - a) * (i / n) - Vector((0, 0, 0.05))
        alto_p = azar.uniform(0.45, 0.9)
        ancho_p = azar.uniform(0.35, 0.6)
        rot = Matrix.Rotation(math.atan2(lado.y, lado.x), 4, "Z")
        bmesh.ops.create_cube(prendas[azar.choice(list(prendas))], size=1,
                              matrix=Matrix.Translation(c - Vector((0, 0, alto_p / 2))) @ rot
                              @ Matrix.Diagonal((ancho_p, 0.03, alto_p, 1)))
    malla_suelta(col, "Tendedero_Cuerda", cuerda, "LIRIA_Madera")
    for cat, bm in prendas.items():
        if len(bm.verts):
            malla_suelta(col, "Tendedero_Ropa", bm, cat)
        else:
            bm.free()


def puesto(col, x, y, giro, semilla):
    """Puesto de mercado: cuatro postes, mostrador y toldo inclinado, con algo de género encima."""
    azar = random.Random(semilla)
    madera, toldo = bmesh.new(), bmesh.new()
    for sx in (-1.1, 1.1):
        for sy in (-0.7, 0.7):
            viga(madera, (sx, sy, 0), (sx, sy, 2.4 if sy > 0 else 2.0), 0.12)
    caja(madera, (0, -0.2, 0.9), (2.3, 1.0, 0.12))
    caja(madera, (0, -0.2, 0.45), (2.2, 0.9, 0.9))
    bmesh.ops.create_cube(toldo, size=1, matrix=Matrix.Translation((0, 0, 2.25))
                          @ Matrix.Rotation(math.radians(-17), 4, "X") @ Matrix.Diagonal((2.6, 1.8, 0.05, 1)))
    objetos = [malla_suelta(col, "Puesto_Madera", madera, "LIRIA_Madera"),
               malla_suelta(col, "Puesto_Toldo", toldo, azar.choice(("LIRIA_Toldo", "LIRIA_FaroRojo", "LIRIA_Tela")))]
    colocar(objetos, x, y, 0.0, giro)
    for k in range(azar.randint(1, 3)):
        poner(col, "D_Props_Box" if k % 2 else "D_Props_Barrel", x + azar.uniform(-1.2, 1.2),
              y + azar.uniform(-1.6, -1.0), z_abajo=0, giro=azar.uniform(0, 90), tipo="prop", escala=0.8)


def barca(col, nombre, x, y, giro, escala):
    poner(col, nombre, x, y, z_abajo=AGUA_Z - 0.35 * escala, giro=giro, tipo="barca", escala=escala)


# ---------------------------------------------------------------- bloques del pueblo
# Trazado (planta de Blender, el mar hacia -Y): B01 en el centro; el barrio residencial al oeste,
# el canal al este, el muelle industrial al sureste junto a la cabeza de B01 y el faro sobre un
# peñasco al noroeste. Todo lo pisable queda unido por pasarelas a z = 0.

def bloque_residencial(col):
    """B02: la casa grande de dos alas, una casa alargada detrás y una caseta, al oeste de B01."""
    pasarela(col, (-7.5, 10.0), (-24.6, 10.0))
    grupo_casas(col, CASA_GRANDE, destino=(-30.0, 8.0))
    pasarela(col, (-30.0, 15.4), (-30.0, 21.9))
    grupo_casas(col, CASA_ALARGADA, destino=(-30.0, 26.2))
    pasarela(col, (-35.4, 10.0), (-41.6, 10.0))
    grupo_casas(col, CASETA, destino=(-43.5, 10.0), giro=90)
    for xy in ((-24.5, 2.0), (-24.0, 2.6)):
        poner(col, "D_Props_Barrel", *xy, z_abajo=0, tipo="prop")
    poner(col, "D_Boat_A_2", -21.0, -1.5, z_abajo=AGUA_Z - 0.25, giro=90, tipo="barca")


def bloque_canal(col):
    """B03: el canal principal. Dos orillas de tablas con casas, un canal de 8 m en medio y un
    puente de tablas con barandillas que las une."""
    pasarela(col, (7.5, 10.0), (16.2, 10.0))
    plataforma(col, 16.0, 2.0, 2, 3)        # orilla oeste, x 16..24, y 2..14
    plataforma(col, 32.0, 2.0, 2, 3)        # orilla este, x 32..40, y 2..14
    pasarela(col, (23.8, 10.0), (32.2, 10.0))   # puente
    barandilla(col, (24.0, 9.0), (32.0, 9.0))
    barandilla(col, (24.0, 11.0), (32.0, 11.0))
    barandilla(col, (24.0, 2.0), (24.0, 9.0))
    barandilla(col, (32.0, 2.0), (32.0, 9.0))
    grupo_casas(col, CASA_MEDIANA, destino=(20.0, 16.85), giro=90)
    grupo_casas(col, CASA_MEDIANA, destino=(36.0, 16.85), giro=-90)
    # Barcas por el canal.
    poner(col, "D_Boat_B_1", 28.0, 4.0, z_abajo=AGUA_Z - 0.25, tipo="barca")
    poner(col, "D_Boat_A_3", 28.0, 17.0, z_abajo=AGUA_Z - 0.25, giro=170, tipo="barca")
    for x, y in ((17.0, 3.0), (17.6, 3.5), (39.0, 3.2)):
        poner(col, "D_Props_Barrel", x, y, z_abajo=0, tipo="prop")


def bloque_industrial(col):
    """B04: el muelle industrial, una explanada de tablas con la grúa, carga y barcas."""
    yt = -12.5
    pasarela(col, (2.8, yt), (11.2, yt))
    plataforma(col, 11.0, -26.0, 5, 4)      # x 11..31, y -26..-10
    grua(col, 26.0, -21.0, giro=200)
    for k, (x, y) in enumerate(((15.0, -13.0), (16.0, -13.2), (15.4, -14.1), (20.0, -24.5),
                                (21.0, -24.5), (14.0, -24.0))):
        poner(col, "D_Props_Box" if k % 2 else "D_Props_Barrel", x, y, z_abajo=0, giro=k * 23, tipo="prop")
    poner(col, "D_Props_Crate", 18.0, -18.0, z_abajo=0, tipo="prop")
    poner(col, "D_Props_Crate", 18.6, -17.5, z_abajo=0, giro=30, tipo="prop")
    for x in (13.0, 19.0, 25.0, 30.0):
        poner(col, "D_Dock_Bollard_1", x, -25.5, z_abajo=0)
    poner(col, "D_Boat_B_3", 16.0, -28.5, z_abajo=AGUA_Z - 0.25, giro=90, tipo="barca")
    poner(col, "D_Boat_A_1", 24.0, -28.5, z_abajo=AGUA_Z - 0.25, giro=95, tipo="barca")


def bloque_faro(col):
    """B05: el faro sobre el peñasco más alto, al noroeste, visible desde todo el pueblo."""
    base = islote(col, -38.0, 40.0, 2.4, 21, arboles=0)
    mejor = None
    for i in range(-6, 7):
        for j in range(-6, 7):
            px, py = -38.0 + i * 0.8, 40.0 + j * 0.8
            z = cima(base, px, py)
            if z is not None and (mejor is None or z > mejor[2]):
                mejor = (px, py, z)
    faro(col, mejor[0], mejor[1], mejor[2] - 0.6)
    # Árboles detrás del faro (al norte y al oeste), para que no lo tapen desde el pueblo.
    for k, (dx, dy) in enumerate(((-4.5, 4.0), (3.5, 5.5), (-6.0, -1.0))):
        z = cima(base, mejor[0] + dx, mejor[1] + dy)
        if z is not None:
            arbol(col, mejor[0] + dx, mejor[1] + dy, z, 6.5, 300 + k)


def bloque_piedra(col):
    """B06: el barrio de piedra, al norte de B01. Un muelle de piedra con una calle entre dos
    hileras de casas del kit Medieval Village, faroles, ropa tendida y el depósito cerca."""
    pasarela(col, (0.0, 16.4), (0.0, 24.3))
    muelle_piedra(col, -18.0, 24.0, 16.0, 46.0)
    # Hilera sur (fachadas a +Y, hacia la calle), con un hueco en x = 0 para la pasarela.
    for k, (x, ancho, pisos) in enumerate(((-14.0, 6, 3), (-7.0, 6, 2), (6.0, 6, 3), (12.5, 4, 2))):
        casa_piedra(col, x, 28.0, ancho, 8, pisos, 40 + k, giro=180)
    # Hilera norte (fachadas a -Y), pegadas como en un casco antiguo.
    for k, (x, ancho, pisos) in enumerate(((-14.0, 6, 2), (-7.5, 6, 3), (-1.5, 4, 2), (4.0, 6, 3), (11.0, 8, 2))):
        casa_piedra(col, x, 42.0, ancho, 8, pisos, 60 + k, balcon=k % 2 == 0)
    for x in (-15.0, -6.0, 3.0, 12.0):
        farol(col, x, 32.6, tipo="piedra")
    tendedero(col, (-10.5, 32.4), (-10.5, 37.6), 4.6, 1)
    tendedero(col, (8.0, 32.4), (8.0, 37.6), 4.4, 2)
    poner(col, "KF_fountain-round", -3.0, 35.0, z_abajo=0.0, escala=1.6, tipo="prop")
    for x, y in ((14.5, 34.5), (14.8, 35.4), (-17.0, 35.2)):
        poner(col, "D_Props_Barrel", x, y, z_abajo=0, tipo="prop")


def bloque_palafitos(col):
    """B07: el barrio palafítico del suroeste, con la plaza del mercado sobre pilotes."""
    pasarela(col, (-3.2, -12.5), (-14.2, -12.5))
    plataforma(col, -26.0, -18.0, 3, 2)                 # plaza x -26..-14, y -18..-10
    for k, (x, y, g) in enumerate(((-23.0, -15.5, 0), (-18.5, -15.5, 0), (-23.0, -11.8, 180))):
        puesto(col, x, y, g, 70 + k)
    farol(col, -14.6, -10.6)
    farol(col, -25.4, -17.4)
    grupo_casas(col, CASA_GRANDE, destino=(-33.6, -14.0), giro=90)
    pasarela(col, (-20.0, -18.2), (-20.0, -19.6))
    grupo_casas(col, (-10.5, -23.0, 5.6, -7.5), destino=(-20.0, -27.3), giro=180)
    pasarela(col, (-20.0, -9.8), (-20.0, -6.3))
    grupo_casas(col, CASETA, destino=(-20.5, -3.7))
    pasarela(col, (-41.4, -18.0), (-41.4, -25.0))
    grupo_casas(col, CASA_MEDIANA, destino=(-41.4, -29.4))
    tendedero(col, (-26.4, -21.0), (-26.4, -16.0), 3.4, 3)
    tendedero(col, (-30.0, -6.5), (-24.0, -6.5), 3.6, 4)


def barcos_y_detalles(col):
    """Barcas, barcos y boyas por toda la bahía, faroles en las pasarelas y banderas."""
    for nombre, x, y, g, e in (
            ("KP_ship-medium", 21.0, -32.5, 90, 1.25), ("KP_ship-small", -42.0, -40.0, 30, 1.2),
            ("KW_boat-fishing-small", 12.0, -4.0, 80, 1.8), ("KW_boat-fishing-small", 44.0, -18.0, 200, 1.8),
            ("KW_boat-sail-a", 40.0, -8.0, 15, 1.8), ("KW_boat-sail-b", -50.0, 4.0, 100, 1.8),
            ("KW_boat-tug-a", 36.0, -24.0, 160, 1.8), ("KW_boat-house-a", 46.0, 24.0, 90, 1.8),
            ("KW_boat-row-small", 28.0, -3.5, 10, 1.6), ("KW_boat-row-large", -30.0, -38.0, 60, 1.6),
            ("KW_boat-fishing-small", -48.0, -24.0, 20, 1.8), ("KW_boat-sail-a", 10.0, 50.0, 250, 1.8),
            ("KW_boat-row-small", -22.0, 22.0, 140, 1.6), ("KW_boat-row-small", 18.0, 26.0, 75, 1.6)):
        barca(col, nombre, x, y, g, e)
    for x, y in ((8.0, -40.0), (20.0, -42.0), (-14.0, -44.0), (50.0, -8.0)):
        poner(col, "KW_buoy", x, y, z_abajo=AGUA_Z - 0.6, escala=1.2, tipo="prop")
    # Faroles a lo largo de las pasarelas y en las cabezas de muelle.
    for x, y in ((1.2, -4.0), (1.2, -9.0), (-2.6, -14.0), (-12.0, 11.2), (-19.0, 11.2),
                 (11.0, 11.2), (6.0, -11.4), (12.0, -10.6), (30.4, -10.6), (30.4, -25.4),
                 (24.4, 3.0), (31.6, 3.0), (1.2, 20.0)):
        farol(col, x, y)
    poner(col, "KP_flag-high", 12.0, -25.0, z_abajo=0.0, escala=1.3, tipo="prop")
    poner(col, "KP_flag-high-pennant", -24.0, -10.6, z_abajo=0.0, escala=1.1, tipo="prop")
    tendedero(col, (-2.6, 11.8), (2.6, 11.8), 3.3, 5)


# ---------------------------------------------------------------- vegetación e islotes

def malla_suelta(col, nombre, bm, material):
    """Convierte un bmesh en objeto con un único material de categoría."""
    malla = bpy.data.meshes.new(nombre)
    bm.to_mesh(malla)
    bm.free()
    for p in malla.polygons:
        p.use_smooth = False
    malla.materials.append(bpy.data.materials[material])
    o = bpy.data.objects.new(nombre, malla)
    col.objects.link(o)
    return o


def arbol(col, x, y, z, alto, semilla):
    """Árbol de copa redonda como los de la hoja: tronco de 6 caras algo torcido y una copa de
    bolas facetadas. Sin texturas ni hojas recortadas: así el sombreado en bandas y la tinta lo
    dibujan como el resto del escenario."""
    azar = random.Random(semilla)
    bm = bmesh.new()
    inclina = Vector((azar.uniform(-0.25, 0.25), azar.uniform(-0.25, 0.25), 1)).normalized()
    largo = alto * 0.55
    bmesh.ops.create_cone(bm, cap_ends=True, segments=6, radius1=alto * 0.05, radius2=alto * 0.03,
                          depth=largo, matrix=Matrix.Translation(inclina * largo / 2)
                          @ inclina.to_track_quat("Z", "Y").to_matrix().to_4x4())
    tronco = malla_suelta(col, "Arbol_Tronco", bm, "LIRIA_Madera")
    tronco.location = (x, y, z - 0.3)

    bm = bmesh.new()
    centro = inclina * largo
    radio = alto * 0.26
    for i in range(azar.randint(4, 6)):
        d = Vector((azar.uniform(-1, 1), azar.uniform(-1, 1), azar.uniform(-0.2, 0.8))) * radio * 0.9
        r = radio * azar.uniform(0.6, 1.0)
        res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=r,
                                         matrix=Matrix.Translation(centro + d))
        for v in res["verts"]:
            v.co += Vector((azar.uniform(-1, 1) for _ in range(3))) * r * 0.12
    copa = malla_suelta(col, "Arbol_Copa", bm, "LIRIA_Follaje")
    copa.location = (x, y, z - 0.3)
    return copa


def cima(obj, x, y):
    """Altura de la superficie de un objeto en (x, y), o None si no está debajo."""
    inv = obj.matrix_world.inverted()
    origen = inv @ Vector((x, y, 500))
    destino = inv @ Vector((x, y, -500))
    ok, punto, _, _ = obj.ray_cast(origen, (destino - origen).normalized())
    return (obj.matrix_world @ punto).z if ok else None


def islote(col, x, y, escala, semilla, arboles=3, rocas=("R_B_Rocks2_00", "R_B_Rocks2_03", "R_B_Rocks2_04")):
    """Islote de roca con árboles en lo alto, como los que rodean el pueblo en la vista cenital."""
    azar = random.Random(semilla)
    base = poner(col, azar.choice(rocas), x, y, z_abajo=AGUA_Z - 2.0 * escala,
                 giro=azar.uniform(0, 360), tipo="roca")
    base.scale = (escala, escala, escala * azar.uniform(0.9, 1.3))
    bpy.context.view_layer.update()
    puestos = 0
    for _ in range(arboles * 6):
        if puestos >= arboles:
            break
        ax = x + azar.uniform(-3, 3) * escala
        ay = y + azar.uniform(-2.5, 2.5) * escala
        z = cima(base, ax, ay)
        if z is None or z < 0.3:
            continue
        arbol(col, ax, ay, z, azar.uniform(5, 8) * min(1.6, 0.6 + escala * 0.4), azar.random())
        puestos += 1
    return base


def islotes_cercanos(col):
    """Islotes alrededor de B01: rodean el muelle sin tocar lo pisable."""
    islote(col, 36.0, 50.0, 1.4, 11, arboles=4)
    islote(col, -2.0, 54.0, 1.6, 12, arboles=4)
    islote(col, -58.0, -14.0, 1.1, 13, arboles=2)
    islote(col, 42.0, -32.0, 1.2, 14, arboles=3)
    islote(col, -4.0, -42.0, 0.8, 15, arboles=1)
    islote(col, 52.0, 8.0, 1.3, 16, arboles=3)
    islote(col, -56.0, 28.0, 1.5, 17, arboles=4)


# ---------------------------------------------------------------- fondo: montañas e islas lejanas

def montanas(col):
    """Cordillera en anillo detrás del pueblo, entre 330 y 820 m. Es lo que se ve a lo lejos en las
    cinemáticas. Deja abierto el frente (-Y, hacia donde sale el muelle) para que se vea el horizonte
    del mar con algunas islas. Facetada a propósito: con el sombreado en bandas se lee como las
    montañas dibujadas de la hoja."""
    bm = bmesh.new()
    a0, a1 = math.radians(-50), math.radians(230)   # ángulo desde +X; +Y queda detrás del pueblo
    na, nr = 160, 26
    r0, r1 = 330.0, 820.0
    filas = []
    for i in range(na + 1):
        t = i / na
        a = a0 + (a1 - a0) * t
        extremo = math.sin(math.pi * t) ** 0.35          # baja hacia los dos extremos del arco
        fila = []
        for j in range(nr + 1):
            u = j / nr
            r = r0 + (r1 - r0) * u
            sobre = math.sin(math.pi * u) ** 1.6            # sube poco a poco hacia el centro del anillo
            p = Vector((math.cos(a) * r, math.sin(a) * r, 0))
            q = p * 0.75   # el ruido conserva la escala de antes: misma silueta, más lejos
            n = noise.fractal(q * 0.008, 0.55, 2.1, 5)
            cresta = 1.0 - abs(noise.noise(q * 0.004 + Vector((7, 3, 0))))
            h = (25 + 160 * max(0.0, 0.5 + 0.7 * n) * cresta) * sobre * extremo
            fila.append(bm.verts.new((p.x, p.y, AGUA_Z - 6 + h)))
        filas.append(fila)
    for i in range(na):
        for j in range(nr):
            bm.faces.new((filas[i][j], filas[i + 1][j], filas[i + 1][j + 1], filas[i][j + 1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = malla_suelta(col, "Montanas", bm, "LIRIA_Montana")
    if sum(p.normal.z for p in o.data.polygons) < 0:
        o.data.flip_normals()
    return o


def islas_lejanas(col):
    """Islas sueltas en el frente abierto, entre 90 y 200 m, para que el horizonte no quede vacío."""
    for k, (ang, r, esc) in enumerate(((235, 120, 3.5), (255, 190, 5.0), (285, 150, 4.0),
                                       (305, 95, 2.6), (320, 210, 6.0), (205, 160, 4.5))):
        a = math.radians(ang)
        islote(col, math.cos(a) * r, math.sin(a) * r, esc, 100 + k, arboles=3)


# ---------------------------------------------------------------- unificación y exportación

EXPORTAR = os.path.join(RAIZ, "Assets", "Meshes", "Liria")
CELDA = 0.25          # resolución del mapa de paso
ALTO_LIMITE = 2.5     # altura de las paredes invisibles
GROSOR_LIMITE = 0.2   # y su grosor
INICIO_NILO = (0.0, 0.0)  # en planta de Blender: arranque de la pasarela, junto a las casas


def unificar(bloque, nombre):
    """Copia real de todo el bloque (menos el agua) fusionada en una sola malla, en 90_EXPORT."""
    destino = bpy.data.collections["90_EXPORT"]
    copias = []
    for o in bloque.objects:
        if o.type != "MESH" or o.name.startswith("Agua"):
            continue
        c = o.copy()
        c.data = o.data.copy()
        destino.objects.link(c)
        copias.append(c)
    bpy.ops.object.select_all(action="DESELECT")
    for c in copias:
        c.select_set(True)
    bpy.context.view_layer.objects.active = copias[0]
    bpy.ops.object.join()
    geo = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    geo.name = geo.data.name = nombre + "_Geo"
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=0.0005)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.material_slot_remove_unused()
    return geo


def _bordes(n_a, n_b, celda, fijo, x0, y0, horizontal):
    """Tramos de borde entre filas (horizontal) o columnas, fusionados en segmentos largos."""
    tramos = []
    for lado in (0, 1):
        k = 0
        while k < n_a:
            def es_borde(k):
                a, b = celda(k, fijo - 1) if horizontal else celda(fijo - 1, k), celda(k, fijo) if horizontal else celda(fijo, k)
                return (a and not b) if lado == 0 else (b and not a)
            if not es_borde(k):
                k += 1
                continue
            ini = k
            while k < n_a and es_borde(k):
                k += 1
            if horizontal:
                y = y0 + fijo * CELDA
                tramos.append(((x0 + ini * CELDA, y), (x0 + k * CELDA, y)))
            else:
                x = x0 + fijo * CELDA
                tramos.append(((x, y0 + ini * CELDA), (x, y0 + k * CELDA)))
    return tramos


def limites(geos, nombre):
    """Paredes invisibles en el borde de lo que se puede pisar.

    Nilo no tiene gravedad ni salto (PlayerController.cs): lo único que le impide salir al agua
    son colisiones. Se hace un mapa de celdas de CELDA m: una celda es pisable si un rayo desde
    1 m baja y encuentra suelo plano a la altura de la cubierta. Donde una celda pisable toca una
    que no lo es, se levanta una pared."""
    deps = bpy.context.evaluated_depsgraph_get()
    nombres = {g.name for g in geos}
    esquinas = [g.matrix_world @ Vector(c) for g in geos for c in g.bound_box]
    mn = Vector([min(c[i] for c in esquinas) for i in range(3)])
    mx = Vector([max(c[i] for c in esquinas) for i in range(3)])
    nx = int((mx.x - mn.x) / CELDA) + 2
    ny = int((mx.y - mn.y) / CELDA) + 2
    x0, y0 = mn.x - CELDA, mn.y - CELDA
    escena = bpy.context.scene

    def pisable(i, j):
        # El rayo baja desde la altura de Nilo: lo primero que toca tiene que ser el suelo. Si toca
        # antes un barril, una pared o un poste, la celda no se pisa y queda dentro de los límites.
        # Así los límites son la única colisión que necesita Nilo (ver PrepararLiria.cs).
        p = Vector((x0 + (i + 0.5) * CELDA, y0 + (j + 0.5) * CELDA, NILO_ALTO))
        ok, punto, normal, _, obj, _ = escena.ray_cast(deps, p, Vector((0, 0, -1)), distance=NILO_ALTO + 0.4)
        return ok and obj.name in nombres and normal.z > 0.7 and abs(punto.z) < 0.3

    crudo = [[pisable(i, j) for j in range(ny)] for i in range(nx)]

    # Solo cuenta lo que está conectado con el arranque de la pasarela: así una roca plana a
    # ras de agua no se convierte en una isla con paredes propias.
    mapa = [[False] * ny for _ in range(nx)]
    inicio = (int((INICIO_NILO[0] - x0) / CELDA), int((INICIO_NILO[1] - y0) / CELDA))
    pendientes = [inicio] if crudo[inicio[0]][inicio[1]] else []
    while pendientes:
        i, j = pendientes.pop()
        if not (0 <= i < nx and 0 <= j < ny) or mapa[i][j] or not crudo[i][j]:
            continue
        mapa[i][j] = True
        pendientes += [(i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1)]

    def celda(i, j):
        return 0 <= i < nx and 0 <= j < ny and mapa[i][j]

    tramos = []
    for j in range(ny + 1):
        tramos += _bordes(nx, ny, celda, j, x0, y0, horizontal=True)
    for i in range(nx + 1):
        tramos += _bordes(ny, nx, celda, i, x0, y0, horizontal=False)

    # Cada tramo es una caja de GROSOR_LIMITE, no un plano: el CharacterController atraviesa
    # las caras de un solo lado cuando las toca por detrás.
    verts, caras = [], []
    g = GROSOR_LIMITE / 2
    for (ax, ay), (bx, by) in tramos:
        ex, ey = (g, 0) if ax == bx else (0, g)   # hacia los dos lados del borde
        sx, sy = (0, g) if ax == bx else (g, 0)   # y un poco más largo, para cerrar esquinas
        planta = [(ax - ex - sx, ay - ey - sy), (bx - ex + sx, by - ey + sy),
                  (bx + ex + sx, by + ey + sy), (ax + ex - sx, ay + ey - sy)]
        k = len(verts)
        verts += [(x, y, -0.5) for x, y in planta] + [(x, y, ALTO_LIMITE) for x, y in planta]
        caras += [(k, k + 3, k + 2, k + 1), (k + 4, k + 5, k + 6, k + 7),
                  (k, k + 1, k + 5, k + 4), (k + 1, k + 2, k + 6, k + 5),
                  (k + 2, k + 3, k + 7, k + 6), (k + 3, k, k + 4, k + 7)]
    malla = bpy.data.meshes.new(nombre + "_Limites")
    malla.from_pydata(verts, [], caras)
    # Normales hacia fuera en cada caja: con alguna cara invertida, el CharacterController
    # atravesaba la cara de su lado, quedaba dentro de la caja y ya no podía salir.
    bm = bmesh.new()
    bm.from_mesh(malla)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(malla)
    bm.free()
    o = bpy.data.objects.new(nombre + "_Limites", malla)
    bpy.data.collections["90_EXPORT"].objects.link(o)
    o.display_type = "WIRE"
    celdas = sum(map(sum, mapa))
    print(f"LIMITES {nombre}: {celdas} celdas pisables ({celdas * CELDA * CELDA:.0f} m2), {len(tramos)} tramos")
    return o


def exportar(objetos, nombre):
    os.makedirs(EXPORTAR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objetos:
        o.select_set(True)
    ruta = os.path.join(EXPORTAR, nombre + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=ruta, use_selection=True, object_types={"MESH"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
        bake_space_transform=True, mesh_smooth_type="FACE", use_tspace=False,
        add_leaf_bones=False, bake_anim=False, path_mode="STRIP", embed_textures=False)
    tris = sum(len(p.vertices) - 2 for o in objetos for p in o.data.polygons)
    print("EXPORTADO", ruta, "| triángulos:", tris)


# ---------------------------------------------------------------- vista previa

def vista_previa():
    s = bpy.context.scene
    s.render.engine = "BLENDER_WORKBENCH"
    sh = s.display.shading
    sh.light = "STUDIO"
    sh.color_type = "MATERIAL"
    sh.show_shadows = True
    sh.show_cavity = True
    sh.cavity_type = "BOTH"
    s.display.light_direction = (0.4, -0.5, 0.75)
    s.render.resolution_x, s.render.resolution_y = 1600, 900

    ref = bpy.data.collections["00_REF"]
    cam = bpy.data.objects.new("CAM_Previa", bpy.data.cameras.new("CAM_Previa"))
    ref.objects.link(cam)
    cam.location = (-24.0, -36.0, 20.0)
    objetivo = bpy.data.objects.new("CAM_Objetivo", None)
    objetivo.location = (1.0, 0.0, 0.5)
    ref.objects.link(objetivo)
    c = cam.constraints.new("TRACK_TO")
    c.target = objetivo
    cam.data.lens = 35
    s.camera = cam


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = bpy.context.scene
    s.unit_settings.system = "METRIC"
    s.unit_settings.scale_length = 1.0

    ref = coleccion("00_REF")
    kit = coleccion("10_KIT")
    bloques = coleccion("20_BLOQUES")
    coleccion("90_EXPORT")
    b01 = coleccion("B01_Muelle", bloques)
    pueblo = {
        "B02_Residencial": (coleccion("B02_Residencial", bloques), bloque_residencial),
        "B03_Canal": (coleccion("B03_Canal", bloques), bloque_canal),
        "B04_Industrial": (coleccion("B04_Industrial", bloques), bloque_industrial),
        "B05_Faro": (coleccion("B05_Faro", bloques), bloque_faro),
        "B06_Piedra": (coleccion("B06_Piedra", bloques), bloque_piedra),
        "B07_Palafitos": (coleccion("B07_Palafitos", bloques), bloque_palafitos),
        "B08_Bahia": (coleccion("B08_Bahia", bloques), barcos_y_detalles),
    }
    fondo = coleccion("30_FONDO")

    importar_kits()
    materiales_neutros()
    referencias(ref, x=1.6, y=-1.5)
    bloque_muelle(b01)
    for col_bloque, construir in pueblo.values():
        construir(col_bloque)
    montanas(fondo)
    islas_lejanas(fondo)

    # El kit queda en el archivo, pero fuera de la vista y del render.
    bpy.context.view_layer.layer_collection.children["10_KIT"].exclude = True
    kit.hide_render = True

    # Cada bloque se unifica y exporta por separado; los límites son uno solo para todo el pueblo,
    # porque las pasarelas cruzan de un bloque a otro.
    geos = {"B01_Muelle": unificar(b01, "B01_Muelle")}
    for nombre, (col_bloque, _) in pueblo.items():
        geos[nombre] = unificar(col_bloque, nombre)
    # Los bloques editables ocupan el mismo sitio que sus copias unificadas: se apartan para los rayos.
    capa = bpy.context.view_layer.layer_collection.children["20_BLOQUES"]
    capa.exclude = True
    lim = limites(list(geos.values()), "Liria")
    capa.exclude = False
    for nombre, geo in geos.items():
        exportar([geo], nombre)
    exportar([lim], "Liria_Limites")
    # En la vista previa se ven los bloques editables; las versiones unificadas quedan ocultas.
    for o in list(geos.values()) + [lim]:
        o.hide_set(True)
        o.hide_render = True

    # El fondo se exporta aparte, sin límites: no se pisa y en Unity no lleva colisiones.
    bpy.context.view_layer.layer_collection.children["30_FONDO"].exclude = False
    geo_fondo = unificar(fondo, "Fondo_Liria")
    exportar([geo_fondo], "Fondo_Liria")
    geo_fondo.hide_set(True)
    geo_fondo.hide_render = True

    vista_previa()
    os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=SALIDA, compress=True)
    print("LIRIA guardado:", SALIDA, "| piezas de kit:", len(KIT), "| objetos en B01:", len(b01.objects))


if __name__ == "__main__":
    main()
