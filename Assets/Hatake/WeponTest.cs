using UnityEngine;
using DG.Tweening;

public class WeponTest : MonoBehaviour
{
    void Start()
    {
        Vector3[] path = {
            new Vector3(0, 5, 0),
            new Vector3(5, 0, 0),
            new Vector3(0, -5, 0),
            new Vector3(-5, 0, 0),
            };

        transform.DOLocalPath(path, 5.0f, PathType.CatmullRom)
            .SetOptions(true);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}