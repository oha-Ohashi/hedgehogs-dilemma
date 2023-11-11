
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
    
    private GameObject[] PiecesSlot = new GameObject[100];

    public float OneTickLength = 0.2f;                           // 遅延時間の最小単位
    private float _millis = 0f;
    private int _boardSizeCode;
    private int _realBoardSize;
    private int _nSquaresOnBoard;
    private float _gridWidth = 0.4195f / 3.0f;
    private Vector3[] _allPositions;

    void Start()
    {
        AdamPieceBehaviour.SetMaterialMode("transparent");

    }

    private float Tick(int argNtick) {
        return this.OneTickLength * argNtick;
    }
    public void ChangeTick(float argTickLength) {
        this.OneTickLength = argTickLength;
    }

    // マスターから叩く想定
    // ボードサイズの確定
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

    // 時系列に組み直して投げる
    public int AcceptDecodeProcessAnimPackage( int[] argAnimPackage )
    {
        // 50行4列の配列にして時系列を整えてから上の業から再生 (Nとなりネズミ <= 4 なので)
        // 同じ行には同じ種類のアニメーションしかないから遅延時間も揃えられる
        int nAnims =  argAnimPackage[0];
        int[][] animStock = new int[50][];       

        for ( int i = 0; i < 50; i++ ) {
            animStock[i] = new int[4];
        }

        // AnimPackage[1 + i] を読む度に インクリメントして AnimPackage を順番に見ていく
        int iAnims = 0;              
        int row = 0; int col = 0;

        // スポーンフェーズ     必ず1回ある
        animStock[row][0] = argAnimPackage[1 + iAnims];
        row++;
        iAnims++;

        // どのタイプも検知されなかったらソート終了
        bool loopHasSomething = false;
        while ( loopHasSomething ) {
            string[] typeNames = new string[] {"surprise", "rotate", "forward"};

            for (int iType = 0; iType < 3; iType++) {
                col = 0;

                while ( NameOfSingleAnimType(argAnimPackage[1 + iAnims]) == typeNames[iType] ) {
                    animStock[row][col] = argAnimPackage[1 + iAnims];
                    iAnims++;
                    col++;
                }

                if ( col > 0 ) {
                    row++;
                    loopHasSomething = true;      // このループ意味あったわ
                }
            }
        }

        for ( int r = 0; r < 50; r++ ) {
            for ( int c = 0; c < 4; c++ ) {
                int value = animStock[r][c];
                if ( value > 0 ) {
                    PlaySingleAnim(value);
                }
            }
        }

        return 0;
    }

    private string NameOfSingleAnimType(int argSingleAnim)
    {
        int animType   = (argSingleAnim & 0x00F0) >> 4;

        // スポーン
        if ( animType == 0b0001 ) 
        {   
            return "spawn";
        }
        // びっくり
        else if ( animType == 0b1111 ) 
        {  
            return "surprise";
        }
        // 前進
        else if ( animType == 0b1000 ) 
        {
            return "forward";
        }
        // 回転
        else if ( animType == 0b1010 ) 
        {
            return "rotate";
        }
        // エモート変更
        else if ( animType == 0b0011 ) 
        { 
            return "alternate";
        } 
        // N tick 待機
        else if ( animType == 0b0100 )
        {
            return "wait";
        } 
        else 
        {
            return "none";
        }
    }

    // SingleAnimを再生
    private int PlaySingleAnim( int argSingleAnim )
    {
        int animGridId = (argSingleAnim & 0xFF00) >> 8;
        int animType   = (argSingleAnim & 0x00F0) >> 4;
        int animValue  = (argSingleAnim & 0x000F);

        // スポーン
        if ( animType == 0b0000 ) 
        {   
            string color = (animValue & 0b0100) == 0 ? "blue" : "orange";
            int rotCode = animValue & 0b0011;
            SpawnPiece(animGridId, rotCode, color, Tick(1));
        }
        // びっくりエモート
        else if ( animType == 0b1111 ) 
        {  
            GiveSurpriseEmote(animGridId, Tick(5));
        }
        // 前進
        else if ( animType == 0b1000 ) 
        {
            MoveForward(animGridId, Tick(5));
        }
        // 回転
        else if ( animType == 0b1010 ) 
        {
            Rotate(animGridId, animValue, Tick(10));
        }
        // エモート変更
        else if ( animType == 0b0011 ) 
        { 
            GiveAlternateEmote(animGridId, animValue);
        } 
        // N tick 待機
        else if ( animType == 0b0100 )      // animValueに tick数を入れる
        {
            MakeItWaiting(animGridId, animValue);
        }

        return 0;
    }

    // 新しいハリネズミをスポーン、スロットに登録
    public void SpawnPiece(int argGridId, int argRotCode, string argColor, float argDuration) {
        // 生産
        GameObject newPieceObj = Instantiate(AdamPieceObj, HousingComplexTransform);
        newPieceObj.GetComponent<Piece>().Initialize(
            argColor,
            _realBoardSize,
            _allPositions,
            argGridId,                       
            argRotCode
        );

        // Clock を追加
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

    // びっくり
    private void GiveSurpriseEmote(int argGridId, float argDuration) {
    }
    // 前進
    private void MoveForward(int argGridId, float argDuration) {

    }
    // 回転
    private void Rotate(int argGridId, int argAnimValue, float argDuration) {

    }
    // エモート変更
    private void GiveAlternateEmote(int argGridId, int argAnimValue) {

    }
    // N tick 待機
    private void MakeItWaiting(int argGridId, int argAnimValue) {

    }

    public void Demo() {
        GameObject newPieceObj = Instantiate(AdamPieceObj, HousingComplexTransform);
        string color = i % 2 == 0 ? "blue": "orange";
        newPieceObj.GetComponent<Piece>().Initialize(
            color,
            _realBoardSize,
            _allPositions,
            0,                       // 0, 3でテスト
            3
        );

        for (int i = 0; i < 5; i++) {
            GameObject newClock = Instantiate(AdamClockObj, newPieceObj.transform);
            newClock.GetComponent<Clock>().Initialize(
                "namaeeee",
                new int[13] {0, 5, 5, 6, 6, 11, 11, 16, 16, 15, 15, 10, 10},
                new int[13] {2, 2, 1, 1, 2, 2,  2,  2,  3,  3,  0,  0,  0 },
                new float[13] {0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f},
                true
            );
        }
    }

    public void AssignDecodedSingleAnim() {
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

    void Update()
    {
        // 時刻を加算
        _millis += Time.deltaTime;

        // キーボボ入力
        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("すぺーーーす");
            InitializeMove(5);
            Demo();
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

}
