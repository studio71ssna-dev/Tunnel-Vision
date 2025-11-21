using UnityEngine;

public class translation : MonoBehaviour
{
    private bool hitted;
 public void translating()
    {
        transform.Translate(Vector3.forward * Time.deltaTime * 1f);
    }

    public void translatingOther()
    {
        transform.Translate(Vector3.right * Time.deltaTime * 1f);
        Debug.Log("Move");
    }


    private void Update()
    {
        if(hitted)
        translating();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("wp"))
        {
            translatingOther();
        }
    }
}
