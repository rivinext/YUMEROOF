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
import bmesh
import struct
import os
from mathutils import Vector
from collections import defaultdict

def write_chunk(f, chunk_id, content):
    """VOXファイルのチャンクを書き込む"""
    f.write(chunk_id.encode('ascii'))
    f.write(struct.pack('<I', len(content)))  # content size
    f.write(struct.pack('<I', 0))  # children size (0 for now)
    f.write(content)

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

def analyze_voxel_mesh(obj):
    """メッシュからボクセル情報を抽出"""
    # メッシュデータを取得
    depsgraph = bpy.context.evaluated_depsgraph_get()
    eval_obj = obj.evaluated_get(depsgraph)
    mesh = eval_obj.to_mesh()

    # ワールド座標に変換
    mesh.transform(obj.matrix_world)

    # 立方体の中心位置を特定
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()

    voxels = {}
    palette = {}

    # 面ごとに処理
    for face in bm.faces:
        # 面の中心を計算
        center = face.calc_center_median()

        # 色情報を取得
        if len(mesh.vertex_colors) > 0:
            color_layer = mesh.vertex_colors[0]
            # 面の最初のループから色を取得
            loop_idx = face.loops[0].index
            color = color_layer.data[loop_idx].color
            r = int(color[0] * 255)
            g = int(color[1] * 255)
            b = int(color[2] * 255)
        elif len(mesh.materials) > 0 and face.material_index < len(mesh.materials):
            mat = mesh.materials[face.material_index]
            if mat and mat.use_nodes:
                # プリンシプルBSDFノードから色を取得
                bsdf = mat.node_tree.nodes.get("Principled BSDF")
                if bsdf:
                    base_color = bsdf.inputs['Base Color'].default_value
                    r = int(base_color[0] * 255)
                    g = int(base_color[1] * 255)
                    b = int(base_color[2] * 255)
                else:
                    r, g, b = 255, 255, 255
            else:
                r, g, b = 255, 255, 255
        else:
            r, g, b = 255, 255, 255

        # ボクセル位置を整数座標に丸める
        vox_pos = (round(center.x), round(center.y), round(center.z))
        voxels[vox_pos] = (r, g, b)

    bm.free()
    eval_obj.to_mesh_clear()

    return voxels

def export_vox(filepath, obj):
    """VOX形式でエクスポート"""
    # ボクセル情報を抽出
    voxels = analyze_voxel_mesh(obj)

    if not voxels:
        return {'CANCELLED'}, "No voxels found in mesh"

    # 座標の範囲を計算
    positions = list(voxels.keys())
    min_x = min(p[0] for p in positions)
    min_y = min(p[1] for p in positions)
    min_z = min(p[2] for p in positions)
    max_x = max(p[0] for p in positions)
    max_y = max(p[1] for p in positions)
    max_z = max(p[2] for p in positions)

    # サイズを計算(1を加える)
    size_x = max_x - min_x + 1
    size_y = max_y - min_y + 1
    size_z = max_z - min_z + 1

    # サイズ制限チェック
    if size_x > 256 or size_y > 256 or size_z > 256:
        return {'CANCELLED'}, f"Model too large: {size_x}x{size_y}x{size_z} (max: 256x256x256)"

    # パレットを構築
    palette = {}
    voxel_data = []

    for pos, color in voxels.items():
        # 座標を正規化(0始まり)
        x = pos[0] - min_x
        y = pos[1] - min_y
        z = pos[2] - min_z

        # パレットインデックスを取得
        color_idx = rgb_to_palette_index(color[0], color[1], color[2], palette)

        voxel_data.append((x, y, z, color_idx))

    # ファイルに書き込み
    with open(filepath, 'wb') as f:
        # ヘッダー
        f.write(b'VOX ')
        f.write(struct.pack('<I', 150))  # version

        # MAINチャンク(空)
        f.write(b'MAIN')

        # 子チャンクのサイズを計算
        size_chunk_size = 12 + 12  # chunk header + content
        xyzi_chunk_content_size = 4 + len(voxel_data) * 4
        xyzi_chunk_size = 12 + xyzi_chunk_content_size
        rgba_chunk_size = 12 + 256 * 4

        children_size = size_chunk_size + xyzi_chunk_size + rgba_chunk_size

        f.write(struct.pack('<I', 0))  # content size
        f.write(struct.pack('<I', children_size))

        # SIZEチャンク
        size_content = struct.pack('<III', size_x, size_y, size_z)
        write_chunk(f, 'SIZE', size_content)

        # XYZIチャンク
        xyzi_content = struct.pack('<I', len(voxel_data))
        for x, y, z, color_idx in voxel_data:
            xyzi_content += struct.pack('BBBB', x, y, z, color_idx)
        write_chunk(f, 'XYZI', xyzi_content)

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

        write_chunk(f, 'RGBA', rgba_content)

    return {'FINISHED'}, f"Exported {len(voxel_data)} voxels"

class EXPORT_OT_vox(bpy.types.Operator):
    """Export voxelized mesh to MagicaVoxel .vox format"""
    bl_idname = "export_scene.vox"
    bl_label = "Export VOX"
    bl_options = {'PRESET'}

    filepath: bpy.props.StringProperty(subtype="FILE_PATH")

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

        result, message = export_vox(self.filepath, obj)

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
