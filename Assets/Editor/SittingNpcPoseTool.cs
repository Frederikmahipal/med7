using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public static class SittingNpcPoseTool
{
    private const string SittingRootName = "Sitting";
    private const string SourceNpcName = "npc_csl_00_character_02m";
    private const string TargetNpcPrefix = "npc_csl_00_character";

    private struct LocalTransformPose
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    [MenuItem("Tools/NPC Pose/Apply Sitting Pose From Template")]
    public static void ApplySittingPoseFromTemplate()
    {
        Transform sittingRoot = FindSceneObject(SittingRootName);
        if (sittingRoot == null)
        {
            Debug.LogWarning($"Could not find a scene object named '{SittingRootName}'.");
            return;
        }

        Transform source = FindDirectChildByName(sittingRoot, SourceNpcName);
        if (source == null)
        {
            Debug.LogWarning($"Could not find '{SourceNpcName}' directly under '{SittingRootName}'.");
            return;
        }

        Dictionary<string, LocalTransformPose> sourcePose = CaptureDescendantPose(source);
        int changedTargets = 0;

        foreach (Transform child in sittingRoot)
        {
            if (child == source || !child.name.StartsWith(TargetNpcPrefix))
                continue;

            int changedBones = ApplyDescendantPose(child, sourcePose, source.localScale);
            if (changedBones > 0)
            {
                changedTargets++;
                Debug.Log($"Applied sitting pose to '{child.name}' ({changedBones} matching child transforms).");
            }
        }

        Debug.Log($"Finished applying sitting pose from '{SourceNpcName}' to {changedTargets} NPC(s) under '{SittingRootName}'.");
    }

    private static Dictionary<string, LocalTransformPose> CaptureDescendantPose(Transform root)
    {
        var poseByPath = new Dictionary<string, LocalTransformPose>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root)
                continue;

            poseByPath[GetRelativePath(root, child)] = new LocalTransformPose
            {
                Position = child.localPosition,
                Rotation = child.localRotation,
                Scale = child.localScale
            };
        }

        return poseByPath;
    }

    private static int ApplyDescendantPose(Transform targetRoot, Dictionary<string, LocalTransformPose> sourcePose, Vector3 sourceRootScale)
    {
        Transform[] targetTransforms = targetRoot.GetComponentsInChildren<Transform>(true);
        Undo.RecordObjects(targetTransforms, "Apply Sitting NPC Pose");

        targetRoot.localScale = sourceRootScale;
        EditorUtility.SetDirty(targetRoot);

        int changedCount = 0;
        foreach (Transform target in targetTransforms)
        {
            if (target == targetRoot)
                continue;

            string path = GetRelativePath(targetRoot, target);
            if (!sourcePose.TryGetValue(path, out LocalTransformPose pose))
                continue;

            target.localPosition = pose.Position;
            target.localRotation = pose.Rotation;
            target.localScale = pose.Scale;
            EditorUtility.SetDirty(target);
            changedCount++;
        }

        return changedCount;
    }

    private static Transform FindSceneObject(string objectName)
    {
        foreach (GameObject rootObject in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform result = FindChildRecursive(rootObject.transform, objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform result = FindChildRecursive(child, objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static Transform FindDirectChildByName(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static string GetRelativePath(Transform root, Transform child)
    {
        var names = new Stack<string>();
        Transform current = child;
        while (current != null && current != root)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }
}

