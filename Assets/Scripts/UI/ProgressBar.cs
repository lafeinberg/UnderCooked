using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [SerializeField]
    private Image ProgressBar;
    [SerializeField]
    private float animationSpeed = 1f;
    [SerializeField]
    private UnityEvent<float> OnProgress;
    [SerializeField]
    private UnityEvent OnCompleted;

    private Coroutine AnimationCoroutine;

    private void Start()
    {
        StartCoroutine(TestProgressLoop());
    }

    /*
    * Set progress bar fill to represent recipe progress by giving float >0 and <1
    */
    public void SetProgress(float targetProgress)
    {

        if (targetProgress != ProgressBar.fillAmount)
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
        float startProgress = ProgressBar.fillAmount;

        while (time < 1)
        {
            ProgressBar.fillAmount = Mathf.Lerp(startProgress, targetProgress, time);
            time += Time.deltaTime * animationSpeed;

            OnProgress?.Invoke(ProgressBar.fillAmount);
            yield return null;
        }

        ProgressBar.fillAmount = targetProgress;
        OnProgress?.Invoke(targetProgress);
        OnCompleted?.Invoke();
    }

    private IEnumerator TestProgressLoop()
    {
        while (true)
        {
            float randomProgress = Random.Range(0.2f, 1f);
            SetProgress(randomProgress);
            yield return new WaitForSeconds(5f);
        }
    }
}