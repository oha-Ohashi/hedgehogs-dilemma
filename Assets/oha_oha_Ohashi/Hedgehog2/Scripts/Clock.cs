
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Clock : UdonSharpBehaviour
{
    public Move Boss;

    private bool _inAction = false;
    private Piece _parentalPieceBehaviour;
    private string _name = "anonymous";
    private float _millis = 0f;
    private float _millisOnStart = 0f;
    private int _goalGridId;
    private int _goalRotCode;
    private float _duration;
    private int _iWorldIknow;
    private bool _debugMode = true;

    public void Initialize(
        string argName,
        int argGoalGridId,
        int argGoalRotCode,
        float argDuration,
        int argIWorld,
        bool argDebugMode
    )
    {
        _parentalPieceBehaviour = transform.parent.gameObject.GetComponent<Piece>();
        _name = argName;
        _goalGridId = argGoalGridId;
        _goalRotCode = argGoalRotCode;
        _duration = argDuration;
        _iWorldIknow = argIWorld;
        _debugMode = argDebugMode;

        _parentalPieceBehaviour.OnClockRegistered();
    }

    void Start()
    {

    }

    public void RunOneLerp()
    {
        if ( _iWorldIknow != Boss.IWorld) {
            return;
        }

        // 時間は必ず測っちゃう
        _millis += Time.deltaTime;
        
        // 本領発揮
        // 処理されてる最中
        if ( _inAction ) 
        {
            float timeElapsed = _millis - _millisOnStart;

            // インタポを計算
            float interpolatePos = Mathf.Pow((timeElapsed / _duration), 7.0f);
            float interpolateRot = Mathf.Pow((timeElapsed / _duration), 3.0f);

            // ちょっとうごかす
            _parentalPieceBehaviour.StepOneLerp(interpolatePos, interpolateRot);

            // 出番おわった
            if ( timeElapsed >= _duration ){
                _parentalPieceBehaviour.OnClockIsDone();
            }
        }
    }

    public void OnNudged()
    {
        if ( _iWorldIknow != Boss.IWorld) {
            return;
        }

        _inAction = true;
        _millisOnStart = _millis;

        string msg = "目標グリッドID: " + _goalGridId.ToString();
        msg += "目標 RotCode: " + _goalRotCode.ToString();
        msg += "予定時間(duration): " + _duration.ToString();
        if ( _debugMode ) Debug.Log(msg);

        _parentalPieceBehaviour.ChangeGridIdToGo(_goalGridId);
        _parentalPieceBehaviour.ChangeRotCodeToGo(_goalRotCode);
    }
}
