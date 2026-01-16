bl_info = {
    "name": "Empty Parent FBX Exporter",
    "author": "OpenAI",
    "version": (1, 0, 0),
    "blender": (3, 0, 0),
    "location": "View3D > Sidebar > Export",
    "description": "選択オブジェクトを空の子にしてFBXを書き出します",
    "category": "Import-Export",
}

import math
import os
import bpy
from bpy.props import StringProperty, PointerProperty
from bpy.types import Operator, Panel, PropertyGroup


def _ensure_selection(context):
    selected = [obj for obj in context.selected_objects if obj.type != 'CAMERA']
    if not selected:
        return None, "オブジェクトが選択されていません。"
    return selected, None


class EmptyParentExportSettings(PropertyGroup):
    export_dir: StringProperty(
        name="出力フォルダ",
        description="FBXを書き出すフォルダを指定します",
        subtype='DIR_PATH',
    )
    file_name: StringProperty(
        name="ファイル名",
        description="拡張子を除いたファイル名",
        default="export",
    )


class OBJECT_OT_empty_parent_fbx_export(Operator):
    bl_idname = "object.empty_parent_fbx_export"
    bl_label = "空を親にしてFBX出力"
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        selected, error = _ensure_selection(context)
        if error:
            self.report({'WARNING'}, error)
            return {'CANCELLED'}

        settings = context.scene.empty_parent_export_settings
        if not settings.export_dir:
            self.report({'WARNING'}, "出力フォルダを指定してください。")
            return {'CANCELLED'}
        if not settings.file_name.strip():
            self.report({'WARNING'}, "ファイル名を入力してください。")
            return {'CANCELLED'}

        filepath = bpy.path.abspath(
            bpy.path.ensure_ext(
                os.path.join(settings.export_dir, settings.file_name),
                ".fbx",
            )
        )

        previous_active = context.view_layer.objects.active
        previous_selection = [obj for obj in context.selected_objects]

        empty = bpy.data.objects.new(
            name=f"{settings.file_name}_empty",
            object_data=None,
        )
        context.collection.objects.link(empty)
        empty.rotation_euler = (math.radians(90.0), 0.0, 0.0)

        for obj in selected:
            obj.parent = empty

        for obj in context.selected_objects:
            obj.select_set(False)

        empty.select_set(True)
        for obj in selected:
            obj.select_set(True)
        context.view_layer.objects.active = empty

        bpy.ops.export_scene.fbx(
            filepath=filepath,
            use_selection=True,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_ALL',
            object_types={'EMPTY', 'MESH', 'ARMATURE', 'OTHER'},
        )

        for obj in context.selected_objects:
            obj.select_set(False)
        for obj in previous_selection:
            if obj and obj.name in context.view_layer.objects:
                obj.select_set(True)
        if previous_active and previous_active.name in context.view_layer.objects:
            context.view_layer.objects.active = previous_active

        self.report({'INFO'}, f"FBXを出力しました: {filepath}")
        return {'FINISHED'}


class VIEW3D_PT_empty_parent_fbx_export(Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'Export'
    bl_label = '空を親にしてFBX出力'

    def draw(self, context):
        layout = self.layout
        settings = context.scene.empty_parent_export_settings

        layout.prop(settings, "export_dir")
        layout.prop(settings, "file_name")
        layout.operator(OBJECT_OT_empty_parent_fbx_export.bl_idname)


classes = (
    EmptyParentExportSettings,
    OBJECT_OT_empty_parent_fbx_export,
    VIEW3D_PT_empty_parent_fbx_export,
)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Scene.empty_parent_export_settings = PointerProperty(type=EmptyParentExportSettings)


def unregister():
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
    del bpy.types.Scene.empty_parent_export_settings


if __name__ == "__main__":
    register()
