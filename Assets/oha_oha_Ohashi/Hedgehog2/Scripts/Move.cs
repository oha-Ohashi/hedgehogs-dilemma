
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Move : UdonSharpBehaviour
{
    public Transform HousingComplexTransform;
    public GameObject AdamPieceObj;
    public Piece AdamPieceBehaviour;
    public GameObject AdamClockObj;
    public Clock AdamClockBehaviour;

    private float _millis = 0f;
    private int _boardSizeCode;
    private int _realBoardSize;
    private int _nSquaresOnBoard;
    private float _gridWidth = 0.4195f / 3.0f;
    private Vector3[] _allPositions;

    void Start()
    {
        AdamPieceBehaviour.SetMaterialMode("transparent");

        Demo();
    }

    // マスターから叩く想定
    public int InitializeMove(int argRealBoardSize)
    {
        _realBoardSize = argRealBoardSize;
        _nSquaresOnBoard = _realBoardSize * _realBoardSize;
        _allPositions = new Vector3[_nSquaresOnBoard];
        for ( int i = 0; i < _nSquaresOnBoard; i++ ) {
            int col = i % _realBoardSize;
            int row = i / _realBoardSize;
            float minZ = _gridWidth * ((-1 * (float)argRealBoardSize / 2) + 0.5f);
            float maxY = -minZ - 0.01f;

            //Debug.Log("col: " + col.ToString() + " row:" + row.ToString());
            _allPositions[i] = new Vector3(
                0,
                maxY - row * _gridWidth,
                minZ + col * _gridWidth
            );
        }
        return 0;
    }


    // ターンずれる可能性あるかもな
    public int AcceptNTurnAndMakeSureItsUpToDate( int nTurn ) 
    {
        return 0;
    }

    // 総括マン of 盤上のハリネズミたち
    public int AcceptBrandNewAnimPackage( int[] argAnimPackage )
    {
        int nAnims =  argAnimPackage[0];
        Debug.Log(nAnims);
        
        for ( int i = 0; i < nAnims; i++ )
        {
            int animGridId = (argAnimPackage[i] & 0xFF00) >> 16;
            int animType   = (argAnimPackage[i] & 0x00F0) >> 4;
            int animValue  = (argAnimPackage[i] & 0x000F);

            if ( animType == 0b0000 ) {   // 青スポーン

            } else if ( animType == 0b0011 ) {  // 黄色スポーン

            } else if ( animType == 0b1111 ) {  // びっくりする
            } else if ( animType == 0b1000 ) {  // 前進
            } else if ( animType == 0b1010 ) {  // 回頭
            } else if ( animType == 0b0011 ) {  // エモート変更
            } 
        }

        return 0;
    }

    void Update()
    {
        // 時刻を加算
        _millis += Time.deltaTime;

        // キーボボ入力
        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("すぺーーーす");
            InitializeMove(5);
        }
        if (Input.GetKeyDown(KeyCode.Return)) {
            Debug.Log("えんたーーーー");
        }
        if (Input.GetKeyDown(KeyCode.A)) {
            Debug.Log("A");
            //_pieceBehaviours[0].GetComponent<Animator>().SetTrigger("slide up");
        }
        if (Input.GetKeyDown(KeyCode.E)) {
            Debug.Log("E");
        }
        if (Input.GetKeyDown(KeyCode.I)) {
            Debug.Log("I");
        }
        if (Input.GetKeyDown(KeyCode.O)) {
            Debug.Log("O");
        }
        if (Input.GetKeyDown(KeyCode.G)) {
            Debug.Log("S");
        }
        if (Input.GetKeyDown(KeyCode.G)) {
            Debug.Log("G");

            for (int i = 0; i < 2; i++) {
                GameObject newPieceObj = Instantiate(AdamPieceObj, HousingComplexTransform);
                string color = i % 2 == 0 ? "blue": "orange";
                newPieceObj.GetComponent<Piece>().Initialize(
                    color,
                    _realBoardSize,
                    _allPositions,
                    0,                       // 0, 3でテスト
                    3
                );
                GameObject newClock = Instantiate(AdamClockObj, newPieceObj.transform);
                float[] dur = new float[10];
                for (int iDur = 0; iDur < 10; iDur++) {
                    dur[iDur] = 0.5f;
                }
                newClock.GetComponent<Clock>().Initialize(
                    "namaeeee",
                    new int[13] {0, 5, 5, 6, 6, 11, 11, 16, 16, 15, 15, 10, 10},
                    new int[13] {2, 2, 1, 1, 2, 2,  2,  2,  3,  3,  0,  0,  0 },
                    new float[13] {0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f},
                    true
                );
            }
        }
        if (Input.GetKeyDown(KeyCode.M)) {
            Debug.Log("M");
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            Debug.Log("D");
        }
        if (Input.GetKeyDown(KeyCode.F)) {
            Debug.Log("F");
        }
    }

    /////////////////////   Demo   //////////////////////
    private void Demo() 
    {

    }
}
