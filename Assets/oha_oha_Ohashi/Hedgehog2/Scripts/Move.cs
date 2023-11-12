
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
    
    private GameObject[] _pieceObjsSlot = new GameObject[100];  // コマ母艦がグリッドID番目にある
    private int[] _rotCodes = new int[100];

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
        Debug.Log("Move Initialized");
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

    // すべてのハリネズミを Destroy (マスターから呼び出し想定)
    public int DestroyAllThePieces()
    {
        int response = 0;
        foreach(GameObject pieceObj in _pieceObjsSlot)
        {
            Destroy(pieceObj);
            response++;
        }
        return response;
    }

    // ターンずれる可能性あるかもな
    public int AcceptNTurnAndMakeSureItsUpToDate( int nTurn ) 
    {
        return 0;
    }

    // 時系列に組み直して投げる
    public int AcceptDecodeProcessAnimPackage( int[] argAnimPackage )
    {
        Debug.Log("Move Controller に渡ってきたね");
        // 50行4列の配列にして時系列を整えてから上の業から再生 (Nとなりネズミ <= 4 なので)
        // 同じ行には同じ種類のアニメーションしかないから遅延時間も揃えられる
        int nAnims =  argAnimPackage[0];
        Debug.Log("int[] argAnimPackage の長さ: " + nAnims.ToString());
        int[][] animStock = new int[50][];       

        for ( int i = 0; i < 50; i++ ) {
            animStock[i] = new int[4];
        }

        // AnimPackage[1 + i] を読む度に インクリメントして AnimPackage を順番に見ていく
        int iAnims = 0;              
        int row = 0; int col = 0;

        Debug.Log("スポーンフェーズ");
        // スポーンフェーズ     必ず1回ある
        animStock[row][0] = argAnimPackage[1 + iAnims];
        row++;
        iAnims++;

        // どのタイプも検知されなかったらソート終了
        bool loopHasSomething = true;
        while ( loopHasSomething ) {
            loopHasSomething = false;   // 推定無罪の法則
            string[] typeNames = new string[] {"surprise", "rotate", "forward"};

            Debug.Log( ((row - 1) / 3).ToString() + " 回目の[びっくり, 回転, 直進]");
                
            for (int iType = 0; iType < 3; iType++) {
                string phaseMsg = typeNames[iType] + " フェーズ";
                phaseMsg += " (" + (iType + 1).ToString() + "/" + 3 + ")";
                Debug.Log(phaseMsg);

                col = 0;

                while ( NameOfSingleAnimType(argAnimPackage[1 + iAnims]) == typeNames[iType] ) {
                    animStock[row][col] = argAnimPackage[1 + iAnims];
                    iAnims++;
                    col++;
                }

                // このフェーズになにかあったなら
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
                    int[] valueDesembled = DesembleSingleAnim(value);
                    string singleAnimMsg = "animStock[" + r.ToString() + "][" + c.ToString() + "]:";
                    singleAnimMsg += "\nGridId: " + valueDesembled[0];
                    singleAnimMsg += "\nOrder Type: " + valueDesembled[1];
                    singleAnimMsg += "\nOrder Value: " + valueDesembled[2];
                    Debug.Log(singleAnimMsg);

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
    // tick 数をここで決定
    private int PlaySingleAnim( int argSingleAnim )
    {
        int animGridId = (argSingleAnim & 0xFF00) >> 8;
        int animType   = (argSingleAnim & 0x00F0) >> 4;
        int animValue  = (argSingleAnim & 0x000F);

        string msg = "グリッドID: " + animGridId.ToString();
        msg += "\nオーダータイプ: " + animType.ToString();
        msg += "\nオーダーValue: " + animValue.ToString();
        Debug.Log(msg);

        // スポーン
        if ( animType == 0b0001 ) 
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

    public void SpawnWholeBoard(byte[] argBoard)
    {
        DestroyAllThePieces();
        //
        for ( int i = 0; i < 100; i++ ) {
            byte squareState = argBoard[3 + i];
            bool isBlue = (squareState & 0b0010_0000) > 0;
            bool isOrange = (squareState & 0b0001_0000) > 0;
            int rotCode = (squareState & 0b0000_1100) >> 2;
            if (isBlue || isOrange) {
                string color = isBlue ? "blue" : "orange";
                SpawnPiece(i, rotCode, color, 0);
            } 
        }
    }

    // 新しいハリネズミをスポーン、スロットに登録
    public void SpawnPiece(int argGridId, int argRotCode, string argColor, float argDuration) {
        // 生産
        GameObject newPieceObj = Instantiate(AdamPieceObj, HousingComplexTransform);
        int result = newPieceObj.GetComponent<Piece>().Initialize(
            argColor,
            _realBoardSize,
            _allPositions,
            argGridId,                       
            argRotCode
        );
        Debug.Log("スポーン(Instantiate)したよ。グリッドID: " + argGridId.ToString());
        // スロットに登録
        _pieceObjsSlot[argGridId] = newPieceObj;
        _rotCodes[argGridId] = argRotCode;
    }

    // びっくり
    private void GiveSurpriseEmote(int argGridId, float argDuration) {
    }
    
    // 前進
    private void MoveForward(int argGridId, float argDuration) {
        int[] diff = new int[4]{ -_realBoardSize, 1, _realBoardSize, -1 };
        int toBeGridId = argGridId + diff[_rotCodes[argGridId]];
        _pieceObjsSlot[toBeGridId] = _pieceObjsSlot[argGridId];
        //_pieceObjsSlot[argGridId] = new GameObject();
        _rotCodes[toBeGridId] = _rotCodes[argGridId];
        //_rotCodes[argGridId] = 0;
        
        GameObject newClock = Instantiate(AdamClockObj, _pieceObjsSlot[toBeGridId].transform);
        newClock.GetComponent<Clock>().Initialize(
            "forward",
            toBeGridId,
            _rotCodes[toBeGridId],
            argDuration,
            true
        );
    }

    // 回転
    private void Rotate(int argGridId, int argAnimValue, float argDuration) {
        int toBeRotCode = this.gameObject.GetComponent<Hedgehog>().AlterRotCode(
            _rotCodes[argGridId],
            argAnimValue
        );
        _rotCodes[argGridId] = toBeRotCode;

        GameObject newClock = Instantiate(AdamClockObj, _pieceObjsSlot[argGridId].transform);
        newClock.GetComponent<Clock>().Initialize(
            "rotate",
            argGridId,
            _rotCodes[argGridId],
            argDuration,
            true
        );
    }
    // エモート変更
    private void GiveAlternateEmote(int argGridId, int argAnimValue) {

    }
    // N tick 待機
    private void MakeItWaiting(int argGridId, int argAnimValue) {

    }

    public void Demo() {
        GameObject newPieceObj = Instantiate(AdamPieceObj, HousingComplexTransform);
        newPieceObj.GetComponent<Piece>().Initialize(
            "orange",
            _realBoardSize,
            _allPositions,
            0,                       // 右向きの0でテスト
            1
        );

        GameObject newClock = Instantiate(AdamClockObj, newPieceObj.transform);
        newClock.GetComponent<Clock>().Initialize("test", 1, 1, 0.5f, true);

        newClock = Instantiate(AdamClockObj, newPieceObj.transform);
        newClock.GetComponent<Clock>().Initialize("test", 1, 2, 0.5f, true);

        newClock = Instantiate(AdamClockObj, newPieceObj.transform);
        newClock.GetComponent<Clock>().Initialize("test", 6, 2, 0.5f, true);

        newClock = Instantiate(AdamClockObj, newPieceObj.transform);
        newClock.GetComponent<Clock>().Initialize("test", 11, 2, 0.5f, true);

        newClock = Instantiate(AdamClockObj, newPieceObj.transform);
        newClock.GetComponent<Clock>().Initialize("test", 11, 1, 0.5f, true);

        newClock = Instantiate(AdamClockObj, newPieceObj.transform);
        newClock.GetComponent<Clock>().Initialize("test", 12, 1, 0.5f, true);

        newClock = Instantiate(AdamClockObj, newPieceObj.transform);
        newClock.GetComponent<Clock>().Initialize("test", 13, 1, 0.5f, true);
        newClock = Instantiate(AdamClockObj, newPieceObj.transform);
        newClock.GetComponent<Clock>().Initialize("test", 13, 3, 0.5f, true);
        newClock = Instantiate(AdamClockObj, newPieceObj.transform);
        newClock.GetComponent<Clock>().Initialize("test", 12, 3, 0.5f, true);
    }

    private void Demo2()
    {
        InitializeMove(5);
        SpawnPiece(0, 0, "orange", 0.5f); 
        SpawnPiece(10, 1, "blue", 0.5f); 
        SpawnPiece(20, 2, "orange", 0.5f); 
        Rotate(0,  1, 0.3f);
        Rotate(10, 0, 0.3f);
        Rotate(20, 2, 0.3f);
        MoveForward(0, 0.5f);
        MoveForward(10, 0.5f);
        MoveForward(20, 0.5f);
        Rotate(1 , 1, 0.3f);
        Rotate(11, 3, 0.3f);
        Rotate(21, 2, 0.3f);

        MoveForward( 1, 0.5f);
        MoveForward(11, 0.5f);
        MoveForward(21, 0.5f);
    }

    void Update()
    {
        // 時刻を加算
        _millis += Time.deltaTime;

        // キーボボ入力
        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("すぺーーーす");
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
        if (Input.GetKeyDown(KeyCode.P)) {
            Debug.Log("P");
            InitializeMove(5);
            Demo();
        }
        if (Input.GetKeyDown(KeyCode.F)) {
            Debug.Log("F");
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            Debug.Log("D");

        }
        if (Input.GetKeyDown(KeyCode.G)) {
            Debug.Log("G");
        }
        if (Input.GetKeyDown(KeyCode.M)) {
            Debug.Log("M");
            InitializeMove(5);
            //PlaySingleAnim(AssembleSingleAnim( , 0b, 0b));
            PlaySingleAnim((1 << 8) + (0b0001 << 4) + 0b0001); // gird 1, スポーン, 青右
            PlaySingleAnim((3 << 8) + (0b0001 << 4) + 0b0111); // gird 3, スポーン, オレンジ左
            PlaySingleAnim((3 << 8) + (0b1010 << 4) + 0b0010); // gird 3, 回転, 左回頭(→下)
            PlaySingleAnim((3 << 8) + (0b1000 << 4) + 0b0000); // gird 3, 直進(→8)
            PlaySingleAnim((8 << 8) + (0b1010 << 4) + 0b0001); // gird 8, 回転, 右回頭(→左)
            PlaySingleAnim((8 << 8) + (0b1000 << 4) + 0b0000); // gird 8, 直進(→7)
            PlaySingleAnim((7 << 8) + (0b1010 << 4) + 0b0011); // gird 8, 回転, 反転(→右)
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            Debug.Log("D");
            /*
            InitializeMove(5);

            SpawnPiece(10, 1, "orange", Tick(1));
            MoveForward(10, Tick(5));
            Rotate(11, 3, Tick(3));

            SpawnPiece(15, 1, "blue", Tick(1));
            MoveForward(15, Tick(5));
            Rotate(16, 1, Tick(3));

            SpawnPiece(20, 1, "orange", Tick(1));
            MoveForward(20, Tick(5));
            Rotate(21, 2, Tick(3));
            */
        }
    }
    public int AssembleSingleAnim(int argGridId, int argAnimOrderType, int argAnimOrderValue)
    {
        int res = 0;
        res += argGridId << 8;
        res += argAnimOrderType << 4;
        res += argAnimOrderValue;
        return res;
    }
    public int[] DesembleSingleAnim(int argSingleAnim)
    {
        return new int[3] {
            (argSingleAnim & 0xFF00) >> 8,
            (argSingleAnim & 0x00F0) >> 4,
            (argSingleAnim & 0x000F)    
        };
    }
}
