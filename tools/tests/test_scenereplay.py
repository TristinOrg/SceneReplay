import argparse
import json
import tempfile
import unittest
from contextlib import redirect_stdout
from io import StringIO
from pathlib import Path
from unittest.mock import patch

from tools import scenereplay


class FakeDocumentClient:
    def __init__(self, _instance):
        pass

    def call(self, name, _arguments):
        if name == "execute_menu_item":
            (scenereplay.WORK / "result.json").write_text(
                json.dumps({"success": True, "data": {"isValid": True}}), encoding="utf-8")
        return {"success": True}


class FakeVerifyClient:
    filters = []

    def __init__(self, instance):
        self.instance = instance or "SceneReplay@test"

    def call(self, name, arguments):
        if name in ("read_console", "execute_menu_item", "set_active_instance"):
            return {"success": True, "data": []}
        if name == "run_tests":
            key = "assembly_names" if "assembly_names" in arguments else "test_names"
            self.filters.append((key, arguments[key]))
            return {"success": True, "data": {"job_id": str(len(self.filters))}}
        if name == "get_test_job":
            return {
                "success": True,
                "data": {
                    "status": "succeeded",
                    "result": {"summary": {"total": 2, "passed": 2}},
                    "progress": {"failures_so_far": []},
                },
            }
        raise AssertionError(name)


class FakeVerifyClientWithStaleFailure(FakeVerifyClient):
    def call(self, name, arguments):
        result = super().call(name, arguments)
        if name == "get_test_job":
            result["data"]["progress"]["failures_so_far"] = [{"full_name": "Old.Test", "message": "old"}]
        return result


class SceneReplayCliTests(unittest.TestCase):
    def test_parser_exposes_all_commands(self):
        help_text = scenereplay.parser().format_help()
        for command in (
            "validate", "diff", "patch", "quality", "inventory", "game",
            "plan", "step", "export-glb", "validate-glb", "export-external", "discover",
            "repository", "verify"
        ):
            self.assertIn(command, help_text)

    def test_document_command_writes_requested_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "shape.json"
            output = root / "out" / "result.json"
            source.write_text("{}", encoding="utf-8")
            args = argparse.Namespace(
                command="validate", source=str(source), other=None,
                output=str(output), instance=None,
            )
            with patch.object(scenereplay, "WORK", root / "work"), patch.object(
                scenereplay, "McpClient", FakeDocumentClient
            ):
                self.assertEqual(scenereplay.run_document(args), 0)
            self.assertEqual(json.loads(output.read_text(encoding="utf-8")), {"isValid": True})

    def test_verify_runs_each_scenereplay_test_assembly_and_aggregates(self):
        FakeVerifyClient.filters.clear()
        args = argparse.Namespace(instance=None, tests=None, timeout=30, settle=0)
        output = StringIO()
        with patch.object(scenereplay, "McpClient", FakeVerifyClient), redirect_stdout(output):
            self.assertEqual(scenereplay.run_verify(args), 0)
        self.assertEqual(len(FakeVerifyClient.filters), 3)
        self.assertIn("EditMode: 6/6 passed", output.getvalue())

    def test_verify_ignores_stale_failures_for_successful_job(self):
        FakeVerifyClient.filters.clear()
        args = argparse.Namespace(instance=None, tests=["Current.Test"], timeout=30, settle=0)
        with patch.object(scenereplay, "McpClient", FakeVerifyClientWithStaleFailure):
            self.assertEqual(scenereplay.run_verify(args), 0)

    def test_verify_rejects_zero_test_false_positive(self):
        class EmptyClient(FakeVerifyClient):
            def call(self, name, arguments):
                result = super().call(name, arguments)
                if name == "get_test_job":
                    result["data"]["result"]["summary"] = {"total": 0, "passed": 0}
                return result

        args = argparse.Namespace(instance=None, tests=["Missing.Tests"], timeout=30, settle=0)
        with patch.object(scenereplay, "McpClient", EmptyClient):
            self.assertEqual(scenereplay.run_verify(args), 1)


if __name__ == "__main__":
    unittest.main()
