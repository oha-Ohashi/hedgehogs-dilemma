
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
    // プレイグラウンドのモード
    ////// 1. コマ起き放題モード
        ////// 相互に切り替え可能
        ////// a. 青コマ起き放題モード 
        ////// b. オレンジコマ起き放題モード 
        ////// c. オート再現モード
    ////// 2. 合法手表示モード
        ////// a. 青合法手表示
        ////// b. 青合法手表示
    ////// 3. 着手受付モード
    ////// 4. 終了待ちモード


    // キーボード入力で入る
    public void EnterBoardDebuging(int argRealBoardSize)
    {
        if ( DebugMode ) Debug.Log("プレイグラウンドします。通常の入力をしないでください。");

        // 初期化
        _realBoardSize = argRealBoardSize;
        RepositionIndicators(_realBoardSize);
        _localPlaygroundBoard = Rule.GetInitialBoard(_realBoardSize);

        // フリープレイス突入
        EnterFreePlaceMode("nobody");
    }

    public void RepositionIndicators(int argRealBoardSize) 
    {
        if ( DebugMode ) Debug.Log("インディケータを再配置する");
        for (int i = 0; i < 200; i++) {
            // プレイグラウンドインディケータ
            _playgroundIndicatorsObjs[i].SetActive(false);
            _playgroundIndicatorsBehaviours[i].MoveToSquareAsPlayGroundIndicator(
                i % 100,
                argRealBoardSize,
                i < 100
            );      // これで <Indicatorの属性変わる>
        }
    }

    // フリープレイスを開始
    public void EnterFreePlaceMode(string argColor)
    {
        bool showBlue = false;
        bool showOrange = false;

        if (argColor == "nobody")
        {
            step1buttons[1].interactable = true;
            step1buttons[2].interactable = true;
        }
        else if (argColor == "blue")
        {
            showBlue = true;
            step1buttons[1].interactable = false;
            step1buttons[2].interactable = true;
        }
        else if (argColor == "orange")
        {
            showOrange = true;
            step1buttons[1].interactable = true;
            step1buttons[2].interactable = false;
        }
        else
        {
            if (DebugMode) Debug.Log("嘘だ！！");
        }

        // 置き放題インディケータを表示
        for (int i = 0; i < 200; i++) {
            // 有効なグリッドIDなら見せるかも
            if ( i % 100 < _realBoardSize * _realBoardSize ) {
                // 前半青、後半オレンジ
                if ( i < 100 ){
                    _playgroundIndicatorsObjs[i].SetActive(showBlue);
                } else {
                    _playgroundIndicatorsObjs[i].SetActive(showOrange);
                }
            } else {
                _playgroundIndicatorsObjs[i % 100].SetActive(false);
            }
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
        SendCustomEventDelayedSeconds(nameof(Master.ResetOneMoveHappyset), Master.MoveFireRateLimit * 0.7f);
    }

    // 誰かが動かしたね
    // Masterの同期の関数と連動
    public void NewBoardDelivered(byte[] argNewBoard)
    {
        // 盤を強制同期
        _localPlaygroundBoard = argNewBoard;
        Master.ShowCheapPieces(_localPlaygroundBoard);

        MoveController.ForceSync(_localPlaygroundBoard);
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
        PushLocalBoardAsHappyset();
    }

    // オールクリア
    public void CleanUpTheBoard()
    {
        for ( int i = 0; i < _realBoardSize * _realBoardSize; i++)
        {
            _localPlaygroundBoard[3 + i] = (byte)0b0100_0000;
        }
    }

    // Button 用
    public void ExitBoardDebuging()
    {
        foreach(Button btn in step2buttons) {
            btn.interactable = true;
        }
        foreach(Button btn in step1buttons) {
            btn.interactable = true;
        }

        Master.SetGamePhase(0);
    }


    /////////////////////// STEP 1 //////////////////
    // placement reset
    public void PlayGroundButtonPressed1() {
        EnterFreePlaceMode("nobody");

    }
    // place blue
    public void PlayGroundButtonPressed2() {
        EnterFreePlaceMode("blue");
    }
    // place orage
    public void PlayGroundButtonPressed3() {
        EnterFreePlaceMode("orange");
    }
    // preset1
    public void PlayGroundButtonPressed4() {
        EnterFreePlaceMode("nobody");

        RunAutoPlacePreset(0);
    }
    // preset2
    public void PlayGroundButtonPressed5() {
        EnterFreePlaceMode("nobody");

        RunAutoPlacePreset(1);
    }
    // preset3
    public void PlayGroundButtonPressed6() {
        EnterFreePlaceMode("nobody");

        RunAutoPlacePreset(2);
    }
    /////////////////////// STEP 2 //////////////////
    // nobody Plays
    public void PlayGroundButtonPressed7() {

        EnableStep1or2(false, true);
    }
    // Play blue
    public void PlayGroundButtonPressed8() {

        EnableStep1or2(false, true);
    }
    // Play orange
    public void PlayGroundButtonPressed9() {

        EnableStep1or2(false, true);
    }
    // exit Play phase
    public void PlayGroundButtonPressed10() {

        EnableStep1or2(false, true);
    }
    // Exit Playground
    public void PlayGroundButtonPressed11() {
        EnableStep1or2(false, true);
    }

    public void EnableOneOfStep1(int argWhich) {
        
    }
    public void EnableStep1or2(bool arg1, bool arg2) {
        foreach(Button btn in step1buttons) {
            btn.interactable = arg1;
        }
        foreach(Button btn in step2buttons) {
            btn.interactable = arg2;
        }
    }
}
