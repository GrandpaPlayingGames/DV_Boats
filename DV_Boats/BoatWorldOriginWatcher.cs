
using System;
using UnityEngine;

namespace DV_Boats
{
    public class BoatWorldOriginWatcher : MonoBehaviour
    {
        private static BoatWorldOriginWatcher _instance;

        private Vector3 _lastWorldMove;
        private bool _worldMovePrimed;
        public static Vector3 CurrentMove { get; private set; }

        private bool _wosDetachPending;
        private int _reattachAtFrame;
        private Transform _wosPlayerTf;
        private Transform _wosBoatTf;
        private Vector3 _wosPlayerWorldPos;
        private Quaternion _wosPlayerWorldRot;


        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Update()
        {
            DetectWorldOriginShift_FromWorldMover(Time.time);
        }

        private void LateUpdate()
        {
            if (!_wosDetachPending)
                return;

            if (Time.frameCount < _reattachAtFrame)
                return;

            _wosDetachPending = false;

            if (_wosPlayerTf == null || _wosBoatTf == null)
                return;

            _wosPlayerTf.SetParent(_wosBoatTf, true);
            _wosPlayerTf.position = _wosPlayerWorldPos;
            _wosPlayerTf.rotation = _wosPlayerWorldRot;

        }


        private void DetectWorldOriginShift_FromWorldMover(float time)
        {
            Vector3 currentMove = WorldMover.currentMove;

            if (!_worldMovePrimed)
            {
                if (currentMove.sqrMagnitude < 1f)
                    return;

                _lastWorldMove = currentMove;
                _worldMovePrimed = true;

                CurrentMove = currentMove;
                Main.Log($"🌍 [WOS] BASELINE SET currentMove={currentMove}");
                return;
            }

            Vector3 delta = currentMove - _lastWorldMove;

            if (delta.sqrMagnitude > 1f)
            {
                Main.Log($"🌍 [WOS] DETECTED delta={delta} currentMove={currentMove}");

                CurrentMove = currentMove;
   
                BoatWorldShiftManager.OnWorldShiftDetected(delta);
                
                if (Main.Settings.debugLogging)
                {
                        UIHelpers.ShowDialog(
                        "World Origin Shift Detected",
                        $"Delta: {delta}\nCurrentMove: {currentMove}",
                        () => { },
                        null,
                        scale: 0.75f
                    );
                }

                if (!_wosDetachPending)
                {
                    BoatController activeBoat = BoatDriveManager.ActiveBoat;
                    if (activeBoat != null)
                    {
                        Transform boatTf = activeBoat.transform;

                        Transform playerTf = PlayerManager.PlayerTransform;

                        if (playerTf != null && boatTf != null && playerTf.parent == boatTf)
                        {
                            _wosPlayerTf = playerTf;
                            _wosBoatTf = boatTf;

                            _wosPlayerWorldPos = playerTf.position;
                            _wosPlayerWorldRot = playerTf.rotation;

                            playerTf.SetParent(null, true);

                            _wosDetachPending = true;
                            _reattachAtFrame = Time.frameCount + 1;
                        }
                    }
                }


            }

            _lastWorldMove = currentMove;
        }
    }
}

namespace DV_Boats
{
       public static class BoatWorldShiftManager
    {
        public static event Action<Vector3> WOSDeltaAdjustment;

        public static void OnWorldShiftDetected(Vector3 delta)
        {
            var handlers = WOSDeltaAdjustment;
            if (handlers == null)
                return;

            foreach (var d in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<Vector3>)d)(delta);
                }
                catch (Exception ex)
                {
                    Main.Log($"[WOS] ❌ Subscriber threw: {d.Method.DeclaringType}.{d.Method.Name} :: {ex}");
                }
            }
        }

    }

}
