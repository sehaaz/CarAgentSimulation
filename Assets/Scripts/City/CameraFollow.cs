using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    [Tooltip("Takip edilecek obje (araba)")]
    [SerializeField] private Transform target;

    [Header("Kamera Ayarlari")]
    [Tooltip("Kameranin ne kadar uzaktan bakacagi (orthographic size)")]
    [SerializeField] private float zoomSize = 15f;
    [Tooltip("Takip yumusakligi - dusuk = daha yavas/sinematik, yuksek = daha yakin takip")]
    [SerializeField] private float followSpeed = 3f;
    [Tooltip("Kameranin hedeften Y eksenindeki ofseti")]
    [SerializeField] private float offsetY = 2f;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            cam.orthographicSize = zoomSize;

        if (target != null)
            transform.position = new Vector3(target.position.x, target.position.y + offsetY, -10f);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y + offsetY, -10f);
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
    }
}
