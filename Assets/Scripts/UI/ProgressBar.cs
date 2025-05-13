using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ProgressBar : MonoBehaviour
{
    [SerializeField]
    private Image ProgressBarImage;

    [SerializeField]
    private TextMeshProUGUI ProgressPercentText;
    [SerializeField]
    private float animationSpeed = 1f;
    private Coroutine AnimationCoroutine;


    /*
    * Set progress bar fill to represent recipe progress by giving float >0 and <1
    */
    public void SetProgress(float targetProgress)
    {
        Debug.Log($"SETTING PROGRESS TO {targetProgress * 100}%");
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
        float time = 0f;
        float startProgress = ProgressBarImage.fillAmount;

        while (time < 1f)
        {
            float currentProgress = Mathf.Lerp(startProgress, targetProgress, time);
            ProgressBarImage.fillAmount = currentProgress;
            ProgressPercentText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
            time += Time.deltaTime * animationSpeed;
            yield return null;
        }

        ProgressBarImage.fillAmount = targetProgress;
        ProgressPercentText.text = $"{Mathf.RoundToInt(targetProgress * 100)}%";

        if (targetProgress >= 1f)
            gameObject.SetActive(false);
    }

    private void EndProgress()
    {
        gameObject.SetActive(false);
    }
}