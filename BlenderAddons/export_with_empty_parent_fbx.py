bl_info = {
    "name": "Export FBX With Empty Parent",
    "author": "OpenAI",
    "version": (1, 0, 0),
    "blender": (2, 93, 0),
    "location": "View3D > Sidebar > Export",
    "description": "Parent selected objects to an Empty and export as FBX",
    "category": "Import-Export",
}

import os
import math
import bpy


class OBJECT_OT_export_with_empty_parent(bpy.types.Operator):
    bl_idname = "object.export_with_empty_parent"
    bl_label = "Export FBX With Empty Parent"
    bl_options = {"REGISTER", "UNDO"}

    export_dir: bpy.props.StringProperty(
        name="Export Directory",
        subtype="DIR_PATH",
        description="Directory to export FBX file",
    )

    def execute(self, context):
        selected_objects = [obj for obj in context.selected_objects if obj.type != "EMPTY"]
        if not selected_objects:
            self.report({"ERROR"}, "対象のオブジェクトが選択されていません。")
            return {"CANCELLED"}

        export_dir = bpy.path.abspath(self.export_dir)
        if not export_dir:
            self.report({"ERROR"}, "エクスポート先フォルダを指定してください。")
            return {"CANCELLED"}
        if not os.path.isdir(export_dir):
            self.report({"ERROR"}, "指定したフォルダが存在しません。")
            return {"CANCELLED"}

        empty = bpy.data.objects.new("Empty_Export", None)
        empty.empty_display_type = "PLAIN_AXES"
        empty.rotation_euler = (math.radians(90.0), 0.0, 0.0)
        empty.scale = (0.01, 0.01, 0.01)
        context.collection.objects.link(empty)

        for obj in selected_objects:
            obj.parent = empty
            obj.matrix_parent_inverse = empty.matrix_world.inverted()

        for obj in context.selected_objects:
            obj.select_set(False)
        empty.select_set(True)
        context.view_layer.objects.active = empty

        for obj in selected_objects:
            obj.select_set(True)

        export_path = os.path.join(export_dir, f"{empty.name}.fbx")
        bpy.ops.export_scene.fbx(
            filepath=export_path,
            use_selection=True,
            apply_scale_options='FBX_SCALE_ALL',
            add_leaf_bones=False,
        )

        self.report({"INFO"}, f"FBXを書き出しました: {export_path}")
        return {"FINISHED"}


class VIEW3D_PT_export_with_empty_parent(bpy.types.Panel):
    bl_label = "Export FBX With Empty"
    bl_idname = "VIEW3D_PT_export_with_empty_parent"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Export"

    def draw(self, context):
        layout = self.layout
        layout.label(text="空オブジェクトで親子付けしてFBX出力")
        layout.prop(context.scene, "export_with_empty_parent_dir")
        operator = layout.operator(OBJECT_OT_export_with_empty_parent.bl_idname, text="Export FBX")
        operator.export_dir = context.scene.export_with_empty_parent_dir


def register():
    bpy.utils.register_class(OBJECT_OT_export_with_empty_parent)
    bpy.utils.register_class(VIEW3D_PT_export_with_empty_parent)
    bpy.types.Scene.export_with_empty_parent_dir = bpy.props.StringProperty(
        name="Export Directory",
        subtype="DIR_PATH",
    )


def unregister():
    del bpy.types.Scene.export_with_empty_parent_dir
    bpy.utils.unregister_class(VIEW3D_PT_export_with_empty_parent)
    bpy.utils.unregister_class(OBJECT_OT_export_with_empty_parent)


if __name__ == "__main__":
    register()
