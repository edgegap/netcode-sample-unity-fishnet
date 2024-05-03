using UnityEngine;

namespace SpaceEdge
{
    public class MenuCamera : MonoBehaviour
    {
        [SerializeField] private float rotateRate = 6;


        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            var rot = _camera.transform.eulerAngles;
            rot.y += rotateRate * Time.deltaTime;
            _camera.transform.rotation =
                Quaternion.Slerp(_camera.transform.rotation, Quaternion.Euler(rot), rotateRate * Time.deltaTime);
        }
    }
}
