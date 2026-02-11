using UnityEngine;

namespace DV_Boats
{
    internal class BoatFollowAnchor : MonoBehaviour
    {
        private static BoatFollowAnchor _instance;
        public static BoatFollowAnchor Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindObjectOfType<BoatFollowAnchor>();
                if (_instance != null)
                    return _instance;

                var go = new GameObject("DV_Boats_BoatFollowAnchor");
                go.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(go);

                _instance = go.AddComponent<BoatFollowAnchor>();
                return _instance;
            }
        }

        private bool _active;
        private float _yaw;
        private float _pitch;
        

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void BeginFollowing(Transform boat, Vector3 localOffset, Quaternion initialWorldRotation)
        {
            if (boat == null)
                return;

            transform.SetParent(boat, false);
            transform.localPosition = localOffset;

            Quaternion localRot = Quaternion.Inverse(boat.rotation) * initialWorldRotation;
            transform.localRotation = localRot;

            Vector3 euler = transform.localEulerAngles;
            _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            _yaw = euler.y;

            _active = true;
        }

        public void StopFollowing()
        {
            if (!_active)
                return;

            transform.SetParent(null, true);
            _active = false;
        }

        private void LateUpdate()
        {
            if (!_active)
                return;

            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            _yaw += mouseX * 3.5f;
            _pitch -= mouseY * 3.5f;
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);

            transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}

