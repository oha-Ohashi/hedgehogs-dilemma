
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Piece : UdonSharpBehaviour
{
    public Renderer Renderer;
    public Material MaterialBlue;
    public Material MaterialOrange;

    private int _id;
    // グリッドIDとは: 左上から数えた 0-indexの数字
    private int _asIsGridId = 0;
    private int _toBeGridId = 0;
    private int _asIsRotCode = 0;
    private int _toBeRotCode = 0;

    private int _realBoardSize;
    private Vector3[] _allPositions;
    private Vector3[] _allRotations = new Vector3[4] {
        new Vector3(  0f, 0f, 0f),
        new Vector3( 90f, 0f, 0f),
        new Vector3(180f, 0f, 0f),
        new Vector3(270f, 0f, 0f)
    };

    public void Initialize(string argColor,
        int argRealBoardSize,
        Vector3[] argAllPositions,
        int argInitialGridId,
        int argInitialRotCode
    )
    {
        _realBoardSize = argRealBoardSize;
        _allPositions = argAllPositions;

        _asIsGridId = -1;                        // すぐまともになるよ
        _toBeGridId = argInitialGridId;

        _asIsRotCode = -1;                        // すぐまともになるよ
        _toBeRotCode = argInitialRotCode;

        MoveToGridIdAndRotCode(argInitialGridId, argInitialRotCode);
        SetMaterialMode(argColor);
        ChangeGridIdToGo(_toBeGridId);
        ChangeRotCodeToGo(_toBeRotCode);
    }

    void Start()
    {
    }

    void Update()
    {

    }

    // グリッドIDにテレポート 
    public void MoveToGridIdAndRotCode(int argGridId, int argRotCode)
    {
        this.gameObject.transform.localPosition = _allPositions[argGridId];
        this.gameObject.transform.localEulerAngles = _allRotations[argRotCode]; 
    }

    // 目的のグリッドIDを変える
    public void ChangeGridIdToGo(int argToBeGridId)
    {
        this._asIsGridId = this._toBeGridId;
        this._toBeGridId = argToBeGridId;
    }

    // 目的のRotCodeを変える
    public void ChangeRotCodeToGo(int argToBeRotCode)
    {
        Debug.Log("mbyaaaaa");
        this._asIsRotCode = this._toBeRotCode;
        this._toBeRotCode = argToBeRotCode;
        Debug.Log(this._asIsRotCode);
        Debug.Log(this._toBeRotCode);
    }

    // Lerpしていく
    public void StepOneLerp(float argInterpolatePos, float argInterpolateRot)
    {
        //////////////  位置  /////////////
        Vector3 asIsPos = _allPositions[_asIsGridId];
        Vector3 toBePos = _allPositions[_toBeGridId];
        Vector3 posBetween = Vector3.Lerp(asIsPos, toBePos, argInterpolatePos);
        
        this.gameObject.transform.localPosition = posBetween;

        //////////////  角度  /////////////
        Vector3 asIsRot = _allRotations[_asIsRotCode];
        Vector3 toBeRot = _allRotations[_toBeRotCode];
        Vector3 rotBetween = Vector3.Lerp(asIsRot, toBeRot, argInterpolateRot); 

        this.gameObject.transform.localEulerAngles = rotBetween;
    }

    // as-is グリッドIDを教えてあげる
    public int GetAsIsGridId()
    {
        return _asIsGridId;
    }
    // as-is RotCodeを教えてあげる
    public int GetAsIsRotCode()
    {
        return _asIsRotCode;
    }
     
    // マテリアルを設定
    public void SetMaterialMode(string argMode)
    {
        if (argMode == "transparent") {
            Renderer.enabled = false;
        } else if (argMode == "blue") {
            Renderer.enabled = true;
            Renderer.material = MaterialBlue;
        } else {
            Renderer.enabled = true;
            Renderer.material = MaterialOrange;
        }
    }
}
