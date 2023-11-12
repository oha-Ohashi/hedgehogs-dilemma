// データの格納とゲームの進行を司る
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Hedgehog : UdonSharpBehaviour
{
    public bool DebugMode = false;

    public Panel Panel;
    public GameObject[] Boards;
    public Rule Rule;
    public Move MoveController;
    public int[] ActualNumbersOfBoardSize;

    //////////////////  インディケーターの登録  ////////////////////////////
    public Transform ParentOfIndicator;
    private GameObject[] _indicatorObjs = new GameObject[200];
    private Indicator[] _indicatorBehaviours = new Indicator[200];


    ///////////////////////////////////////////////////////////////////////////////////   デバグ    //////////////////////////////
    public Transform ParentOfCheapPieces;
    private GameObject[] _cheapPiecesObjs = new GameObject[200];
    private Indicator[] _cheapPiecesBehaviours = new Indicator[200];

    ///////////////////////   ローカルの変数   //////////////////////
    ///////////////////////   ローカルの変数   //////////////////////
    ///////////////////////   ローカルの変数   //////////////////////
    public float MoveFireRateLimit;
    public bool MoveAllowed = true;
    private int _realBoardSize = 5;
    private int[] _historyOfMoves = new int[103];
    private byte[] _historyOfBoard = new byte[103];
    private byte[] _localCurrentBoard = new byte[103];
    ///////////////////////   ローカルの変数   //////////////////////
    ///////////////////////   ローカルの変数   //////////////////////
    ///////////////////////   ローカルの変数   //////////////////////

    
    //////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////
    ///////////////////////   同期変数   //////////////////////
    ///////////////////////   同期変数   //////////////////////
    ///////////////////////   同期変数   //////////////////////

    //////////////////////////////////////////////////////////
    ///////////////////////  BoardSize  //////////////////////
    //////////////////////////////////////////////////////////
    [UdonSynced, FieldChangeCallback(nameof(BoardSize))] 
    private int intBoardSize = 0;
    public int BoardSize
    {
        set { intBoardSize = value; BoardSizeChanged(); }
        get => intBoardSize;
    }

    public void SetBoardSize(int arg)
    {
        // 同期変数を更新するためにオブジェクトオーナー取得
        if (!Networking.LocalPlayer.IsOwner(this.gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        // 同期変数の更新。この場合、更新するのはHogeの方
        BoardSize = arg;

        // 同期を要求
        RequestSerialization();
    }
    
    private void BoardSizeChanged()
    {
        // 同期変数によって行いたい処理を記述。例えば水パーティクルのON/OFFなど。
        // オブジェクトオーナーの場合は自身でHogeをsetしたタイミングでこの処理が走り、
        // オブジェクトオーナーでない場合は、同期変数が同期されたタイミングでこの処理が走る。
        if ( DebugMode ) if ( DebugMode ) Debug.Log(ActualNumbersOfBoardSize[BoardSize]);
        _realBoardSize = ActualNumbersOfBoardSize[BoardSize];
        SetActiveOneOfAll(ref Boards, BoardSize);               // ボードオブジェクト切り替え
        Panel.SwitchWhichBoardSizeIconToShow();

        MoveController.InitializeMove(_realBoardSize);
    }

    //////////////////////////////////////////////////////////
    ///////////////////////  GamePhase  //////////////////////
    //////////////////////////////////////////////////////////
    [UdonSynced, FieldChangeCallback(nameof(GamePhase))] 
    private int intGamePhase = -1;                      // 誰かがPlay押すまでは無効な値
    public int GamePhase
    {
        set { intGamePhase = value; GamePhaseChanged(); }
        get => intGamePhase;
    }

    public void InclimentGamePhase()
    {
        int gp = GamePhase;
        if (gp == 0 || gp == 1){
            SetGamePhase(gp + 1);
        } else {
            SetGamePhase(0);
        }
    }

    public void SetGamePhase(int arg)
    {
        // 同期変数を更新するためにオブジェクトオーナー取得
        if (!Networking.LocalPlayer.IsOwner(this.gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        // 同期変数の更新。この場合、更新するのはHogeの方
        GamePhase = arg;

        // 同期を要求
        RequestSerialization();
    }

    private void GamePhaseChanged()
    {
        // 同期変数によって行いたい処理を記述。例えば水パーティクルのON/OFFなど。
        // オブジェクトオーナーの場合は自身でHogeをsetしたタイミングでこの処理が走り、
        // オブジェクトオーナーでない場合は、同期変数が同期されたタイミングでこの処理が走る。
        if ( DebugMode ) Debug.Log("フェーズ変わったねぇ: " + GamePhase.ToString());
         
        // パネルを切り替え
        Panel.SwitchWhichPanelToShow();
        
        // ゲームを初期状態に
        if (GamePhase == 0) {
            if ( DebugMode ) Debug.Log("すべてを……最初からやり直す！！");
            // 全消し
            TerminateGame();
        } else if (GamePhase == 1) {                            //ゲーム開始のゴング
            // 全消し
            TerminateGame();

            // 最初の合法手提示
            LegalActions(_localCurrentBoard);
            
            MoveController.InitializeMove(_realBoardSize);
        } else if ( GamePhase == 2 ) {
            // なにもしないよ
        }
    }

    //////////////////////////////////////////////////////////
    ////////////////////  OneMoveHappyset  ///////////////////
    //////////////////////////////////////////////////////////
    private uint[] _tmpHappyset;     // 遅延送信用の置き場
    [UdonSynced, FieldChangeCallback(nameof(OneMoveHappyset))] 
    private uint[] uintArrayOneMoveHappyset = new uint[1] {0xFA};
    public uint[] OneMoveHappyset
    {
        set { Debug.Log("(setter says) happy is set."); uintArrayOneMoveHappyset = value; OneMoveHappysetChanged(); }
        get => uintArrayOneMoveHappyset;
    }

    // ハッピーセットエンコーダ
    public uint[] AssembleOneMoveHappyset(int argNTurnAndGridIdToBeSynced, byte[] argBoardToBeSynced)
    {
        // uint[27 + nTurn] にターン&グリッドとボードを詰め込む
        // 配列型の同期変数は一度長さを変えてあげないと受信者に検知されない。(多分)
        // nTurn を使えばユニークな長さにできる。(多分)
        //int nTurn = (argNTurnAndGridIdToBeSynced & 0xFFFF_0000) >> 16;
        //uint[] happysetTobeSubmitted = new uint[27 + nTurn];
        uint[] happysetTobeSubmitted = new uint[27];
        happysetTobeSubmitted[0] = (uint)argNTurnAndGridIdToBeSynced;

        // Board のヘッダー
        uint happysetHeader = 0;
        happysetHeader += (uint)argBoardToBeSynced[0] << (8 * 3); // ボードサイズ
        happysetHeader += (uint)argBoardToBeSynced[1] << (8 * 2); // ボードサイズ
        // ヘッダーを格納
        happysetTobeSubmitted[1] = happysetHeader;

        for ( int i = 0; i < 25; i++ ) {
            uint chunk = 0;                        // 32bit。 ここに4つ分の SquareState を入れる
            chunk += ((uint)argBoardToBeSynced[3 + (i*4 + 0)]) << (8 * 3);
            chunk += ((uint)argBoardToBeSynced[3 + (i*4 + 1)]) << (8 * 2);
            chunk += ((uint)argBoardToBeSynced[3 + (i*4 + 2)]) << (8 * 1);
            chunk += ((uint)argBoardToBeSynced[3 + (i*4 + 3)]) << (8 * 0);

            happysetTobeSubmitted[i + 2] = chunk;         // [2] ～ [26] に格納
        }

        // SetOneMoveHappyset(happysetTobeSubmitted);
        return happysetTobeSubmitted;
    }

    public void SetOneMoveHappyset(uint[] argHappysetTobeSubmitted)
    {
        if ( DebugMode ) Debug.Log("---------  SetOneMoveHappyset() やります  ---------");
        // 同期変数を更新するためにオブジェクトオーナー取得
        if (!Networking.LocalPlayer.IsOwner(this.gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        // 同期変数の更新。この場合、更新するのはHogeの方
        OneMoveHappyset = argHappysetTobeSubmitted;

        // 同期を要求
        RequestSerialization();
    }

    // デコードして (uint[27] なら) Process君に投げる
    private void OneMoveHappysetChanged()
    {
        if ( DebugMode ) Debug.Log("ハッピーセット届きました。デコード前です。");
        
        // 特殊な意味を持つハッピーセットならば
        if ( OneMoveHappyset.Length == 1 ) 
        {
            if ( DebugMode ) Debug.Log("ハッピーセットの長さ: " + OneMoveHappyset.Length.ToString());
            // 避難用の値ならば
            if ( OneMoveHappyset[0] == 0xFA )
            {
                if ( DebugMode ) Debug.Log("ハッピーセットは 0xFA でした。");
            }
            else {
                if ( DebugMode ) Debug.Log("ハッピーセットは " + OneMoveHappyset[0].ToString() + " でした。");
            } 
        }
        else if ( OneMoveHappyset.Length == 2 )
        {
            if ( DebugMode ) Debug.Log("ハッピーセットは サブリミナル new int[2]でした");
            if ( DebugMode ) Debug.Log("同期検知させたいんやね");
        }
        // たぶん正常な値 ならば
        else if ( OneMoveHappyset.Length == 27 )
        {
            // 結果その1
            uint brandNewNTurnAndGridId = OneMoveHappyset[0];

            // 結果その2
            byte[] absolutelyNewBoard = new byte[103];
            absolutelyNewBoard[0] = (byte)((OneMoveHappyset[1] & 0xFF00_0000) >> (8 * 3));
            absolutelyNewBoard[1] = (byte)((OneMoveHappyset[1] & 0x00FF_0000) >> (8 * 2));
            absolutelyNewBoard[2] = 0;                                      // 廃墟
    
            for ( int i = 0; i < 25; i++ ) {
                absolutelyNewBoard[3 + (i*4 + 0)] = (byte)((OneMoveHappyset[2 + i] & 0xFF00_0000) >> (8 * 3));
                absolutelyNewBoard[3 + (i*4 + 1)] = (byte)((OneMoveHappyset[2 + i] & 0x00FF_0000) >> (8 * 2));
                absolutelyNewBoard[3 + (i*4 + 2)] = (byte)((OneMoveHappyset[2 + i] & 0x0000_FF00) >> (8 * 1));
                absolutelyNewBoard[3 + (i*4 + 3)] = (byte)((OneMoveHappyset[2 + i] & 0x0000_00FF) >> (8 * 0));
            }   

            ProcessSeparatedOneMoveHappyset(brandNewNTurnAndGridId, absolutelyNewBoard);
        }
        else
        {
            if ( DebugMode ) Debug.Log("ハッピーセットは意味不明な長さ( " + OneMoveHappyset.Length.ToString() + " )でした。");
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////  分解された着手ハッピーセット、          ////////////////
    ////////////////////  つまりターン&グリッドとボードが来たとき  ////////////////
    ////////////////////  実際の処理はしない、場合分け担当大臣     ////////////////
    ////////////////////////////////////////////////////////////////////////////
    private void ProcessSeparatedOneMoveHappyset(uint argNewNTurnAndGridId, byte[] argNewBoard)
    {
        if ( DebugMode ) Debug.Log("------------ 令和最新版ハッピーセット ------------");
        // デコード
        int givenNTurn = (int)(argNewNTurnAndGridId >> 16);
        int givenGridId = (int)(argNewNTurnAndGridId & 0xFFFF);
        if ( DebugMode ) Debug.Log("与えられたnTurnは " + givenNTurn.ToString());
        if ( DebugMode ) Debug.Log("ローカルのターンは" + _localCurrentBoard[1].ToString());

        // またはローカルの手数が0xF9で かつ ハッピーセットから nTurn = 0 が来た場合
        // 
        if (_localCurrentBoard[1] == 0xF9)
        {
            if ( givenNTurn == 0 )
            {
                // アニメーションつき1ターン同期
                SycnBoardFromNewNTurnAndGridId(argNewNTurnAndGridId);
            }
            else 
            {
                // 瞬間同期
                SycnBoardFromNewBoard(argNewBoard);
            }
        }
        // もうJOINしたてじゃないのよ
        else
        {
            // グローバルの方が進んでる場合
            if ( givenNTurn > _localCurrentBoard[1] )
            {
                if ( DebugMode ) Debug.Log("ハッピーセットのターン > ローカルターン");

                // 手数の差がちゃんと 1 になってる場合
                if ( (givenNTurn == _localCurrentBoard[1] + 1) )
                {
                    if ( DebugMode ) Debug.Log("差は 1 です");
                    // アニメーションつき1ターン同期
                    // 最高です。ずっとこれやりたい
                    SycnBoardFromNewNTurnAndGridId(argNewNTurnAndGridId);
                }
                else
                {
                    if ( DebugMode ) Debug.Log("差は 2, またはそれ以上 です");
                    // 2ターン以上遅れてます。自分を恥じましょう
                    // アニメーションなし強制同期
                    SycnBoardFromNewBoard(argNewBoard);
                }

            }
            // グローバルとローカル、ターン数が一致
            // 自分が着手者で、すでにハリネズミを動かしちゃった場合が該当する
            else if ( givenNTurn == _localCurrentBoard[1])
            {
                if ( DebugMode ) Debug.Log("ハッピーセットのターン == ローカルターン");
                // 何もしないよ
                // 自分が着手したってことだよ
            }
            // なぜかローカルの方が進んでる(そんなことある？)
            else if ( givenNTurn < _localCurrentBoard[1] )
            {
                if ( DebugMode ) Debug.Log("ハッピーセットのターン < ローカルターン");

                // よくわからんけど怖いので同期します
                SycnBoardFromNewBoard(argNewBoard);
            }
            // ありえない
            else
            {
                // 入るわけないゾーン
                if ( DebugMode ) Debug.Log("いやああああああ  入ってこないでえええええ");

                // アニメーションなし強制同期
                SycnBoardFromNewBoard(argNewBoard);
            }
        }
    }

    //////////////////////////////////////////////////////////
    ////////////////////  PlayerIdBlue  //////////////////////
    //////////////////////////////////////////////////////////
    [UdonSynced, FieldChangeCallback(nameof(PlayerIdBlue))] 
    private int intPlayerIdBlue = -1;
    public int PlayerIdBlue
    {
        set { intPlayerIdBlue = value; PlayerOccupationChanged(); }
        get => intPlayerIdBlue;
    }

    public void SetPlayerIdBlue()
    {
        // 同期変数を更新するためにオブジェクトオーナー取得
        if (!Networking.LocalPlayer.IsOwner(this.gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        // 同期変数の更新。この場合、更新するのはHogeの方
        PlayerIdBlue = Networking.LocalPlayer.playerId;

        // 同期を要求
        RequestSerialization();
    }
    //////////////////////////////////////////////////////////
    //////////////////  PlayerIdOrange  //////////////////////
    //////////////////////////////////////////////////////////
    [UdonSynced, FieldChangeCallback(nameof(PlayerIdOrange))] 
    private int intPlayerIdOrange = -1;
    public int PlayerIdOrange
    {
        set { intPlayerIdOrange = value; PlayerOccupationChanged(); }
        get => intPlayerIdOrange;
    }

    public void SetPlayerIdOrange()
    {
        // 同期変数を更新するためにオブジェクトオーナー取得
        if (!Networking.LocalPlayer.IsOwner(this.gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        // 同期変数の更新。この場合、更新するのはHogeの方
        PlayerIdOrange = Networking.LocalPlayer.playerId;

        // 同期を要求
        RequestSerialization();
    }

    /////////////////////   プレイ中ですか  //////////////////////
    public bool[] WhoAmI()
    {
        int playerId = Networking.LocalPlayer.playerId;
        return new bool[2] {
            playerId == PlayerIdBlue,
            playerId == PlayerIdOrange
        };
    }
    /////////////////////   ボタン状態を変更  //////////////////////
    public void PlayerOccupationChanged()
    {
        int myId = Networking.LocalPlayer.playerId;
        Panel.SwitchOccupyingPlayersIconsToShow(
            PlayerIdBlue   != myId,                   // 自分が当事者でない時、ボタンを表示
            PlayerIdOrange != myId                   // 自分が当事者でない時、ボタンを表示
        );

        // プレイ中の着手者入れ替わり
        if (GamePhase == 1)
        {
            // 次の合法手提示
            LegalActions(_localCurrentBoard);
        }
    }


    ///////////////////////   同期変数   //////////////////////
    ///////////////////////   同期変数   //////////////////////
    ///////////////////////   同期変数   //////////////////////
    //////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////

    // オブジェクトの登録
    void Start()
    {
        SetActiveOneOfAll(ref Boards, BoardSize);

        for ( int i = 0; i < 200; i++ ) {
            // インディケーターを登録
            _indicatorObjs[i] = ParentOfIndicator.GetChild(i).gameObject;
            _indicatorObjs[i].SetActive(false);
            _indicatorBehaviours[i] = _indicatorObjs[i].GetComponent<Indicator>();

            _cheapPiecesObjs[i] = ParentOfCheapPieces.GetChild(i).gameObject;                       //////////////////////////////////////////////////////////////デバグ
            _cheapPiecesObjs[i].SetActive(false);
            _cheapPiecesBehaviours[i] = _cheapPiecesObjs[i].GetComponent<Indicator>();
        }
    }

    ////////////////   キーボボ入力  ////////////////////
    void Update()
    {
    }

    // ローカルのものしかいじらないでね
    private void TerminateGame() 
    {
        // データ全消し / 初期化
        _localCurrentBoard = Rule.GetInitialBoard(_realBoardSize);

        // インディケーター全消し
        ShowLegalMoves(new int[1] {0}, true);

        // コマ全消し
        for ( int i = 0; i < 100; i++ ) {
            _cheapPiecesObjs[i].SetActive(false);
            _indicatorBehaviours[i].MoveToSquare( i, _realBoardSize );
            _cheapPiecesBehaviours[i].MoveToSquare( i, _realBoardSize );
        }
        for ( int i = 0; i < 100; i++ ) {
            int biggerI = i + 100;
            _cheapPiecesObjs[biggerI].SetActive(false);
            _indicatorBehaviours[biggerI].MoveToSquare( i, _realBoardSize );
            _cheapPiecesBehaviours[biggerI].MoveToSquare( i, _realBoardSize );
        }
    }

    //////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////
    ////////////////////////// システムの根幹 /////////////////////////
    //////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////

    // 手番進むバターン その1
    // 送信 (インディケーターをタッチ)
    ///////////////////////////////////////////////////////////////////////////////
    //////////////////     新たなハッピーセットを作成/提出     //////////////////////
    ///////////////////////////////////////////////////////////////////////////////
    public void IndicatorInteracted(int argGridId)
    {
        // 着手を MoveFireRateLimit 秒間禁止
        MoveAllowed = false;
        SendCustomEventDelayedSeconds(nameof(AllowNewMove), MoveFireRateLimit);

        if ( DebugMode ) Debug.Log("--------- ここは IndicatorInteracted() ---------");
         
        // 前回の ハッピーセット から最新の手数が分かるぞい
        int nTurnAndGridIdPlusOne;      // 送りたいターングリッド
        int nTurn = 0;                  // あとで使いたいからいっこ外に宣言
        if ( OneMoveHappyset[0] == (0xFA) ){
            if ( DebugMode ) Debug.Log("初めて 着手送信しました");
            nTurnAndGridIdPlusOne = (0x0000_0000 << 16) + argGridId;
        } else {
            if ( DebugMode ) Debug.Log("手慣れた 着手送信です");
            // 最新のハッピーセットの手数 に 1 を足してハッピーセットに入れる
            nTurn = (int)((OneMoveHappyset[0] & (0xFFFF << 16)) >> 16); 
            nTurn++;
            nTurnAndGridIdPlusOne = (nTurn << 16) + argGridId;
        }
        string msg = "あなたの着手: ターン=" + nTurn.ToString();
        msg += ", グリッドID=" + argGridId + "(" + (nTurnAndGridIdPlusOne & 0x0000_FFFF) + ")";
        if ( DebugMode ) Debug.Log(msg);

        // 着手者はその場でハリネズミ動かしちゃう
        // こいつ副作用でローカルボード動かしちゃうから注意してね
        int[] animPackage = GenerateAnimPackage(nTurn, argGridId, ref _localCurrentBoard);
        ExecuteAnimPackage(animPackage);

        // チープピースを表示
        ShowCheapPieces(_localCurrentBoard);

        /////////////////////////  同期送信始まり  ////////////////////////
        if ( DebugMode ) Debug.Log("---------  インディケータからの同期、やります  ---------");
        uint[] happyset = AssembleOneMoveHappyset(nTurnAndGridIdPlusOne, _localCurrentBoard);
        
        // 配列型の同期変数は一度長さを変えてあげないと受信者に検知されない。(多分)
        // 長さ: 2 はリセット用の長さ。サブリミナル的に現れる
        SetOneMoveHappyset(new uint[2]);
        // 遅延入れて送信
        _tmpHappyset = happyset;
        SendCustomEventDelayedSeconds(nameof(JustSetTmpOneMoveHappyset), MoveFireRateLimit * 0.8f);

        // 次の合法手提示
        LegalActions(_localCurrentBoard);
    }
    ////////////////////////////////////////////////////////////////////////////
    ///////////////  盤が変わる場合その1: ターングリッドを処理  ///////////////////
    ////////////////////////////////////////////////////////////////////////////
    private void SycnBoardFromNewNTurnAndGridId(uint argNTurnAndGridId)
    {
        if ( DebugMode ) Debug.Log("--------- 盤を変えます。ターングリッド頂きましたので。 ---------");
        // デコード
        int nTurn = (int)(argNTurnAndGridId >> 16);
        int gridId = (int)(argNTurnAndGridId & 0xFFFF);

        ////////////////   アニメーション｢指示セット｣を生成   /////////////////
        ////////////////   _localCurrentBoard も書き換え   //////////////////
        int[] animPackage = GenerateAnimPackage(nTurn, gridId, ref _localCurrentBoard);
        ExecuteAnimPackage(animPackage);

        // チープピースを表示
        ShowCheapPieces(_localCurrentBoard);

        // 次の合法手提示
        LegalActions(_localCurrentBoard);
    }

    ////////////////////////////////////////////////////////////////////////////
    //////////////// 盤が変わる場合その2: ボードを強制同期  ///////////////////////
    ////////////////////////////////////////////////////////////////////////////
    private void SycnBoardFromNewBoard(byte[] argBoard)
    {
        if ( DebugMode ) Debug.Log("--------- 盤を変えます。ボード頂きましたので。 ---------");
        if ( DebugMode ) Debug.Log("一瞬でボードが最新になります");

        // 強制同期
        _localCurrentBoard = argBoard;

        // チープピースを表示
        ShowCheapPieces(argBoard);

        // 次の合法手提示
        LegalActions(_localCurrentBoard);
    }

    ////////////////////////////////////////////////////////////////////////
    ///////////////////  アニメーション｢指示セット｣を生成   ///////////////////
    ////////////////////////////////////////////////////////////////////////
    public int[] GenerateAnimPackage(int nTurn, int spawningGridId, ref byte[] refBoard)
    {
        if ( DebugMode ) Debug.Log("--------- ここは GenerateAnimPackage ---------");
        refBoard[1] = (byte)nTurn;      // _localCurrentBoardより1こ先の手が来てるからね

        string msg = "_localCurrentBoardのターン数を" + refBoard[1].ToString() + "に追いつかせました\n";
        msg += "スポーンネズミのグリッドIDは " + spawningGridId.ToString() + "らしいです";
        if ( DebugMode ) Debug.Log(msg);

        int[] resAnimPackage = new int[200];
        bool isBlueTurn = nTurn % 2 == 0;
        int singleAnim;  // resにアニメーション追加したい時のtmpみたいなやつ

        // タイムラインの有効長   逐一インクリメント
        resAnimPackage[0] = 0;                   

        ////////////////////////////////////////////////////////////////////////
        ///////////////////////////   スポーンネズミ   //////////////////////////
        ////////////////////////////////////////////////////////////////////////
        if ( DebugMode ) Debug.Log( "------- スポーンネズミ --------" );
        // スポーンネズミの情報
        // 初手は上向き、2手目以降は敵のうち誰かの方を向く
        int newPieceRotCode = nTurn == 0 ? 
                                0 : GetRotCodeWhenSpawned(spawningGridId, refBoard, isBlueTurn ? 0b001 : 0b010);

        // アニメーション生成してrefBoard操作( スポーン )
        int diffOrderValue = isBlueTurn ? 0b0000 : 0b0100;
        int letsSpawnSingleAnim = (spawningGridId << 8) +
                                    (0b0001 << 4) + diffOrderValue + newPieceRotCode;
        resAnimPackage[++resAnimPackage[0]] = letsSpawnSingleAnim;
        ApplySingleAnimToBoard(letsSpawnSingleAnim, ref refBoard);

        ////////////////////////////////////////////////////////////////////////
        /////////////////////////   たらい回しネズミ   //////////////////////////
        ////////////////////////////////////////////////////////////////////////
        if ( DebugMode ) Debug.Log( "------- たらい回しネズミ --------" );
        // このメソッドの主人公 "となりネズミ"。 この4つのグリッドIDでしつこく となりネズミを追跡
        // 空マスや壁なら 値は -1
        int notEmptyFilter = 0b011;
        int[] neighborsInActionGridIds = new int[4] { 
            Rule.GetGridIdIfLookingAtWhatYouWant(spawningGridId, refBoard, 0, notEmptyFilter),
            Rule.GetGridIdIfLookingAtWhatYouWant(spawningGridId, refBoard, 1, notEmptyFilter),
            Rule.GetGridIdIfLookingAtWhatYouWant(spawningGridId, refBoard, 2, notEmptyFilter),
            Rule.GetGridIdIfLookingAtWhatYouWant(spawningGridId, refBoard, 3, notEmptyFilter)
        };
        msg = "となりネズミのグリッドID:\n";
        msg += neighborsInActionGridIds[0] + ", ";
        msg += neighborsInActionGridIds[1] + ", ";
        msg += neighborsInActionGridIds[2] + ", ";
        msg += neighborsInActionGridIds[3] + "";
        if ( DebugMode ) Debug.Log(msg);

        // となりネズミの見てる(向かおうとしてる)方向。逐時実際の向きに反映
        // 最初はみんなスポーンネズミを恐れて外側に向かおうとしてる
        // いづれ ｢右左折フェーズ(連鎖検出)｣で90度ずつ曲げられることになる
        // 0:上 1:右 2:下 3:左
        int[] checkingRotCodes = new int[4] { 0, 1, 2, 3 };

        /////////////////////////////////////    // びっくりして顔を背ける
        ////////// 初回びっくり回転 //////////    // 実際に進むところまではやらん
        /////////////////////////////////////
        // 回転差分を算出
        int[] deltaRot = new int[4];  // 0b00:0度, 0b01:右90度, 0b10:左90度, 0b11:右180度
        for (int i = 0; i < 4; i++) {
            // 今が存在するとなりネズミのターン(1/4)ならば
            if ( neighborsInActionGridIds[i] >= 0 ) {
                // 回頭すべき角度を 今の方向と見てる方向の差分で決定
                deltaRot[i] = GetDeltaRot(
                    (refBoard[3 + neighborsInActionGridIds[i]] & 0b1100) >> 2, // 今実際に向いてる方向
                    checkingRotCodes[i]                                        // チェックしてる方向
                );
            } else {
                // となりネズミ様以外も来られるから一応初期化しないと
                deltaRot[i] = 0;
            }
        }
        // 回転差分を実行
        for (int i = 0; i < 4; i++) {
            // 今が存在するとなりネズミのターン(1/4)ならば
            if ( neighborsInActionGridIds[i] >= 0 ) {
                // アニメーション生成( びっくり )
                singleAnim = (neighborsInActionGridIds[i] << 8) + (0b1111 << 4);
                resAnimPackage[++resAnimPackage[0]] = singleAnim;
                ApplySingleAnimToBoard(singleAnim, ref refBoard);
                
                // アニメーション生成してrefBoard操作( 回転 )
                // 全員が回るつもりナシなら ゼロ回転アニメーション(待機)は必要ないよね
                if ( Rule.IntArraySum(deltaRot) > 0 ){
                    singleAnim = (neighborsInActionGridIds[i] << 8) +
                        (0b1010 << 4) + (deltaRot[i]);
                    resAnimPackage[++resAnimPackage[0]] = singleAnim;
                    ApplySingleAnimToBoard(singleAnim, ref refBoard);
                }
            } else {
                // 空マスにアニメーションを送れますか？ いいえ！送れませんとも！
            }
        }

        /////////////////////////////////////
        ////////////// 連鎖反応 //////////////   /// 初回の前進を含む無限ループ
        /////////////////////////////////////
        
        while (Rule.IntArraySum(neighborsInActionGridIds) > -4)  // となりネズミが全停止するまで
        {
            //////////////////////
            //// 直進フェーズ /////        // 進むか否かしか考えないよ
            //////////////////////
            int[] wannaGoGridIds = new int[4] {-1, -1, -1, -1};
            int[] gonnaGoGridIds = new int[4] {-1, -1, -1, -1};
            for (int i = 0; i < 4; i++) {
                // -1 のみなさまはとなりネズミ問い合わせサービスをご利用いただけません。
                // 今が存在するとなりネズミのターン(1/4)ならば          
                if ( neighborsInActionGridIds[i] >= 0 ) {
                    // 現在のチェックマスが空マスだったら wanna go に追加
                    wannaGoGridIds[i] = Rule.GetGridIdIfLookingAtWhatYouWant(
                        neighborsInActionGridIds[i], refBoard, checkingRotCodes[i], 0b100
                    );
                }
            }
            // 連鎖の末の鉢合わせを潰したら wanna go が gonna go になる
            // wanna go に重複がある場合、両者とも -2 になる
            gonnaGoGridIds = Rule.RemoveDuplicatesAndReplace(wannaGoGridIds, -2);
            
            //////// gonna go に基づいた処理 ////////
            for (int i = 0; i < 4; i++) {
                // 今が存在するとなりネズミのターン(1/4)ならば
                if ( neighborsInActionGridIds[i] >= 0 ) {
                    // 直進できる
                    if ( gonnaGoGridIds[i] > -1 )
                    {
                        // アニメーション生成してboard操作( 直進 )
                        singleAnim = (neighborsInActionGridIds[i] << 8) + (0b1000 << 4);
                        resAnimPackage[++resAnimPackage[0]] = singleAnim;
                        ApplySingleAnimToBoard(singleAnim, ref refBoard);

                        // となりネズミのグリッドID貼りなおす
                        neighborsInActionGridIds[i] = wannaGoGridIds[i];
                    }
                    // 行き止まり
                    else if ( gonnaGoGridIds[i] == -1 )
                    {
                        // アニメーションを追加することなく となりネズミを忘れる
                        neighborsInActionGridIds[i] = -1;
                    }
                    // たらい回しネズミ同士の鉢合わせ
                    else 
                    {
                        // アニメーション生成( びっくり )
                        singleAnim = (neighborsInActionGridIds[i] << 8) + (0b1111 << 4);
                        resAnimPackage[++resAnimPackage[0]] = singleAnim;
                        ApplySingleAnimToBoard(singleAnim, ref refBoard);

                        // となりネズミを忘れる
                        neighborsInActionGridIds[i] = -1;
                    }
                }
            }

            //////////////////////
            /// 右左折フェーズ ////     // すなわち連鎖検出
            //////////////////////
            for (int i = 0; i < 4; i++) {
                // 今が存在するとなりネズミのターン(1/4)ならば
                if ( neighborsInActionGridIds[i] >= 0 ) {
                    // 直進フェーズを終えてもなお進む意思があるのならば
                    if ( neighborsInActionGridIds[i] >= 0 ) {
                        // 今の彼らにとっての右(左)って rotCode でいうといくら？
                        int rightRotCodeForThem = AlterRotCode(checkingRotCodes[i], 0b01);  // ここやばい
                        int leftRotCodeForThem = AlterRotCode(checkingRotCodes[i], 0b10);
                        if ( DebugMode ) Debug.Log("相対する右, 左のrotCode: " + rightRotCodeForThem + ", " + leftRotCodeForThem);

                        // 敵おりゅ？ (ここでいう敵とはとなりネズミにとっての敵だよ!)
                        byte neighborSquareState = refBoard[3 + neighborsInActionGridIds[i]];
                        int enemyFilter = (neighborSquareState & 0b001_00000) > 0 ? 0b001 : 0b010; 
                        bool enemyOnRight = ( Rule.GetGridIdIfLookingAtWhatYouWant(
                            neighborsInActionGridIds[i], refBoard, rightRotCodeForThem, enemyFilter
                        ) >= 0 );
                        if ( DebugMode ) Debug.Log("右(絶対的rotCode: "+rightRotCodeForThem+") に敵いた。");
                        bool enemyOnLeft = ( Rule.GetGridIdIfLookingAtWhatYouWant(
                            neighborsInActionGridIds[i], refBoard, leftRotCodeForThem, enemyFilter
                        ) >= 0 );
                        if ( DebugMode ) Debug.Log("左(絶対的rotCode: "+ leftRotCodeForThem+") から敵出た。");

                        // 
                        if ( !enemyOnRight && !enemyOnLeft )
                        {
                            if ( DebugMode ) Debug.Log("はえー右にも左にも敵おらんくて安心したンゴねぇ");

                            // となりネズミを忘れる
                            neighborsInActionGridIds[i] = -1;
                        }
                        else if ( enemyOnRight && !enemyOnLeft )
                        {
                            if ( DebugMode ) Debug.Log("ファッ！？ 右に敵おるやんけ！！ はよ左に逃げな！！");

                            // アニメーション生成( びっくり )
                            singleAnim = (neighborsInActionGridIds[i] << 8) + (0b1111 << 4);
                            resAnimPackage[++resAnimPackage[0]] = singleAnim;
                            ApplySingleAnimToBoard(singleAnim, ref refBoard);

                            // アニメーション生成してboard操作( 回転 ) ( 左 )
                            int deltaLeftRot = 0b10;
                            singleAnim = (neighborsInActionGridIds[i] << 8) +
                                (0b1010 << 4) + (deltaLeftRot);
                            resAnimPackage[++resAnimPackage[0]] = singleAnim;
                            ApplySingleAnimToBoard(singleAnim, ref refBoard);
                            checkingRotCodes[i] = leftRotCodeForThem;           // 向き変えてもっかい
                        }
                        else if ( !enemyOnRight && enemyOnLeft )
                        {
                            if ( DebugMode ) Debug.Log("おいおい左に敵おるやんけ！ 右に逃げよかぁ");
                            
                            // アニメーション生成( びっくり )
                            singleAnim = (neighborsInActionGridIds[i] << 8) + (0b1111 << 4);
                            resAnimPackage[++resAnimPackage[0]] = singleAnim;
                            ApplySingleAnimToBoard(singleAnim, ref refBoard);
                            
                            // アニメーション生成してboard操作( 回転 ) ( 右 )
                            int deltaRightRot = 0b01;
                            singleAnim = (neighborsInActionGridIds[i] << 8) +
                                (0b1010 << 4) + (deltaRightRot);
                            resAnimPackage[++resAnimPackage[0]] = singleAnim;
                            ApplySingleAnimToBoard(singleAnim, ref refBoard);
                            checkingRotCodes[i] = rightRotCodeForThem;          // 向き変えてもっかい
                        }
                        else {
                            if ( DebugMode ) Debug.Log("オワタ…… 右にも左にもおる…… もはやここまでか");

                            // アニメーション生成( びっくり )
                            singleAnim = (neighborsInActionGridIds[i] << 8) + (0b1111 << 4);
                            resAnimPackage[++resAnimPackage[0]] = singleAnim;
                            ApplySingleAnimToBoard(singleAnim, ref refBoard);

                            // となりネズミを忘れる
                            neighborsInActionGridIds[i] = -1;
                        }
                    }
                }
            }
        }
        return resAnimPackage;
    }

    ///////////////////////   盤を動かす AnimPackage を実行   ///////////////////////
    private void ExecuteAnimPackage(int[] argAnimPackage)
    {
        MoveController.AcceptDecodeProcessAnimPackage(argAnimPackage);

        string msg = "MoveController で1ターン分動かすニキが終わりましたわ。";
        msg += "\n_localCurrentBoard[0]: " + _localCurrentBoard[0].ToString();
        msg += "\n_localCurrentBoard[1]: " + _localCurrentBoard[1].ToString();
        if ( DebugMode ) Debug.Log(msg);
    }

    // となりネズミ(敵) のいる方向のうちの1つを rotCode で返す
    private int GetRotCodeWhenSpawned( int spawningGridId, byte[] board, int enemyFilter )
    { 
        int[] neighborGridIds = new int[4] {
            Rule.GetGridIdIfLookingAtWhatYouWant(spawningGridId, board, 0, enemyFilter),
            Rule.GetGridIdIfLookingAtWhatYouWant(spawningGridId, board, 1, enemyFilter),
            Rule.GetGridIdIfLookingAtWhatYouWant(spawningGridId, board, 2, enemyFilter),
            Rule.GetGridIdIfLookingAtWhatYouWant(spawningGridId, board, 3, enemyFilter)
        };
        
        // ひとりでも敵いれば彼らのうち1つを選び方向を return
        // otherwise, return -1
        if (Rule.IntArraySum(neighborGridIds) > -4) {
            // 割りとすぐおわループ
            // ターン数とグリッドIDからランダムっぽい0～3を出す
            int[] arr = new int[10]{0,3,3,1,2,3,2,3,1,0};
            int seed = board[1] + spawningGridId;
            while (true) {
                int i = arr[ seed % arr.Length ];
                if (neighborGridIds[i] >= 0){
                    return i;
                }
                seed++;
            }
        } else {
            return -1;
        }
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

    // ある rotCode に deltaRot を適用して新しい rotCode 作る
    public int AlterRotCode( int asisRotCode, int deltaRot )
    {
       int[][] rotCodesAlteredByDeltaRot = new int[4][];
       rotCodesAlteredByDeltaRot[0b00] = new int[4] {0, 1, 2, 3}; // 回転量: 0
       rotCodesAlteredByDeltaRot[0b01] = new int[4] {1, 2, 3, 0}; // 回転量: 右90度
       rotCodesAlteredByDeltaRot[0b10] = new int[4] {3, 0, 1, 2}; // 回転量: 左90度
       rotCodesAlteredByDeltaRot[0b11] = new int[4] {2, 3, 0, 1}; // 回転量: 右180度
       return rotCodesAlteredByDeltaRot[deltaRot][asisRotCode];
    }

    // Genの中。逐次animをboardに適用  board は ref 型
    private void ApplySingleAnimToBoard( int argSingleAnim, ref byte[] refBoard )
    {
        // Warnig: board[3] から追加だよ
        int argAnimOrderType = ((argSingleAnim & 0b1111_0000) >> 4);
        int argAnimOrderValue = (argSingleAnim & 0b0000_1111);

        int asisGridId = (argSingleAnim >> 8);
        byte asisSquareState = refBoard[3 + asisGridId];
        int asisRotCode = (asisSquareState & 0b1100) >> 2;

        string msg = "---------- ここはApplySingleAnimationToBoard()です -----------";
        string msgDetail = "\nasisSquareState: " + Rule.TrimBinary(asisSquareState, 8);
        msgDetail += "\nasisGridId(現状のグリッドID): " + asisGridId.ToString();
        msgDetail += "\nasisRotCode: " + asisRotCode.ToString();
        msgDetail += "\n order type: " + Rule.TrimBinary(argAnimOrderType, 4);
        msgDetail += "\n order value: " + Rule.TrimBinary(argAnimOrderValue, 4);

        // スポーン(青)
        if ( argAnimOrderType == 0b0000 )
        {
            byte spawningSquareState = (byte)((0b0010 << 4) | (argAnimOrderValue << 2));
            //byte spawningSquareState = (byte)((0b0010 << 4) | (0b1110));
            msg += "\n青スポーンをrefBoardに書き込みました。 squareState = " + Rule.TrimBinary(spawningSquareState, 8);
            if ( DebugMode ) Debug.Log(msg + msgDetail);
            refBoard[3 + asisGridId] = spawningSquareState;
        }
        // スポーン(黄色)
        else if ( argAnimOrderType == 0b0011 )
        {
            byte spawningSquareState = (byte)((0b0001 << 4) | (argAnimOrderValue << 2));
            //byte spawningSquareState = (byte)((0b0001 << 4) | (0b1101));
            msg += "\n黄色スポーンをrefBoardに書き込みました。 squareState = " + Rule.TrimBinary(spawningSquareState, 8);
            if ( DebugMode ) Debug.Log(msg + msgDetail);
            refBoard[3 + asisGridId] = spawningSquareState;
        }
        // スポーン
        else if ( argAnimOrderType == 0b0001 )
        {
            bool spawningBlue = (argAnimOrderValue & 0b0100) == 0;
            byte pieceTypePart = spawningBlue ? (byte)(0b0010_0000) : (byte)(0b0001_0000);
            byte pieceRotCodePart = (byte)((argAnimOrderValue & 0b0011) << 2);
            byte spawningSquareState = (byte)(pieceTypePart | pieceRotCodePart);

            refBoard[3 + asisGridId] = spawningSquareState;

            msg += spawningBlue ? "\n青スポーンを書き込みました: " : "\nオレンジスポーンを書き込みました: ";
            msg += "SquareState = " + Rule.TrimBinary(spawningSquareState, 8);
            if ( DebugMode ) Debug.Log(msg + msgDetail);
        }
        // びっくりする
        else if ( argAnimOrderType == 0b1111 )
        {
            // board には影響をあたえない
            msg = "---------- ここはApplySingleAnimationToBoard()です -----------\n";
            msg += "グリッドID: " + asisGridId.ToString() + " をびっくりさせときました。";
            if ( DebugMode ) Debug.Log(msg);
        }
        // 直進する
        else if ( argAnimOrderType == 0b1000 )
        {
            // SquareState を board のなかでお引越し
            int[] diff = new int[4]{ -refBoard[0], 1, refBoard[0], -1 }; // となりとの位置関係
            int tobeGridId = asisGridId + diff[asisRotCode];             // diffを適用
            byte nobodySquareState = 0b0100_0000;                         // nobody
            refBoard[3 + asisGridId] = nobodySquareState;                // 旧居を退去
            refBoard[3 + tobeGridId] = asisSquareState;                  // 新居にasisコピー

            msg += "\n直進部門です。 tobeGridId(行き先のグリッドID): " + tobeGridId.ToString();
            if ( DebugMode ) Debug.Log(msg + msgDetail);
        }
        // 回転
        else if ( argAnimOrderType == 0b1010 )
        {
            // 
            int deltaRot = argAnimOrderValue;
            int tobeRotCode = AlterRotCode(asisRotCode, deltaRot);
            byte tobeSquareState     = (byte)(asisSquareState & 0b11110011);             // SquareStateの向き部分を切り落とし
            refBoard[3 + asisGridId] = (byte)(tobeSquareState | (tobeRotCode << 2));

            msg += "\n回転部門です。deltaRot は order value と一致します。";
            msg += "\ntobeSquareState: " + Rule.TrimBinary(tobeSquareState, 8);
            if ( DebugMode ) Debug.Log(msg + msgDetail);
        }
        // ハリちゃんのその場エモートを変更
        else if ( argAnimOrderType == 0b0001 )
        {
            int tobeEmote = argAnimOrderValue;
            refBoard[3 + asisGridId] = (byte)(asisSquareState & 0b11111100);
            refBoard[3 + asisGridId] = (byte)(asisSquareState | tobeEmote);

            msg += "\nエモート変更部門です。 tobeEmote: " + Rule.TrimBinary(tobeEmote, 8);
            if ( DebugMode ) Debug.Log(msg + msgDetail);
        }
    }

    // 合法手カウント、場合によっては終了
    private void LegalActions(byte[] argLocalCurrentBoard)
    {
        if ( DebugMode ) Debug.Log("法 の 番 人");

        // 次の合法手提示
        int[] legalMoveGridIds = Rule.GetLegalMoves(argLocalCurrentBoard);

        bool boardHasBlueTurn = (argLocalCurrentBoard[1] % 2 == 0);

        ShowLegalMoves(legalMoveGridIds, boardHasBlueTurn);

        if ( legalMoveGridIds[0] <= 0 )
        {
            if ( DebugMode ) Debug.Log("合法手が 0 個以下だったので終了します");
            bool blueWins = boardHasBlueTurn;
            Panel.SwitchWinnersmarks(blueWins);
            InclimentGamePhase();
        }
    }

    //////////////////////   遅れて実行チーム   ////////////////////
    // tmpをセットするだけのメソッド
    public void JustSetTmpOneMoveHappyset()
    {
        if ( DebugMode ) Debug.Log("JustSetTmp ハッピーセットです");
        SetOneMoveHappyset(_tmpHappyset);
    }
    // 
    public void AllowNewMove()
    {
        if ( DebugMode ) Debug.Log("着手リミット解除!");
        MoveAllowed = true;
    }

    //////////////////////////////////////////////////////////////////
    //////////////////////  おわり: システムの根幹 /////////////////////
    //////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////
    ///////////////////////  はじまり: ビジュアル //////////////////////
    //////////////////////////////////////////////////////////////////

    // 合法手を表示
    public void ShowLegalMoves(int[] legalMoveGridIds, bool argBoardHasBlueTurn)
    {
        if ( DebugMode ) Debug.Log("--------- ここはShowLegalMoves() ---------");
        // インディケーター全消し
        for ( int i = 0; i < 200; i++ )
        {
            _indicatorBehaviours[i].GetComponent<Collider>().enabled = false;
            _indicatorObjs[i].SetActive(false);
        }
        
        if ( legalMoveGridIds[0] > 0 ) {
            if ( DebugMode ) Debug.Log("インディケータ箱( " + legalMoveGridIds[0].ToString() + " 個)出したいねぇ");

            for (int i = 0; i < legalMoveGridIds[0]; i++){ // 有効範囲全探索
                int index = legalMoveGridIds[1+i];
                // 今のボードが青ターンなら次に表示すべきはオレンジインディケータ
                if ( argBoardHasBlueTurn ) {
                    index += 100;
                }

                // インディケーター出現
                _indicatorObjs[index].SetActive(true);
                // 今のボードが青ターンならオレンジのプレイヤーが着手できる
                if ( 
                     (argBoardHasBlueTurn && WhoAmI()[1]) ||    // 青着手後 かつ 私はオレンジ
                    (!argBoardHasBlueTurn && WhoAmI()[0])       // オレンジ着手後 かつ 私は青
                )
                {
                    _indicatorBehaviours[index].GetComponent<Collider>().enabled = true;
                }
            }
        } else {
            if ( DebugMode ) Debug.Log("全消ししたかっただけです");
        }
    }

    // GameObject[] のうち指定した一個だけをONにする
    public void SetActiveOneOfAll(ref GameObject[] objs, int arg)
    {
        for (int i = 0; i < objs.Length; i++){
            if (i == arg) {
                objs[i].SetActive(true);
            } else {
                objs[i].SetActive(false);
            }
        }
    }

    //////////////////////////////////////////////////////////////////
    ////////////////////////  おわり: ビジュアル //////////////////////
    //////////////////////////////////////////////////////////////////

    // if ( (squareStatd & 0b0001_0000) >= 0 ){  このカッコは必要
    // ((squareState & 0b0010_0000) > 0)        instead of >=

    public void ShowCheapPieces(byte[] argBoard) 
    {
        for (int i = 0; i < argBoard[0] * argBoard[0]; i++){
            byte squareState = argBoard[3 + i];
            if ( (squareState & 0b0010_0000) > 0 ){
                _cheapPiecesObjs[i].SetActive(true);
            } else {
                _cheapPiecesObjs[i].SetActive(false);
            }
            if ( (squareState & 0b0001_0000) > 0 ){
                _cheapPiecesObjs[100+i].SetActive(true);
            } else {
                _cheapPiecesObjs[100+i].SetActive(false);
            }
        }
    }
}
