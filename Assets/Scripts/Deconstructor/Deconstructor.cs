using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Deconstructor : MonoBehaviour
{
    public GameObject BuildingGO;

    private VoxelGrid _voxelGrid;
    private Vector3Int _gridDimensions;
    private GameObject _gridGO;
    private Material _matTrans;
    private List<Collider> _colliders = new();
    private Bounds _meshBounds;
    private int _voxelOffset = 2;
    private float _voxelSize = 0.2f;

    private Color _supportsColor;
    private Color _floorsColor;
    private Color _othersColor;
    private Color _liftsColor;
    private Color _wallsColor;
    private Color _columnsColor;

    // Storing voxels according to the colliders they belong to
    private Dictionary<Voxel, List<Collider>> _voxelisedElements = new();

    // Start is called before the first frame update
    void Start()
    {
        _supportsColor = new Color(248 / 255f, 223/ 255f, 129/255f, 0.3f);
        _othersColor = new Color(160/255f, 206/255f, 217/255f, 0.3f);
        _wallsColor = new Color(255/255f, 16/255f, 255/255f, 0.3f);
        _columnsColor = new Color(213/255f, 182 / 255f, 213 / 255f, 0.3f);
        _liftsColor = new Color(186 / 255f, 223 / 255f, 218 / 255f, 0.3f);
        _floorsColor = new Color(155 / 255f, 208 / 255f, 183 / 255f, 0.3f);

        GameObject[] boundingMeshes = GameObject.FindGameObjectsWithTag("BoundingMesh");
        foreach (var boundingMesh in boundingMeshes)
        {
            int childCount = boundingMesh.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                _colliders.Add(boundingMesh.transform.GetChild(i).GetComponent<Collider>());
            }
        }

        Bounds meshBounds = new();
        foreach (var collider in _colliders)
        {
            meshBounds.Encapsulate(collider.bounds);
        }
        _meshBounds = meshBounds;
        _gridDimensions = GetGridDimensions(_voxelOffset, _voxelSize);
        _gridGO = new GameObject("VoxelGrid");
        _matTrans = Resources.Load<Material>("Materials/matTrans");
        _voxelGrid = new VoxelGrid(_gridDimensions, _voxelSize, GetOrigin(_voxelOffset, _voxelSize), _gridGO, _matTrans);
        StaticBatchingUtility.Combine(_gridGO);
    }
    private Vector3Int GetGridDimensions(int voxelOffset, float voxelSize) =>
        (_meshBounds.size / voxelSize).ToVector3IntRound() + Vector3Int.one * voxelOffset * 2;

    private Vector3 GetOrigin(int voxelOffset, float voxelSize) =>
        _meshBounds.center - (Vector3)GetGridDimensions(voxelOffset, voxelSize) * voxelSize / 2;

    #region VOXELISING EXTRACTED PARTS
    private void RecolorVoxelsByPart()
    {
        foreach (var kv in _voxelisedElements)
        {
            foreach (var collider in kv.Value)
            {
                if (collider.transform.CompareTag("DECO_Floors"))
                {
                    kv.Key.SetColor(_floorsColor);
                }
                if (collider.transform.CompareTag("DECO_Lifts"))
                {
                    kv.Key.SetColor(_liftsColor);
                }
                if (collider.transform.CompareTag("DECO_Walls"))
                {
                    kv.Key.SetColor(_wallsColor);
                }
                if (collider.transform.CompareTag("DECO_Columns"))
                {
                    kv.Key.SetColor(_columnsColor);
                }
            }
        }
    }
    #endregion

    public List<Collider> IsInsideCentre(Voxel voxel)
    {
        Physics.queriesHitBackfaces = true;

        var point = voxel.Centre;

        //Add a collection to store the number of hits per collider
        var sortedHits = new Dictionary<Collider, int>();
        foreach (var collider in _colliders)
            sortedHits.Add(collider, 0);

        //Shoot a ray from the point in a direction and check how many times the ray hits any mesh collider
        while (Physics.Raycast(new Ray(point, Vector3.forward), out RaycastHit hit))
        {
            var collider = hit.collider;

            //Check if the hit collider is one of the bounding mesh colliders
            //add to the colliders count
            if (sortedHits.ContainsKey(collider))
                sortedHits[collider]++;

            //A ray will stop when it hits something. We need to continue the ray, so we offset the startingpoint by a
            //minimal distanse in the diretion of the ray and continue castin the ray
            point = hit.point + Vector3.forward * 0.00001f;
        }

        //If any of the bounding mesh colliders is hit an odd number of times, this means the point is inside the bounding colliders
        var colliders = sortedHits.Where(kv => kv.Value % 2 != 0).Select(kv => kv.Key);
        return colliders.ToList();
    }

    #region BUTTONS
    public void KillVoxelsInOutBounds()
    {
        var voxels = _voxelGrid.GetVoxels().ToList();
        var voxelChunks = voxels.ChunkBy(voxels.Count / 6);

        foreach (var list in voxelChunks)
        {
            StartCoroutine(SpeedUpVoxeliseBounds(list));
        }
    }

    IEnumerator SpeedUpVoxeliseBounds(List<Voxel> voxels)
    {
        foreach (Voxel voxel in voxels)
        {
            var colliders = IsInsideCentre(voxel);
            bool isInside = colliders.Count > 0;
            if (isInside)
            {
                _voxelisedElements.Add(voxel, colliders);
            }
            if (!isInside)
                voxel.Status = VoxelState.Dead;
        }
        yield return new WaitForSeconds(0.1f);
    }
    public void ShowAliveVoxels()
    {
        _voxelGrid.ShowAliveVoxels = !_voxelGrid.ShowAliveVoxels;
    }

    public void ShowAvailableVoxels()
    {
        _voxelGrid.ShowAvailableVoxels = !_voxelGrid.ShowAvailableVoxels;
    }

    public void RecolorVoxels()
    {
        RecolorVoxelsByPart();
    }

    public void ToggleMesh()
    {
        BuildingGO.SetActive(!BuildingGO.activeSelf);
    }

    /// <summary>
    /// Find GO with script, set variables to make available in the other class
    /// </summary>
    public void InitializeAggregator()
    {
        var aggregator = GameObject.Find("AggregatorForVoxelisedBuildings").GetComponent<AggregatorForVoxelisedBuildings>();
        aggregator.Grid = _voxelGrid;
        aggregator.VoxelisedElements = _voxelisedElements;
        aggregator.Initialise(_voxelSize);
        aggregator.PlaceFirstWallPart();
        aggregator.PlaceFirstFloorPart();
    }
    #endregion
}
