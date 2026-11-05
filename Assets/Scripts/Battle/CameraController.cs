using UnityEngine;

/// <summary>
/// Keeps the Main Camera fully tweakable from the Inspector at edit-time and runtime.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Camera Tuning")]
    [SerializeField] private float fov = 60f;
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 0f, -5f);
    [SerializeField] private Vector3 cameraRotation = Vector3.zero;

    private Camera _camera;

    public float Fov
    {
        get => fov;
        set => fov = value;
    }

    public Vector3 CameraPosition
    {
        get => cameraPosition;
        set => cameraPosition = value;
    }

    public Vector3 CameraRotation
    {
        get => cameraRotation;
        set => cameraRotation = value;
    }

    private void OnEnable()
    {
        CacheCamera();
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void OnValidate()
    {
        CacheCamera();
        Apply();
    }

    private void CacheCamera()
    {
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }
    }

    private void Apply()
    {
        if (_camera == null)
        {
            return;
        }

        transform.localPosition = cameraPosition;
        transform.localRotation = Quaternion.Euler(cameraRotation);
        _camera.fieldOfView = fov;
    }
}
