
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Piece : UdonSharpBehaviour
{
    public Renderer Renderer;
    public Material MaterialBlue;
    public Material MaterialOrange;

    private int _nClockChildrenAdded = 0;
    private int _iClockChildrenInAction = 1;
    private bool _clockRunning = false;

    // グリッドIDとは: 左上から数えた 0-indexの数字
    private int _asIsGridId = 0;
    private int _toBeGridId = 0;
    private int _asIsRotCode = 0;
    private int _toBeRotCode = 0;

    public int pubpub;
    private int _realBoardSize;
    private Vector3[] _allPositions;
    private Vector3[] _allRotations = new Vector3[4] {
        new Vector3(  0f, 0f, 0f),
        new Vector3( 90f, 0f, 0f),
        new Vector3(180f, 0f, 0f),
        new Vector3(270f, 0f, 0f)
    };

    public int Initialize(string argColor,
        int argRealBoardSize,
        Vector3[] argAllPositions,
        int argInitialGridId,
        int argInitialRotCode
    )
    {
        //Debug.Log("piece initialized");

        pubpub = argRealBoardSize;
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

        return 0;
    }

    void Start()
    {
    }

    void Update()
    {

    }

    // Clock が訪れた場合
    public int OnClockVisit(int argTicketNumber)
    {   
        // チケット番号が自然数ならば受け付け済の人の催促か完了報告
        // レスポンス: -1(まだはやい) n(お前の出番だ) 0(完了との旨了解)
        if ( argTicketNumber >= 1 && argTicketNumber <= _iClockChildrenInAction )
        {
            // 出番でないなら -1 で突っぱねる
            if ( argTicketNumber < _iClockChildrenInAction )
            {
                return -1;
            }
            // 出番です
            else if ( argTicketNumber == _iClockChildrenInAction )
            {
                // 実行に入る
                if ( !_clockRunning )
                {
                    Debug.Log("お前の出番だ");
                    _clockRunning = true;
                    return 0;         // お前の出番だ
                }
                else
                {
                    Debug.Log("完了とのこと了解");
                    _clockRunning = false;
                    _iClockChildrenInAction++;
                    return 0;                       // 了解。
                }
            }
            // 起こらないことになってる
            else
            {
                Debug.Log("見えるわけない チケット持ちが迷っとる");
                return -1;
            }
        }
        // チケット番号 0 の場合は登録申し込み
        // 新規登録してチケット番号振ってあげる
        else if ( argTicketNumber == 0 )
        {
            _nClockChildrenAdded++;
            return _nClockChildrenAdded;
        }
        // 起こらないことになってる
        else
        {
            Debug.Log("見えるわけない 初回チケット登録のミスか");
            return -1;
        }
    }

    // グリッドIDにテレポート 
    public void MoveToGridIdAndRotCode(int argGridId, int argRotCode)
    {
        string msg = "テレポート   グリッドID: " + argGridId.ToString();
        msg += "\nVecter3.x = " + _allPositions[argGridId].x.ToString();
        msg += "\nVecter3.y = " + _allPositions[argGridId].y.ToString();
        msg += "\nVecter3.z = " + _allPositions[argGridId].z.ToString();
        Debug.Log(msg);

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
