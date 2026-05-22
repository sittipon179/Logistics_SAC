namespace LogisticsSAC.Core
{
    [UnityEngine.RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraController : UnityEngine.MonoBehaviour
    {
        [UnityEngine.Header("Target Tracking")]
        public UnityEngine.Transform target;
        public float followSpeed = 5f;

        [UnityEngine.Header("Zoom Settings")]
        public float minZoom = 30f;
        public float maxZoom = 290f;
        public float zoomSpeed = 100f;

        private UnityEngine.Camera cam;

        private void Awake()
        {
            cam = this.GetComponent<UnityEngine.Camera>();
        }

        private void Update()
        {
            HandleZoom();
        }

        private void LateUpdate()
        {
            FollowTarget();
        }

        private void HandleZoom()
        {
            float scrollInput = UnityEngine.Input.GetAxis("Mouse ScrollWheel");

            if (scrollInput != 0f)
            {
                float newSize = cam.orthographicSize - (scrollInput * zoomSpeed);
                cam.orthographicSize = UnityEngine.Mathf.Clamp(newSize, minZoom, maxZoom);
            }
        }

        private void FollowTarget()
        {
            if (target != null)
            {
                UnityEngine.Vector3 targetPosition = new UnityEngine.Vector3(target.position.x, target.position.y, this.transform.position.z);
                this.transform.position = UnityEngine.Vector3.Lerp(this.transform.position, targetPosition, followSpeed * UnityEngine.Time.deltaTime);
            }
        }
    }
}