
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Indicator : UdonSharpBehaviour
{
    public Hedgehog Master;

    // グリッドIDとは: 左上から数えた 0-indexの数字
    private int _gridId;
    private float _gridWidth = 0.4195f / 3.0f;

    void Start()
    {
        
    }

    public override void Interact()
    {
        Debug.Log("Grid interacted: " + _gridId.ToString());
        Master.IndicatorInteracted(_gridId);

    }

    public void MoveToSquare(int gridId, int realBoardSize)
    {
        //Debug.Log("Grid ID: " + gridId.ToString());
        //Debug.Log("Real Board Size: " + realBoardSize.ToString());

        // _gridIdの烙印を押す
        _gridId = gridId;

        // 0-indexedのcol, row
        int col = (gridId) % realBoardSize;
        int row = (gridId) / realBoardSize;

        string msg = "ここはMoveToSquare()です。このIndicatorは";
        msg += "col: " + col.ToString() + " row: " + row.ToString() ;
        msg += " に移動しようとしています";
        //Debug.Log(msg);

        float minZ = _gridWidth * ((-1 * (float)realBoardSize / 2) + 0.5f);
        float maxY = -minZ;
        this.gameObject.transform.localPosition = new Vector3(
            0,
            maxY - row * _gridWidth,
            minZ + col * _gridWidth
        );
    }
}
