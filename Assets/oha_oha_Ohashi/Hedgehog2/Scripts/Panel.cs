// Pre-Game, In-Game, Post-Gameの3つのパネルを操作
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Panel : UdonSharpBehaviour
{
    public Hedgehog Master;
    public GameObject[] Panels;
     
    // Pre-Game
    public GameObject[] BoardSizeIcons;
    // In-Game
    // Post-Game
    public GameObject[] WinnersMarks;
    
    // Start()でやっていいのは同期変数に基づく処理だけ
    // 遅れてJOINニキによるフォローアップ
    void Start()
    {
        // GamePhase が原始時代のときだけ初期描画してくれる
        if ( Master.GamePhase == -1 )
        {
            Master.SetActiveOneOfAll(ref BoardSizeIcons, Master.BoardSize);
            Master.SetActiveOneOfAll(ref Panels, 0);        // GamePhase == 0 用のパネル
        }
        // Late Joinner は同期変数をもとに表示
        else
        {
            Master.SetActiveOneOfAll(ref BoardSizeIcons, Master.BoardSize);
            Master.SetActiveOneOfAll(ref Panels, Master.GamePhase);
        }
    }
    
    ////////////// button用  メソッドたち  ////////////////////
    // Pre-Game
    public void SwitchBoardSizeTo5(){SwitchBoardSize(0);}
    public void SwitchBoardSizeTo6(){SwitchBoardSize(1);}
    public void SwitchBoardSizeTo7(){SwitchBoardSize(2);}
    public void SwitchBoardSizeTo8(){SwitchBoardSize(3);}

    private void SwitchBoardSize(int boardSize)
    {
        Debug.Log("ボードサイズ: " + boardSize.ToString());
        Master.SetBoardSize(boardSize);
    }

    public void PlayButtonPressed()
    {
        Debug.Log("Plaaaaaaaaaaayyy");
        // 最初にPLAY押した人が初めて GamePhase を有効な値にする
        Master.SetGamePhase(1);
    }

    // In-Game
    public void BackButtonPressed()
    {
        Debug.Log("もどるうううう");
    }

    public void ForwardButtonPressed()
    {
        Debug.Log("すすむうううう");
        // 次のゲームフェーズGamePhase
        Master.InclimentGamePhase();
    }

    public void ResetButtonPressed()
    {
        Debug.Log("Reセットオオオイ");
        Master.SetGamePhase(0);
    }

    // Post-Game
    public void PlayAgainButtonPressed(){
        Debug.Log("あげーーーーーーん");
        Master.InclimentGamePhase();
    }


    /////////////  Masterから呼び出されるメソッドたち  ////////////
    // ボードサイズアイコン切り替え
    public void SwitchWhichBoardSizeIconToShow()
    {
        Master.SetActiveOneOfAll(ref BoardSizeIcons, Master.BoardSize);
    }

    // 今使ってないね
    public void SwitchWhichPanelToShow()
    {
        Master.SetActiveOneOfAll(ref Panels, Master.GamePhase);
    }
}