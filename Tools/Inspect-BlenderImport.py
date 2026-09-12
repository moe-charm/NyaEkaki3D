"""Read-only FBX/BLEND inventory. Run in Blender background; results may contain private names.

blender --background --factory-startup --python-exit-code 1 --python this.py --
    --output private/import-inventory.json input.fbx [input.blend ...]
No source file is saved or exported.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy


def inventory(path):
    path = Path(path).resolve(strict=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if path.suffix.lower() == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(path))
    elif path.suffix.lower() == ".blend":
        bpy.ops.wm.open_mainfile(filepath=str(path), load_ui=False, use_scripts=False)
    else:
        raise ValueError("Expected FBX or BLEND")
    objects = []
    for obj in bpy.data.objects:
        item = {"name": obj.name, "type": obj.type, "parent": obj.parent.name if obj.parent else None,
                "local_matrix": [list(row) for row in obj.matrix_local]}
        if obj.type == "MESH":
            mesh = obj.data
            mesh.calc_loop_triangles()
            bone_names = {b.name for m in obj.modifiers if m.type == "ARMATURE" and m.object
                          for b in m.object.data.bones if b.use_deform}
            influences = [sum(g.weight > 0 and obj.vertex_groups[g.group].name in bone_names
                              for g in vertex.groups) for vertex in mesh.vertices]
            item.update(vertices=len(mesh.vertices), triangles=len(mesh.loop_triangles),
                        materials=[slot.material.name if slot.material else None for slot in obj.material_slots],
                        uv_layers=len(mesh.uv_layers), vertex_groups=len(obj.vertex_groups),
                        max_deform_bone_influences=max(influences, default=0),
                        vertices_over_four=sum(n > 4 for n in influences),
                        unweighted_vertices=sum(n == 0 for n in influences),
                        shape_keys=[key.name for key in mesh.shape_keys.key_blocks] if mesh.shape_keys else [],
                        armature_targets=[m.object.name if m.object else None for m in obj.modifiers if m.type == "ARMATURE"])
        elif obj.type == "ARMATURE":
            item["bones"] = [{"name": b.name, "parent": b.parent.name if b.parent else None,
                              "deform": b.use_deform, "rest_matrix": [list(row) for row in b.matrix_local]}
                             for b in obj.data.bones]
        objects.append(item)
    meshes = [o for o in objects if o["type"] == "MESH"]
    return {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            "bytes": path.stat().st_size, "objects": objects,
            "summary": {"meshes": len(meshes), "vertices": sum(o["vertices"] for o in meshes),
                        "triangles": sum(o["triangles"] for o in meshes),
                        "armatures": sum(o["type"] == "ARMATURE" for o in objects),
                        "bones": sum(len(o.get("bones", [])) for o in objects),
                        "max_deform_bone_influences": max((o["max_deform_bone_influences"] for o in meshes), default=0),
                        "vertices_over_four": sum(o["vertices_over_four"] for o in meshes),
                        "shape_keys_excluding_basis": sum(max(0, len(o["shape_keys"]) - 1) for o in meshes)}}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("inputs", nargs="+")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    result = {"blender": bpy.app.version_string, "inputs": [inventory(p) for p in args.inputs]}
    output = Path(args.output).resolve()
    if output in [Path(p).resolve() for p in args.inputs]:
        raise ValueError("Report must not overwrite a source")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    for item in result["inputs"]:
        print("IMPORT_INVENTORY", Path(item["path"]).name, json.dumps(item["summary"]))


if __name__ == "__main__":
    main()
