using UnityEngine;

public class shadow : MonoBehaviour
{   
    private Transform transformer;
    [SerializeField] private GameObject grass;
    void Start()
    {
        transformer = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {   if (grass.transform.rotation.eulerAngles.z > 0)
        {
            transformer.rotation = Quaternion.Euler(0, 0, -grass.transform.rotation.eulerAngles.z);
        }
        else
        {
            transformer.rotation = Quaternion.Euler(0, 0, grass.transform.rotation.eulerAngles.z); 
        }
    }

}

