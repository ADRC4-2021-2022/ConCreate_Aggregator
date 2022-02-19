using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationManager : MonoBehaviour
{
    [SerializeField]
    //Part to move
    private GameObject _goSourcePart;
    [SerializeField]
    //Part to move towards
    private GameObject _goTargetPart;
    // Start is called before the first frame update

    [SerializeField]
    private Material _sourceMaterial;
    [SerializeField]
    private Material _targetMaterial;

    private Part _sourcePart;
    private Part _targetPart;
    private Connection _sourceConnection;
    private Connection _targetConnection;
    void Start()
    {
        _sourcePart = new Part(_goSourcePart);
        _targetPart = new Part(_goTargetPart);

        int rndIndex = Random.Range(0, _sourcePart.Connections.Count);
        _sourceConnection = _sourcePart.Connections[rndIndex];

        int rndIndex2 = Random.Range(0, _targetPart.Connections.Count);
        _targetConnection = _targetPart.Connections[rndIndex2];

        SetConnectionMaterials(_sourceConnection, _sourceMaterial);
        SetConnectionMaterials(_targetConnection, _targetMaterial);


    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnGUI()
    {
        //if(GUI.Button(new Rect(10,10,200,50),"Rotate"))
        //{
        //    RotateQuaternions();
        //}

        //if (GUI.Button(new Rect(10, 60, 200, 50), "Rotate using from to"))
        //{
        //    Vector3 from = _goSourcePart.transform.forward;
        //    Vector3 to = _targetConnection.Normal;

        //    Quaternion rotation = RotateFromTo(from, to);

        //    //_goSourcePart.transform.rotation = rotation;
        //    _goSourcePart.transform.Rotate(rotation.eulerAngles);
        //}
        if (GUI.Button(new Rect(10, 120, 200, 50), "Rotate parent"))
        {
            Util.RotatePositionFromToUsingParent(_sourceConnection,_targetConnection);
        }
    }

    private void SetConnectionMaterials(Connection connection, Material material)
    {
        connection.GOConnection.GetComponentInChildren<MeshRenderer>().material = material;
    }

    

    //private void RotateMatrices()
    //{
    //    //Matrix4x4 mSource = Matrix4x4.Rotate(_sourceConnection.NormalAsQuaternion);
    //    Matrix4x4 mSource = Matrix4x4.Rotate(_sourcePart.GOPart.transform.rotation);
    //    Matrix4x4 mTarget = Matrix4x4.Rotate(_targetConnection.NormalAsQuaternion);

    //    Matrix4x4 mRotation = mTarget  *  mSource.inverse;

    //    _goSourcePart.transform.rotation = mRotation.rotation;
    //}

    //private void RotateQuaternions()
    //{
    //    //Matrix4x4 mSource = Matrix4x4.Rotate(_sourceConnection.NormalAsQuaternion);
    //    Quaternion qSource = _sourcePart.GOPart.transform.rotation.normalized;
    //    Quaternion qTarget = _targetConnection.NormalAsQuaternion.normalized;

    //    Quaternion qRotation = /*Quaternion.Euler(0, -90, 0) * */  qTarget * Quaternion.Inverse(qSource);

    //    _goSourcePart.transform.Rotate( qRotation.eulerAngles);
    //}

    ///// <summary>
    ///// Get the quaternion for rotation between two vectors. (this only take one axis into account)
    ///// </summary>
    ///// <param name="origin">Original orientation vector</param>
    ///// <param name="target">Target orientation vector</param>
    //public Quaternion RotateFromTo(Vector3 origin, Vector3 target)
    //{
    //    origin.Normalize();
    //    target.Normalize();

    //    float dot = Vector3.Dot(origin, target);
    //    float s = Mathf.Sqrt((1 + dot) * 2);
    //    float invs = 1 / s;

    //    Vector3 c = Vector3.Cross(origin, target);

    //    Quaternion rotation = new Quaternion();

    //    rotation.x = c.x * invs;
    //    rotation.y = c.y * invs;
    //    rotation.z = c.z * invs;
    //    rotation.w = s * 0.5f;

    //    rotation.Normalize();

    //    return rotation;

    //    //source: https://stackoverflow.com/questions/21828801/how-to-find-correct-rotation-from-one-vector-to-another
    //}
}
