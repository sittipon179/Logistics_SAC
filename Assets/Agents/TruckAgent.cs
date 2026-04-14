namespace LogisticsSAC.Agents
{
    [UnityEngine.RequireComponent(typeof(UnityEngine.Rigidbody2D))]
    public class TruckAgent : UnityEngine.MonoBehaviour
    {
        [UnityEngine.Header("Truck Parameters")]
        public float moveSpeed = 5f;
        public float currentCapacity = 0f;
        public float maxCapacity = 50f;

        private UnityEngine.Rigidbody2D rb;
        private UnityEngine.Vector2 movementInput;

        private void Awake()
        {
            rb = this.GetComponent<UnityEngine.Rigidbody2D>();
            rb.bodyType = UnityEngine.RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private void Update()
        {
            movementInput.x = UnityEngine.Input.GetAxisRaw("Horizontal");
            movementInput.y = UnityEngine.Input.GetAxisRaw("Vertical");
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = movementInput.normalized * moveSpeed;
        }

        private void OnTriggerEnter2D(UnityEngine.Collider2D collision)
        {
            if (collision.gameObject.CompareTag("TrashNode"))
            {
                LogisticsSAC.Environment.TrashNode node = collision.gameObject.GetComponent<LogisticsSAC.Environment.TrashNode>();

                if (node != null)
                {
                    float availableSpace = maxCapacity - currentCapacity;

                    if (availableSpace > 0)
                    {
                        float collectedAmount = node.CollectTrash(availableSpace);
                        currentCapacity += collectedAmount;
                        UnityEngine.Debug.Log("Collected " + collectedAmount + " trash. Current Capacity: " + currentCapacity + "/" + maxCapacity);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("Truck capacity is full! Must return to Depot.");
                    }
                }
            }
        }
    }
}