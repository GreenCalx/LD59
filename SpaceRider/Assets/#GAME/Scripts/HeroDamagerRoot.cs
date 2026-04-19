using UnityEngine;

public class HeroDamagerRoot : MonoBehaviour
{
    private void Start() => Refresh();

    public void Refresh()
    {
        foreach (var col in GetComponentsInChildren<Collider>(true))
            if (col.GetComponent<HeroDamager>() == null)
                col.gameObject.AddComponent<HeroDamager>();
    }
}
