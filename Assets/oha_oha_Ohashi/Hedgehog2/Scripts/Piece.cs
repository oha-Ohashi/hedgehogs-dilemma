
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Piece : UdonSharpBehaviour
{
    private int _id;
    // グリッドIDとは: 左上から数えた 0-indexの数字
    private int _gridId;

    void Start()
    {
        
    }

    public void ShowUp(int realBoardSize, int col, int row)    // 0-indexed
    {
        MoveToSquare(realBoardSize, col, row);
    }

    // 強制テレポート
    public void MoveToSquare(int realBoardSize, int col, int row)
    {
        // this.gameObject.transform.localPosition = 
    }
}
