import json
import struct
import sys
import tempfile
import unittest
from pathlib import Path

from tools.scenereplay_glb_validate import BIN_CHUNK, GLB_MAGIC, JSON_CHUNK, validate_glb


def write_glb(path: Path, document: dict, binary: bytes = b"") -> None:
    encoded = json.dumps(document, separators=(",", ":")).encode()
    encoded += b" " * (-len(encoded) % 4)
    chunks = struct.pack("<II", len(encoded), JSON_CHUNK) + encoded
    if binary:
        binary += b"\0" * (-len(binary) % 4)
        chunks += struct.pack("<II", len(binary), BIN_CHUNK) + binary
    path.write_bytes(struct.pack("<III", GLB_MAGIC, 2, 12 + len(chunks)) + chunks)


class SceneReplayGlbValidationTests(unittest.TestCase):
    def test_reports_structure_and_runs_external_validator(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "model.glb"
            write_glb(path, {
                "asset": {"version": "2.0"}, "scene": 0,
                "scenes": [{"nodes": [0]}], "nodes": [{"mesh": 0}],
                "meshes": [{"primitives": []}], "buffers": [{"byteLength": 4}],
            }, b"data")
            result = validate_glb(path, [sys.executable, "-c", "import sys;sys.exit(0)", "{input}"])
            self.assertTrue(result["valid"])
            self.assertEqual(result["summary"]["meshes"], 1)
            self.assertTrue(result["externalValidator"]["valid"])

    def test_reports_invalid_references_without_hiding_the_document(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "model.glb"
            write_glb(path, {"asset": {"version": "2.0"}, "scenes": [{}], "nodes": [{"mesh": 2}]})
            result = validate_glb(path)
            self.assertFalse(result["valid"])
            self.assertIn("nodes[0].mesh is out of range", result["errors"])

    def test_rejects_corrupt_container_length(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "model.glb"
            path.write_bytes(struct.pack("<III", GLB_MAGIC, 2, 99) + b"invalid!")
            with self.assertRaisesRegex(ValueError, "declared length"):
                validate_glb(path)


if __name__ == "__main__":
    unittest.main()
