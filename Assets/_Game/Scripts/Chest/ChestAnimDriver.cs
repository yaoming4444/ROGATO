using UnityEngine;

public class ChestAnimDriver : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private string idleState = "ChestIdle";
    [SerializeField] private string openState = "ChestOpen"; 
    [SerializeField] private float openDuration = 0.6f;

    private Coroutine _c;

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
    }

    public void PlayOpen()
    {
        if (!anim) return;

        if (_c != null) StopCoroutine(_c);
        _c = StartCoroutine(Co());
    }

    private System.Collections.IEnumerator Co()
    {
        // Hard reset so Open always starts from frame 0
        anim.Rebind();
        anim.Update(0f);

        anim.Play(openState, 0, 0f);

        yield return new WaitForSecondsRealtime(openDuration);

        // anim.Play(idleState, 0, 0f);
    }
}

