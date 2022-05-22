using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class VoxelGrid
{
    #region public fields
    public Vector3Int GridDimensions { get; private set; }
    public float VoxelSize { get; private set; }
    public Vector3 Origin { get; private set; }

    public Vector3 Centre => Origin + (Vector3)GridDimensions * VoxelSize / 2;

    public GameObject GOGrid;

    public Corner[,,] Corners;
    public Vector3 Corner;

    public Voxel[,,] Voxels;

    public Material MatTrans;

    public bool ShowAliveVoxels
    {
        get
        {
            return _showAliveVoxels;
        }
        set
        {
            _showAliveVoxels = value;
            foreach (Voxel voxel in GetVoxels())
            {
                voxel.ShowAliveVoxel = value;
            }
        }
    }

    public bool ShowAvailableVoxels
    {
        get
        {
            return _showAvailableVoxels;
        }
        set
        {
            _showAvailableVoxels = value;
            foreach (Voxel voxel in GetVoxels())
            {
                voxel.ShowAvailableVoxel = value;
            }
        }
    }


    /// <summary>
    /// what percentage of the available grid has been filled up in percentage
    /// </summary>
    public float Efficiency
    {
        get
        {
            //We're storing the voxels as a list so that we don't have to get them twice. counting a list is more efficient than counting an IEnumerable
            List<Voxel> flattenedVoxels = GetVoxels().ToList();
            //if we don't cast this value to a float, it always returns 0 as it is rounding down to integer values
            return (float)flattenedVoxels.Count(v => v.Status == VoxelState.Alive) / flattenedVoxels.Where(v => v.Status != VoxelState.Dead).Count() * 100;
        }
    }
    #endregion

    #region private fields
    private bool _showAliveVoxels = false;
    private bool _showAvailableVoxels = false;
    #endregion

    #region Constructors
    /// <summary>
    /// Create a new voxelgrid
    /// </summary>
    /// <param name="gridDimensions">X,Y,Z dimensions of the grid</param>
    /// <param name="voxelSize">The size of the voxels</param>
    /// <param name="origin">Where the voxelgrid starts</param>
    public VoxelGrid(Vector3Int gridDimensions, float voxelSize, Vector3 origin, GameObject goGrid, Material matTrans)
    {
        GridDimensions = gridDimensions;
        VoxelSize = voxelSize;
        Origin = origin;
        GOGrid = goGrid;
        MatTrans = matTrans;

        MakeVoxels();
        MakeCorners();
    }

    //Copy constructor with different signature. will refer to the original constructor
    /// <summary>
    /// Create a new voxelgrid
    /// </summary>
    /// <param name="x">X dimensions of the grid</param>
    /// <param name="y">Y dimensions of the grid</param>
    /// <param name="z">Z dimensions of the grid</param>
    /// <param name="voxelSize">The size of the voxels</param>
    /// <param name="origin">Where the voxelgrid starts</param>
    public VoxelGrid(int x, int y, int z, float voxelSize, Vector3 origin, GameObject goGrid, Material matTrans) : this(new Vector3Int(x, y, z), voxelSize, origin, goGrid, matTrans) { }
    #endregion

    #region private functions
    /// <summary>
    /// Create all the voxels in the grid
    /// </summary>
    private void MakeVoxels()
    {
        Voxels = new Voxel[GridDimensions.x, GridDimensions.y, GridDimensions.z];
        for (int x = 0; x < GridDimensions.x; x++)
        {
            for (int y = 0; y < GridDimensions.y; y++)
            {
                for (int z = 0; z < GridDimensions.z; z++)
                {
                    Voxels[x, y, z] = new Voxel(x, y, z, this);
                }
            }
        }

        ShowAvailableVoxels = true;
        ShowAliveVoxels = true;
    }

    #endregion

    #region public function
    /// <summary>
    /// Get the Voxels of the <see cref="VoxelGrid"/>
    /// </summary>
    /// <returns>A flattened collections of all the voxels</returns>
    public IEnumerable<Voxel> GetVoxels()
    {
        for (int x = 0; x < GridDimensions.x; x++)
            for (int y = 0; y < GridDimensions.y; y++)
                for (int z = 0; z < GridDimensions.z; z++)
                {
                    yield return Voxels[x, y, z];
                }
    }

    //Shorthand syntax for a function returning the output of GetVoxelByIndex
    //Two function with the same name, but different parameters ==> different signature
    /// <summary>
    /// Return a voxel at a certain index
    /// </summary>
    /// <param name="x">x index</param>
    /// <param name="y">y index</param>
    /// <param name="z">z index</param>
    /// <returns>Voxel at x,y,z index. null if the voxel doesn't exist or is out of bounds</returns>
    public Voxel GetVoxelByIndex(int x, int y, int z) => GetVoxelByIndex(new Vector3Int(x, y, z));

    /// <summary>
    /// Return a voxel at a certain index
    /// </summary>
    /// <param name="index">x,y,z index</param>
    /// <returns>Voxel at x,y,z index. null if the voxel doesn't exist or is out of bounds</returns>
    public Voxel GetVoxelByIndex(Vector3Int index)
    {
        if (!Util.CheckInBounds(GridDimensions, index) || Voxels[index.x, index.y, index.z] == null)
        {
            Debug.Log($"A Voxel at {index} doesn't exist");
            return null;
        }
        return Voxels[index.x, index.y, index.z];
    }

    /// <summary>
    /// 
    /// Get all the voxels at a certain XZ layer
    /// </summary>
    /// <param name="yLayer">Y index of the layer</param>
    /// <returns>List of voxels in XZ layer</returns>
    public List<Voxel> GetVoxelsYLayer(int yLayer)
    {
        List<Voxel> yLayerVoxels = new List<Voxel>();

        //Check if the yLayer is within the bounds of the grid
        if (yLayer < 0 || yLayer >= GridDimensions.y)
        {
            Debug.Log($"Requested Y Layer {yLayer} is out of bounds");
            return null;
        }

        for (int x = 0; x < GridDimensions.x; x++)
            for (int z = 0; z < GridDimensions.z; z++)
                yLayerVoxels.Add(Voxels[x, yLayer, z]);

        return yLayerVoxels;
    }

    /// <summary>
    /// Set the entire grid 'Alive' to a certain state
    /// </summary>
    /// <param name="state">the state to set</param>
    public void SetGridState(VoxelState state)
    {
        foreach (var voxel in Voxels)
        {
            voxel.Status = state;
        }
    }

    /// <summary>
    /// Set the non dead voxels of the  grid to a certain state
    /// </summary>
    /// <param name="state">the state to set</param>
    public void SetNonDeadGridState(VoxelState state)
    {
        foreach (var voxel in GetVoxels().Where(v => v.Status != VoxelState.Dead))
        {
            voxel.Status = state;
        }
    }
    /// <summary>
    /// Copy all the layers one layer up, starting from the top layer going down.
    /// The bottom layer will remain and the top layer will dissapear.
    /// </summary>
    public void CopyLayersUp()
    {
        //Check the signature of the for loop. Starting at the top layer and going down
        for (int y = GridDimensions.y - 1; y > 0; y--)
        {
            for (int x = 0; x < GridDimensions.x; x++)
            {
                for (int z = 0; z < GridDimensions.z; z++)
                {
                    Voxels[x, y, z].Status = Voxels[x, y - 1, z].Status;
                }
            }
        }
    }

    /// <summary>
    /// Get the number of neighbouring voxels that are alive
    /// </summary>
    /// <param name="voxel">the voxel to get the neighbours from</param>
    /// <returns>number of alive neighbours</returns>
    public int GetNumberOfAliveNeighbours(Voxel voxel)
    {
        int nrOfAliveNeighbours = 0;
        foreach (var vox in voxel.GetFaceNeighbourList())
        {
            if (vox.Status == VoxelState.Alive) nrOfAliveNeighbours++;
        }

        return nrOfAliveNeighbours;
    }

    /// <summary>
    /// Get a random voxel with the Status Available
    /// </summary>
    /// <returns>The random available voxel</returns>
    public Voxel GetRandomAvailableVoxel()
    {
        List<Voxel> voxels = new List<Voxel>(GetVoxels().Where(v => v.Status == VoxelState.Available));
        return voxels[Random.Range(0, voxels.Count)];
    }

    /// <summary>
    /// Creates the Corners of each Voxel
    /// </summary>
    private void MakeCorners()
    {
        Corner = new Vector3(Origin.x - VoxelSize / 2, Origin.y - VoxelSize / 2, Origin.z - VoxelSize / 2);

        Corners = new Corner[GridDimensions.x + 1, GridDimensions.y + 1, GridDimensions.z + 1];

        for (int x = 0; x < GridDimensions.x + 1; x++)
            for (int y = 0; y < GridDimensions.y + 1; y++)
                for (int z = 0; z < GridDimensions.z + 1; z++)
                {
                    Corners[x, y, z] = new Corner(new Vector3Int(x, y, z), this);
                }
    }

    /// <summary>
    /// Get the Corners of the <see cref="VoxelGrid"/>
    /// </summary>
    /// <returns>All the Corners</returns>
    public IEnumerable<Corner> GetCorners()
    {
        for (int x = 0; x < GridDimensions.x + 1; x++)
            for (int y = 0; y < GridDimensions.y + 1; y++)
                for (int z = 0; z < GridDimensions.z + 1; z++)
                {
                    yield return Corners[x, y, z];
                }
    }
    #endregion
}
