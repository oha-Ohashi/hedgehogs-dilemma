﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Indicator : UdonSharpBehaviour
{
    public Hedgehog Master;
    public Playground Playground;

    // グリッドIDとは: 左上から数えた 0-indexの数字
    private int _gridId;
    private bool _isPlayGroundIndicator = false;
    private float _gridWidth = 0.4195f / 3.0f;

    void Start()
    {
    }

    void Update()
    {
        /*
        // 軽量化対象筆頭
        if (Master.MoveAllowed) {
            this.gameObject.GetComponent<Collider>().enabled = true;
        } else {
            this.gameObject.GetComponent<Collider>().enabled = false;
        }
        */
    }

    public override void Interact()
    {
        Debug.Log("Grid interacted: " + _gridId.ToString());

        if ( Master.TryFire() )
        {
            if (!_isPlayGroundIndicator)
            {
                Master.IndicatorInteracted(_gridId);
            }
            else
            {
                Playground.IndicatorInteracted(_gridId);

                //
                
            }
        }
        else
        {
            Debug.Log("着手は " + Master.MoveFireRateLimit.ToString() + " 秒に1回だけ！！");
        }
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

    public void MoveToSquareAsPlayGroundIndicator(int gridId, int realBoardSize)
    {
        MoveToSquare(gridId, realBoardSize);
        _isPlayGroundIndicator = true;
    }
}
