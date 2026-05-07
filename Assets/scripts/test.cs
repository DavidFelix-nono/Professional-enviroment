using UnityEngine;

public class test : MonoBehaviour
{
    public Transform transformer;
    public float x;
    public float y;
    public float rotation;
    
    public void Update()
    { 
        transformer.rotation = Quaternion.Euler(0, 0, rotation);
    }
}
