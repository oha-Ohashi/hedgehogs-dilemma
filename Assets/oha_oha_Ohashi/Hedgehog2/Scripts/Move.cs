
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Move : UdonSharpBehaviour
{
    public Transform HousingComplex;
    public GameObject TinyWorld;
    public int IWorld = -1;
    private GameObject[] _hedgehogsHide = new GameObject[10000];
    public GameObject AdamPieceObj;
    public Piece AdamPieceBehaviour;
    public GameObject AdamClockObj;
    public Clock AdamClockBehaviour;
    
    private GameObject[] _pieceObjsSlot = new GameObject[100];  // コマ母艦がグリッドID番目にある
    private int[] _rotCodes = new int[100];                     // すべてのコマの回転

    public float OneTickLength;                           // 遅延時間の最小単位。インスペクターにて。
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
    // と見せかけて Inactive にするだけ
    public void DestroyAllThePieces()
    {
        // 初期値 -1
        IWorld++;

        // 元の空オブジェクトを増殖
        _hedgehogsHide[IWorld] = Instantiate(TinyWorld, HousingComplex);

        // 前の世界を見えなくする
        if (IWorld >= 1) {
            _hedgehogsHide[IWorld - 1].SetActive(false);
        }

        // 無限ループを止める
        _letRecursiveGo = false;

        /*Transform children = _hedgehogsHide.GetComponentInChildren < Transform > ();

        foreach(Transform ob in children) {
            Destroy(ob.gameObject);

            response++;
        }*/
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
        int nAnims =  argAnimPackage[0];
        Debug.Log("int[] argAnimPackage の長さ: " + nAnims.ToString());

        for ( int i = 0 + 1; i < argAnimPackage[0] + 1; i++ ) {
            Debug.Log("アニパケ["+i.ToString()+"]: " + ShowBinary(argAnimPackage[i], 16));
        }

        // ループニキを記憶
        int[] iAnimsOfInfs = new int[4]  {-1, -1, -1, -1};
        int[] singleAnimsOfInfs = new int[4] {-1, -1, -1, -1};       // [0~3]: アニパケの中のインデックス
        int[] gridIdsOfInfs = new int[4] {-1, -1, -1, -1};
        int[] deltaRotsOfInfs = new int[4] {-1, -1, -1, -1};
        int iOutOfFour = 0;
        for ( int i = 0 + 1; i < nAnims + 1; i++ ) {
            int[] valueDesembled = DesembleSingleAnim(argAnimPackage[i]);

            if ( valueDesembled[1] == 0b1001 ) {
                iAnimsOfInfs[iOutOfFour] = i;
                singleAnimsOfInfs[iOutOfFour] = argAnimPackage[i];
                gridIdsOfInfs[iOutOfFour] = valueDesembled[0];
                deltaRotsOfInfs[iOutOfFour] = valueDesembled[2];
                iOutOfFour++;
            }
            else
            {
                PlaySingleAnim(argAnimPackage[i]);
            }
        }
        Debug.Log("無限ループ " + (iOutOfFour).ToString() + "人発見！！");
        
        /*
        // 無限ループニキだけ後で足す
        for (int i = 0 + 1; i < iAnimsOfInfs[0] + 1; i++){
            Debug.Log("無限ループくん(SingleAnim)のアニパケの中のインデックス: " + iAnimsOfInfs[i]);
            PlaySingleAnim(argAnimPackage[iAnimsOfInfs[i]]);
        }
        */

        
        // 無限ループニキだけ後で足す
        /*for (int i = 0 + 1; i < iAnimsOfInfs[0] + 1; i++){

            bool flagItsInfAnim = false;
            for (int iOutOfFour = 0; iOutOfFour < 4 + 1; iOutOfFour++){
                if (iAnimsOfInfs[iOutOfFour] >= 0) {
                    flagItsInfAnim = true;
                }
            }

            if (!flagItsInfAnim) {
                PlaySingleAnim(argAnimPackage[i]);
            }
        }*/
    

        StartRecursiveLoop(
            gridIdsOfInfs,
            deltaRotsOfInfs,
            new float[3] {
                Tick(50), Tick(80), Tick(300)
            },
            new float[3] {
                Tick(10), Tick(10), Tick(10)
            },
            new float[3] {
                Tick(5), Tick(5), Tick(5)
            } 
        );

        return 0;
    }

    private int PlusTwoWhenItsIn(int argGridId, int[] argIntArray)
    {
        for (int i = 0; i < argIntArray[0]; i++){
            if (argGridId == argIntArray[1 + i]){
                return argGridId + 2;
            }
        }
        return argGridId + 1;
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
        msg += ", タイプ: " + animType.ToString();
        msg += ", Value: " + animValue.ToString();
        // Debug.Log(msg);

        // スポーン
        if ( animType == 0b0001 ) 
        {
            string color = (animValue & 0b0100) == 0 ? "blue" : "orange";
            int rotCode = animValue & 0b0011;
            SpawnPiece(animGridId, rotCode, color, Tick(20));
        }
        // びっくりエモート
        else if ( animType == 0b1111 ) 
        {  
            GiveSurpriseEmote(animGridId, Tick(50));
        }
        // 前進
        else if ( animType == 0b1000 ) 
        {
            MoveForward(animGridId, Tick(50));
        }
        // 回転
        else if ( animType == 0b1010 ) 
        {
            Rotate(animGridId, animValue, Tick(80));
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
        GameObject newPieceObj = Instantiate(AdamPieceObj, _hedgehogsHide[IWorld].transform);
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
    
    // 前進 目的地のグリッドIDを返す
    private int MoveForward(int argGridId, float argDuration) {
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
            IWorld,
            true
        );
        return toBeGridId;
    }

    // 回転
    private void Rotate(int argGridId, int argAnimValue, float argDuration) {
        //Debug.Log(" Rotate Rotate Rotate ");
        //Debug.Log("適用前 RotCode: " + _rotCodes[argGridId].ToString());
        int toBeRotCode = this.gameObject.GetComponent<Hedgehog>().AlterRotCode(
            _rotCodes[argGridId],
            argAnimValue
        );
        _rotCodes[argGridId] = toBeRotCode;
        //Debug.Log("適用後 RotCode: " + _rotCodes[argGridId].ToString());

        GameObject newClock = Instantiate(AdamClockObj, _pieceObjsSlot[argGridId].transform);
        newClock.GetComponent<Clock>().Initialize(
            "rotate",
            argGridId,
            _rotCodes[argGridId],
            argDuration,
            IWorld,
            true
        );
    }

    // エモート変更
    private void GiveAlternateEmote(int argGridId, int argAnimValue) {

    }
    // N tick 待機
    private void MakeItWaiting(int argGridId, int argAnimValue) {

    }

    private bool _letRecursiveGo;
    private float _recursiveDelay;
    private int[] _gridIdsR = new int[4];
    private int[] _deltaRotsR = new int[4];
    private float[] _durationsR;
    private float[] _minDurationsR;
    private float[] _diffsR;
    private void StartRecursiveLoop(int[] argGridIds, int[] argDeltaRots, float[] argDurations, float[] argMinDurations, float[] argDiffs) 
    {
        // 無限ループくん

        _letRecursiveGo = true;
        //_recursiveDelay = 5.0f;

        _gridIdsR = argGridIds;
        _deltaRotsR = argDeltaRots;

        _durationsR = argDurations;
        _minDurationsR = argMinDurations;
        _diffsR = argDiffs;

        RecursiveLoop();
    }
    public void RecursiveLoop()
    {
        // 世界線確認してね
        Debug.Log("いっかいのリカーシブ");
        if (_letRecursiveGo) {
            _recursiveDelay = (_durationsR[0] + _durationsR[1] ) * 2 + _durationsR[2];
            SendCustomEventDelayedSeconds(nameof(RecursiveLoop), _recursiveDelay);
            //_recursiveDelay += 5.0f;
        }


        // Debug.Log("1ループの遅延: " + _recursiveDelay.ToString());

        int[] currentGridIds = _gridIdsR;
        // 4回で1週
        for (int i = 0; i < 4; i++) {
            // Debug.Log("今のグリッドID: " + currentGridId.ToString());
            for (int iTonari = 0; iTonari < 4; iTonari++) {
                if (_gridIdsR[iTonari] > -1) {
                    int nextGridId = MoveForward(currentGridIds[iTonari], _durationsR[0]);
                    Rotate(nextGridId, _deltaRotsR[iTonari], _durationsR[1]);
                    currentGridIds[iTonari] = nextGridId;
                }
            }
            // Debug.Log("新しいグリッドID: " + nextGridId.ToString());
        }

        // ループをだんだん速くする
        for (int i = 0; i < 3; i++) {
            _durationsR[i] = (_durationsR[i] < _minDurationsR[i]) ? 
                                //(argDurations[i]) : (argDurations[i] - diffs[i]);
                                (_durationsR[i]) : (float)(_durationsR[i] * 0.8);
        }
    }

    public void Demo() {
        DestroyAllThePieces();
        InitializeMove(5);
        SpawnPiece(12, 0, "orange", 0.01f); 
        StartRecursiveLoop(
            new int[4] {12, -1, -1, -1},
            new int[4] {0b0001, -1, -1, -1},
            new float[3] {Tick(50), Tick(80), Tick(100)},
            new float[3] {Tick(5), Tick(8), Tick(3)},
            new float[3] {0.1f, 0.1f, 0.1f}
        );
        /*SpawnPiece(12, 0, "orange", 0.5f); 
        for(int i = 0; i < 100; i++){
            Rotate(12,  1, 0.1f);
            Rotate(12,  0, 0.1f);
        }*/
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
            //ClockMan cm1 = new ClockMan();
        }
        if (Input.GetKeyDown(KeyCode.I)) {
            Debug.Log("I");
        }
        if (Input.GetKeyDown(KeyCode.O)) {
            Debug.Log("O");
        }
        if (Input.GetKeyDown(KeyCode.P)) {
            Debug.Log("P");
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

    // 下から何bitか取り出して文字列にする
    public  string TrimBinary(int target, int nDigits)
    {
        // ターゲットを2進数の文字列に変換
        string binaryString = "";
        while (target > 0)
        {
            int remainder = target % 2;
            binaryString = remainder + binaryString;
            target /= 2;
        }

        // 指定された桁数で切り取る
        if (binaryString.Length <= nDigits)
        {
            return binaryString; // 桁数が文字列の長さ以下の場合、そのまま返す
        }
        else
        {
            int startIndex = binaryString.Length - nDigits;
            int remainingDigits = binaryString.Length - startIndex;
            int numberOfSpaces = (remainingDigits - 1) / 4; // 4文字ごとに1つのスペースを挿入
            string formattedBinary = binaryString.Substring(startIndex, nDigits);
            
            for (int i = 0; i < numberOfSpaces; i++)
            {
                int spaceIndex = nDigits - (i + 1) * 4;
                formattedBinary = formattedBinary.Insert(spaceIndex, " ");
            }

            string res = "";
            for (int i = 0; i < 32; i += 4) {
                res += "_" + formattedBinary.Substring(i, i + 4);
                if ((i + 4) == nDigits){
                    return res;
                }
            }
            return formattedBinary;
        }
    }

    public static string ShowBinary(int target, int nDigits)
    {
        uint trueTarget = (uint)target;
        uint counterpart = (uint)0b1000_0000_0000_0000_0000_0000_0000_0000;

        string[] one_zero = new string[32];
        for (int i = 0; i < 32; i++)
        {
            one_zero[i] = ( (trueTarget >> (31 - i)) & 0b01 ) == 1 ? "1" : "0";
        }
        
        string res = "0b";
        for (int i = 0; i < nDigits; i++)
        {
            int nOthers = 32 - nDigits;
            
                if (i % 4 == 0){
                    res += "_";
                }
            
                res += one_zero[(32-nDigits) + i];
            
        }

        return res;
    }
    
}