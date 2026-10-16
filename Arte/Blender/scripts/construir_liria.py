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

import bpy
from mathutils import Matrix, Vector

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
FUENTES = os.path.join(RAIZ, "Arte", "Fuentes")
MUELLE_GLB = os.path.join(FUENTES, "woodendock_fbxgltftextures", "Glb", "WoodenDockSet.glb")
CASAS_FBX = os.path.join(FUENTES, "Houses_on_the_water", "Houses_on_the_water", "Models", "houses_on_the_water.fbx")
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
    ("LIRIA_Tejado", ("roof", "ceilng")),
    ("LIRIA_Madera", ("wood", "dock", "floor", "beam", "boat", "door", "wall", "harpoon", "box")),
)
COLOR_CATEGORIA = {
    "LIRIA_Madera": (0.62, 0.48, 0.34, 1),
    "LIRIA_Chapa": (0.55, 0.57, 0.6, 1),
    "LIRIA_Tejado": (0.5, 0.36, 0.3, 1),
    "LIRIA_Tela": (0.75, 0.68, 0.55, 1),
    "LIRIA_Cristal": (0.4, 0.55, 0.6, 1),
    "LIRIA_Varios": (0.6, 0.6, 0.55, 1),
}


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
    for o in KIT.values():
        for slot in o.material_slots:
            if slot.material is not None and slot.material.name not in neutros:
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


def poner(col, nombre, x, y, z_arriba=None, z_abajo=None, giro=0, tipo="muelle"):
    """Duplicado enlazado de una pieza del kit, centrada en (x, y) en planta.
    z_arriba coloca su cara superior a esa altura; z_abajo, su base."""
    base = KIT[nombre]
    o = bpy.data.objects.new(nombre[2:], base.data)
    rot = Matrix.Rotation(math.radians(giro), 4, "Z")
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


def grupo_casas(col, rect, destino):
    """Copia las piezas del poblado cuyo centro cae en rect (x0, y0, x1, y1), trasladadas para que
    el centro del rect quede en destino (x, y). La cubierta de las casas ya está en z = 0."""
    x0, y0, x1, y1 = rect
    dx = destino[0] - (x0 + x1) / 2
    dy = destino[1] - (y0 + y1) / 2
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
        o.location = (dx, dy, 0)
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

def pilotes(col, x, y):
    """Cuatro pilotes bajo una tabla de 2 x 4 m, desde la cubierta hasta el fondo."""
    for ex in (-0.85, 0.85):
        for ey in (-1.85, 1.85):
            poner(col, "D_Support_Post", x + ex, y + ey, z_arriba=-0.05)
            poner(col, "D_Support_Post_Half", x + ex, y + ey, z_arriba=-2.05)


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

    # Agua provisional.
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, -4, AGUA_Z))
    agua = bpy.context.object
    agua.scale = (60, 60, 1)
    agua.name = "Agua_provisional"
    agua.color = COLOR["agua"]
    mover_a(agua, col)


# ---------------------------------------------------------------- unificación y exportación

EXPORTAR = os.path.join(RAIZ, "Assets", "Meshes", "Liria")
CELDA = 0.25          # resolución del mapa de paso
ALTO_LIMITE = 2.5     # altura de las paredes invisibles
GROSOR_LIMITE = 0.2   # y su grosor


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


def limites(geo, nombre):
    """Paredes invisibles en el borde de lo que se puede pisar.

    Nilo no tiene gravedad ni salto (PlayerController.cs): lo único que le impide salir al agua
    son colisiones. Se hace un mapa de celdas de CELDA m: una celda es pisable si un rayo desde
    1 m baja y encuentra suelo plano a la altura de la cubierta. Donde una celda pisable toca una
    que no lo es, se levanta una pared."""
    deps = bpy.context.evaluated_depsgraph_get()
    esquinas = [geo.matrix_world @ Vector(c) for c in geo.bound_box]
    mn = Vector([min(c[i] for c in esquinas) for i in range(3)])
    mx = Vector([max(c[i] for c in esquinas) for i in range(3)])
    nx = int((mx.x - mn.x) / CELDA) + 2
    ny = int((mx.y - mn.y) / CELDA) + 2
    x0, y0 = mn.x - CELDA, mn.y - CELDA
    escena = bpy.context.scene

    def pisable(i, j):
        p = Vector((x0 + (i + 0.5) * CELDA, y0 + (j + 0.5) * CELDA, 1.0))
        ok, punto, normal, _, obj, _ = escena.ray_cast(deps, p, Vector((0, 0, -1)), distance=1.6)
        return ok and obj.name == geo.name and normal.z > 0.7 and abs(punto.z) < 0.3

    mapa = [[pisable(i, j) for j in range(ny)] for i in range(nx)]

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
    cam.location = (-17.0, -27.0, 14.0)
    objetivo = bpy.data.objects.new("CAM_Objetivo", None)
    objetivo.location = (1.0, -2.0, 0.5)
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

    importar_kits()
    materiales_neutros()
    referencias(ref, x=1.6, y=-1.5)
    bloque_muelle(b01)

    # El kit queda en el archivo, pero fuera de la vista y del render.
    bpy.context.view_layer.layer_collection.children["10_KIT"].exclude = True
    kit.hide_render = True

    geo = unificar(b01, "B01_Muelle")
    # El bloque editable ocupa el mismo sitio que la copia unificada: se aparta para los rayos.
    capa_b01 = bpy.context.view_layer.layer_collection.children["20_BLOQUES"].children["B01_Muelle"]
    capa_b01.exclude = True
    lim = limites(geo, "B01_Muelle")
    capa_b01.exclude = False
    exportar([geo, lim], "B01_Muelle")
    # En la vista previa se ve el bloque editable; la versión unificada queda oculta.
    for o in (geo, lim):
        o.hide_set(True)
        o.hide_render = True

    vista_previa()
    os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=SALIDA, compress=True)
    print("LIRIA guardado:", SALIDA, "| piezas de kit:", len(KIT), "| objetos en B01:", len(b01.objects))


if __name__ == "__main__":
    main()
