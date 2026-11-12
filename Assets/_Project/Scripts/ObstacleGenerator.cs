using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObstacleGenerator : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private MeshFilter _planeMesh;

    [SerializeField] private GameObject _obstaclePrefab;

    [Header("Generation")] [SerializeField]
    private int _seed = 12345;

    [SerializeField, Min(0f)] private float _density = 0.5f;

    [SerializeField, Min(0f)] private float _minimumSpacing = 1f;

    [SerializeField, Min(0f)] private float _edgePadding = 0.5f;

    [SerializeField, Min(0)] private int _maxObstacles = 2000;

    [Header("Placement")] [SerializeField] private bool _randomRotation = true;
    [SerializeField] private float _surfaceOffset = 0f;

    [SerializeField] private bool _generateOnStart = true;

    [SerializeField, HideInInspector] private Transform _generatedContainer;

    private const string GeneratedRootName = "Generated Obstacles";

    public int Seed
    {
        get => _seed;
        set => _seed = value;
    }

    private void Start()
    {
        if (_generateOnStart)
            Generate();
    }

    private void Reset()
    {
        _planeMesh = GetComponent<MeshFilter>();
    }

    [ContextMenu("Regenerate Obstacles")]
    public void Generate()
    {
        if (_planeMesh == null || _planeMesh.sharedMesh == null ||
            _obstaclePrefab == null)
        {
            Debug.LogError("Assign a plane mesh and obstacle prefab.");
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying &&
            !PrefabUtility.IsPartOfPrefabAsset(_obstaclePrefab))
        {
            Debug.LogError("Assign a prefab asset, not a scene object.");
            return;
        }
#endif

        Clear();

        Transform container = GetOrCreateContainer();
        Transform plane = _planeMesh.transform;
        Bounds bounds = _planeMesh.sharedMesh.bounds;

        Vector3 axisX = plane.TransformVector(Vector3.right);
        Vector3 axisZ = plane.TransformVector(Vector3.forward);

        float scaleX = axisX.magnitude;
        float scaleZ = axisZ.magnitude;

        if (scaleX <= 0f || scaleZ <= 0f)
            return;

        float minX = bounds.min.x + _edgePadding / scaleX;
        float maxX = bounds.max.x - _edgePadding / scaleX;

        float minZ = bounds.min.z + _edgePadding / scaleZ;
        float maxZ = bounds.max.z - _edgePadding / scaleZ;

        if (minX >= maxX || minZ >= maxZ)
        {
            Debug.LogWarning("Edge padding is too large.");
            return;
        }

        float area = Vector3.Cross(axisX, axisZ).magnitude *
                     (maxX - minX) * (maxZ - minZ);

        int targetCount = Mathf.Clamp(
            Mathf.RoundToInt(area * _density),
            0,
            _maxObstacles
        );

        System.Random random = new System.Random(_seed);

        List<Vector3> positions = new();

        float minSpacingSqr = _minimumSpacing * _minimumSpacing;
        int attempts = 0;
        int maxAttempts = Mathf.Max(100, targetCount * 25);

        Vector3 normal = plane.up;

        while (positions.Count < targetCount &&
               attempts < maxAttempts)
        {
            attempts++;

            float x = Mathf.Lerp(
                minX, maxX, (float)random.NextDouble()
            );

            float z = Mathf.Lerp(
                minZ, maxZ, (float)random.NextDouble()
            );

            Vector3 localPosition = new Vector3(
                x, bounds.max.y, z
            );

            Vector3 position = plane.TransformPoint(localPosition);

            bool valid = true;

            foreach (Vector3 existing in positions)
            {
                if ((existing - position).sqrMagnitude < minSpacingSqr)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
                continue;

            positions.Add(position);

            float angle = _randomRotation
                ? (float)random.NextDouble() * 360f
                : 0f;

            Quaternion rotation =
                Quaternion.AngleAxis(angle, normal) *
                Quaternion.FromToRotation(Vector3.up, normal) *
                _obstaclePrefab.transform.rotation;

            GameObject instance = CreateInstance(container);

            if (instance == null)
                continue;

            instance.transform.SetPositionAndRotation(
                position, rotation
            );

            AlignToSurface(instance, position, normal);
        }

        Debug.Log(
            $"Generated {positions.Count}/{targetCount} obstacles " +
            $"using seed {_seed}."
        );
    }

    [ContextMenu("Clear Obstacles")]
    public void Clear()
    {
        if (_generatedContainer == null)
            return;

        for (int i = _generatedContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child =
                _generatedContainer.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(child);
            else
#endif
                Destroy(child);
        }
    }


    private Transform GetOrCreateContainer()
    {
        if (_generatedContainer != null)
            return _generatedContainer;

        GameObject root = new GameObject(
            $"{name}_Generated Obstacles"
        );

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(
                root, "Create Obstacles Root"
            );

            Undo.RecordObject(this, "Assign Obstacles Root");
        }
#endif

        // Keep at scene root to avoid inheriting plane scale.
        root.transform.SetPositionAndRotation(
            Vector3.zero, Quaternion.identity
        );

        root.transform.localScale = Vector3.one;

        _generatedContainer = root.transform;

        return _generatedContainer;
    }

    private GameObject CreateInstance(Transform parent)
    {
        GameObject instance;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            instance = PrefabUtility.InstantiatePrefab(
                _obstaclePrefab, parent
            ) as GameObject;

            if (instance != null)
                Undo.RegisterCreatedObjectUndo(
                    instance, "Generate Obstacle"
                );

            return instance;
        }
#endif

        instance = Instantiate(_obstaclePrefab, parent);
        return instance;
    }

    private void AlignToSurface(
        GameObject instance,
        Vector3 surfacePoint,
        Vector3 normal)
    {
        // Prefer collider bounds for placing the obstacle
        // on the plane rather than embedding it in the surface.
        Collider[] colliders =
            instance.GetComponentsInChildren<Collider>();

        Bounds bounds;

        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;

            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);
        }
        else
        {
            Renderer[] renderers =
                instance.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                instance.transform.position +=
                    normal * _surfaceOffset;
                return;
            }

            bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 extents = bounds.extents;

        float projectedExtent =
            Mathf.Abs(normal.x) * extents.x +
            Mathf.Abs(normal.y) * extents.y +
            Mathf.Abs(normal.z) * extents.z;

        float bottomDistance =
            Vector3.Dot(bounds.center - surfacePoint, normal)
            - projectedExtent;

        instance.transform.position +=
            normal * (_surfaceOffset - bottomDistance);
    }
}