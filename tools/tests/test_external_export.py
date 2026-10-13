import json
import sys
import tempfile
import unittest
from pathlib import Path

from tools.scenereplay_external_export import export_external, export_with_profile


class SceneReplayExternalExportTests(unittest.TestCase):
    def test_converter_receives_literal_paths_and_output_is_verified(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            source = root / "input model.glb"
            output = root / "output model.fbx"
            source.write_bytes(b"glb")
            command = [
                sys.executable,
                "-c",
                "import pathlib,sys; pathlib.Path(sys.argv[2]).write_bytes(pathlib.Path(sys.argv[1]).read_bytes())",
                "{input}",
                "{output}",
            ]

            result = export_external(source, output, command)

            self.assertEqual(result["format"], "fbx")
            self.assertEqual(output.read_bytes(), b"glb")

    def test_rejects_missing_placeholders_and_unknown_formats(self):
        with tempfile.TemporaryDirectory() as folder:
            source = Path(folder) / "model.glb"
            source.write_bytes(b"glb")
            with self.assertRaisesRegex(ValueError, "Unsupported"):
                export_external(source, Path(folder) / "model.obj", ["tool", "{input}", "{output}"])
            with self.assertRaisesRegex(ValueError, "placeholders"):
                export_external(source, Path(folder) / "model.usd", ["tool", str(source)])

    def test_profile_records_version_and_license(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            source = root / "model.glb"
            output = root / "model.usda"
            profile = root / "converter.json"
            source.write_bytes(b"glb")
            profile.write_text(json.dumps({
                "schema": "scenereplay.converter-profile/1.0",
                "id": "test/copy", "version": "1.2.3", "license": "MIT",
                "formats": ["usda"],
                "command": [sys.executable, "-c", "import pathlib,sys;pathlib.Path(sys.argv[2]).write_bytes(pathlib.Path(sys.argv[1]).read_bytes())", "{input}", "{output}"],
            }), encoding="utf-8")

            result = export_with_profile(source, output, profile)

            self.assertEqual(result["converterProfile"]["id"], "test/copy")
            self.assertEqual(result["converterProfile"]["version"], "1.2.3")
            self.assertEqual(result["converterProfile"]["license"], "MIT")

    def test_profile_rejects_undeclared_target_format(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            source = root / "model.glb"
            profile = root / "converter.json"
            source.write_bytes(b"glb")
            profile.write_text(json.dumps({
                "schema": "scenereplay.converter-profile/1.0",
                "id": "test/copy", "version": "1", "license": "MIT",
                "formats": ["fbx"], "command": ["tool", "{input}", "{output}"],
            }), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "does not support usd"):
                export_with_profile(source, root / "model.usd", profile)


if __name__ == "__main__":
    unittest.main()
