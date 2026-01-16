bl_info = {
    "name": "Export with Empty Parent",
    "author": "OpenAI",
    "version": (1, 0, 0),
    "blender": (2, 80, 0),
    "location": "View3D > Sidebar > Export",
    "description": "Parent selected objects to an Empty and export as FBX",
    "category": "Import-Export",
}

import os

import bpy


class OBJECT_OT_export_with_empty_parent(bpy.types.Operator):
    bl_idname = "object.export_with_empty_parent"
    bl_label = "Export with Empty Parent"
    bl_options = {"REGISTER", "UNDO"}

    export_dir: bpy.props.StringProperty(
        name="Export Directory",
        subtype="DIR_PATH",
    )

    def execute(self, context):
        selected_objects = list(context.selected_objects)
        if not selected_objects:
            self.report({"WARNING"}, "No objects selected")
            return {"CANCELLED"}

        export_dir = bpy.path.abspath(
            self.export_dir or context.scene.export_with_empty_parent_dir
        )
        if not export_dir:
            self.report({"WARNING"}, "Export directory is not set")
            return {"CANCELLED"}

        empty = bpy.data.objects.new("ExportEmpty", None)
        context.collection.objects.link(empty)

        for obj in selected_objects:
            obj.parent = empty

        empty.select_set(True)
        context.view_layer.objects.active = empty

        filename = f"{empty.name}.fbx"
        filepath = os.path.join(export_dir, filename)

        bpy.ops.export_scene.fbx(filepath=filepath, use_selection=True)

        return {"FINISHED"}


class VIEW3D_PT_export_with_empty_parent(bpy.types.Panel):
    bl_label = "Export with Empty Parent"
    bl_idname = "VIEW3D_PT_export_with_empty_parent"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Export"

    def draw(self, context):
        layout = self.layout
        layout.prop(context.scene, "export_with_empty_parent_dir")
        layout.operator(OBJECT_OT_export_with_empty_parent.bl_idname)


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
