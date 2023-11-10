
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Move : UdonSharpBehaviour
{
    public Transform ParentOFPieces;
    private GameObject[] _pieceObjs = new GameObject[10];
    private Piece[] _pieceBehaviours = new Piece[10];

    private int _localNTurn;
    private int _boardSizeCode;
    private int _realBoardSize;
    private int _nSquaresOnBoard;
    private float _gridWidth = 0.4195f / 3.0f;
    private Vector3[] _allPositions;

    void Start()
    {
        for ( int i = 0; i < 10; i++ ) {
            // ハリネズミをを登録
            _pieceObjs[i] = ParentOFPieces.GetChild(i).gameObject;
            _pieceObjs[i].SetActive(false);
            _pieceBehaviours[i] = _pieceObjs[i].GetComponent<Piece>();
        }
        _pieceObjs[0].SetActive(true);
    }

    // マスターから叩く想定
    public int Reset(int argBoardSizeCode, int argRealBoardSize)
    {
        _localNTurn = 0;
        _boardSizeCode = argBoardSizeCode;
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

        _pieceObjs[3].SetActive(true);
        _pieceObjs[2].SetActive(true);
        _pieceObjs[5].SetActive(true);
        _pieceObjs[3].transform.localPosition = _allPositions[24];
        _pieceObjs[2].transform.localPosition = _allPositions[0];
        _pieceObjs[5].transform.localPosition = _allPositions[12];
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

    // boardをもらってアニメーションなしの強制同期
    public int ForcedSyncOfBoard( byte argBoard )
    {

        return 0;
    }

    ////////////////   キーボボ入力  ////////////////////
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("SSSSPPPPPAAAACE");
            Reset(2, 7);
        }
        if (Input.GetKeyDown(KeyCode.Return)) {
            Debug.Log("EEEEENTURRRRREEEE");
            Reset(2, 7);
            Demo(0);
        }
        if (Input.GetKeyDown(KeyCode.A)) {
            Debug.Log("A");
            _pieceBehaviours[0].GetComponent<Animator>().SetTrigger("slide up");
        }
        if (Input.GetKeyDown(KeyCode.E)) {
            Debug.Log("E");
            _pieceBehaviours[0].GetComponent<Animator>().SetTrigger("slide right");
        }
        if (Input.GetKeyDown(KeyCode.I)) {
            Debug.Log("I");
            // _pieceBehaviours[0].GetComponent<Animator>().SetTrigger("deltaRot", 1);
            _pieceBehaviours[0].GetComponent<Animator>().SetTrigger("slide down");
        }
        if (Input.GetKeyDown(KeyCode.O)) {
            Debug.Log("O");
            _pieceBehaviours[0].GetComponent<Animator>().SetTrigger("slide left");
        }
        if (Input.GetKeyDown(KeyCode.G)) {
            Debug.Log("G");
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

    /////////////////////   DEMO   //////////////////////
    private void Demo(int argMode) 
    {
        if ( argMode == 0 ) {
            Debug.Log("DEEEEEEMO");
        }
    }
}
