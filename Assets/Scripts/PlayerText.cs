using UnityEngine;

public class PlayerText : MonoBehaviour
{
    [SerializeField] GameObject followObj;
    [SerializeField] float yOffset;
    [SerializeField] float xOffset;

    private void Update()
    {
        transform.position = followObj.transform.position;
        Vector3 newPos = new Vector3(transform.position.x + xOffset, transform.position.y + yOffset, transform.position.z);
        transform.position = newPos;
    }
    
}
