namespace LogisticsSAC.Scripts
{
    [UnityEngine.RequireComponent(typeof(UnityEngine.BoxCollider2D))]
    public class Depot : UnityEngine.MonoBehaviour
    {
        [UnityEngine.Header("Depot Settings")]
        public string depotID = "Depot_01";

        private void Awake()
        {
            UnityEngine.BoxCollider2D col = this.GetComponent<UnityEngine.BoxCollider2D>();
            col.isTrigger = true;
        }
    }
}