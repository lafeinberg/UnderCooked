using UnityEngine;

namespace XRMultiplayer
{
    public class PopoutUI : MonoBehaviour
    {
        [SerializeField] bool m_HideOnStart = false;
        [SerializeField] float m_DistanceFromFace = .25f;
        [SerializeField] float m_YOffset;
        Transform m_MainCamTransform;

        private void Start()
        {
            if (m_HideOnStart)
                gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (m_MainCamTransform == null)
            {
                m_MainCamTransform = Camera.main.transform;
            }

            transform.position = m_MainCamTransform.position;
            Debug.Log($"[UIAlign] Step 1 - Initial Position Set To Camera: {transform.position}");

            transform.position += transform.forward * m_DistanceFromFace;
            transform.position += Vector3.up * -m_YOffset;
            Debug.Log($"[UIAlign] Step 4 - After Y Offset ({-m_YOffset}): {transform.position}");
        }
    }
}
