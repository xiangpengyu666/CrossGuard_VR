using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CrossGuard.EditorTools
{
    /// One-click builder for the first-person player rig. Run it from the menu
    /// (CrossGuard > Setup First-Person Player) to assemble a Player GameObject
    /// with a CharacterController, the PlayerController, and the scene's
    /// Main Camera reparented as the eye/cameraPivot. Safe to re-run: if a
    /// PlayerController already exists it just reports and does nothing.
    public static class PlayerSetup
    {
        const float EyeHeight = 1.6f;   // camera height above the player origin (feet)
        const float BodyHeight = 1.8f;
        const float BodyRadius = 0.3f;

        [MenuItem("CrossGuard/Setup First-Person Player")]
        public static void Build()
        {
            var existing = Object.FindFirstObjectByType<PlayerController>();
            if (existing != null)
            {
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[CrossGuard] Player already set up: " + existing.name);
                return;
            }

            // --- root player object ---
            var player = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(player, "Setup First-Person Player");
            player.transform.position = new Vector3(0f, BodyHeight * 0.5f + 0.1f, 0f);
            if (TagExists("Player")) player.tag = "Player";

            var cc = Undo.AddComponent<CharacterController>(player);
            cc.height = BodyHeight;
            cc.radius = BodyRadius;
            cc.center = new Vector3(0f, BodyHeight * 0.5f, 0f);

            var controller = Undo.AddComponent<PlayerController>(player);

            // --- camera: reuse the scene's Main Camera as the eye ---
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                Undo.RegisterCreatedObjectUndo(camGo, "Setup First-Person Player");
                camGo.tag = "MainCamera";
                cam = camGo.GetComponent<Camera>();
            }
            Undo.SetTransformParent(cam.transform, player.transform, "Setup First-Person Player");
            cam.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            cam.transform.localRotation = Quaternion.identity;

            controller.cameraPivot = cam.transform;

            EditorSceneManager.MarkSceneDirty(player.scene);
            Selection.activeGameObject = player;
            Debug.Log("[CrossGuard] First-person player created. Press Play and use WASD / mouse / Shift / Space.");
        }

        static bool TagExists(string tag)
        {
            foreach (var t in UnityEditorInternal.InternalEditorUtility.tags)
                if (t == tag) return true;
            return false;
        }
    }
}
