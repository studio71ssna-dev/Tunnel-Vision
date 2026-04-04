// IAnimatable.cs
// Anything that can play dictionary-keyed animations implements this.
// Decouples states from the concrete EnemyAnimator — states only call
// PlayAnimation / CrossFade, never touch Animator hashes directly.

public interface IAnimatable
{
    void PlayAnimation(string key);
    void CrossFadeAnimation(string key, float transitionDuration = 0.15f);
}
