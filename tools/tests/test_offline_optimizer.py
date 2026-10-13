import unittest

from tools.scenereplay_offline_optimizer import discover_parameters, optimize_model


class SceneReplayOfflineOptimizerTests(unittest.TestCase):
    def test_staged_search_improves_transform_parameter_and_profile(self):
        model = {"root": {
            "id": "root",
            "transform": {"scale": {"x": 1.6, "y": 1.0, "z": 1.0},
                          "position": {"x": 0.0, "y": 0.0, "z": 0.0}},
            "parameters": {"radius": 1.4},
            "profile": [{"x": -0.7, "y": 0.0}, {"x": 0.7, "y": 0.0}, {"x": 0.0, "y": 1.0}],
            "children": [],
        }}

        def evaluate(candidate, _index):
            root = candidate["root"]
            error = abs(root["transform"]["scale"]["x"] - 1.0)
            error += abs(root["parameters"]["radius"] - 1.0)
            error += abs(root["profile"][0]["x"] + 0.5)
            return 1.0 / (1.0 + error), {}

        optimized, report = optimize_model(model, evaluate, maximum_evaluations=60)

        self.assertGreater(report["bestScore"], evaluate(model, 0)[0])
        self.assertLess(optimized["root"]["transform"]["scale"]["x"], 1.6)
        self.assertGreater(report["acceptedChanges"], 0)

    def test_discovery_orders_root_before_detail_geometry(self):
        model = {"root": {"transform": {"scale": {"x": 1, "y": 1, "z": 1}},
                          "profile": [{"x": 0, "y": 0}, {"x": 1, "y": 0}, {"x": 0, "y": 1}],
                          "children": []}}

        parameters = discover_parameters(model)

        self.assertEqual(parameters[0].stage, 0)
        self.assertEqual(parameters[-1].stage, 2)


if __name__ == "__main__":
    unittest.main()
