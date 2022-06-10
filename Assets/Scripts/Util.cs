using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//static classes can be accessed in the entire solution without creating an object of the class
public static class Util
{
    /// <summary>
    /// Extension method to Unities Vector3Int class. Now you can use a Vector3 variable and use the .ToVector3InRound to get the vector rounded to its integer values
    /// </summary>
    /// <param name="v">the Vector3 variable this method is applied to</param>
    /// <returns>the rounded Vector3Int value of the given Vector3</returns>
    public static Vector3Int ToVector3IntRound(this Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));


    /// <summary>
    /// List of the Carthesian directions (along the x, y, z axis)
    /// </summary>
    public static List<Vector3Int> Directions = new List<Vector3Int>
    {
        new Vector3Int(-1,0,0),// min x
        new Vector3Int(1,0,0),// plus x
       // new Vector3Int(0,-1,0),// min y
      //  new Vector3Int(0,1,0),// plus y
        new Vector3Int(0,0,-1),// min z
        new Vector3Int(0,0,1)// plus z
    };

    public static Dictionary<int, int> InversedDirections = new Dictionary<int, int>()
    {
        { 2, 3 },
        { 3, 2 },
        { 1, 0 },
        { 0, 1 },
        { 4, 5 },
        { 5, 4 }
    };

/// <summary>
/// Generate a random color
/// </summary>
public static Color RandomColor
    {
        get
        {
            float r = Random.Range(0, 255) / 255f;
            float g = Random.Range(0, 255) / 255f;
            float b = Random.Range(0, 255) / 255f;
            return new Color(r, g, b);
        }
    }

    public static Color RandomColorWithTransparency
    {
        get
        {
            float r = Random.Range(0, 255) / 255f;
            float g = Random.Range(0, 255) / 255f;
            float b = Random.Range(0, 255) / 255f;
            return new Color(r, g, b, 0.5f);
        }
    }

    public static bool TryOrientIndex(Vector3Int localIndex, Vector3Int anchor, Quaternion rotation, Vector3Int gridDimensions, out Vector3Int worldIndex)
    {
        Vector3 rotated = rotation * localIndex;
        worldIndex = anchor + rotated.ToVector3IntRound();
        return CheckInBounds(gridDimensions, worldIndex);
    }

    /// <summary>
    /// Check if an index is inside a given bounds.
    /// </summary>
    /// <param name="gridDimensions">Dimensions of the grid</param>
    /// <param name="index">index to check</param>
    /// <returns>true if the point is inside the bounds.</returns>
    public static bool CheckInBounds(Vector3Int gridDimensions, Vector3Int index)
    {
        if (index.x < 0 || index.x >= gridDimensions.x) return false;
        if (index.y < 0 || index.y >= gridDimensions.y) return false;
        if (index.z < 0 || index.z >= gridDimensions.z) return false;

        return true;
    }

    /// <summary>
    /// Check if a point is inside a collider. The collider needs to be watertight!
    /// </summary>
    /// <param name="point">point to check</param>
    /// <param name="collider">collider to check</param>
    /// <returns>true if inside the collider</returns>
    public static bool PointInsideCollider(Vector3 point, Collider collider)
    {
        Physics.queriesHitBackfaces = true;

        int hitCounter = 0;

        //Shoot a ray from the point in a direction and check how many times the ray hits the mesh collider
        while (Physics.Raycast(new Ray(point, Vector3.forward), out RaycastHit hit))
        {
            //Check if the hit collider is the mesh you're currently checking
            if (hit.collider == collider)
                hitCounter++;

            //A ray will stop when it hits something. We need to continue the ray, so we offset the startingpoint by a
            //minimal distanse in the diretion of the ray and continue castin the ray
            point = hit.point + Vector3.forward * 0.00001f;
        }

        //If the mesh is hit an odd number of times, this means the point is inside the collider
        bool isInside = hitCounter % 2 != 0;
        return isInside;
    }

    /// <summary>
    /// Generate a random index within voxelgrid dimensions
    /// </summary>
    /// <returns>A random index</returns>
    public static Vector3Int RandomIndex(Vector3Int gridDimensions)
    {
        int x = Random.Range(0, gridDimensions.x);
        int y = Random.Range(0, gridDimensions.y);
        int z = Random.Range(0, gridDimensions.z);
        return new Vector3Int(x, y, z);
    }

    /// <summary>
    /// Get a random rotation alligned with the x,y or z axis
    /// </summary>
    /// <returns>A random rotation</returns>
    public static Quaternion RandomCarthesianRotation()
    {
        int x = Random.Range(0, 4) * 90;
        int y = Random.Range(0, 4) * 90;
        int z = Random.Range(0, 4) * 90;
        return Quaternion.Euler(x, y, z);
    }

    public static Quaternion RotateFromTo(Vector3 origin, Vector3 target)
    {
        origin.Normalize();
        target.Normalize();

        float dot = Vector3.Dot(origin, target);
        float s = Mathf.Sqrt((1 + dot) * 2);
        float invs = 1 / s;

        Vector3 c = Vector3.Cross(origin, target);

        Quaternion rotation = new Quaternion();

        rotation.x = c.x * invs;
        rotation.y = c.y * invs;
        rotation.z = c.z * invs;
        rotation.w = s * 0.5f;

        rotation.Normalize();

        return rotation;
    }

    public static void Shuffle<T>(this IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    public static void RotatePositionFromToUsingParent(Connection movingConnection, Connection targetConnection)
    {
        if (movingConnection.GOConnection == null || targetConnection.GOConnection == null) return;

        Part movingPart = movingConnection.ThisPart;

        //Instantiate a copy of the source connection and set as parent of the part object
        GameObject connectionParent =
            GameObject.Instantiate(movingConnection.GOConnection,
            movingConnection.GOConnection.transform.position,
            movingConnection.GOConnection.transform.rotation);
        movingPart.GOPart.transform.SetParent(connectionParent.transform);

        //Get the rotation quateernion for 180 degrees over the y axis so make the connection meet in oposite direction 
        Quaternion rotate180 = Quaternion.LookRotation(Vector3.back, Vector3.up);
        if (movingConnection.GOConnection.CompareTag("StackingConnectionNormal")
            && targetConnection.GOConnection.CompareTag("StackingConnectionNormal"))
        //    && movingConnection.GOConnection.transform.GetChild(0).GetComponent<MeshRenderer>().bounds.size.Equals(
        //        targetConnection.GOConnection.transform.GetChild(0).GetComponent<MeshRenderer>().bounds.size))
        {
            rotate180 = Quaternion.identity;
        }

        //Set the rotation and position of the parrent to match the target  and rotate with the 180 rotation quaternion
        connectionParent.transform.SetPositionAndRotation(targetConnection.Position, targetConnection.NormalAsQuaternion * rotate180);

        //Set the part back in the root of the hierarchy and destroy the temporary parent object. The part wil not move
        movingPart.GOPart.transform.parent = null;
        GameObject.Destroy(connectionParent);
    }

    public static List<GameObject> GetChildObject(Transform parent, string tag)
    {
        List<GameObject> taggedChildren = new();

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.CompareTag(tag))
            {
                taggedChildren.Add(child.gameObject);
            }
            if (child.childCount > 0)
            {
                GetChildObject(child, tag);
            }
        }

        return taggedChildren;
    }

    public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
    {
        return source
            .Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
    }

    public static T FindComponentInChildren<T>(Transform parent)
    {
        var parentComponent = parent.GetComponent<T>();
        if (parentComponent != null) return parentComponent;
        else
        {
            var childComponent = parent.GetComponentInChildren<T>();
            if (childComponent == null) Debug.Log("Did not find component in children");

            return childComponent;
        }
    }

    public static Mesh ScaleMesh(Mesh original, Vector3 scale)
    {
        var originalVertices = original.vertices;
        var vertices = new Vector3[originalVertices.Length];

        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = originalVertices[i];
            vertices[i] = vertex;
            vertices[i].Scale(scale);
        }
        Mesh result = new();
        result.vertices = vertices;
        result.RecalculateBounds();
        result.RecalculateNormals();
        return result;
    }
}
