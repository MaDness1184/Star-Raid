using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private float highlightDuration = 0.1f;
    [SerializeField] private SpriteRenderer[] highlightableSprites;

    private void Start()
    {
        // Automate shader init process
    }

    [Client]
    public void LocalHighlight(bool highlight)
    {
        if (highlightableSprites.Length <= 0)
        {
            DebugConsole.LogWarning(name + "interactable has no highlightable sprite");
            return;
        }
        
        foreach(SpriteRenderer renderer in highlightableSprites)
        {
            StartCoroutine(HighlightCoroutine(renderer, highlight));
        }
        
    }

    [Client]
    IEnumerator HighlightCoroutine(SpriteRenderer renderer, bool highlight)
    {
        float alpha = renderer.material.GetFloat("_Alpha");

        float elapsedTime = 0;

        while (elapsedTime < highlightDuration)
        {
            if (highlight)
                alpha = Mathf.Lerp(alpha, 1, 0.01f * elapsedTime / highlightDuration);
            else
                alpha = Mathf.Lerp(alpha, 0, 0.01f * elapsedTime / highlightDuration);

            renderer.material.SetFloat("_Alpha", alpha);

            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        renderer.material.SetFloat("_Alpha", highlight ? 1 : 0);
    }

}
