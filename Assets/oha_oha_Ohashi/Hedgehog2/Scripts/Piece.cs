
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Piece : UdonSharpBehaviour
{
    public Renderer Renderer;
    public Material MaterialBlue;
    public Material MaterialOrange;

    private Clock _runningClock;
    private int _iClockChildAddedLastTime = 0;
    private int _iClockChildRunning = 0;
    private int _iClockChildHasRun = 0;
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
    private float[] rotDiffList = new float[4] {0f, 90f, -90f, 180f};
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
        // AddedLastTime    // インクリ on registered
        // Running          // インクリ here                追い越さないように1個ずつすすむ
        // HasRun           // インクリ on clock done
        //
        // 未処理タイマーあるねぇ
        if ( _iClockChildAddedLastTime > _iClockChildHasRun )
        {
            // つついてあげないと
            // それもう終わってますよ
            if ( _iClockChildRunning == _iClockChildHasRun )
            {
                _iClockChildRunning++;
                
                GameObject runningObj = this.transform.GetChild(_iClockChildRunning).gameObject;
                _runningClock = runningObj.GetComponent<Clock>();

                _runningClock.OnNudged();
            } 
            // いま処理中みたいやね
            else if ( _iClockChildRunning == _iClockChildHasRun + 1 )
            {
                _runningClock.RunOneLerp();
            }
            else
            {
            }
        }
    }

    // 最後尾を覚える
    public void OnClockRegistered()
    {
        _iClockChildAddedLastTime++;
    }

    // 最後尾が終わったならハングリー精神なくなる可能性ある
    public void OnClockIsDone()
    {
        _iClockChildHasRun++;
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
            //Debug.Log("見えるわけない 初回チケット登録のミスか");
            Debug.Log("投げられたチケット番号: " + argTicketNumber.ToString());
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
        // Debug.Log("change rot code to go 前の RotCode: " + this._asIsRotCode);
        // Debug.Log("change rot code to go 後の RotCode: " + this._toBeRotCode);
    }
    
    private Vector3[] GetRotationsWithout270(int argAsIsRotCode, int argToBeRotCode)
    {
        int deltaRot = GetDeltaRot(argAsIsRotCode, argToBeRotCode);
        Vector3 asIsRot = _allRotations[argAsIsRotCode];
        float newXrot = asIsRot.x + rotDiffList[deltaRot];
        Vector3 toBeRot = new Vector3(newXrot, asIsRot.y, asIsRot.z);
        return new Vector3[2] { asIsRot, toBeRot }; 
    }

    // 旧 → 新 2つの0123方向コードを比較して回転量を算出
    private int GetDeltaRot(int asisRotCode, int tobeRotCode)
    {
        int[][] resDelta = new int[4][];
        resDelta[0] = new int[4] {0b00, 0b01, 0b11, 0b10}; 
        resDelta[1] = new int[4] {0b10, 0b00, 0b01, 0b11}; 
        resDelta[2] = new int[4] {0b11, 0b10, 0b00, 0b01}; 
        resDelta[3] = new int[4] {0b01, 0b11, 0b10, 0b00}; 
        return resDelta[asisRotCode][tobeRotCode];
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
        Vector3[] rots = GetRotationsWithout270(_asIsRotCode, _toBeRotCode);
        Vector3 rotBetween = Vector3.Lerp(rots[0], rots[1], argInterpolateRot); 

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
