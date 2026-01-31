bl_info = {
    "name": "Voxel Exporter (.vox)",
    "author": "Claude",
    "version": (1, 0, 0),
    "blender": (3, 0, 0),
    "location": "File > Export > MagicaVoxel (.vox)",
    "description": "Export voxelized mesh to MagicaVoxel .vox format",
    "category": "Import-Export",
}

import bpy
import struct
import os
from mathutils import Vector, geometry
from collections import defaultdict

def write_chunk(f, chunk_id, content):
    """VOXファイルのチャンクを書き込む"""
    f.write(chunk_id.encode('ascii'))
    f.write(struct.pack('<I', len(content)))  # content size
    f.write(struct.pack('<I', 0))  # children size (0 for now)
    f.write(content)

def encode_vox_dict(data):
    """VOXの辞書データをエンコード"""
    content = struct.pack('<I', len(data))
    for key, value in data.items():
        key_bytes = key.encode('utf-8')
        value_bytes = value.encode('utf-8')
        content += struct.pack('<I', len(key_bytes))
        content += key_bytes
        content += struct.pack('<I', len(value_bytes))
        content += value_bytes
    return content

def build_scene_graph_chunks(model_count, model_offsets):
    """シーングラフ用のチャンクを作成"""
    chunks = []
    root_trn_id = 0
    root_grp_id = 1
    root_trn_content = struct.pack('<I', root_trn_id)
    root_trn_content += encode_vox_dict({})
    root_trn_content += struct.pack('<i', root_grp_id)
    root_trn_content += struct.pack('<i', -1)
    root_trn_content += struct.pack('<i', 0)
    root_trn_content += struct.pack('<I', 1)
    root_trn_content += encode_vox_dict({})
    chunks.append(('nTRN', root_trn_content))

    children_ids = []
    for idx in range(model_count):
        trn_id = 2 + idx * 2
        children_ids.append(trn_id)

    root_grp_content = struct.pack('<I', root_grp_id)
    root_grp_content += encode_vox_dict({})
    root_grp_content += struct.pack('<I', len(children_ids))
    for child_id in children_ids:
        root_grp_content += struct.pack('<I', child_id)
    chunks.append(('nGRP', root_grp_content))

    for idx in range(model_count):
        trn_id = 2 + idx * 2
        shp_id = 3 + idx * 2
        offset = model_offsets[idx]
        frame_dict = {}
        if offset != (0, 0, 0):
            frame_dict["_t"] = f"{offset[0]} {offset[1]} {offset[2]}"
        trn_content = struct.pack('<I', trn_id)
        trn_content += encode_vox_dict({"_name": f"model_{idx}"})
        trn_content += struct.pack('<i', shp_id)
        trn_content += struct.pack('<i', -1)
        trn_content += struct.pack('<i', 0)
        trn_content += struct.pack('<I', 1)
        trn_content += encode_vox_dict(frame_dict)
        chunks.append(('nTRN', trn_content))

        shp_content = struct.pack('<I', shp_id)
        shp_content += encode_vox_dict({})
        shp_content += struct.pack('<I', 1)
        shp_content += struct.pack('<I', idx)
        shp_content += encode_vox_dict({})
        chunks.append(('nSHP', shp_content))

    return chunks

def rgb_to_palette_index(r, g, b, palette):
    """RGB値を最も近いパレットインデックスに変換"""
    color = (r, g, b)
    if color in palette:
        return palette[color]

    # 新しい色をパレットに追加
    index = len(palette) + 1
    if index <= 255:
        palette[color] = index
        return index

    # パレットが満杯の場合は最も近い色を探す
    min_dist = float('inf')
    closest_idx = 1
    for pal_color, idx in palette.items():
        dist = sum((a - b) ** 2 for a, b in zip(color, pal_color))
        if dist < min_dist:
            min_dist = dist
            closest_idx = idx
    return closest_idx

def analyze_voxel_mesh(obj, voxel_size):
    """メッシュからボクセル情報を抽出"""
    if voxel_size <= 0:
        return {}
    # メッシュデータを取得
    depsgraph = bpy.context.evaluated_depsgraph_get()
    eval_obj = obj.evaluated_get(depsgraph)
    mesh = eval_obj.to_mesh()

    # ワールド座標に変換
    mesh.transform(obj.matrix_world)
    mesh.calc_loop_triangles()

    voxels = {}
    inv_size = 1.0 / voxel_size
    max_distance = voxel_size * 0.5

    # 三角形ごとに処理
    for tri in mesh.loop_triangles:
        verts = [mesh.vertices[i].co for i in tri.vertices]
        min_x = min(v.x for v in verts)
        min_y = min(v.y for v in verts)
        min_z = min(v.z for v in verts)
        max_x = max(v.x for v in verts)
        max_y = max(v.y for v in verts)
        max_z = max(v.z for v in verts)

        grid_min_x = int(min_x * inv_size) - 1
        grid_min_y = int(min_y * inv_size) - 1
        grid_min_z = int(min_z * inv_size) - 1
        grid_max_x = int(max_x * inv_size) + 1
        grid_max_y = int(max_y * inv_size) + 1
        grid_max_z = int(max_z * inv_size) + 1

        r, g, b = get_loop_color(mesh, tri.loops[0], tri.material_index)

        for x in range(grid_min_x, grid_max_x + 1):
            world_x = x * voxel_size
            for y in range(grid_min_y, grid_max_y + 1):
                world_y = y * voxel_size
                for z in range(grid_min_z, grid_max_z + 1):
                    world_pos = Vector((world_x, world_y, z * voxel_size))
                    closest = geometry.closest_point_on_tri(world_pos, verts[0], verts[1], verts[2])
                    if (closest - world_pos).length <= max_distance:
                        voxels[(x, y, z)] = (r, g, b)

        # ボクセル位置を整数座標に丸める
        inv_size = 1.0 / voxel_size
        vox_pos = (
            round(center.x * inv_size),
            round(center.y * inv_size),
            round(center.z * inv_size),
        )
        voxels[vox_pos] = (r, g, b)

    bm.free()
    eval_obj.to_mesh_clear()

    return voxels

def export_vox(filepath, obj, voxel_size):
    """VOX形式でエクスポート"""
    # ボクセル情報を抽出
    voxels = analyze_voxel_mesh(obj, voxel_size)

    if not voxels:
        if voxel_size <= 0:
            return {'CANCELLED'}, "Voxel size must be greater than 0"
        return {'CANCELLED'}, "No voxels found in mesh"

    # 座標の範囲を計算
    positions = list(voxels.keys())
    min_x = min(p[0] for p in positions)
    min_y = min(p[1] for p in positions)
    min_z = min(p[2] for p in positions)
    max_x = max(p[0] for p in positions)
    max_y = max(p[1] for p in positions)
    max_z = max(p[2] for p in positions)

    # ボクセルを256ごとに分割
    chunk_voxels = defaultdict(list)
    for pos, color in voxels.items():
        cx = (pos[0] - min_x) // 256
        cy = (pos[1] - min_y) // 256
        cz = (pos[2] - min_z) // 256
        origin_x = min_x + cx * 256
        origin_y = min_y + cy * 256
        origin_z = min_z + cz * 256
        local_x = pos[0] - origin_x
        local_y = pos[1] - origin_y
        local_z = pos[2] - origin_z
        chunk_voxels[(cx, cy, cz)].append((local_x, local_y, local_z, color))

    # パレットを構築
    palette = {}
    chunk_data = []
    model_offsets = []
    chunk_keys = sorted(chunk_voxels.keys())
    for chunk_key in chunk_keys:
        vox_list = chunk_voxels[chunk_key]
        max_local_x = max(v[0] for v in vox_list)
        max_local_y = max(v[1] for v in vox_list)
        max_local_z = max(v[2] for v in vox_list)
        size_chunk_x = max_local_x + 1
        size_chunk_y = max_local_y + 1
        size_chunk_z = max_local_z + 1

        voxel_data = []
        for local_x, local_y, local_z, color in vox_list:
            color_idx = rgb_to_palette_index(color[0], color[1], color[2], palette)
            voxel_data.append((local_x, local_y, local_z, color_idx))

        chunk_data.append({
            "size": (size_chunk_x, size_chunk_y, size_chunk_z),
            "voxels": voxel_data,
        })

        origin_x = min_x + chunk_key[0] * 256
        origin_y = min_y + chunk_key[1] * 256
        origin_z = min_z + chunk_key[2] * 256
        model_offsets.append((origin_x - min_x, origin_y - min_y, origin_z - min_z))

    # ファイルに書き込み
    with open(filepath, 'wb') as f:
        # ヘッダー
        f.write(b'VOX ')
        f.write(struct.pack('<I', 150))  # version

        # MAINチャンク(空)
        f.write(b'MAIN')

        # チャンク一覧を構築
        chunks = []
        if len(chunk_data) > 1:
            pack_content = struct.pack('<I', len(chunk_data))
            chunks.append(('PACK', pack_content))

        for chunk in chunk_data:
            size_chunk_x, size_chunk_y, size_chunk_z = chunk["size"]
            size_content = struct.pack('<III', size_chunk_x, size_chunk_y, size_chunk_z)
            chunks.append(('SIZE', size_content))

            voxel_data = chunk["voxels"]
            xyzi_content = struct.pack('<I', len(voxel_data))
            for x, y, z, color_idx in voxel_data:
                xyzi_content += struct.pack('BBBB', x, y, z, color_idx)
            chunks.append(('XYZI', xyzi_content))

        # RGBAチャンク(パレット)
        rgba_content = b''

        # パレットを256色分用意
        palette_list = [None] * 256
        for color, idx in palette.items():
            palette_list[idx - 1] = color

        for i in range(256):
            if palette_list[i] is not None:
                r, g, b = palette_list[i]
                rgba_content += struct.pack('BBBB', r, g, b, 255)
            else:
                # 未使用色はグレー
                rgba_content += struct.pack('BBBB', 128, 128, 128, 255)

        chunks.append(('RGBA', rgba_content))

        # シーングラフチャンク
        scene_chunks = build_scene_graph_chunks(len(chunk_data), model_offsets)
        chunks.extend(scene_chunks)

        children_size = sum(12 + len(content) for _, content in chunks)

        f.write(struct.pack('<I', 0))  # content size
        f.write(struct.pack('<I', children_size))

        for chunk_id, content in chunks:
            write_chunk(f, chunk_id, content)

    return {'FINISHED'}, f"Exported {len(voxels)} voxels in {len(chunk_data)} model(s)"

class EXPORT_OT_vox(bpy.types.Operator):
    """Export voxelized mesh to MagicaVoxel .vox format"""
    bl_idname = "export_scene.vox"
    bl_label = "Export VOX"
    bl_options = {'PRESET'}

    filepath: bpy.props.StringProperty(subtype="FILE_PATH")
    voxel_size: bpy.props.FloatProperty(
        name="Voxel Size (m)",
        description="1 voxelあたりのサイズ(メートル)",
        default=1.0,
        min=0.0001,
        soft_min=0.01,
        soft_max=10.0,
    )

    filter_glob: bpy.props.StringProperty(
        default="*.vox",
        options={'HIDDEN'},
    )

    def execute(self, context):
        # アクティブオブジェクトを取得
        obj = context.active_object

        if not obj or obj.type != 'MESH':
            self.report({'ERROR'}, "No active mesh object selected")
            return {'CANCELLED'}

        result, message = export_vox(self.filepath, obj, self.voxel_size)

        if result == {'FINISHED'}:
            self.report({'INFO'}, message)
        else:
            self.report({'ERROR'}, message)

        return result

    def invoke(self, context, event):
        if not self.filepath:
            self.filepath = "untitled.vox"

        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

def menu_func_export(self, context):
    self.layout.operator(EXPORT_OT_vox.bl_idname, text="MagicaVoxel (.vox)")

def register():
    bpy.utils.register_class(EXPORT_OT_vox)
    bpy.types.TOPBAR_MT_file_export.append(menu_func_export)

def unregister():
    bpy.utils.unregister_class(EXPORT_OT_vox)
    bpy.types.TOPBAR_MT_file_export.remove(menu_func_export)

if __name__ == "__main__":
    register()
