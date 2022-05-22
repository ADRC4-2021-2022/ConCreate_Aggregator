using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Deconstructor : MonoBehaviour
{
    private VoxelGrid _voxelGrid;
    private Vector3Int _gridDimensions;
    private GameObject _gridGO;
    private Material _matTrans;
    private List<Collider> _colliders = new();
    private Bounds _meshBounds;
    private int _voxelOffset = 2;
    private float _voxelSize = 0.1f;

    private Color _supportsColor;
    private Color _floorsColor;
    private Color _othersColor;
    private Color _liftsColor;
    private Color _wallsColor;
    private Color _columnsColor;

    private Dictionary<Voxel, List<Collider>> _voxelisedElements = new();

    // Start is called before the first frame update
    void Start()
    {
        _supportsColor = Color.gray;
        _floorsColor = Color.blue;
        _othersColor = Color.green;
        _liftsColor = Color.cyan;
        _wallsColor = Color.magenta;
        _columnsColor = Color.yellow;

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
                if (collider.transform.CompareTag("DECO_Supports"))
                {
                    kv.Key.SetColor(_supportsColor);
                }
                if (collider.transform.CompareTag("DECO_Floors"))
                {
                    kv.Key.SetColor(_floorsColor);
                }
                if (collider.transform.CompareTag("DECO_Lifts"))
                {
                    kv.Key.SetColor(_liftsColor);
                }
                if (collider.transform.CompareTag("DECO_Others"))
                {
                    kv.Key.SetColor(_othersColor);
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

    #region BUTTONS
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
        yield return new WaitForSeconds(1f);
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
        var building = GameObject.Find("DecoBuilding");
        building.SetActive(!building.activeSelf);
    }
    #endregion
}
