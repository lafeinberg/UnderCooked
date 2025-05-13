using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [SerializeField]
    private Image ProgressBarImage;
    [SerializeField]
    private float animationSpeed = 1f;
    [SerializeField]
    private UnityEvent<float> OnProgress;
    [SerializeField]
    private UnityEvent OnCompleted;

    private Coroutine AnimationCoroutine;


    /*
    * Set progress bar fill to represent recipe progress by giving float >0 and <1
    */
    public void SetProgress(float targetProgress)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (targetProgress != ProgressBarImage.fillAmount)
        {
            if (AnimationCoroutine != null)
            {
                StopCoroutine(AnimationCoroutine);
            }

            AnimationCoroutine = StartCoroutine(AnimateProgress(targetProgress));
        }
    }

    private IEnumerator AnimateProgress(float targetProgress)
    {
        float time = 0;
        float startProgress = ProgressBarImage.fillAmount;

        while (time < 1)
        {
            ProgressBarImage.fillAmount = Mathf.Lerp(startProgress, targetProgress, time);
            time += Time.deltaTime * animationSpeed;

            OnProgress?.Invoke(ProgressBarImage.fillAmount);
            yield return null;
        }

        ProgressBarImage.fillAmount = targetProgress;
        OnProgress?.Invoke(targetProgress);
        OnCompleted?.Invoke();
    }
}