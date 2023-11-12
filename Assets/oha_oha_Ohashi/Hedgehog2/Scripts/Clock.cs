
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Clock : UdonSharpBehaviour
{
    private int _lifeCycle = -1;
    private int _ticketNumber = 0;               // クロックチケット未登録
    private Piece _parentalPieceBehaviour;
    private string _name = "anonymous";
    private float _millis = 0f;
    private float _millisOnStart = 0f;
    private int _goalGridId;
    private int _goalRotCode;
    private float _duration;
    private bool _debugMode = true;

    public void Initialize(
        string argName,
        int argGoalGridId,
        int argGoalRotCode,
        float argDuration,
        bool argDebugMode
    )
    {
        _parentalPieceBehaviour = transform.parent.gameObject.GetComponent<Piece>();
        _ticketNumber = _parentalPieceBehaviour.OnClockVisit(_ticketNumber);
        _name = argName;
        _goalGridId = argGoalGridId;
        _goalRotCode = argGoalRotCode;
        _duration = argDuration;
        _debugMode = argDebugMode;
        _lifeCycle++;
    }

    void Start()
    {

    }

    void Update()
    {
        // 時間は必ず測っちゃう
        _millis += Time.deltaTime;
        
        if ( _lifeCycle < 0 )
        {
            return;
        }

        // 処理される前
        if ( _lifeCycle == 0 )
        {
            // 自分のチケットを投げ続けてずっと
            int response = _parentalPieceBehaviour.OnClockVisit(_ticketNumber);
            if ( response == 0 ) {
                _lifeCycle++;
                _millisOnStart = _millis;

                string msg = "目標グリッドID: " + _goalGridId.ToString();
                msg += "目標 RotCode: " + _goalRotCode.ToString();
                msg += "予定時間(duration): " + _duration.ToString();
                if ( _debugMode ) Debug.Log(msg);

                _parentalPieceBehaviour.ChangeGridIdToGo(_goalGridId);
                _parentalPieceBehaviour.ChangeRotCodeToGo(_goalRotCode);
            }
        }
        // 処理されてる最中
        else if ( _lifeCycle == 1 ) 
        {
            float timeElapsed = _millis - _millisOnStart;

            float interpolatePos = Mathf.Pow((timeElapsed / _duration), 7.0f);
            float interpolateRot = Mathf.Pow((timeElapsed / _duration), 3.0f);
            _parentalPieceBehaviour.StepOneLerp(interpolatePos, interpolateRot);

            // 出番おわった
            if ( timeElapsed >= _duration ){
                int response = _parentalPieceBehaviour.OnClockVisit(_ticketNumber);
                if ( response == 0 ) {
                    if ( _debugMode ) Debug.Log("出番終わり: チケット " + _ticketNumber.ToString());
                    _parentalPieceBehaviour.OnClockVisit(_ticketNumber);
                    _lifeCycle++;
                }
            }
        }
        // 処理された後
        else 
        {
            // なにもしないよ
        }
    }
}
