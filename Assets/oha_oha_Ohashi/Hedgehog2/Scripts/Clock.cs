
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Clock : UdonSharpBehaviour
{
    private bool _initialized = false;
    private Piece _parentalPiece;
    private string _name = "anonymous";
    private float _millis = 0f;
    private int _phase = 0;
    private int[] _queuedGridIds;
    private int[] _queuedRotCodes;
    private float[] _durations;
    private float _millisLastPhaseWasDone;
    private bool _debugMode = true;

    public void Initialize(
        string argName,
        int[] argQueuedGridIds,
        int[] argQueuedRotCodes,
        float[] argDuration,
        bool argDebugMode
    )
    {
        _initialized = true;
        _parentalPiece = transform.parent.gameObject.GetComponent<Piece>();
        _name = argName;
        _durations = argDuration;
        _debugMode = argDebugMode;

        _millisLastPhaseWasDone = 0f;

        if ( argQueuedGridIds.Length >= 2 ) {
            _queuedGridIds = argQueuedGridIds;
            _queuedRotCodes = argQueuedRotCodes;
        } else {
            // デモ: ランダム目的地
            _queuedGridIds = new int[_durations.Length];
            int asIsGridId = _parentalPiece.GetAsIsGridId();
            for (int i = 0; i < 10; i++){
                // 奇数なら休憩
                if (i % 2 == 1) 
                { 
                    _queuedGridIds[i] = asIsGridId;
                }
                // 偶数なら移動
                else
                {
                    int[] diff = new int[4] {-5, 1, 5, -1};
                    int randomGridId = -1;
                    // 有効なグリッドIDが出るまでガチャ
                    while (randomGridId < 0 || randomGridId >= 25) {
                        int rand = Random.Range(0, 4);
                        randomGridId = asIsGridId + diff[rand];
                        if ((rand == 1 || rand == 3) &&
                           (asIsGridId / 5  != randomGridId / 5)) 
                        {
                            randomGridId = -1;
                        }
                    }
                    _queuedGridIds[i] = randomGridId;
                }
                asIsGridId = _queuedGridIds[i];
            }
        }
        for (int i = 0; i < 10; i++){
            Debug.Log("目的地" + i.ToString() + " グリッドID: " + _queuedGridIds[i].ToString());
        }
    }

    void Start()
    {

    }

    void Update()
    {
        _millis += Time.deltaTime;
        float timeInThePhase = _millis - _millisLastPhaseWasDone;

        if ( _initialized && _phase < _durations.Length ) {

            if ( timeInThePhase > _durations[_phase] )
            {
                //if ( _debugMode ) Debug.Log("スレホ[" + (_phase).ToString() + "]超えた");
                if ( _debugMode ) Debug.Log("完了したはずの目的地 " + (_phase).ToString() + ": " + (_queuedGridIds[_phase]).ToString() + "");

                _parentalPiece.ChangeGridIdToGo(_queuedGridIds[_phase]);
                _parentalPiece.ChangeRotCodeToGo(_queuedRotCodes[_phase]);

                _millisLastPhaseWasDone = _millis;
                _phase++;
            }
            else
            {
                float interpolatePos = Mathf.Pow((timeInThePhase / _durations[_phase]), 5.0f);
                float interpolateRot = Mathf.Pow((timeInThePhase / _durations[_phase]), 7.0f);
                _parentalPiece.StepOneLerp(interpolatePos, interpolateRot);
            }
        }

    }
}
