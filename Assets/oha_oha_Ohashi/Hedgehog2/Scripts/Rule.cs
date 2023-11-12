using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Rule : UdonSharpBehaviour
{
    public bool DebugMode = false;

    void Start()
    {
        
    }
    
    // 初期のボード
    public byte[] GetInitialBoard(int actualBoardSize)
    {
        byte[] resBoard = new byte[103];
        resBoard[0] = (byte)actualBoardSize;          // ボードサイズ
        resBoard[1] = 0xF9;                           // nPlayedTurns 初期は無効な値、 ただし奇数
        resBoard[2] = 0;                              // 廃墟
        for ( int i = 0; i < 100; i++ ) {
            resBoard[3 + i] = 0b0100_0000;             // ゲームで使われないマスにも None がいる
        }
        return resBoard;
    }
    
    // 合法手のグリッドIDリスト
    public int[] GetLegalMoves(byte[] board)
    {
        if ( DebugMode ) Debug.Log("--------  ここはGetLegelMoves()です --------");
        int[] resGridIds = new int[board[0] * board[0]];  // 溢れない
        resGridIds[0] = 0;                                  // resGridIds[0] は以降の要素数

        // ターン数が偶数なら青のターンであって、黄色の存在が知りたいよね
        //if ( DebugMode ) Debug.Log("board[1] " + board[1].ToString() + " -- board[1]==0xF9 " + (board[1]==FA));
        int nNextTurn = (board[1] == 0xF9) ? 0 : board[1] + 1;      // 今から打ちたいNターンだね(一時int)
        if ( DebugMode ) Debug.Log("nNextTurnは: " + nNextTurn.ToString());

        // 初回分岐。 初回は最も外側の辺以外置ける
        if ( nNextTurn == 0 )
        {
            // すべてのグリッドIDのうち辺でないものをresGridIdsに追加
            // 1時的に1-indexed
            for (int i = 1; i <= board[0] * board[0]; i++){ 
                bool onEdge = (i <= board[0]) ||                 // 上辺
                                (i % board[0] == 1) ||           // 左辺
                                (i% board[0] == 0) ||            // 右辺
                                (i > board[0] * (board[0] - 1)); // 下辺
                // 辺でないなら返り値に i - 1 を追加 (ここでグリッドIDを 0-indexedにする)
                if (!onEdge) {
                    resGridIds[++resGridIds[0]] = i - 1;
                }
                //if ( DebugMode ) Debug.Log("グリッドID: " + i.ToString() + "は辺にあるか？: " + onEdge.ToString() + " resGridIds[0]: " + resGridIds[0].ToString());
            }
        }
        // 通常の合法手探索
        else
        {
            // すべての有効グリッドIDから敵を探す
            for (int targetGridId = 0; targetGridId < board[0] * board[0]; targetGridId++){
                string msg2 = "targetGridId は: " + targetGridId.ToString();
                byte targetSquareState = board[3 + targetGridId];
                msg2 +="仮想敵squareState: " + TrimBinary(targetSquareState, 8);
                // 敵なら
                bool isBlueTurn = (nNextTurn % 2) == 0;
                msg2 += "  isBlueTurn: " + isBlueTurn.ToString();
                // WhatYouWantで使うよ
                int enemyFilter = isBlueTurn ? 0b0001_0000 : 0b0010_0000;
                bool thisSquareHasAnEnemy = (targetSquareState & enemyFilter) > 0;
                msg2 += "   thisSquareHasEnemy: " + thisSquareHasAnEnemy.ToString();
                if ( DebugMode ) Debug.Log(msg2);
                // ターン数が偶数なら青のターンであって、黄色の存在が知りたいよね
                // ようやく敵のいるマスが見つかったね
                if ( thisSquareHasAnEnemy )
                {
                    // 空きマスだったら -1 以外のまともな値が入ってる
                    int emptyFilter = 0b100;
                    int[] gridIds = new int[4] {
                        GetGridIdIfLookingAtWhatYouWant(targetGridId, board, 0, emptyFilter),
                        GetGridIdIfLookingAtWhatYouWant(targetGridId, board, 1, emptyFilter),
                        GetGridIdIfLookingAtWhatYouWant(targetGridId, board, 2, emptyFilter),
                        GetGridIdIfLookingAtWhatYouWant(targetGridId, board, 3, emptyFilter)
                    };
                    // 上下ともに空きなら
                    if ( (gridIds[0] >= 0) && (gridIds[2] >= 0) ) {
                        resGridIds[++resGridIds[0]] = gridIds[0];
                        resGridIds[++resGridIds[0]] = gridIds[2];
                        if ( DebugMode ) Debug.Log("上下空いてたんで、 " + gridIds[0].ToString() + " と " + gridIds[2] + " を返り値に追加しときました。");
                    }
                    // 左右ともに空きなら
                    if ( (gridIds[1] >= 0) && (gridIds[3] >= 0) ) {
                        resGridIds[++resGridIds[0]] = gridIds[1];
                        resGridIds[++resGridIds[0]] = gridIds[3];
                        if ( DebugMode ) Debug.Log("左右空いてたんで、 " + gridIds[1].ToString() + " と " + gridIds[3] + " を返り値に追加しときました。");
                    }
                }
            }
        }
        string msg = "!!!!合法手の数は " + resGridIds[0].ToString() + " 個でしたねぇ!!!!\n";
        msg += "内訳: ";
        for (int i = 1; i <= resGridIds[0]; i++){
            msg += resGridIds[i].ToString() + ", ";
        }
        if ( DebugMode ) Debug.Log(msg);
        return resGridIds;
    }

    public int[] GetLegalMoves(byte[] board, bool isBlueTurn)
    {
        if ( DebugMode ) Debug.Log("オーバーライド？");
        return new int[1] {123};
    }

    // 見てる方向が目当てのコマ/空きマスだったらグリッドID返す。それ以外なら -1
    // 起点のグリッドIDと見たい方向、欲しいカテゴリフィルターを指定する
    public  int GetGridIdIfLookingAtWhatYouWant(int baseGridID, byte[] board, int targetRotCode, int filter)
    {
        // 見たい方向が壁でも大丈夫、-1返してくれるから
        // filter、欲しいカテゴリのビットを立てる。 
        // 0b100: nobody, 0b010: blue, 0b001: yellow
        // 0b011: yellow or blue
        int[] diff = new int[4]{ -board[0], 1, board[0], -1 };
        int targetGridId = baseGridID + diff[targetRotCode];
        // 存在できない、無効グリッドIDだったら false
        if ( targetGridId < 0 || targetGridId >= (board[0] * board[0]) ) 
        {
            ReportOfGetGridIdIfLookingAtWhatYouWant(0, baseGridID, targetGridId, targetRotCode, (byte)0b00, filter, false);
            return -1; 
        }
        // 左か右を見てるのに同じ行にない
        else if ( 
            (targetRotCode == 1 || targetRotCode == 3 ) &&
            (targetGridId / board[0] != baseGridID / board[0])  // ボードサイズで割った商が違う
        )
        { 
            ReportOfGetGridIdIfLookingAtWhatYouWant(1, baseGridID, targetGridId, targetRotCode, (byte)0b00, filter, false);
            return -1;
        }
        // ちゃんと、となりに目当ての種類のマスがある
        else
        {
            byte targetSquareState = board[3 + targetGridId];
            bool isWanted = ((targetSquareState >> 4) & filter) > 0;
            ReportOfGetGridIdIfLookingAtWhatYouWant(2, baseGridID, targetGridId, targetRotCode, targetSquareState, filter, isWanted);

            if (isWanted) {
                return targetGridId;
            } else {
                return -1;
            }
        }
    }

    // GetGridIdIfLookingAtWhatYouWant() の報告
    private  void ReportOfGetGridIdIfLookingAtWhatYouWant(
        int resultCode,
        int baseGridID,
        int targetGridId,
        int targetRotCode,
        byte squareState,
        int filter,
        bool isWanted
    )
    {
        string msg = "-------- 私は GetGridIdIfLookingAtWhatYouWant()です。---------\n";
        msg += "baseGridID は " + baseGridID.ToString() + "でした。\n";
        msg += "rotCode: " + TrimBinary(targetRotCode, 2) +"(" + targetGridId.ToString() + ")を調べたところ、\n";
        if (resultCode == 0) {
            msg += "該当番地は無効なグリッドIDでした。";
        } else if (resultCode == 1) {
            msg += "該当rotCodeは異なる行をまたいでいました。";
        } else {
            msg += "このマスのsquareStateは " + TrimBinary(squareState, 8) + "でしたので、\n";
            msg += "filter = " + TrimBinary(filter, 3);
            msg += isWanted ? "を合格という結果になりました。" : "で弾かれました。";
        }
        if ( DebugMode ) Debug.Log(msg);
    }
    

    ////////////////////////////////////////////////////////
    /////////////////    便利ライブラリ     /////////////////
    ////////////////////////////////////////////////////////

    // int[] の和を int で返す
    public  int IntArraySum(int[] arg)
    {
        int resSum = 0;
        for (int i = 0; i < arg.Length; i++) {
            resSum += arg[i];
        }
        return resSum;
    }

    // int[] の中に重複要素があったらそれらを特別な値に書き換える
    public  int[] RemoveDuplicatesAndReplace(int[] argArray, int replacer)
    {
        for (int i = 0; i < argArray.Length - 1; i++)
        {
            if (argArray[i] == replacer)
            {
                continue; // 既に置き換えられた要素はスキップ
            }
    
            bool hasDuplication = false;
            for (int j = i + 1; j < argArray.Length; j++)
            {
                if (argArray[i] == argArray[j])
                {
                    hasDuplication = true;
                    argArray[j] = replacer;
                }
            }
            if (hasDuplication) {
                argArray[i] = replacer;     // 両成敗よ？？
            }
        }
        return argArray;
    }
    
    // int[] をメッセージ付きで表示
    public  void IntArrayDump(int[] argArray, string message){
        string content = "This is IntArrayDump.\nmessage: " + message + "\n";
        for (int i = 0; i < argArray.Length; i++){
            content += argArray[i] + ", ";
        }
        if ( DebugMode ) Debug.Log(content);
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

            return formattedBinary;
        }
    }
}
