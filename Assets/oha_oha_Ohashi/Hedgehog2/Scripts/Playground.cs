
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class Playground : UdonSharpBehaviour
{
    public Hedgehog Master;
    public Rule Rule;
    public Move MoveController;

    public bool DebugMode;

    private byte[] _localPlaygroundBoard;
    private int _realBoardSize;
    private bool _isFreePlace;
    private bool _freePlaceBlue;
    private int _nTurn;

    ///////////////////////     プレイグラウンドのインディケータ    /////////////////////////////
    public Transform ParentOfPlaygroundIndicators;
    private GameObject[] _playgroundIndicatorsObjs = new GameObject[200];
    private Indicator[] _playgroundIndicatorsBehaviours = new Indicator[200];

    public Button[] step1buttons;
    public Button[] step2buttons;

    void Start()
    {
        for ( int i = 0; i < 200; i++ ) {
            _playgroundIndicatorsObjs[i] = ParentOfPlaygroundIndicators.GetChild(i).gameObject;
            _playgroundIndicatorsObjs[i].SetActive(false);
            _playgroundIndicatorsBehaviours[i] = _playgroundIndicatorsObjs[i].GetComponent<Indicator>();
        }
    }


    //////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////
    //////////////////////////  プレイグラウンド //////////////////////////
    //////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////
    //////////////////////////  プレイグラウンド //////////////////////////
    //////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////

    // メモメモ
        // 盤やアニメーションの同期はハッピーセットがやってくれるよ

    // プレイグラウンドのモード

    // GamePhase == 3 に入った時
    // プレイグラウンド初期化(enter = true0
    // プレグラフェーズ → 0

        // プレグラフェーズ == 0
        // できること

    // GamePhase == 3 を出る時 (Exit Playground)
    // プレイグラウンド初期化(enter = false)

    ////// 1. コマフリープレイスモード
        ////// 常にあおかオレンジかのインディケータ出てる
        ////// 相互に切り替え可能
        ////// a. 青コマ起き放題モード 
        ////// b. オレンジコマ起き放題モード 
        ////// c. オート再現モード
    ////// 2. プレイモード
        ////// a. 青合法手表示
        ////// b. 青合法手表示
    ////// 3. 着手受付モード
    ////// 4. 終了待ちモード


    // GamePhase == 3 に入るとき、及び出ていくときに叩かれる
    // 全員一度通ってくれる
    // 他のでバガーに迷惑かけないように、ローカルのものだけ動かしてね
    public void InitializePlayground(int argRealBoardSize)
    {
        if ( DebugMode ) Debug.Log("プレイグラウンドします。通常の入力をしないでください。");

        // 初期化
        _realBoardSize = argRealBoardSize;
        RepositionIndicators(_realBoardSize);
        _localPlaygroundBoard = Rule.GetInitialBoard(_realBoardSize);

        // リアルコマ全消し
        MoveController.InitializeMove(_realBoardSize);

        EnterFreePlaceMode(false, false);

        step1buttons[0].interactable = true;
        step1buttons[1].interactable = true;
        step1buttons[2].interactable = true;
        step1buttons[3].interactable = true;
        step1buttons[4].interactable = true;
        step1buttons[5].interactable = true;
        step2buttons[0].interactable = true;
        step2buttons[1].interactable = true;
        step2buttons[2].interactable = true;
    }

    // すべてのボタンを消す
    public void WaitUntilDeath()
    {
        // 合法手ゼロにする
        ShowLegalMoves(new int[1] {0}, false);

        step1buttons[0].interactable = false;
        step1buttons[1].interactable = false;
        step1buttons[2].interactable = false;
        step1buttons[3].interactable = false;
        step1buttons[4].interactable = false;
        step1buttons[5].interactable = false;
        step2buttons[0].interactable = false;
        step2buttons[1].interactable = false;
        step2buttons[2].interactable = false;
    }

    // インディケータを再配置 (ボードサイズ的な意味で) (全部 inactive)
    public void RepositionIndicators(int argRealBoardSize) 
    {
        if ( DebugMode ) Debug.Log("インディケータを再配置する");
        for (int i = 0; i < 200; i++) {
            // プレイグラウンドインディケータ
            _playgroundIndicatorsObjs[i].SetActive(false);
            _playgroundIndicatorsBehaviours[i].MoveToSquareAsPlayGroundIndicator(
                i % 100,
                argRealBoardSize
            ); 
        }
    }

    // 合法手を表示 (アタマにリセット入り)
    private void ShowLegalMoves(int[] argLegalGridIds, bool isBlue)
    {
        for (int i = 0; i < 200; i++) {
            _playgroundIndicatorsObjs[i].SetActive(false);
        }

        if (argLegalGridIds[0] > 0)
        {
            for (int i = 0; i < argLegalGridIds[0]; i++) {
                int gridId = argLegalGridIds[1 + i];
                int index = isBlue ? gridId : gridId + 100;
                _playgroundIndicatorsObjs[index].SetActive(true);
            }
        }
    }

    // フリープレイスを開始
    public void EnterFreePlaceMode(bool argIsBlue, bool argIsOrange)
    {
        _isFreePlace = true;
        _freePlaceBlue = argIsBlue;

        //ExitStep2Button.SetActive(false);

        step1buttons[1].interactable = !argIsBlue;
        step1buttons[2].interactable = !argIsOrange;

        // インディケータ切り替え
        SetPlaygroundIndicatorsVisible(argIsBlue, argIsOrange);
    }

    // 置き放題インディケータ切り替え
    private void SetPlaygroundIndicatorsVisible(bool argShowBlue, bool argShowOrange)
    {
        // 置き放題インディケータを表示
        for (int i = 0; i < 200; i++) {
            // 有効なグリッドIDなら見せるかも
            if ( i % 100 < _realBoardSize * _realBoardSize ) {
                // 前半青、後半オレンジ
                if ( i < 100 ){
                    _playgroundIndicatorsObjs[i].SetActive(argShowBlue);
                } else {
                    _playgroundIndicatorsObjs[i].SetActive(argShowOrange);
                }
            } else {
                _playgroundIndicatorsObjs[i % 100].SetActive(false);
            }
        }

    }

    // ターン数は Interactの序盤でインクリメントするので合ってる
    public void IndicatorInteracted(int argGridId)
    {
        if ( _isFreePlace )
        {
            FreePlace(argGridId, true, _freePlaceBlue);  // 2こめはhasColor
            PushLocalBoardAsHappyset();
        }
        else
        {
            if ( DebugMode ) Debug.Log("--------- ここは プレイグラウンドの IndicatorInteracted() ---------");

            if (DebugMode) Debug.Log("グリッドIDのみのハッピーセット作ります");

            // スペシャルハッピーセットを作る
            uint[] happyset = new uint[3] {
                (uint)0xFFFF_FFFE,
                (uint)((_nTurn << 16) | argGridId),
                (uint)0
            };
            
            // 配列型の同期変数は一度長さを変えてあげないと受信者に検知されない。(多分)
            // 長さ: 2 はリセット用の長さ。目的が済んだらとりあえず入れとく。
            Master.SetOneMoveHappyset(happyset);

            SendCustomEventDelayedSeconds(
                nameof(ResetOneMoveHappyset), 
                Master.MoveFireRateLimit * 0.7f
            );
        }

    }

    // 呼び出し元: フリープレイスインディケータ、 プリセット
    public void FreePlace(int argGridId, bool hasColor, bool isBlue)
    {
        if ( hasColor )
        {
            int filter = isBlue ? 0b0010 : 0b0001;

            int nPieces = 0;
            for (int i = 0; i < 100; i++){
                if (( _localPlaygroundBoard[i + 3] & 0b0011_0000 ) > 0){
                    nPieces++;
                }
            }
            int rotCode = nPieces % 4;

            // 塗りつぶす
            if (( _localPlaygroundBoard[3 + argGridId] & filter) == 0 ) 
            {
                _localPlaygroundBoard[3 + argGridId] = (byte)((filter << 4) | (rotCode << 2));
            }
            // 取り消す
            else 
            {
                _localPlaygroundBoard[3 + argGridId] = (byte)0b0100_0000;
            }
        }
        else
        {
            _localPlaygroundBoard[3 + argGridId] = (byte)0b0100_0000;
        }
    }

    // プリセットを実行
    public void RunAutoPlacePreset(int argOption)
    {
        int[][] presetGridIds = new int[3][];
        bool[][] presetIsBlues = new bool[3][];

        presetGridIds[0] = new int[] {0, 6, 12, 18, 24};
        presetIsBlues[0] = new bool[] {true, false, true, false, true};

        presetGridIds[1] = new int[] {};
        presetIsBlues[1] = new bool[] {};

        presetGridIds[2] = new int[] {};
        presetIsBlues[2] = new bool[] {};

        for ( int i = 0; i < presetGridIds[argOption].Length; i++)
        {
            FreePlace(
                presetGridIds[argOption][i],
                true,
                presetIsBlues[argOption][i]
            );
        }
    }

    // オールクリア
    public void CleanUpTheBoard()
    {
        for ( int i = 0; i < _realBoardSize * _realBoardSize; i++)
        {
            _localPlaygroundBoard[3 + i] = (byte)0b0100_0000;
        }
    }
    // ボードを同期
    public void PushLocalBoardAsHappyset()
    {
        if (DebugMode) Debug.Log("ターングリッドなしのハッピーセット作ります");
        // スペシャルハッピーセットを作る
        uint[] happysetTobeSubmitted = Master.AssembleOneMoveHappyset(
            (uint)0xFFFF_FFFF, 
            _localPlaygroundBoard
        );

        // 配列型の同期変数は一度長さを変えてあげないと受信者に検知されない。(多分)
        // 長さ: 2 はリセット用の長さ。サブリミナル的に現れる
        Master.SetOneMoveHappyset(happysetTobeSubmitted);
        // 遅延入れて送信
        SendCustomEventDelayedSeconds(nameof(ResetOneMoveHappyset), Master.MoveFireRateLimit * 0.7f);
    }

    // リセット用ハッピーセット送信するだけのメソッド
    public void ResetOneMoveHappyset()
    {
        if ( DebugMode ) Debug.Log("リセット用 ハッピーセットです");
        Master.SetOneMoveHappyset(new uint[2]);
    }


    // 誰かが動かしたね
    // Masterの同期の関数と連動
    public void NewBoardDelivered(byte[] argNewBoard)
    {
        // 盤を強制同期
        _localPlaygroundBoard = argNewBoard;
        Master.ShowCheapPieces(_localPlaygroundBoard);

        MoveController.SpawnWholeBoard(_localPlaygroundBoard);
    }

    public void NewNTurnAndGridIdDelivered(uint argNTurnAndGridId)
    {
        if ( DebugMode ) Debug.Log("--------- プレイグラウンドプレイグラウンド            ---------");
        if ( DebugMode ) Debug.Log("--------- 盤を変えます。ターングリッド頂きましたので。 ---------");

        // デコード
        int nTurn = (int)(argNTurnAndGridId >> 16);
        int gridId = (int)(argNTurnAndGridId & 0xFFFF);

        ////////////////   アニメーション｢指示セット｣を生成   /////////////////
        ////////////////   _localCurrentBoard も書き換え   //////////////////
        int[] animPackage = Master.GenerateAnimPackage(nTurn, gridId, ref _localPlaygroundBoard);
        MoveController.AcceptDecodeProcessAnimPackage(animPackage);

        // チープピースを表示
        Master.ShowCheapPieces(_localPlaygroundBoard);

        
        WaitUntilDeath();
    }

    // プレイモードに入る
    public void EnterExamineMoveMode(string argWhosTurn)
    {
        _isFreePlace = false;

        // フリープレイスインディケータを非表示   
        SetPlaygroundIndicatorsVisible(false, false);

        if ( argWhosTurn == "blue" || argWhosTurn == "orange" )
        {
            if (argWhosTurn == "blue") {
                _nTurn = 2;
            }
            else {
                _nTurn = 3;
            }

            // Playground ゲームの始まり
            // Legal 関係は現状のターン数でいいよ
            int[] legalMoves = Rule.GetLegalMoves(_localPlaygroundBoard, _nTurn);

            ShowLegalMoves(legalMoves, _nTurn % 2 == 0);
        }
        // 合法手表示しない(写真撮影用？)
        else
        {

        }
    }

   /////////////////////// STEP 1 //////////////////
    // placement reset
    public void PlayGroundButtonPressed1() {
        EnterFreePlaceMode(false, false);

        CleanUpTheBoard();
        PushLocalBoardAsHappyset();
    }
    // place blue
    public void PlayGroundButtonPressed2() {
        EnterFreePlaceMode(true, false);
    }
    // place orage
    public void PlayGroundButtonPressed3() {
        EnterFreePlaceMode(false, true);
    }
    // preset1
    public void PlayGroundButtonPressed4() {
        EnterFreePlaceMode(false, false);

        CleanUpTheBoard();
        RunAutoPlacePreset(0);
        PushLocalBoardAsHappyset();
    }
    // preset2
    public void PlayGroundButtonPressed5() {
        EnterFreePlaceMode(false, false);

        CleanUpTheBoard();
        RunAutoPlacePreset(1);
        PushLocalBoardAsHappyset();
    }
    // preset3
    public void PlayGroundButtonPressed6() {
        EnterFreePlaceMode(false, false);

        CleanUpTheBoard();
        RunAutoPlacePreset(2);
        PushLocalBoardAsHappyset();
    }
    /////////////////////// STEP 2 //////////////////
    // nobody Plays
    public void PlayGroundButtonPressed7() {
        EnterExamineMoveMode("nobody");
    }
    // Play blue
    public void PlayGroundButtonPressed8() {
        EnterExamineMoveMode("blue");
    }
    // Play orange
    public void PlayGroundButtonPressed9() {
        EnterExamineMoveMode("orange");
    }
    // exit Play phase  (廃止予定))
    public void PlayGroundButtonPressed10() {
    }
    // Exit Playground
    public void PlayGroundButtonPressed11() {
        // Exit して GamePhase == 0
        Master.SetGamePhase(0);
    }
}
